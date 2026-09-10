using System.Collections.Immutable;
using CrystariumBoutique.Core.Appearance;
using CrystariumBoutique.Core.Catalog;

namespace CrystariumBoutique.Core.Tests.Catalog;

public sealed class ItemAcquisitionFormatterTests
{
    [Fact]
    public void DetailedVendorUsesNpcLocationAndCostLayout()
    {
        var source = new ItemAcquisitionSource(
            ItemAcquisitionKind.Vendor,
            "Mowen's Merchant",
            "The Crystarium",
            "2 Helm of Early Antiquity",
            "Requires: Eden's Gate");

        var text = ItemAcquisitionFormatter.Format(
            source,
            ItemAcquisitionDetailLevel.Detailed);

        Assert.Equal(
            "Vendor | NPC: Mowen's Merchant | In: The Crystarium | Cost: 2 Helm of Early Antiquity | Requires: Eden's Gate",
            text);
    }

    [Fact]
    public void StandardCraftingOmitsRecipeDetail()
    {
        var source = new ItemAcquisitionSource(
            ItemAcquisitionKind.Crafting,
            "Goldsmith",
            Detail: "Recipe Lv. 100 / Master Goldsmith XII");

        var text = ItemAcquisitionFormatter.Format(
            source,
            ItemAcquisitionDetailLevel.Standard);

        Assert.Equal("Crafted | Goldsmith", text);
    }

    [Fact]
    public void SourceOnlyUsesCompactBadgeLabel()
    {
        var source = new ItemAcquisitionSource(
            ItemAcquisitionKind.SeasonalEvent,
            "Moonfire Faire 2026");

        var text = ItemAcquisitionFormatter.Format(
            source,
            ItemAcquisitionDetailLevel.SourceOnly);

        Assert.Equal("Seasonal Event", text);
    }

    [Fact]
    public void DetailedDutyIncludesStructuredClassification()
    {
        var source = new ItemAcquisitionSource(
            ItemAcquisitionKind.DutyDrop,
            "The Final Coil of Bahamut - Turn 4",
            "Final encounter",
            Detail: "Direct boss drop",
            Metadata: new ItemAcquisitionMetadata(
                193,
                "ContentFinderCondition",
                "Raid",
                "Savage",
                "Bahamut Prime",
                "The Binding Coil of Bahamut",
                "Final Coil",
                "Verified local fixture",
                DropType: DutyAcquisitionDropType.BossDrop));

        var text = ItemAcquisitionFormatter.Format(
            source,
            ItemAcquisitionDetailLevel.Detailed);

        Assert.Equal(
            "RAID — SAVAGE | The Final Coil of Bahamut - Turn 4 | Bahamut Prime — Boss Drop",
            text);
    }

    [Fact]
    public void GenericDutyChestUsesSingleLineWithoutEmptyBossField()
    {
        var source = new ItemAcquisitionSource(
            ItemAcquisitionKind.DutyDrop,
            "The Meso Terminal",
            Metadata: new ItemAcquisitionMetadata(
                ContentType: "Dungeon",
                Difficulty: "Normal",
                DropType: DutyAcquisitionDropType.DutyChest));

        var text = ItemAcquisitionFormatter.Format(
            source,
            ItemAcquisitionDetailLevel.Detailed);

        Assert.Equal("DUNGEON | The Meso Terminal | Duty Chest", text);
        Assert.DoesNotContain('\n', text);
        Assert.DoesNotContain('>', text);
    }

    [Theory]
    [InlineData("Dungeon", "Hard", "DUNGEON — HARD")]
    [InlineData("Trial", "Extreme", "TRIAL — EXTREME")]
    [InlineData("Raid", "Savage", "RAID — SAVAGE")]
    [InlineData("Alliance Raid", "Alliance Raid", "ALLIANCE RAID")]
    public void DutyDifficultyHeadingRemainsEmDashFormatted(
        string contentType,
        string difficulty,
        string expectedHeading)
    {
        var source = new ItemAcquisitionSource(
            ItemAcquisitionKind.DutyDrop,
            "Fixture Duty",
            Metadata: new ItemAcquisitionMetadata(
                ContentType: contentType,
                Difficulty: difficulty,
                BossName: "Fixture Boss",
                DropType: DutyAcquisitionDropType.BossChest));

        var text = ItemAcquisitionFormatter.Format(
            source,
            ItemAcquisitionDetailLevel.Detailed);

        Assert.Equal($"{expectedHeading} | Fixture Duty | Fixture Boss — Boss Chest", text);
        Assert.DoesNotContain('\n', text);
    }

    [Fact]
    public void LocalSourcesAddMarketAndOnlineStoreWithoutDuplicates()
    {
        var item = CreateItem() with
        {
            IsMarketable = true,
            IsMogStationExclusive = true,
            AcquisitionSources =
            [
                new ItemAcquisitionSource(ItemAcquisitionKind.Crafting, "Weaver"),
                new ItemAcquisitionSource(ItemAcquisitionKind.OnlineStore),
            ],
        };

        var sources = ItemAcquisitionFormatter.GetLocalSources(item);

        Assert.Equal(3, sources.Length);
        Assert.Single(sources, source => source.Kind == ItemAcquisitionKind.MarketBoard);
        Assert.Single(sources, source => source.Kind == ItemAcquisitionKind.OnlineStore);
        Assert.Single(sources, source => source.Kind == ItemAcquisitionKind.Crafting);
    }

    [Fact]
    public void CompactLabelsDoNotIncludeStackableSourceDetails()
    {
        Assert.Equal("Vendor", ItemAcquisitionFormatter.GetCompactLabel(ItemAcquisitionKind.Vendor));
        Assert.Equal("Crafted", ItemAcquisitionFormatter.GetCompactLabel(ItemAcquisitionKind.Crafting));
        Assert.Equal("Marketboard", ItemAcquisitionFormatter.GetCompactLabel(ItemAcquisitionKind.MarketBoard));
        Assert.Equal("Duty drop", ItemAcquisitionFormatter.GetCompactLabel(ItemAcquisitionKind.DutyDrop));
    }

    private static EquipmentItem CreateItem()
        => new(
            42,
            "Test Coat",
            "Body",
            100,
            700,
            42,
            2,
            new AppearanceKey(42, 0),
            ContentGroupResolver.Resolve(100),
            ImmutableArray.Create(EquipmentSlot.Body));
}
