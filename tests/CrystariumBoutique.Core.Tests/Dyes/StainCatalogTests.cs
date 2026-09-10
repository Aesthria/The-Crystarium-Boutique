using CrystariumBoutique.Core.Appearance;
using CrystariumBoutique.Core.Dyes;

namespace CrystariumBoutique.Core.Tests.Dyes;

public sealed class StainCatalogTests
{
    [Fact]
    public void PackedRgbColorUsesTheGameSheetsChannelOrder()
    {
        var color = StainColor.FromPackedRgb(0x00E4DFD0);

        Assert.Equal(0xE4, color.Red);
        Assert.Equal(0xDF, color.Green);
        Assert.Equal(0xD0, color.Blue);
    }

    [Theory]
    [InlineData(2, StainGroup.Neutral)]
    [InlineData(4, StainGroup.Red)]
    [InlineData(8, StainGroup.Blue)]
    [InlineData(10, StainGroup.Special)]
    [InlineData(99, StainGroup.Other)]
    public void SheetShadesMapToStableGroups(byte shade, StainGroup expected)
        => Assert.Equal(expected, StainGroups.FromShade(shade));

    [Fact]
    public void CatalogSearchNormalizesNamesAndRestrictsGroups()
    {
        var catalog = new StainCatalog(
        [
            Stain(1, "Snow White", StainGroup.Neutral, 2),
            Stain(2, "Dalamud Red", StainGroup.Red, 4),
            Stain(3, "Rolanberry Red", StainGroup.Red, 3),
        ]);

        var normalized = catalog.Search("  DALAMUD-red ");
        var grouped = catalog.Search("red", StainGroup.Red);
        var excluded = catalog.Search("white", StainGroup.Red);

        Assert.Equal("Dalamud Red", Assert.Single(normalized).Name);
        Assert.Equal(["Rolanberry Red", "Dalamud Red"], grouped.Select(stain => stain.Name));
        Assert.Empty(excluded);
    }

    [Fact]
    public void RecentHistoryMovesDuplicatesForwardAndRemainsBounded()
    {
        var history = new RecentStainHistory(3);
        history.Record(new StainId(1));
        history.Record(new StainId(2));
        history.Record(new StainId(3));
        history.Record(new StainId(2));
        history.Record(new StainId(4));
        history.Record(StainId.None);

        Assert.Equal([new StainId(4), new StainId(2), new StainId(3)], history.Items);
    }

    [Fact]
    public void AppearanceSelectionUpdatesOneOfTwoStainChannels()
    {
        var selection = AppearanceSelection.WithoutStains(new AppearanceId(100), 100)
            .WithStain(1, new StainId(9))
            .WithStain(0, new StainId(5));

        Assert.Equal([new StainId(5), new StainId(9)], selection.Stains);
        Assert.Throws<ArgumentOutOfRangeException>(() => selection.WithStain(2, new StainId(1)));
    }

    private static StainDefinition Stain(
        byte id,
        string name,
        StainGroup group,
        byte sortOrder)
        => new(
            new StainId(id),
            name,
            new StainColor(id, id, id),
            group,
            sortOrder,
            false);
}
