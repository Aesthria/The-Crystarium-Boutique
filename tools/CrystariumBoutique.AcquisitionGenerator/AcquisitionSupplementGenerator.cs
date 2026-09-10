using System.Buffers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CrystariumBoutique.Core.Catalog;

namespace CrystariumBoutique.AcquisitionGenerator;

public static class AcquisitionSupplementGenerator
{
    public const string GeneratorVersion = "2.0.0";

    private static readonly SearchValues<char> HexDigits = SearchValues.Create("0123456789abcdefABCDEF");

    private static readonly JsonSerializerOptions OutputJsonOptions = CreateOutputJsonOptions();

    public static GenerationResult Generate(
        AcquisitionGeneratorInput input,
        DateTimeOffset? generatedAtUtc = null)
    {
        ArgumentNullException.ThrowIfNull(input);
        ValidateSource(input.Source, input.TrackyInputHash);
        var metadata = ValidateMetadata(input.DutyMetadata);
        var tracky = TrackyDatasetReader.Read(input.TrackyJson);
        var aggregated = Aggregate(tracky.Observations);
        var conflictKeys = FindConflicts(aggregated);
        var accepted = new List<GeneratedDutySource>();
        var quarantined = new List<ReviewedObservation>();
        var rejected = new List<ReviewedObservation>();

        foreach (var observation in aggregated
                     .OrderBy(value => value.ItemId)
                     .ThenBy(value => value.ContentFinderConditionId)
                     .ThenBy(value => value.TreasureId)
                     .ThenBy(value => value.MapId))
        {
            if (TryReject(observation, input.GameData, input.Policy, out var rejectionReason))
            {
                rejected.Add(ToReview(
                    observation,
                    ItemAcquisitionEvidenceClassification.Rejected,
                    rejectionReason));
                continue;
            }

            if (conflictKeys.Contains(new ConflictKey(observation.ItemId, observation.TreasureId)))
            {
                quarantined.Add(ToReview(
                    observation,
                    ItemAcquisitionEvidenceClassification.NeedsReview,
                    "ConflictingStableRelationship"));
                continue;
            }

            if (TryQuarantine(observation, input.GameData, input.Policy, metadata, out var quarantineReason))
            {
                quarantined.Add(ToReview(
                    observation,
                    ItemAcquisitionEvidenceClassification.NeedsReview,
                    quarantineReason));
                continue;
            }

            _ = input.GameData.TryGetDuty(observation.ContentFinderConditionId, out var duty);
            metadata.TryGetValue(observation.ContentFinderConditionId, out var dutyMetadata);
            var boss = dutyMetadata?.ChestBossAssociations
                .SingleOrDefault(value => value.TreasureId == observation.TreasureId);
            accepted.Add(CreateAccepted(input, observation, duty, dutyMetadata, boss));
        }

        var review = new GenerationReviewDocument
        {
            GeneratorVersion = GeneratorVersion,
            Quarantined = SortReview(quarantined),
            Rejected = SortReview(rejected),
        };
        var supplement = new SupplementDocument
        {
            GameBuild = input.GameData.GameBuild,
            GeneratorVersion = GeneratorVersion,
            DutySources = accepted,
        };
        var manifest = new GenerationManifest
        {
            GameBuild = input.GameData.GameBuild,
            LuminaVersion = input.GameData.LuminaVersion,
            GeneratorVersion = GeneratorVersion,
            GeneratedAtUtc = generatedAtUtc ?? DateTimeOffset.UtcNow,
            UpstreamSource = input.Source.UpstreamSource.Trim(),
            UpstreamRevision = input.Source.UpstreamRevision.Trim(),
            InputSha256 = NormalizeHash(input.TrackyInputHash),
            UpstreamDatasetSha256 = NormalizeHash(input.Source.UpstreamDatasetSha256),
            Licenses = [new GenerationLicense(input.Source.License.Trim(), input.Source.LicenseUrl.Trim())],
            TotalInputRecords = tracky.InputRecordCount,
            AcceptedRecords = accepted.Count,
            QuarantinedRecords = quarantined.Count,
            RejectedRecords = rejected.Count,
            RejectionReasons = CountReasons(rejected),
            QuarantineReasons = CountReasons(quarantined),
        };
        return new GenerationResult(supplement, review, manifest);
    }

