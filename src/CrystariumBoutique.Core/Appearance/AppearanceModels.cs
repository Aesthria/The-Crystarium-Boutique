using System.Collections.Immutable;

namespace CrystariumBoutique.Core.Appearance;

public readonly record struct AppearanceId(ulong Value)
{
    public static AppearanceId None => default;
}

public readonly record struct StainId(byte Value)
{
    public static StainId None => default;

    public bool IsNone => Value == 0;
}

public sealed record AppearanceSelection(
    AppearanceId AppearanceId,
    uint? SourceItemId,
    ImmutableArray<StainId> Stains)
{
    public static AppearanceSelection WithoutStains(AppearanceId appearanceId, uint? sourceItemId = null)
        => new(appearanceId, sourceItemId, ImmutableArray<StainId>.Empty);

    public AppearanceSelection WithStain(byte dyeChannel, StainId stain)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(dyeChannel, (byte)1);
        var first = Stains.Length > 0 ? Stains[0] : StainId.None;
        var second = Stains.Length > 1 ? Stains[1] : StainId.None;
        return this with
        {
            Stains = dyeChannel == 0
                ? [stain, second]
                : [first, stain],
        };
    }
}

public sealed record AppearanceSnapshot(
    string SerializedState,
    string Format,
    string? SourceVersion,
    ImmutableArray<CapturedEquipmentState> Equipment = default)
{
    public static AppearanceSnapshot Create(
        string serializedState,
        string format,
        string? sourceVersion = null,
        ImmutableArray<CapturedEquipmentState> equipment = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serializedState);
        ArgumentException.ThrowIfNullOrWhiteSpace(format);
        return new AppearanceSnapshot(serializedState, format, sourceVersion, equipment);
    }
}

public sealed record CapturedEquipmentState(
    EquipmentSlot Slot,
    uint ItemId,
    ImmutableArray<StainId> Stains);
