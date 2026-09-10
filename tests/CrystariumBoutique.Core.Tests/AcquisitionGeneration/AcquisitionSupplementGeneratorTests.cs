using System.Text.Json;
using CrystariumBoutique.AcquisitionGenerator;
using CrystariumBoutique.Core.Catalog;

namespace CrystariumBoutique.Core.Tests.AcquisitionGeneration;

public sealed class AcquisitionSupplementGeneratorTests
{
    [Fact]
    public void ValidObservedEquipmentDropIsAccepted()
    {
        var result = Generate(TrackyJson(new Observation(100, 10, 20, 30, 40, 7)));

        var source = Assert.Single(result.Supplement.DutySources);
        Assert.Equal(100u, source.ItemId);
        Assert.Equal(ItemAcquisitionEvidenceClassification.VerifiedObserved, source.Evidence.Classification);
        Assert.Equal(7, source.Evidence.ObservationCount);
        Assert.Empty(result.Review.Quarantined);
        Assert.Empty(result.Review.Rejected);
    }

    [Fact]
    public void InvalidItemIdIsRejected()
    {
        var result = Generate(
            TrackyJson(new Observation(999, 10, 20, 30, 40, 7)),
            items: [new GameItem(100, "Known Gear", true)]);

        Assert.Empty(result.Supplement.DutySources);
        var rejected = Assert.Single(result.Review.Rejected);
        Assert.Equal("InvalidItemId", rejected.Reason);
    }

    [Fact]
    public void InvalidDutyIdIsRejected()
    {
        var result = Generate(
            TrackyJson(new Observation(100, 999, 20, 30, 40, 7)),
            duties: [new GameDuty(10, "Known Duty", "Raid", 20)]);

        Assert.Empty(result.Supplement.DutySources);
        Assert.Equal("InvalidContentFinderConditionId", Assert.Single(result.Review.Rejected).Reason);
    }

    [Fact]
    public void CofferRemainsItsExactItemAndIsNeverExpanded()
    {
        var result = Generate(
            TrackyJson(new Observation(200, 10, 20, 30, 40, 7)),
            items:
            [
                new GameItem(200, "Observed Coffer", false),
                new GameItem(201, "Equipment Inside Coffer", true),
            ]);

        Assert.Empty(result.Supplement.DutySources);
        var rejected = Assert.Single(result.Review.Rejected);
        Assert.Equal(200u, rejected.ItemId);
        Assert.Equal("NonEquipmentExactItem", rejected.Reason);
        Assert.DoesNotContain(result.Supplement.DutySources, value => value.ItemId == 201);
    }

    [Fact]
    public void TokenRemainsItsExactItemAndIsNeverExpanded()
    {
        var result = Generate(
            TrackyJson(new Observation(300, 10, 20, 30, 40, 7)),
            items:
            [
                new GameItem(300, "Observed Raid Token", false),
                new GameItem(301, "Token Exchange Equipment", true),
            ]);

        Assert.Empty(result.Supplement.DutySources);
        Assert.Equal(300u, Assert.Single(result.Review.Rejected).ItemId);
        Assert.DoesNotContain(result.Supplement.DutySources, value => value.ItemId == 301);
    }

    [Fact]
    public void SharedAppearanceDoesNotInheritAcquisition()
    {
        var result = Generate(
            TrackyJson(new Observation(100, 10, 20, 30, 40, 7)),
            items:
            [
                new GameItem(100, "Observed Equipment", true),
                new GameItem(101, "Same Model Different Item", true),
            ]);

        Assert.Single(result.Supplement.DutySources, value => value.ItemId == 100);
        Assert.DoesNotContain(result.Supplement.DutySources, value => value.ItemId == 101);
    }

    [Fact]
    public void UnknownBossRemainsEmpty()
    {
        var result = Generate(TrackyJson(new Observation(100, 10, 20, 30, 40, 7)));

        var source = Assert.Single(result.Supplement.DutySources);
        Assert.Equal(string.Empty, source.BossName);
        Assert.Equal(string.Empty, source.Evidence.BossAssociationProvenance);
    }

    [Fact]
    public void VerifiedChestBossMappingEnrichesBossName()
    {
        var metadata = Metadata(new CuratedChestBossAssociation
        {
            TreasureId = 30,
            BossName = "Verified Boss",
            Fight = "Final encounter",
            Evidence = "Exact chest association",
            Provenance = "Reviewed fixture revision 1",
        });

        var result = Generate(
            TrackyJson(new Observation(100, 10, 20, 30, 40, 7)),
            metadata: metadata);

        var source = Assert.Single(result.Supplement.DutySources);
        Assert.Equal("Verified Boss", source.BossName);
        Assert.Equal("Reviewed fixture revision 1", source.Evidence.BossAssociationProvenance);
        Assert.Contains("Final encounter", source.Location, StringComparison.Ordinal);
    }

    [Fact]
    public void SingleObservationIsQuarantined()
    {
        var result = Generate(TrackyJson(new Observation(100, 10, 20, 30, 40, 1)));

        Assert.Empty(result.Supplement.DutySources);
        var quarantined = Assert.Single(result.Review.Quarantined);
        Assert.Equal(ItemAcquisitionEvidenceClassification.NeedsReview, quarantined.Classification);
        Assert.Equal("InsufficientObservations", quarantined.Reason);
    }

