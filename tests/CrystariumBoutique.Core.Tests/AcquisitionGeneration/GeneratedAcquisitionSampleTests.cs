using CrystariumBoutique.Core.Catalog;

namespace CrystariumBoutique.Core.Tests.AcquisitionGeneration;

public sealed class GeneratedAcquisitionSampleTests
{
    [Fact]
    public void PackagedSupplementLoadsAllPinnedExactRelationships()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "item-acquisition-supplement.json");
        var result = ItemAcquisitionSupplementParser.Parse(File.ReadAllText(path));

        Assert.Empty(result.Warnings);
        Assert.Equal(8003, result.SourceCount);
        Assert.Equal(5764, result.SourcesByItem.Count);
        foreach (var source in result.SourcesByItem.Values.SelectMany(value => value))
        {
            var evidence = Assert.IsType<ItemAcquisitionEvidence>(source.Metadata?.Evidence);
            Assert.Equal(ItemAcquisitionEvidenceClassification.Corroborated, evidence.Classification);
            Assert.Equal(ItemAcquisitionEvidenceOrigin.LuminaSupplemental, evidence.Origin);
            Assert.True(evidence.SupplementalRowId > 0);
            var rendered = ItemAcquisitionFormatter.Format(source, ItemAcquisitionDetailLevel.Detailed);
            Assert.DoesNotContain('\n', rendered);
            Assert.Contains(" | ", rendered, StringComparison.Ordinal);
            Assert.Contains(source.Name, rendered, StringComparison.Ordinal);
        }
    }

    [Theory]
    [InlineData(1636u, 10u, "DUNGEON | the Wanderer's Palace | Duty Chest")]
    [InlineData(2824u, 18u, "DUNGEON — HARD | Copperbell Mines (Hard) | Biggy — Boss Chest")]
    [InlineData(4190u, 63u, "TRIAL — EXTREME | the Bowl of Embers (Extreme) | Ifrit — Boss Chest")]
    [InlineData(4400u, 68u, "TRIAL — EXTREME | the Minstrel's Ballad: Ultima's Bane | the Ultima Weapon — Boss Chest")]
    [InlineData(7168u, 103u, "RAID — SAVAGE | the Second Coil of Bahamut (Savage) - Turn 1 | rafflesia — Boss Chest")]
    [InlineData(2952u, 92u, "ALLIANCE RAID | the Labyrinth of the Ancients | bone dragon — Boss Chest")]
    public void RepresentativeGeneratedSourceUsesCompactSingleLine(
        uint itemId,
        uint dutyId,
        string expected)
    {
        var load = LoadPackagedSupplement();
        var source = Assert.Single(
            load.SourcesByItem[itemId],
            candidate => candidate.Metadata?.SourceId == dutyId
                && ItemAcquisitionFormatter.Format(
                    candidate,
                    ItemAcquisitionDetailLevel.Detailed).Equals(expected, StringComparison.Ordinal));

        var rendered = ItemAcquisitionFormatter.Format(
            source,
            ItemAcquisitionDetailLevel.Detailed);

        Assert.Equal(expected, rendered);
        Assert.DoesNotContain('\n', rendered);
    }

    [Fact]
    public void MultipleAndVeryLongGeneratedSourcesRemainIndependentLogicalLines()
    {
        var load = LoadPackagedSupplement();
        var multiple = load.SourcesByItem[26922];
        Assert.True(multiple.Length >= 3);
        Assert.All(multiple, source => Assert.DoesNotContain(
            '\n',
            ItemAcquisitionFormatter.Format(source, ItemAcquisitionDetailLevel.Detailed)));

        var longSource = Assert.Single(
            load.SourcesByItem[30687],
            candidate => candidate.Metadata?.SourceId == 736
                && candidate.Metadata.BossName.StartsWith("724P-operated", StringComparison.Ordinal));
        var rendered = ItemAcquisitionFormatter.Format(
            longSource,
            ItemAcquisitionDetailLevel.Detailed);
        Assert.StartsWith("ALLIANCE RAID | the Puppets' Bunker | ", rendered, StringComparison.Ordinal);
        Assert.EndsWith(" — Boss Chest", rendered, StringComparison.Ordinal);
        Assert.DoesNotContain('\n', rendered);
    }

    private static ItemAcquisitionSupplementLoad LoadPackagedSupplement()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "item-acquisition-supplement.json");
        return ItemAcquisitionSupplementParser.Parse(File.ReadAllText(path));
    }
}
