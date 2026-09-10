using CrystariumBoutique.AcquisitionGenerator;
using CrystariumBoutique.Core.Catalog;

namespace CrystariumBoutique.Core.Tests.AcquisitionGeneration;

public sealed class LuminaSupplementalAcquisitionGeneratorTests
{
    [Fact]
    public void ExactBossDropRetainsBossAndDropType()
    {
        var result = Generate(bossDrops: "1,10,2,100,1\n");

        var source = Assert.Single(result.Supplement.DutySources);
        Assert.Equal("Boss Alpha", source.BossName);
        Assert.Equal(DutyAcquisitionDropType.BossDrop, source.DropType);
        Assert.Contains("Boss Alpha — Boss Drop", ItemAcquisitionFormatter.Format(
            ToRuntimeSource(source), ItemAcquisitionDetailLevel.Standard));
    }

    [Fact]
    public void BossChestRetainsBossAndDropType()
    {
        var result = Generate(bossChests: "1,100,10,1,2,1\n");

        var source = Assert.Single(result.Supplement.DutySources);
        Assert.Equal("Boss Alpha", source.BossName);
        Assert.Equal(DutyAcquisitionDropType.BossChest, source.DropType);
    }

    [Fact]
    public void GenericDutySourceOmitsBoss()
    {
        var result = Generate(dutyDrops: "1,100,10\n");

        var source = Assert.Single(result.Supplement.DutySources);
        Assert.Equal(string.Empty, source.BossName);
        Assert.Equal(DutyAcquisitionDropType.DutyDrop, source.DropType);
    }

    [Fact]
    public void SpecificSourceSupersedesGenericSourceForSameItemAndDuty()
    {
        var result = Generate(
            bossChests: "1,100,10,1,2,1\n",
            dutyDrops: "1,100,10\n");

        var source = Assert.Single(result.Supplement.DutySources);
        Assert.Equal(DutyAcquisitionDropType.BossChest, source.DropType);
        Assert.Equal(1, result.Manifest.SupersededRecords);
    }

    [Fact]
    public void BossDropOutranksBossChestForSameItemAndDuty()
    {
        var result = Generate(
            bossChests: "1,100,10,1,2,1\n",
            bossDrops: "1,10,2,100,1\n");

        var source = Assert.Single(result.Supplement.DutySources);
        Assert.Equal(DutyAcquisitionDropType.BossDrop, source.DropType);
        Assert.Equal(1, result.Manifest.SupersededRecords);
    }

    [Fact]
    public void DutyChestOutranksGenericDutyDropForSameItemAndDuty()
    {
        var result = Generate(
            chests: "1,1,10,40,20,30,1,0;0;0\n",
            chestItems: "1,100,1,1,1,50\n",
            dutyDrops: "1,100,10\n");

        var source = Assert.Single(result.Supplement.DutySources);
        Assert.Equal(DutyAcquisitionDropType.DutyChest, source.DropType);
        Assert.Equal(1, result.Manifest.SupersededRecords);
    }

    [Fact]
    public void MinstrelsBalladUsesExtremePresentationWithoutAnExtremeSuffix()
        => Assert.Equal(
            "Extreme",
            LuminaGameDataIndex.ResolveDifficulty(
                "the Minstrel's Ballad: Ultima's Bane",
                "Trial"));

    [Fact]
    public void IdenticalPinnedInputsProduceDeterministicSupplementJson()
    {
        var first = Generate(bossChests: "1,100,10,1,2,1\n2,100,11,1,1,1\n");
        var second = Generate(bossChests: "1,100,10,1,2,1\n2,100,11,1,1,1\n");

        Assert.Equal(
            AcquisitionSupplementGenerator.Serialize(first.Supplement),
            AcquisitionSupplementGenerator.Serialize(second.Supplement));
    }

    [Fact]
    public void MultipleLegitimateDutiesAreRetained()
    {
        var result = Generate(
            bossChests: "1,100,10,1,2,1\n2,100,11,1,1,1\n");

        Assert.Equal(2, result.Supplement.DutySources.Count);
        Assert.Equal([10u, 11u], result.Supplement.DutySources.Select(value => value.DutyId));
    }