    public static string Serialize(object value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return JsonSerializer.Serialize(value, OutputJsonOptions)
            .Replace("\r\n", "\n", StringComparison.Ordinal) + "\n";
    }

    public static string ComputeSha256(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return "SHA256:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
    }

    public static string ComputeSha256File(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        using var stream = File.OpenRead(path);
        return "SHA256:" + Convert.ToHexString(SHA256.HashData(stream));
    }

    private static List<AggregatedObservation> Aggregate(IReadOnlyList<ObservedChestReward> observations)
        => observations
            .GroupBy(value => new ObservationKey(
                value.ItemId,
                value.ContentFinderConditionId,
                value.TerritoryTypeId,
                value.TreasureId,
                value.MapId))
            .Select(group => new AggregatedObservation(
                group.Key.ItemId,
                group.Key.ContentFinderConditionId,
                group.Select(value => value.UpstreamDutyName)
                    .Order(StringComparer.Ordinal)
                    .FirstOrDefault() ?? "",
                group.Key.TerritoryTypeId,
                group.Key.TreasureId,
                group.Key.MapId,
                checked(group.Sum(value => value.ObservationCount)),
                string.Join(",", group.Select(value => value.Patch)
                    .Distinct(StringComparer.Ordinal)
                    .Order(StringComparer.Ordinal))))
            .ToList();

    private static HashSet<ConflictKey> FindConflicts(IEnumerable<AggregatedObservation> observations)
        => observations
            .GroupBy(value => new ConflictKey(value.ItemId, value.TreasureId))
            .Where(group => group
                .Select(value => new StableRelationship(
                    value.ContentFinderConditionId,
                    value.TerritoryTypeId,
                    value.MapId))
                .Distinct()
                .Skip(1)
                .Any())
            .Select(group => group.Key)
            .ToHashSet();

    private static bool TryReject(
        AggregatedObservation observation,
        IGameDataIndex gameData,
        GeneratorPolicy policy,
        out string reason)
    {
        if (observation.ItemId == 0 || !gameData.TryGetItem(observation.ItemId, out var item))
        {
            reason = "InvalidItemId";
            return true;
        }

        if (policy.EquipmentOnly && !item.IsEquipment)
        {
            reason = "NonEquipmentExactItem";
            return true;
        }

        if (observation.ContentFinderConditionId == 0
            || observation.ContentFinderConditionId >= 100_000
            || !gameData.TryGetDuty(observation.ContentFinderConditionId, out _))
        {
            reason = "InvalidContentFinderConditionId";
            return true;
        }

        if (observation.TerritoryTypeId == 0 || !gameData.ContainsTerritory(observation.TerritoryTypeId))
        {
            reason = "InvalidTerritoryTypeId";
            return true;
        }

        if (observation.TreasureId == 0 || !gameData.ContainsTreasure(observation.TreasureId))
        {
            reason = "InvalidTreasureId";
            return true;
        }

        if (observation.MapId == 0 || !gameData.TryGetMapTerritory(observation.MapId, out _))
        {
            reason = "InvalidMapId";
            return true;
        }

        reason = "";
        return false;
    }

