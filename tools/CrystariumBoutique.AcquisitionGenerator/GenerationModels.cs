using System.Text.Json.Serialization;
using CrystariumBoutique.Core.Catalog;

namespace CrystariumBoutique.AcquisitionGenerator;

public sealed record GameItem(uint ItemId, string Name, bool IsEquipment);

public sealed record GameDuty(
    uint ContentFinderConditionId,
    string Name,
    string ContentType,
    uint TerritoryTypeId,
    string Difficulty = "",
    string Expansion = "");

public interface IGameDataIndex
{
    string GameBuild { get; }

    string LuminaVersion { get; }

    int EquipmentItemCount { get; }

    bool TryGetItem(uint itemId, out GameItem item);

    bool TryGetDuty(uint contentFinderConditionId, out GameDuty duty);

    bool ContainsTerritory(uint territoryTypeId);

    bool ContainsTreasure(uint treasureId);

    bool TryGetMapTerritory(uint mapId, out uint territoryTypeId);

    bool TryGetBossName(uint bNpcNameId, out string bossName);
}

public sealed class InMemoryGameDataIndex(
    string gameBuild,
    string luminaVersion,
    IEnumerable<GameItem> items,
    IEnumerable<GameDuty> duties,
    IEnumerable<uint> territories,
    IEnumerable<uint> treasures,
    IEnumerable<KeyValuePair<uint, uint>> mapTerritories,
    IEnumerable<KeyValuePair<uint, string>>? bossNames = null) : IGameDataIndex
{
    private readonly Dictionary<uint, GameItem> items = items.ToDictionary(item => item.ItemId);
    private readonly Dictionary<uint, GameDuty> duties = duties.ToDictionary(duty => duty.ContentFinderConditionId);
    private readonly HashSet<uint> territories = [.. territories];
    private readonly HashSet<uint> treasures = [.. treasures];
    private readonly Dictionary<uint, uint> mapTerritories = mapTerritories.ToDictionary();
    private readonly Dictionary<uint, string> bossNames = (bossNames ?? [])
        .ToDictionary();

    public string GameBuild { get; } = gameBuild;

    public string LuminaVersion { get; } = luminaVersion;

    public int EquipmentItemCount => items.Values.Count(item => item.IsEquipment);

    public bool TryGetItem(uint itemId, out GameItem item)
        => items.TryGetValue(itemId, out item!);

    public bool TryGetDuty(uint contentFinderConditionId, out GameDuty duty)
        => duties.TryGetValue(contentFinderConditionId, out duty!);

    public bool ContainsTerritory(uint territoryTypeId)
        => territories.Contains(territoryTypeId);

    public bool ContainsTreasure(uint treasureId)
        => treasures.Contains(treasureId);

    public bool TryGetMapTerritory(uint mapId, out uint territoryTypeId)
        => mapTerritories.TryGetValue(mapId, out territoryTypeId);

    public bool TryGetBossName(uint bNpcNameId, out string bossName)
        => bossNames.TryGetValue(bNpcNameId, out bossName!);
}

public sealed class TrackySourceDescriptor
{
    public string UpstreamSource { get; init; } = "";

    public string UpstreamRevision { get; init; } = "";

    public string UpstreamDatasetSha256 { get; init; } = "";

    public string InputSha256 { get; init; } = "";

    public string License { get; init; } = "";

    public string LicenseUrl { get; init; } = "";
}

public sealed class LuminaSupplementalSourceDescriptor
{
    public string UpstreamSource { get; init; } = "";

    public string UpstreamRevision { get; init; } = "";

    public string UpstreamVersion { get; init; } = "";

    public string License { get; init; } = "";

    public string LicenseUrl { get; init; } = "";

    public SortedDictionary<string, string> Files { get; init; } = new(StringComparer.Ordinal);
}

public sealed class DutyMetadataDocument
{
    public int SchemaVersion { get; init; }

    public List<CuratedDutyMetadata> Duties { get; init; } = [];
}

public sealed class CuratedDutyMetadata
{
    public uint ContentFinderConditionId { get; init; }

    public string CanonicalDutyName { get; init; } = "";

    public string ContentType { get; init; } = "";

    public string Difficulty { get; init; } = "";

    public string Expansion { get; init; } = "";

    public string Series { get; init; } = "";

    public string Tier { get; init; } = "";

    public List<CuratedChestBossAssociation> ChestBossAssociations { get; init; } = [];
}

public sealed class CuratedChestBossAssociation
{
    public uint TreasureId { get; init; }

    public string BossName { get; init; } = "";

    public string Fight { get; init; } = "";

    public string Evidence { get; init; } = "";

    public string Provenance { get; init; } = "";
}

public sealed record GeneratorPolicy(int MinimumObservationCount = 2, bool EquipmentOnly = true);

public sealed record AcquisitionGeneratorInput(
    string TrackyJson,
    string TrackyInputHash,
    TrackySourceDescriptor Source,
    DutyMetadataDocument DutyMetadata,
    IGameDataIndex GameData,
    GeneratorPolicy Policy);

