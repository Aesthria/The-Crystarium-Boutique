using System.Collections.Immutable;
using CrystariumBoutique.Core.Appearance;

namespace CrystariumBoutique.Core.Catalog;

public readonly record struct AppearanceKey(
    ulong ModelMain,
    ulong ModelSub,
    uint IconId = 0,
    byte DyeChannelCount = 0)
{
    public bool IsEmpty => ModelMain == 0 && ModelSub == 0;
}

public readonly record struct ModelFamilyKey(ulong ModelMain, ulong ModelSub)
{
    public bool IsEmpty => ModelMain == 0 && ModelSub == 0;

    public static ModelFamilyKey FromAppearance(AppearanceKey appearanceKey)
        => new(appearanceKey.ModelMain, appearanceKey.ModelSub);
}

public readonly record struct ClassJobMask(ulong Lower, ulong Upper)
{
    public bool IsEmpty => Lower == 0 && Upper == 0;

    public ClassJobMask Add(uint classJobId)
        => classJobId switch
        {
            > 0 and < 64 => this with { Lower = Lower | (1UL << (int)classJobId) },
            >= 64 and < 128 => this with { Upper = Upper | (1UL << (int)(classJobId - 64)) },
            _ => this,
        };

    public bool Contains(uint classJobId)
        => classJobId switch
        {
            > 0 and < 64 => (Lower & (1UL << (int)classJobId)) != 0,
            >= 64 and < 128 => (Upper & (1UL << (int)(classJobId - 64))) != 0,
            _ => false,
        };
}

public sealed record EquipmentItem(
    uint ItemId,
    string Name,
    string CategoryName,
    byte EquipLevel,
    uint ItemLevel,
    uint IconId,
    byte DyeChannelCount,
    AppearanceKey AppearanceKey,
    ContentGroup ContentGroup,
    ImmutableArray<EquipmentSlot> CompatibleSlots,
    EquipmentRoles JobRoles = EquipmentRoles.None,
    bool IsMogStationExclusive = false,
    ClassJobMask EquippableClassJobs = default,
    bool IsMarketable = false,
    byte Rarity = 1,
    ItemEquipRestrictions EquipRestrictions = default,
    ImmutableArray<ItemAcquisitionSource> AcquisitionSources = default,
    bool IsExcludedFromBrowsing = false,
    bool IsShield = false)
{
    public ModelFamilyKey ModelFamilyKey => ModelFamilyKey.FromAppearance(AppearanceKey);

    public bool HasLinkedOffHandComponent
        => AppearanceKey.ModelSub != 0
            && CompatibleSlots.Contains(EquipmentSlot.MainHand)
            && CompatibleSlots.Contains(EquipmentSlot.OffHand);
}

public sealed record AppearanceEntry(
    AppearanceKey AppearanceKey,
    EquipmentItem PrimaryItem,
    ImmutableArray<EquipmentItem> SourceItems);

public sealed record CatalogPage(
    EquipmentSlot Slot,
    ContentGroup ContentGroup,
    int PageIndex,
    int TotalPages,
    int TotalResults,
    ImmutableArray<AppearanceEntry> Items)
{
    public int DisplayPage => TotalPages == 0 ? 0 : PageIndex + 1;

    public TimeSpan QueryDuration { get; init; }

    public bool UsedCachedFilter { get; init; }
}