    private static bool TryQuarantine(
        AggregatedObservation observation,
        IGameDataIndex gameData,
        GeneratorPolicy policy,
        Dictionary<uint, CuratedDutyMetadata> metadata,
        out string reason)
    {
        _ = gameData.TryGetDuty(observation.ContentFinderConditionId, out var duty);
        if (duty.TerritoryTypeId != observation.TerritoryTypeId)
        {
            reason = "ConflictingLuminaRelationship";
            return true;
        }

        if (observation.ObservationCount < policy.MinimumObservationCount)
        {
            reason = "InsufficientObservations";
            return true;
        }

        if (metadata.TryGetValue(observation.ContentFinderConditionId, out var dutyMetadata)
            && (!string.Equals(
                    dutyMetadata.CanonicalDutyName.Trim(),
                    duty.Name,
                    StringComparison.OrdinalIgnoreCase)
                || !string.Equals(
                    dutyMetadata.ContentType.Trim(),
                    duty.ContentType,
                    StringComparison.OrdinalIgnoreCase)))
        {
            reason = "CuratedDutyMetadataMismatch";
            return true;
        }

        reason = "";
        return false;
    }

    private static GeneratedDutySource CreateAccepted(
        AcquisitionGeneratorInput input,
        AggregatedObservation observation,
        GameDuty duty,
        CuratedDutyMetadata? metadata,
        CuratedChestBossAssociation? boss)
    {
        var sourceHash = NormalizeHash(input.TrackyInputHash);
        var provenance = $"{input.Source.UpstreamSource.Trim()} @ {input.Source.UpstreamRevision.Trim()}; {sourceHash}";
        var bossName = boss?.BossName.Trim() ?? "";
        var bossProvenance = boss?.Provenance.Trim() ?? "";
        return new GeneratedDutySource
        {
            ItemId = observation.ItemId,
            DutyId = observation.ContentFinderConditionId,
            DutyName = duty.Name,
            SourceTable = "XIVStats/Tracky ChestDropsV2",
            ContentType = duty.ContentType,
            Difficulty = metadata?.Difficulty.Trim() ?? "",
            BossName = bossName,
            RaidSeries = metadata?.Series.Trim() ?? "",
            Tier = metadata?.Tier.Trim() ?? "",
            Expansion = metadata?.Expansion.Trim() ?? "",
            Location = boss is null || string.IsNullOrWhiteSpace(boss.Fight)
                ? $"Treasure #{observation.TreasureId}"
                : $"{boss.Fight.Trim()} - Treasure #{observation.TreasureId}",
            Detail = boss is null
                ? "Observed duty chest reward"
                : "Observed reward from a separately verified encounter chest",
            DropType = boss is null
                ? DutyAcquisitionDropType.DutyChest
                : DutyAcquisitionDropType.BossChest,
            Provenance = provenance,
            Evidence = new GeneratedEvidence
            {
                Classification = ItemAcquisitionEvidenceClassification.VerifiedObserved,
                UpstreamSource = input.Source.UpstreamSource.Trim(),
                UpstreamRevision = input.Source.UpstreamRevision.Trim(),
                SourceHash = sourceHash,
                ObservationCount = observation.ObservationCount,
                ItemId = observation.ItemId,
                ContentFinderConditionId = observation.ContentFinderConditionId,
                TerritoryTypeId = observation.TerritoryTypeId,
                TreasureId = observation.TreasureId,
                MapId = observation.MapId,
                GeneratorVersion = GeneratorVersion,
                BossAssociationProvenance = bossProvenance,
                Origin = ItemAcquisitionEvidenceOrigin.ObservedChest,
            },
        };
    }

    private static Dictionary<uint, CuratedDutyMetadata> ValidateMetadata(DutyMetadataDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        if (document.SchemaVersion != 1)
        {
            throw new InvalidDataException($"Unsupported curated duty metadata schema {document.SchemaVersion}.");
        }

        var result = new Dictionary<uint, CuratedDutyMetadata>();
        foreach (var duty in document.Duties)
        {
            if (duty.ContentFinderConditionId == 0
                || string.IsNullOrWhiteSpace(duty.CanonicalDutyName)
                || string.IsNullOrWhiteSpace(duty.ContentType))
            {
                throw new InvalidDataException("Curated duties require an ID, canonical name, and content type.");
            }

            if (!result.TryAdd(duty.ContentFinderConditionId, duty))
            {
                throw new InvalidDataException(
                    $"Duplicate curated duty metadata for {duty.ContentFinderConditionId}.");
            }

            var chests = new HashSet<uint>();
            foreach (var association in duty.ChestBossAssociations)
            {
                if (association.TreasureId == 0
                    || string.IsNullOrWhiteSpace(association.BossName)
                    || string.IsNullOrWhiteSpace(association.Evidence)
                    || string.IsNullOrWhiteSpace(association.Provenance))
                {
                    throw new InvalidDataException(
                        $"Boss associations for duty {duty.ContentFinderConditionId} require chest, boss, evidence, and provenance.");
                }

                if (!chests.Add(association.TreasureId))
                {
                    throw new InvalidDataException(
                        $"Duplicate boss association for treasure {association.TreasureId} in duty {duty.ContentFinderConditionId}.");
                }
            }
        }

        return result;
    }

