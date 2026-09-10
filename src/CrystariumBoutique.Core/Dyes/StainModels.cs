using System.Collections.Immutable;
using CrystariumBoutique.Core.Appearance;

namespace CrystariumBoutique.Core.Dyes;

public enum StainGroup : byte
{
    Neutral = 2,
    Red = 4,
    BrownOrange = 5,
    Yellow = 6,
    Green = 7,
    Blue = 8,
    Purple = 9,
    Special = 10,
    Other = byte.MaxValue,
}

public readonly record struct StainColor(byte Red, byte Green, byte Blue)
{
    public static StainColor FromPackedRgb(uint packedRgb)
        => new(
            (byte)((packedRgb >> 16) & 0xFF),
            (byte)((packedRgb >> 8) & 0xFF),
            (byte)(packedRgb & 0xFF));
}

public sealed record StainDefinition(
    StainId Id,
    string Name,
    StainColor Color,
    StainGroup Group,
    byte SortOrder,
    bool IsMetallic);

public static class StainGroups
{
    public static ImmutableArray<StainGroup> All { get; } =
    [
        StainGroup.Neutral,
        StainGroup.Red,
        StainGroup.BrownOrange,
        StainGroup.Yellow,
        StainGroup.Green,
        StainGroup.Blue,
        StainGroup.Purple,
        StainGroup.Special,
        StainGroup.Other,
    ];

    public static StainGroup FromShade(byte shade)
        => shade switch
        {
            2 => StainGroup.Neutral,
            4 => StainGroup.Red,
            5 => StainGroup.BrownOrange,
            6 => StainGroup.Yellow,
            7 => StainGroup.Green,
            8 => StainGroup.Blue,
            9 => StainGroup.Purple,
            10 => StainGroup.Special,
            _ => StainGroup.Other,
        };

    public static string GetDisplayName(StainGroup group)
        => group switch
        {
            StainGroup.Neutral => "Neutral",
            StainGroup.Red => "Red & Pink",
            StainGroup.BrownOrange => "Brown & Orange",
            StainGroup.Yellow => "Yellow",
            StainGroup.Green => "Green",
            StainGroup.Blue => "Blue",
            StainGroup.Purple => "Purple",
            StainGroup.Special => "Special & Metallic",
            _ => "Other",
        };

    public static int GetSortOrder(StainGroup group)
        => group switch
        {
            StainGroup.Neutral => 0,
            StainGroup.Red => 1,
            StainGroup.BrownOrange => 2,
            StainGroup.Yellow => 3,
            StainGroup.Green => 4,
            StainGroup.Blue => 5,
            StainGroup.Purple => 6,
            StainGroup.Special => 7,
            _ => 8,
        };
}
