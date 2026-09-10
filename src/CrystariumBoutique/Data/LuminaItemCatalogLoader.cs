using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using CrystariumBoutique.Core.Catalog;
using CrystariumBoutique.Core.Errors;
using Dalamud.Plugin.Services;
using LuminaClassJob = Lumina.Excel.Sheets.ClassJob;
using LuminaClassJobCategory = Lumina.Excel.Sheets.ClassJobCategory;
using LuminaFittingShopItemSet = Lumina.Excel.Sheets.FittingShopItemSet;
using LuminaItem = Lumina.Excel.Sheets.Item;

namespace CrystariumBoutique.Data;

public sealed record CatalogLoad(
    EquipmentCatalog Catalog,
    int ExaminedRows,
    TimeSpan Duration,
    int FittingShopItemIds,
    string? FittingShopWarning,
    int AcquisitionIndexedItems,
    int AcquisitionSources,
    ImmutableArray<string> AcquisitionWarnings,
    int SupplementalAcquisitionSources,
    string SupplementalGameBuild);

public sealed class LuminaItemCatalogLoader
{
    private const uint ShieldItemUiCategoryId = 11;

    private readonly IDataManager dataManager;
    private readonly string? acquisitionSupplementPath;
    private readonly string? availabilitySupplementPath;

    public LuminaItemCatalogLoader(
        IDataManager dataManager,
        string? acquisitionSupplementPath = null,
        string? availabilitySupplementPath = null)
    {
        this.dataManager = dataManager ?? throw new ArgumentNullException(nameof(dataManager));
        this.acquisitionSupplementPath = acquisitionSupplementPath;
        this.availabilitySupplementPath = availabilitySupplementPath;
    }

    public Result<CatalogLoad> Load()
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var sheet = dataManager.GetExcelSheet<LuminaItem>();
            var fittingShop = LoadFittingShopItemIds();
            var classJobEligibility = LoadClassJobEligibility();
            var availability = string.IsNullOrWhiteSpace(availabilitySupplementPath)
                ? ItemAvailabilitySupplement.Empty
                : ItemAvailabilitySupplement.Load(availabilitySupplementPath);
            var validatedAvailabilityIds = new HashSet<uint>();
            var items = ImmutableArray.CreateBuilder<EquipmentItem>();
            foreach (var row in sheet)
            {
                var hasAvailabilityRecord = availability.Records.TryGetValue(
                    row.RowId,
                    out var availabilityRecord);
                var hasExactAvailabilityGroup = availability.TryGetExactGroup(
                    row.RowId,
                    out var exactAvailabilityGroup);
                if (hasAvailabilityRecord || hasExactAvailabilityGroup)
                {
                    var localName = row.Name.ExtractText().Trim();
                    if (hasAvailabilityRecord && !string.Equals(
                            localName,
                            availabilityRecord!.ItemName,
                            StringComparison.Ordinal))
                    {
                        throw new InvalidDataException(
                            $"Availability Item ID {row.RowId} expected '{availabilityRecord.ItemName}' but local Lumina contains '{localName}'.");
                    }

                    if (hasExactAvailabilityGroup
                        && !localName.StartsWith(
                            exactAvailabilityGroup.RequiredNamePrefix,
                            StringComparison.Ordinal))
                    {
                        throw new InvalidDataException(
                            $"Availability Item ID {row.RowId} must begin with '{exactAvailabilityGroup.RequiredNamePrefix}' but local Lumina contains '{localName}'.");
                    }

                    validatedAvailabilityIds.Add(row.RowId);
                }

                if (TryMap(
                        row,
                        fittingShop.ItemIds.Contains(row.RowId),
                        classJobEligibility,
                        ImmutableArray<ItemAcquisitionSource>.Empty,
                        (hasAvailabilityRecord && availabilityRecord!.Excluded)
                            || hasExactAvailabilityGroup,
                        out var item))
                {
                    items.Add(item);
                }
            }

            var missingAvailabilityIds = availability.AllItemIds
                .Where(itemId => !validatedAvailabilityIds.Contains(itemId))
                .Order()
                .ToArray();
            if (missingAvailabilityIds.Length > 0)
            {
                throw new InvalidDataException(
                    $"Availability Item IDs are absent from local Lumina: {string.Join(", ", missingAvailabilityIds)}.");
            }

