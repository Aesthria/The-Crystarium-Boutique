using System.Collections.Immutable;
using System.Text.Json;
using CrystariumBoutique.Core.Appearance;
using CrystariumBoutique.Core.Catalog;
using CrystariumBoutique.Core.Dyes;
using CrystariumBoutique.Core.Errors;
using CrystariumBoutique.Core.Loadouts;

namespace CrystariumBoutique.Core.ExternalDesigns;

public sealed record EorzeaCollectionEquipment(
    EquipmentSlot Slot,
    string ItemName,
    ImmutableArray<string> DyeNames);

public sealed record EorzeaCollectionDesign(
    long Id,
    string Name,
    ImmutableArray<EorzeaCollectionEquipment> Equipment);

public static class EorzeaCollectionImport
{
    public const string DefaultDesignName = "Eorza Col.";
    public const string ExpectedHost = "ffxiv.eorzeacollection.com";
    private const string ApiPrefix = "https://ffxiv.eorzeacollection.com/api/glamour/";

    private static readonly ImmutableDictionary<string, EquipmentSlot> Slots =
        new Dictionary<string, EquipmentSlot>(StringComparer.Ordinal)
        {
            ["weapon"] = EquipmentSlot.MainHand,
            ["offhand"] = EquipmentSlot.OffHand,
            ["head"] = EquipmentSlot.Head,
            ["body"] = EquipmentSlot.Body,
            ["hands"] = EquipmentSlot.Hands,
            ["legs"] = EquipmentSlot.Legs,
            ["feet"] = EquipmentSlot.Feet,
            ["earrings"] = EquipmentSlot.Ears,
            ["necklace"] = EquipmentSlot.Neck,
            ["bracelets"] = EquipmentSlot.Wrists,
            ["right_ring"] = EquipmentSlot.RightRing,
            ["left_ring"] = EquipmentSlot.LeftRing,
        }.ToImmutableDictionary(StringComparer.Ordinal);

    private static readonly ImmutableDictionary<EquipmentSlot, EmperorItem> EmperorItems =
        new Dictionary<EquipmentSlot, EmperorItem>
        {
            [EquipmentSlot.Head] = new(10032, "The Emperor's New Hat"),
            [EquipmentSlot.Body] = new(10033, "The Emperor's New Robe"),
            [EquipmentSlot.Hands] = new(10034, "The Emperor's New Gloves"),
            [EquipmentSlot.Legs] = new(10035, "The Emperor's New Breeches"),
            [EquipmentSlot.Feet] = new(10036, "The Emperor's New Boots"),
            [EquipmentSlot.Ears] = new(9293, "The Emperor's New Earrings"),
            [EquipmentSlot.Neck] = new(9292, "The Emperor's New Necklace"),
            [EquipmentSlot.Wrists] = new(9294, "The Emperor's New Bracelet"),
            [EquipmentSlot.RightRing] = new(9295, "The Emperor's New Ring"),
            [EquipmentSlot.LeftRing] = new(9295, "The Emperor's New Ring"),
        }.ToImmutableDictionary();

    public static Result<Uri> ConvertToApiUri(string userFacingUrl)
    {
        if (string.IsNullOrWhiteSpace(userFacingUrl)
            || !Uri.TryCreate(userFacingUrl.Trim(), UriKind.Absolute, out var uri)
            || uri.Scheme != Uri.UriSchemeHttps
            || !string.Equals(uri.Host, ExpectedHost, StringComparison.OrdinalIgnoreCase))
        {
            return InvalidUri();
        }

        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length < 2
            || !string.Equals(segments[0], "glamour", StringComparison.Ordinal)
            || !long.TryParse(segments[1], out var glamourId)
            || glamourId <= 0)
        {
            return InvalidUri();
        }