public sealed record LuminaSupplementalGeneratorInput(
    string DatasetDirectory,
    LuminaSupplementalSourceDescriptor Source,
    IGameDataIndex GameData);

public sealed record GenerationResult(
    SupplementDocument Supplement,
    GenerationReviewDocument Review,
    GenerationManifest Manifest);

public sealed class SupplementDocument
{
    public int SchemaVersion { get; init; } = ItemAcquisitionSupplementParser.CurrentSchemaVersion;

    public string GameBuild { get; init; } = "";

    public string GeneratorVersion { get; init; } = "";

    public List<GeneratedDutySource> DutySources { get; init; } = [];
}

public sealed class GeneratedDutySource
{
    public uint ItemId { get; init; }

    public uint DutyId { get; init; }

    public string DutyName { get; init; } = "";

    public string SourceTable { get; init; } = "";

    public string ContentType { get; init; } = "";

    public string Difficulty { get; init; } = "";

    public string BossName { get; init; } = "";

    public string RaidSeries { get; init; } = "";

    public string Tier { get; init; } = "";

    public string Expansion { get; init; } = "";

    public string Location { get; init; } = "";

    public string Detail { get; init; } = "";

    [JsonConverter(typeof(JsonStringEnumConverter<DutyAcquisitionDropType>))]
    public DutyAcquisitionDropType DropType { get; init; }

    public string Provenance { get; init; } = "";

    public GeneratedEvidence Evidence { get; init; } = new();
}

public sealed class GeneratedEvidence
{
    [JsonConverter(typeof(JsonStringEnumConverter<ItemAcquisitionEvidenceClassification>))]
    public ItemAcquisitionEvidenceClassification Classification { get; init; }

    public string UpstreamSource { get; init; } = "";

    public string UpstreamRevision { get; init; } = "";

    public string SourceHash { get; init; } = "";

    public int ObservationCount { get; init; }

    public uint ItemId { get; init; }

    public uint ContentFinderConditionId { get; init; }

    public uint TerritoryTypeId { get; init; }

    public uint TreasureId { get; init; }

    public uint MapId { get; init; }

    public string GeneratorVersion { get; init; } = "";

    public string BossAssociationProvenance { get; init; } = "";

    [JsonConverter(typeof(JsonStringEnumConverter<ItemAcquisitionEvidenceOrigin>))]
    public ItemAcquisitionEvidenceOrigin Origin { get; init; }

    public uint SupplementalRowId { get; init; }

    public uint FightNumber { get; init; }

    public List<uint> BossNpcNameIds { get; init; } = [];
}

public sealed class GenerationReviewDocument
{
    public string GeneratorVersion { get; init; } = "";

    public List<ReviewedObservation> Quarantined { get; init; } = [];

    public List<ReviewedObservation> Rejected { get; init; } = [];
}

public sealed class ReviewedObservation
{
    [JsonConverter(typeof(JsonStringEnumConverter<ItemAcquisitionEvidenceClassification>))]
    public ItemAcquisitionEvidenceClassification Classification { get; init; }

    public string Reason { get; init; } = "";

    public uint ItemId { get; init; }

    public uint ContentFinderConditionId { get; init; }

    public uint TerritoryTypeId { get; init; }

    public uint TreasureId { get; init; }

    public uint MapId { get; init; }

    public int ObservationCount { get; init; }

    public string SourceTable { get; init; } = "";

    public uint SourceRowId { get; init; }
}

public sealed class GenerationManifest
{
    public int SchemaVersion { get; init; } = 1;

    public string GameBuild { get; init; } = "";

    public string LuminaVersion { get; init; } = "";

    public string GeneratorVersion { get; init; } = "";

    public DateTimeOffset GeneratedAtUtc { get; init; }

    public string UpstreamSource { get; init; } = "";

    public string UpstreamRevision { get; init; } = "";

    public string InputSha256 { get; init; } = "";

    public string UpstreamDatasetSha256 { get; init; } = "";

    public List<GenerationLicense> Licenses { get; init; } = [];

    public int TotalInputRecords { get; init; }

    public int AcceptedRecords { get; init; }

    public int QuarantinedRecords { get; init; }

    public int RejectedRecords { get; init; }

    public int SupersededRecords { get; init; }

    public SortedDictionary<string, int> RejectionReasons { get; init; } = new(StringComparer.Ordinal);

    public SortedDictionary<string, int> QuarantineReasons { get; init; } = new(StringComparer.Ordinal);

    public int BoutiqueEquipmentItems { get; init; }

    public int ItemsWithDutyAcquisition { get; init; }

    public int ItemsWithExactBossAcquisition { get; init; }

    public int ItemsWithoutDutyAcquisition { get; init; }

    public int BossDropRecords { get; init; }

    public int BossChestRecords { get; init; }

    public int DutyChestRecords { get; init; }

    public int GenericDutyDropRecords { get; init; }

    public SortedDictionary<string, int> RecordsByContentType { get; init; } = new(StringComparer.Ordinal);

    public SortedDictionary<string, string> SourceFiles { get; init; } = new(StringComparer.Ordinal);
}

public sealed record GenerationLicense(string Name, string Url);