    [Fact]
    public void DuplicateEquivalentSourcesAreCollapsed()
    {
        var result = Generate(
            bossChests: "1,100,10,1,2,1\n2,100,10,1,2,2\n");

        Assert.Single(result.Supplement.DutySources);
        Assert.Equal(1, result.Manifest.SupersededRecords);
    }

    [Fact]
    public void DutyChestUsesStableChestIdsWithoutClaimingBoss()
    {
        var result = Generate(
            chests: "1,1,10,40,20,30,1,0;0;0\n",
            chestItems: "1,100,1,1,1,50\n");

        var source = Assert.Single(result.Supplement.DutySources);
        Assert.Equal(DutyAcquisitionDropType.DutyChest, source.DropType);
        Assert.Equal(string.Empty, source.BossName);
        Assert.Equal(30u, source.Evidence.TreasureId);
        Assert.Empty(source.Evidence.BossNpcNameIds);
    }

    private static GenerationResult Generate(
        string bossChests = "",
        string bossDrops = "",
        string chests = "",
        string chestItems = "",
        string dutyDrops = "")
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "CrystariumBoutique-LuminaSupplementalTests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var files = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["DungeonBoss.csv"] = "RowId,BNpcNameId,ContentFinderConditionId,FightNo\n1,500,10,2\n2,501,11,1\n",
                ["DungeonBossChest.csv"] = "RowId,ItemId,ContentFinderConditionId,Quantity,FightNo,CofferNo\n" + bossChests,
                ["DungeonBossDrop.csv"] = "RowId,ContentFinderConditionId,FightNo,ItemId,Quantity\n" + bossDrops,
                ["DungeonChest.csv"] = "RowId,ChestNo,ContentFinderConditionId,MapId,TerritoryTypeId,TreasureId,DungeonBossId,Position\n" + chests,
                ["DungeonChestItem.csv"] = "RowId,ItemId,ChestId,Min,Max,Probability\n" + chestItems,
                ["DungeonDrop.csv"] = "RowId,ItemId,ContentFinderConditionId\n" + dutyDrops,
            };
            foreach (var file in files)
            {
                File.WriteAllText(Path.Combine(directory, file.Key), file.Value);
            }

            var descriptor = new LuminaSupplementalSourceDescriptor
            {
                UpstreamSource = "Critical-Impact/LuminaSupplemental",
                UpstreamRevision = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
                UpstreamVersion = "fixture",
                License = "GPL-3.0-only",
                LicenseUrl = "https://example.invalid/license",
                Files = new SortedDictionary<string, string>(files.Keys.ToDictionary(
                    fileName => fileName,
                    fileName => AcquisitionSupplementGenerator.ComputeSha256File(Path.Combine(directory, fileName))),
                    StringComparer.Ordinal),
            };
            var gameData = new InMemoryGameDataIndex(
                "fixture-game",
                "fixture-lumina",
                [new GameItem(100, "Fixture Gear", true)],
                [
                    new GameDuty(10, "Fixture Dungeon", "Dungeon", 20, "Normal", "Fixture Expansion"),
                    new GameDuty(11, "Fixture Trial", "Trial", 21, "Extreme", "Fixture Expansion"),
                ],
                [20, 21],
                [30],
                [new KeyValuePair<uint, uint>(40, 20)],
                [
                    new KeyValuePair<uint, string>(500, "Boss Alpha"),
                    new KeyValuePair<uint, string>(501, "Boss Beta"),
                ]);
            return LuminaSupplementalAcquisitionGenerator.Generate(
                new LuminaSupplementalGeneratorInput(directory, descriptor, gameData),
                DateTimeOffset.UnixEpoch);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static ItemAcquisitionSource ToRuntimeSource(GeneratedDutySource source)
        => new(
            ItemAcquisitionKind.DutyDrop,
            source.DutyName,
            Metadata: new ItemAcquisitionMetadata(
                source.DutyId,
                source.SourceTable,
                source.ContentType,
                source.Difficulty,
                source.BossName,
                DropType: source.DropType));
}