    [Fact]
    public void ConflictingStableRelationshipsAreQuarantined()
    {
        var json = TrackyJson(
            new Observation(100, 10, 20, 30, 40, 7),
            new Observation(100, 11, 21, 30, 41, 9));
        var result = Generate(
            json,
            duties:
            [
                new GameDuty(10, "Known Duty", "Raid", 20),
                new GameDuty(11, "Other Duty", "Raid", 21),
            ],
            territories: [20, 21],
            maps:
            [
                new KeyValuePair<uint, uint>(40, 20),
                new KeyValuePair<uint, uint>(41, 21),
            ]);

        Assert.Empty(result.Supplement.DutySources);
        Assert.Equal(2, result.Review.Quarantined.Count);
        Assert.All(
            result.Review.Quarantined,
            value => Assert.Equal("ConflictingStableRelationship", value.Reason));
    }

    [Fact]
    public void IdenticalInputsProduceDeterministicSupplement()
    {
        var json = TrackyJson(
            new Observation(101, 10, 20, 31, 40, 8),
            new Observation(100, 10, 20, 30, 40, 7));
        var items = new[]
        {
            new GameItem(100, "First", true),
            new GameItem(101, "Second", true),
        };

        var first = Generate(json, items: items);
        var second = Generate(json, items: items);

        Assert.Equal(
            AcquisitionSupplementGenerator.Serialize(first.Supplement),
            AcquisitionSupplementGenerator.Serialize(second.Supplement));
        Assert.Equal(new uint[] { 100, 101 }, first.Supplement.DutySources.Select(value => value.ItemId));
    }

    [Fact]
    public void StructuredProvenanceSurvivesGenerationAndRuntimeLoading()
    {
        var result = Generate(TrackyJson(new Observation(100, 10, 20, 30, 40, 7)));
        var json = AcquisitionSupplementGenerator.Serialize(result.Supplement);

        var loaded = ItemAcquisitionSupplementParser.Parse(json);

        Assert.Empty(loaded.Warnings);
        var evidence = Assert.Single(loaded.SourcesByItem[100]).Metadata?.Evidence;
        Assert.NotNull(evidence);
        Assert.Equal("fixture-revision", evidence.UpstreamRevision);
        Assert.Equal(7, evidence.ObservationCount);
        Assert.Equal(20u, evidence.TerritoryTypeId);
        Assert.Equal(30u, evidence.TreasureId);
        Assert.Equal(40u, evidence.MapId);
    }

    private static GenerationResult Generate(
        string trackyJson,
        IEnumerable<GameItem>? items = null,
        IEnumerable<GameDuty>? duties = null,
        IEnumerable<uint>? territories = null,
        IEnumerable<uint>? treasures = null,
        IEnumerable<KeyValuePair<uint, uint>>? maps = null,
        DutyMetadataDocument? metadata = null)
    {
        var inputHash = AcquisitionSupplementGenerator.ComputeSha256(trackyJson);
        var source = new TrackySourceDescriptor
        {
            UpstreamSource = "Tracky fixture",
            UpstreamRevision = "fixture-revision",
            UpstreamDatasetSha256 = inputHash,
            InputSha256 = inputHash,
            License = "MIT fixture",
            LicenseUrl = "https://example.invalid/license",
        };
        var gameData = new InMemoryGameDataIndex(
            "fixture-game-build",
            "fixture-lumina",
            items ?? [new GameItem(100, "Observed Equipment", true)],
            duties ?? [new GameDuty(10, "Known Duty", "Raid", 20)],
            territories ?? [20],
            treasures ?? [30, 31],
            maps ?? [new KeyValuePair<uint, uint>(40, 20)]);
        return AcquisitionSupplementGenerator.Generate(
            new AcquisitionGeneratorInput(
                trackyJson,
                inputHash,
                source,
                metadata ?? Metadata(),
                gameData,
                new GeneratorPolicy()),
            DateTimeOffset.UnixEpoch);
    }

    private static DutyMetadataDocument Metadata(CuratedChestBossAssociation? boss = null)
        => new()
        {
            SchemaVersion = 1,
            Duties =
            [
                new CuratedDutyMetadata
                {
                    ContentFinderConditionId = 10,
                    CanonicalDutyName = "Known Duty",
                    ContentType = "Raid",
                    Difficulty = "Normal",
                    Expansion = "Fixture Expansion",
                    Series = "Fixture Series",
                    Tier = "Fixture Tier",
                    ChestBossAssociations = boss is null ? [] : [boss],
                },
            ],
        };

    private static string TrackyJson(params Observation[] observations)
    {
        var duties = observations.Select(value => new
        {
            Id = value.DutyId,
            Name = $"Duty {value.DutyId}",
            Chests = new Dictionary<string, object[]>
            {
                ["fixture"] =
                [
                    new
                    {
                        Id = value.TreasureId,
                        TerritoryId = value.TerritoryId,
                        MapId = value.MapId,
                        Rewards = new[]
                        {
                            new { Id = value.ItemId, Amount = value.Observations },
                        },
                    },
                ],
            },
        });
        var document = new[]
        {
            new
            {
                Expansions = new[]
                {
                    new
                    {
                        Headers = new[]
                        {
                            new { Duties = duties.ToArray() },
                        },
                    },
                },
            },
        };
        return JsonSerializer.Serialize(document);
    }

    private sealed record Observation(
        uint ItemId,
        uint DutyId,
        uint TerritoryId,
        uint TreasureId,
        uint MapId,
        int Observations);
}