            var mappedItems = items.ToImmutable();
            var mappedItemIds = mappedItems
                .Select(item => item.ItemId)
                .ToHashSet();
            var supplement = string.IsNullOrWhiteSpace(acquisitionSupplementPath)
                ? ItemAcquisitionSupplementLoad.Empty
                : LocalItemAcquisitionSupplementLoader.Load(acquisitionSupplementPath);
            var acquisition = new LuminaItemAcquisitionLoader(
                dataManager,
                mappedItemIds)
                .Load(fittingShop.ItemIds, supplement);
            var enrichedItems = mappedItems
                .Select(item => item with
                {
                    AcquisitionSources = acquisition.GetSources(item.ItemId),
                })
                .ToImmutableArray();
            var catalog = new EquipmentCatalog(enrichedItems);
            stopwatch.Stop();
            return Result.Success(new CatalogLoad(
                catalog,
                sheet.Count,
                stopwatch.Elapsed,
                fittingShop.ItemIds.Count,
                fittingShop.Warning,
                acquisition.IndexedItemCount,
                acquisition.SourceCount,
                acquisition.Warnings,
                supplement.SourcesByItem
                    .Where(pair => mappedItemIds.Contains(pair.Key))
                    .Sum(pair => pair.Value.Length),
                supplement.GameBuild));
        }
        catch (Exception exception)
        {
            stopwatch.Stop();
            return Result.Failure<CatalogLoad>(new BoutiqueError(
                BoutiqueErrorCode.DataLoadFailed,
                "The local equipment catalog could not be loaded.",
                $"{exception.GetType().Name}: {exception.Message}"));
        }
    }

    private FittingShopLoad LoadFittingShopItemIds()
    {
        try
        {
            var itemIds = ImmutableHashSet.CreateBuilder<uint>();
            foreach (var itemSet in dataManager.GetExcelSheet<LuminaFittingShopItemSet>())
            {
                foreach (var item in itemSet.Item)
                {
                    if (item.RowId > 0)
                    {
                        itemIds.Add(item.RowId);
                    }
                }
            }

            return new FittingShopLoad(itemIds.ToImmutable(), null);
        }
        catch (Exception exception)
        {
            return new FittingShopLoad(
                ImmutableHashSet<uint>.Empty,
                $"{exception.GetType().Name}: {exception.Message}");
        }
    }

    private ImmutableDictionary<uint, ClassJobEligibility> LoadClassJobEligibility()
    {
        var classJobDefinitions = dataManager
            .GetExcelSheet<LuminaClassJob>()
            .Where(classJob => classJob.RowId is > 0 and < 128)
            .Select(classJob => new ClassJobDefinition(
                classJob.RowId,
                classJob.Abbreviation.ExtractText().Trim(),
                classJob.IsLimitedJob))
            .Where(classJob => classJob.Abbreviation.Length > 0)
            .OrderBy(classJob => classJob.RowId)
            .ToImmutableArray();
        var categoryProperties = MapClassJobCategoryProperties(classJobDefinitions);

        var result = ImmutableDictionary.CreateBuilder<uint, ClassJobEligibility>();
        foreach (var category in dataManager.GetExcelSheet<LuminaClassJobCategory>())
        {
            var classJobs = default(ClassJobMask);
            if (category.ADV)
            {
                foreach (var classJob in classJobDefinitions)
                {
                    classJobs = classJobs.Add(classJob.RowId);
                }
            }
            else
            {
                foreach (var classJob in classJobDefinitions)
                {
                    if (categoryProperties.TryGetValue(classJob.RowId, out var property)
                        && property.GetValue(category) is true)
                    {
                        classJobs = classJobs.Add(classJob.RowId);
                    }
                }
            }

            result[category.RowId] = new ClassJobEligibility(
                ResolveJobRoles(category, classJobs, classJobDefinitions),
                classJobs);
        }

        return result.ToImmutable();
    }

    internal static ImmutableDictionary<uint, PropertyInfo> MapClassJobCategoryProperties(
        IReadOnlyCollection<ClassJobDefinition> classJobs)
    {
        var booleanProperties = typeof(LuminaClassJobCategory)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(property => property.PropertyType == typeof(bool))
            .ToArray();
        var namedProperties = booleanProperties
            .Where(property => !property.Name.StartsWith("Unknown", StringComparison.Ordinal))
            .ToDictionary(property => property.Name, StringComparer.OrdinalIgnoreCase);
        var mapped = ImmutableDictionary.CreateBuilder<uint, PropertyInfo>();
        var unnamedJobs = new List<ClassJobDefinition>();

        foreach (var classJob in classJobs.OrderBy(classJob => classJob.RowId))
        {
            if (namedProperties.TryGetValue(classJob.Abbreviation, out var property))
            {
                mapped[classJob.RowId] = property;
            }
            else
            {
                unnamedJobs.Add(classJob);
            }
        }

        var unnamedProperties = booleanProperties
            .Select(property => new
            {
                Property = property,
                Index = TryParseUnknownIndex(property.Name),
            })
            .Where(candidate => candidate.Index is not null)
            .OrderBy(candidate => candidate.Index)
            .Select(candidate => candidate.Property)
            .ToArray();

        for (var index = 0; index < Math.Min(unnamedJobs.Count, unnamedProperties.Length); index++)
        {
            mapped[unnamedJobs[index].RowId] = unnamedProperties[index];
        }

        return mapped.ToImmutable();
    }

    private static int? TryParseUnknownIndex(string propertyName)
        => propertyName.StartsWith("Unknown", StringComparison.Ordinal)
            && int.TryParse(propertyName.AsSpan("Unknown".Length), out var index)
                ? index
                : null;

    private static bool TryMap(
        LuminaItem row,
        bool isMogStationExclusive,
        IReadOnlyDictionary<uint, ClassJobEligibility> classJobEligibility,
        ImmutableArray<ItemAcquisitionSource> acquisitionSources,
        bool isExcludedFromBrowsing,
        out EquipmentItem item)
    {
        item = null!;

        if (!row.EquipSlotCategory.IsValid || (row.ModelMain == 0 && row.ModelSub == 0))
        {
            return false;
        }

        var name = row.Name.ExtractText().Trim();
        if (name.Length == 0)
        {
            return false;
        }

        var slotCategory = row.EquipSlotCategory.Value;
        var compatibleSlots = EquipmentSlotMapper.Map(new EquipSlotFlags(
            slotCategory.MainHand != 0,
            slotCategory.OffHand != 0,
            slotCategory.Head != 0,
            slotCategory.Body != 0,
            slotCategory.Gloves != 0,
            slotCategory.Legs != 0,
            slotCategory.Feet != 0,
            slotCategory.Ears != 0,
            slotCategory.Neck != 0,
            slotCategory.Wrists != 0,
            slotCategory.FingerR != 0,
            slotCategory.FingerL != 0));

        if (compatibleSlots.IsEmpty)
        {
            return false;
        }

        var categoryName = row.ItemUICategory.IsValid
            ? row.ItemUICategory.Value.Name.ExtractText().Trim()
            : "Equipment";
        var contentGroup = isMogStationExclusive
            ? SpecialContentGroups.MogStation
            : ContentGroupResolver.Resolve(row.LevelEquip);
        var eligibility = row.ClassJobCategory.IsValid
            && classJobEligibility.TryGetValue(row.ClassJobCategory.RowId, out var mappedEligibility)
                ? mappedEligibility
                : default;

        item = new EquipmentItem(
            row.RowId,
            name,
            categoryName.Length == 0 ? "Equipment" : categoryName,
            row.LevelEquip,
            row.LevelItem.RowId,
            row.Icon,
            row.DyeCount,
            new AppearanceKey(row.ModelMain, row.ModelSub, row.Icon, row.DyeCount),
            contentGroup,
            compatibleSlots,
            eligibility.Roles,
            isMogStationExclusive,
            eligibility.ClassJobs,
            ItemMarketabilityResolver.IsMarketable(
                row.ItemSearchCategory.RowId,
                row.IsUntradable),
            row.Rarity,
            ResolveEquipRestrictions(row),
            acquisitionSources,
            isExcludedFromBrowsing,
            IsShieldItemUiCategory(row.ItemUICategory.RowId));
        return true;
    }

    internal static bool IsShieldItemUiCategory(uint categoryId)
        => categoryId == ShieldItemUiCategoryId;

    private static ItemEquipRestrictions ResolveEquipRestrictions(LuminaItem row)
    {
        var races = CharacterRaceMask.None;
        var sexes = CharacterSexMask.None;
        if (row.EquipRestriction.IsValid)
        {
            var restriction = row.EquipRestriction.Value;
            races |= restriction.Hyur ? CharacterRaceMask.Hyur : CharacterRaceMask.None;
            races |= restriction.Elezen ? CharacterRaceMask.Elezen : CharacterRaceMask.None;
            races |= restriction.Lalafell ? CharacterRaceMask.Lalafell : CharacterRaceMask.None;
            races |= restriction.Miqote ? CharacterRaceMask.Miqote : CharacterRaceMask.None;
            races |= restriction.Roegadyn ? CharacterRaceMask.Roegadyn : CharacterRaceMask.None;
            races |= restriction.AuRa ? CharacterRaceMask.AuRa : CharacterRaceMask.None;
            races |= restriction.Hrothgar ? CharacterRaceMask.Hrothgar : CharacterRaceMask.None;
            races |= restriction.Viera ? CharacterRaceMask.Viera : CharacterRaceMask.None;
            sexes |= restriction.Male ? CharacterSexMask.Male : CharacterSexMask.None;
            sexes |= restriction.Female ? CharacterSexMask.Female : CharacterSexMask.None;
        }

        return new ItemEquipRestrictions(races, sexes, row.GrandCompany.RowId);
    }

    private static EquipmentRoles ResolveJobRoles(
        LuminaClassJobCategory category,
        ClassJobMask eligibleClassJobs,
        IReadOnlyCollection<ClassJobDefinition> classJobs)
    {
        var roles = category.ADV ? EquipmentRoles.All : EquipmentRoles.None;

        if (category.GLA || category.MRD || category.PLD || category.WAR || category.DRK || category.GNB)
        {
            roles |= EquipmentRoles.Tank;
        }

        if (category.CNJ || category.WHM || category.SCH || category.AST || category.SGE)
        {
            roles |= EquipmentRoles.Healer;
        }

        if (category.PGL || category.LNC || category.ROG || category.MNK || category.DRG || category.NIN
            || category.SAM || category.RPR || category.VPR)
        {
            roles |= EquipmentRoles.MeleeDps;
        }

        if (category.ARC || category.BRD || category.MCH || category.DNC)
        {
            roles |= EquipmentRoles.PhysicalRangedDps;
        }

        if (category.THM || category.ACN || category.BLM || category.SMN || category.RDM || category.PCT)
        {
            roles |= EquipmentRoles.MagicalRangedDps;
        }

        roles = IncludeLimitedRole(roles, eligibleClassJobs, classJobs);

        if (category.CRP || category.BSM || category.ARM || category.GSM || category.LTW || category.WVR
            || category.ALC || category.CUL)
        {
            roles |= EquipmentRoles.Crafter;
        }

        if (category.MIN || category.BTN || category.FSH)
        {
            roles |= EquipmentRoles.Gatherer;
        }

        return roles;
    }

    internal static EquipmentRoles IncludeLimitedRole(
        EquipmentRoles roles,
        ClassJobMask eligibleClassJobs,
        IReadOnlyCollection<ClassJobDefinition> classJobs)
        => classJobs.Any(classJob => classJob.IsLimitedJob && eligibleClassJobs.Contains(classJob.RowId))
            ? roles | EquipmentRoles.Limited
            : roles;

    private sealed record FittingShopLoad(
        ImmutableHashSet<uint> ItemIds,
        string? Warning);

    private readonly record struct ClassJobEligibility(
        EquipmentRoles Roles,
        ClassJobMask ClassJobs);

    internal readonly record struct ClassJobDefinition(
        uint RowId,
        string Abbreviation,
        bool IsLimitedJob);
}
