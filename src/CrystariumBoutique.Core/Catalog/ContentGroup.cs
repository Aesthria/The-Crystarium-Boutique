namespace CrystariumBoutique.Core.Catalog;

public sealed record ContentGroup(
    string Key,
    string DisplayName,
    int SortOrder,
    byte MinimumEquipLevel,
    byte MaximumEquipLevel);

public static class SpecialContentGroups
{
    public static ContentGroup MogStation { get; } = new(
        "mogstation",
        "Mogstation",
        1_000_001,
        0,
        byte.MaxValue);

    public static ContentGroup AllExpansions { get; } = new(
        "all-expansions",
        "All",
        -1,
        1,
        byte.MaxValue);
}

public static class ContentGroupResolver
{
    private static readonly ContentGroup Other = new("other", "Other", 1_000_000, 0, 0);

    private static readonly ContentGroup[] KnownGroups =
    [
        new("arr", "A Realm Reborn", 0, 1, 50),
        new("hw", "Heavensward", 1, 51, 60),
        new("sb", "Stormblood", 2, 61, 70),
        new("shb", "Shadowbringers", 3, 71, 80),
        new("ew", "Endwalker", 4, 81, 90),
        new("dt", "Dawntrail", 5, 91, 100),
    ];

    public static ContentGroup Resolve(byte equipLevel)
    {
        if (equipLevel == 0)
        {
            return Other;
        }

        foreach (var group in KnownGroups)
        {
            if (equipLevel >= group.MinimumEquipLevel && equipLevel <= group.MaximumEquipLevel)
            {
                return group;
            }
        }

        var minimum = ((equipLevel - 1) / 10 * 10) + 1;
        var maximum = Math.Min(minimum + 9, byte.MaxValue);
        return new ContentGroup(
            $"levels-{minimum}-{maximum}",
            $"Levels {minimum}–{maximum}",
            minimum,
            (byte)minimum,
            (byte)maximum);
    }
}