    private static void ValidateSource(TrackySourceDescriptor source, string inputHash)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (string.IsNullOrWhiteSpace(source.UpstreamSource)
            || string.IsNullOrWhiteSpace(source.UpstreamRevision)
            || string.IsNullOrWhiteSpace(source.UpstreamDatasetSha256)
            || string.IsNullOrWhiteSpace(source.InputSha256)
            || string.IsNullOrWhiteSpace(source.License)
            || string.IsNullOrWhiteSpace(source.LicenseUrl))
        {
            throw new InvalidDataException("The pinned Tracky source descriptor is incomplete.");
        }

        if (!string.Equals(
                NormalizeHash(source.InputSha256),
                NormalizeHash(inputHash),
                StringComparison.Ordinal))
        {
            throw new InvalidDataException("The Tracky input hash does not match its pinned descriptor.");
        }

        _ = NormalizeHash(source.UpstreamDatasetSha256);
    }

    private static string NormalizeHash(string value)
    {
        var cleaned = value.Trim();
        if (cleaned.StartsWith("SHA256:", StringComparison.OrdinalIgnoreCase))
        {
            cleaned = cleaned[7..];
        }

        if (cleaned.Length != 64 || cleaned.AsSpan().IndexOfAnyExcept(HexDigits) >= 0)
        {
            throw new InvalidDataException("Expected a 64-character SHA-256 value.");
        }

        return "SHA256:" + cleaned.ToUpperInvariant();
    }

    private static ReviewedObservation ToReview(
        AggregatedObservation observation,
        ItemAcquisitionEvidenceClassification classification,
        string reason)
        => new()
        {
            Classification = classification,
            Reason = reason,
            ItemId = observation.ItemId,
            ContentFinderConditionId = observation.ContentFinderConditionId,
            TerritoryTypeId = observation.TerritoryTypeId,
            TreasureId = observation.TreasureId,
            MapId = observation.MapId,
            ObservationCount = observation.ObservationCount,
        };

    private static List<ReviewedObservation> SortReview(IEnumerable<ReviewedObservation> values)
        => values
            .OrderBy(value => value.ItemId)
            .ThenBy(value => value.ContentFinderConditionId)
            .ThenBy(value => value.TreasureId)
            .ThenBy(value => value.Reason, StringComparer.Ordinal)
            .ToList();

    private static SortedDictionary<string, int> CountReasons(IEnumerable<ReviewedObservation> values)
        => new(values
            .GroupBy(value => value.Reason, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal),
            StringComparer.Ordinal);

    private static JsonSerializerOptions CreateOutputJsonOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
        };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    private sealed record ObservationKey(
        uint ItemId,
        uint ContentFinderConditionId,
        uint TerritoryTypeId,
        uint TreasureId,
        uint MapId);

    private sealed record ConflictKey(uint ItemId, uint TreasureId);

    private sealed record StableRelationship(
        uint ContentFinderConditionId,
        uint TerritoryTypeId,
        uint MapId);

    private sealed record AggregatedObservation(
        uint ItemId,
        uint ContentFinderConditionId,
        string UpstreamDutyName,
        uint TerritoryTypeId,
        uint TreasureId,
        uint MapId,
        int ObservationCount,
        string Patches);
}
