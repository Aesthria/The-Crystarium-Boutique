using CrystariumBoutique.Core.Catalog;

namespace CrystariumBoutique.Core.Tests.Catalog;

public sealed class ItemAcquisitionSupplementParserTests
{
    [Fact]
    public void VerifiedDutySourceIsIndexedByExactItemId()
    {
        const string json = """
            {
              "schemaVersion": 3,
              "gameBuild": "2026.08.11.0000.0000",
              "dutySources": [
                {
                  "itemId": 12345,
                  "dutyId": 678,
                  "dutyName": "Example Duty",
                  "sourceTable": "ContentFinderCondition",
                  "contentType": "Trial",
                  "difficulty": "Extreme",
                  "bossName": "Example Boss",
                  "raidSeries": "Example Series",
                  "tier": "Example Tier",
                  "location": "Final encounter",
                  "detail": "Direct boss drop",
                  "dropType": "BossDrop",
                  "provenance": "Verified local fixture",
                  "evidence": {
                    "classification": "VerifiedObserved",
                    "upstreamSource": "Tracky fixture",
                    "upstreamRevision": "fixture-revision",
                    "sourceHash": "SHA256:AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA",
                    "observationCount": 3,
                    "itemId": 12345,
                    "contentFinderConditionId": 678,
                    "territoryTypeId": 900,
                    "treasureId": 901,
                    "mapId": 902,
                    "generatorVersion": "test",
                    "bossAssociationProvenance": "Verified fixture boss mapping",
                    "origin": "ObservedChest"
                  }
                }
              ]
            }
            """;

        var result = ItemAcquisitionSupplementParser.Parse(json);

        Assert.Equal("2026.08.11.0000.0000", result.GameBuild);
        Assert.Equal(1, result.SourceCount);
        Assert.Empty(result.Warnings);
        var source = Assert.Single(result.SourcesByItem[12345]);
        Assert.Equal(ItemAcquisitionKind.DutyDrop, source.Kind);
        Assert.Equal("Example Duty", source.Name);
        Assert.Equal("Extreme", source.Metadata?.Difficulty);
        Assert.Equal("Example Boss", source.Metadata?.BossName);
        Assert.Equal("Verified local fixture", source.Metadata?.Provenance);
        Assert.Equal(3, source.Metadata?.Evidence?.ObservationCount);
        Assert.Equal(901u, source.Metadata?.Evidence?.TreasureId);
    }

    [Fact]
    public void EntryWithoutProvenanceIsRejected()
    {
        const string json = """
            {
              "schemaVersion": 3,
              "gameBuild": "test",
              "dutySources": [
                {
                  "itemId": 12345,
                  "dutyId": 678,
                  "dutyName": "Unverified Duty",
                  "dropType": "DutyChest",
                  "evidence": {
                    "classification": "VerifiedObserved",
                    "upstreamSource": "Tracky fixture",
                    "upstreamRevision": "fixture-revision",
                    "sourceHash": "SHA256:AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA",
                    "observationCount": 3,
                    "itemId": 12345,
                    "contentFinderConditionId": 678,
                    "territoryTypeId": 900,
                    "treasureId": 901,
                    "mapId": 902,
                    "generatorVersion": "test",
                    "origin": "ObservedChest"
                  }
                }
              ]
            }
            """;

        var result = ItemAcquisitionSupplementParser.Parse(json);

        Assert.Equal(0, result.SourceCount);
        Assert.Empty(result.SourcesByItem);
        Assert.Contains(result.Warnings, warning => warning.Contains("provenance", StringComparison.Ordinal));
    }

    [Fact]
    public void EntryWithoutStructuredEvidenceIsRejected()
    {
        const string json = """
            {
              "schemaVersion": 3,
              "gameBuild": "test",
              "dutySources": [
                {
                  "itemId": 12345,
                  "dutyId": 678,
                  "dutyName": "Unverified Duty",
                  "dropType": "DutyDrop",
                  "provenance": "A label without evidence"
                }
              ]
            }
            """;

        var result = ItemAcquisitionSupplementParser.Parse(json);

        Assert.Equal(0, result.SourceCount);
        Assert.Contains(result.Warnings, warning => warning.Contains("structured evidence", StringComparison.Ordinal));
    }

    [Fact]
    public void NeedsReviewEvidenceCannotEnterRuntimeSupplement()
    {
        const string json = """
            {
              "schemaVersion": 3,
              "gameBuild": "test",
              "dutySources": [
                {
                  "itemId": 12345,
                  "dutyId": 678,
                  "dutyName": "Questionable Duty",
                  "dropType": "DutyChest",
                  "provenance": "Fixture",
                  "evidence": {
                    "classification": "NeedsReview",
                    "upstreamSource": "Tracky fixture",
                    "upstreamRevision": "fixture-revision",
                    "sourceHash": "SHA256:AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA",
                    "observationCount": 3,
                    "itemId": 12345,
                    "contentFinderConditionId": 678,
                    "territoryTypeId": 900,
                    "treasureId": 901,
                    "mapId": 902,
                    "generatorVersion": "test",
                    "origin": "ObservedChest"
                  }
                }
              ]
            }
            """;

        var result = ItemAcquisitionSupplementParser.Parse(json);

        Assert.Equal(0, result.SourceCount);
        Assert.Contains(result.Warnings, warning => warning.Contains("classification", StringComparison.Ordinal));
    }

    [Fact]
    public void UnsupportedSchemaFailsWithoutPartialData()
    {
        const string json = """
            {
              "schemaVersion": 99,
              "gameBuild": "test",
              "dutySources": []
            }
            """;

        var result = ItemAcquisitionSupplementParser.Parse(json);

        Assert.Equal(0, result.SourceCount);
        Assert.Empty(result.SourcesByItem);
        Assert.Single(result.Warnings);
    }
}