        return Result.Success(new Uri(ApiPrefix + glamourId, UriKind.Absolute));
    }

    public static Result<EorzeaCollectionDesign> Parse(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        try
        {
            using var document = JsonDocument.Parse(json, new JsonDocumentOptions
            {
                MaxDepth = 32,
            });
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("id", out var idElement)
                || !idElement.TryGetInt64(out var id)
                || id <= 0
                || !root.TryGetProperty("gear", out var gear)
                || gear.ValueKind != JsonValueKind.Object)
            {
                return ParseFailure("The response does not contain a valid glamour ID and gear object.");
            }

            var name = root.TryGetProperty("name", out var nameElement)
                && nameElement.ValueKind == JsonValueKind.String
                ? nameElement.GetString()?.Trim()
                : null;
            var equipment = ImmutableArray.CreateBuilder<EorzeaCollectionEquipment>();
            foreach (var property in gear.EnumerateObject())
            {
                if (!Slots.TryGetValue(property.Name, out var slot)
                    || property.Value.ValueKind == JsonValueKind.Null)
                {
                    continue;
                }

                if (property.Value.ValueKind != JsonValueKind.Object
                    || !property.Value.TryGetProperty("name", out var itemNameElement)
                    || itemNameElement.ValueKind != JsonValueKind.String
                    || string.IsNullOrWhiteSpace(itemNameElement.GetString()))
                {
                    return ParseFailure($"The {property.Name} entry does not contain an item name.");
                }

                var dyeNames = ImmutableArray<string>.Empty;
                if (property.Value.TryGetProperty("dyes", out var dyesElement)
                    && dyesElement.ValueKind == JsonValueKind.String
                    && !string.IsNullOrWhiteSpace(dyesElement.GetString()))
                {
                    dyeNames = dyesElement.GetString()!
                        .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                        .Select(dye => dye.Equals("none", StringComparison.OrdinalIgnoreCase)
                            ? string.Empty
                            : dye)
                        .Take(2)
                        .ToImmutableArray();
                }

                equipment.Add(new EorzeaCollectionEquipment(
                    slot,
                    itemNameElement.GetString()!.Trim(),
                    dyeNames));
            }

            return Result.Success(new EorzeaCollectionDesign(
                id,
                string.IsNullOrWhiteSpace(name) ? $"Eorzea Collection {id}" : name,
                equipment.ToImmutable()));
        }
        catch (JsonException exception)
        {
            return ParseFailure($"{exception.GetType().Name}: {exception.Message}");
        }
    }

    public static Result<BoutiqueLoadout> Resolve(
        EorzeaCollectionDesign design,
        IItemRepository items,
        IStainRepository stains,
        string sourceUrl)
    {
        ArgumentNullException.ThrowIfNull(design);
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(stains);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceUrl);

        var resolved = new Dictionary<EquipmentSlot, (EquipmentItem Item, LoadoutEquipmentState State)>();
        var missingItems = new List<string>();
        foreach (var imported in design.Equipment)
        {
            if (!items.TryFindItem(imported.ItemName, imported.Slot, out var item))
            {
                missingItems.Add($"{EquipmentSlotDefinitions.GetDisplayName(imported.Slot)}: {imported.ItemName}");
                continue;
            }

            resolved[imported.Slot] = (item, CreateState(imported.Slot, item, imported.DyeNames, stains));
        }

        if (missingItems.Count > 0)
        {
            return Result.Failure<BoutiqueLoadout>(new BoutiqueError(
                BoutiqueErrorCode.ExternalImportFailed,
                "One or more Eorzea Collection items were not found in the current local game data.",
                string.Join("; ", missingItems)));
        }

        if (resolved.TryGetValue(EquipmentSlot.MainHand, out var mainHand)
            && mainHand.Item.HasLinkedOffHandComponent
            && !resolved.ContainsKey(EquipmentSlot.OffHand))
        {
            resolved[EquipmentSlot.OffHand] = (
                mainHand.Item,
                mainHand.State with { Slot = EquipmentSlot.OffHand });
        }

        foreach (var (slot, emperor) in EmperorItems)
        {
            if (resolved.ContainsKey(slot))
            {
                continue;
            }

            if (items.TryGetItem(emperor.ItemId, slot, out var emperorItem))
            {
                resolved[slot] = (
                    emperorItem,
                    CreateState(slot, emperorItem, ImmutableArray<string>.Empty, stains));
                continue;
            }

            var fallbackItem = new EquipmentItem(
                emperor.ItemId,
                emperor.Name,
                "Equipment",
                1,
                1,
                0,
                0,
                new AppearanceKey(emperor.ItemId, 0),
                ContentGroupResolver.Resolve(1),
                [slot]);
            resolved[slot] = (
                fallbackItem,
                CreateState(slot, fallbackItem, ImmutableArray<string>.Empty, stains));
        }

        if (resolved.Count == 0)
        {
            return Result.Failure<BoutiqueLoadout>(new BoutiqueError(
                BoutiqueErrorCode.ExternalImportFailed,
                "The Eorzea Collection glamour did not contain any supported equipment."));
        }

        var now = DateTimeOffset.UtcNow;
        return Result.Success(new BoutiqueLoadout(
            LoadoutSchema.CurrentVersion,
            Guid.NewGuid(),
            DefaultDesignName,
            now,
            now,
            resolved.Values
                .Select(value => value.State)
                .OrderBy(equipment => equipment.Slot)
                .ToImmutableArray(),
            ["Eorzea Collection"],
            $"Imported from {sourceUrl}{Environment.NewLine}Source design: {design.Name}"));
    }

    private static LoadoutEquipmentState CreateState(
        EquipmentSlot slot,
        EquipmentItem item,
        ImmutableArray<string> dyeNames,
        IStainRepository stains)
    {
        var resolvedStains = ImmutableArray.CreateBuilder<StainId>(item.DyeChannelCount);
        for (var channel = 0; channel < item.DyeChannelCount; channel++)
        {
            var dyeName = channel < dyeNames.Length ? dyeNames[channel] : string.Empty;
            resolvedStains.Add(ResolveStain(dyeName, stains));
        }

        return new LoadoutEquipmentState(
            slot,
            new AppearanceSelection(
                new AppearanceId(item.ItemId),
                item.ItemId,
                resolvedStains.ToImmutable()),
            item.DyeChannelCount,
            item.Name,
            item.IconId);
    }

    private static StainId ResolveStain(string dyeName, IStainRepository stains)
    {
        if (string.IsNullOrWhiteSpace(dyeName))
        {
            return StainId.None;
        }

        var normalized = CatalogSearch.Normalize(dyeName);
        foreach (var stain in stains.All)
        {
            if (CatalogSearch.Normalize(stain.Name) == normalized)
            {
                return stain.Id;
            }
        }

        return StainId.None;
    }

    private static Result<Uri> InvalidUri()
        => Result.Failure<Uri>(new BoutiqueError(
            BoutiqueErrorCode.ExternalImportFailed,
            "Enter a full Eorzea Collection glamour URL, such as https://ffxiv.eorzeacollection.com/glamour/12345/name."));

    private static Result<EorzeaCollectionDesign> ParseFailure(string technicalContext)
        => Result.Failure<EorzeaCollectionDesign>(new BoutiqueError(
            BoutiqueErrorCode.ExternalImportFailed,
            "The Eorzea Collection response could not be read.",
            technicalContext));

    private sealed record EmperorItem(uint ItemId, string Name);
}
