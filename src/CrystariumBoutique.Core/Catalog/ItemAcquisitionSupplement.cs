using System.Buffers;
using System.Collections.Immutable;
using System.Text.Json;

namespace CrystariumBoutique.Core.Catalog;

public sealed record ItemAcquisitionSupplementLoad(
    string GameBuild,
    ImmutableDictionary<uint, ImmutableArray<ItemAcquisitionSource>> SourcesByItem,
    int SourceCount,
    ImmutableArray<string> Warnings)
{
    public static ItemAcquisitionSupplementLoad Empty { get; } = new(
        string.Empty,
        ImmutableDictionary<uint, ImmutableArray<ItemAcquisitionSource>>.Empty,
        0,
        ImmutableArray<string>.Empty);
}

public static class ItemAcquisitionSupplementParser
{
    public const int CurrentSchemaVersion = 3;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        AllowTrailingCommas = true,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
    };

    private static readonly SearchValues<char> HexDigits = SearchValues.Create("0123456789abcdefABCDEF");

    public static ItemAcquisitionSupplementLoad Parse(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        SupplementDocument? document;
        try
        {
            document = JsonSerializer.Deserialize<SupplementDocument>(json, JsonOptions);
        }
        catch (JsonException exception)
        {
            return Failed($"The local acquisition supplement is not valid JSON: {exception.Message}");
        }

        if (document is null)
        {
            return Failed("The local acquisition supplement is empty.");
        }

        if (document.SchemaVersion != CurrentSchemaVersion)
        {
            return Failed(
                $"Unsupported acquisition supplement schema {document.SchemaVersion}; expected {CurrentSchemaVersion}.");
        }

        var warnings = ImmutableArray.CreateBuilder<string>();
        var indexed = new Dictionary<uint, List<ItemAcquisitionSource>>();
        foreach (var entry in document.DutySources ?? [])
        {
            if (entry.ItemId == 0)
            {
                warnings.Add("Ignored a duty source with item ID 0.");
                continue;
            }

            var dutyName = Clean(entry.DutyName);
            if (dutyName.Length == 0)
            {
                warnings.Add($"Ignored duty source for item {entry.ItemId}: dutyName is required.");
                continue;
            }

            var provenance = Clean(entry.Provenance);
            if (provenance.Length == 0)
            {
                warnings.Add($"Ignored duty source for item {entry.ItemId}: provenance is required.");
                continue;
            }

            if (!Enum.TryParse<DutyAcquisitionDropType>(
                    Clean(entry.DropType),
                    ignoreCase: false,
                    out var dropType))
            {
                warnings.Add($"Ignored duty source for item {entry.ItemId}: dropType is required and must be recognized.");
                continue;
            }

            if (!TryParseEvidence(entry, dropType, warnings, out var evidence))
            {
                continue;
            }

            var source = new ItemAcquisitionSource(
                ItemAcquisitionKind.DutyDrop,
                Name: dutyName,
                Location: Clean(entry.Location),
                Detail: Clean(entry.Detail),
                Metadata: new ItemAcquisitionMetadata(
                    entry.DutyId,
                    Clean(entry.SourceTable),
                    Clean(entry.ContentType),
                    Clean(entry.Difficulty),
                    Clean(entry.BossName),
                    Clean(entry.RaidSeries),
                    Clean(entry.Tier),
                    provenance,
                    Clean(entry.Expansion),
                    evidence,
                    dropType));
            if (!indexed.TryGetValue(entry.ItemId, out var sources))
            {
                sources = [];
                indexed.Add(entry.ItemId, sources);
            }

            sources.Add(source);
        }

        var immutable = indexed.ToImmutableDictionary(
            pair => pair.Key,
            pair => pair.Value
                .GroupBy(source => source.Metadata?.SourceId ?? 0)
                .SelectMany(group =>
                {
                    var priority = group.Max(source => (int)(source.Metadata?.DropType
                        ?? DutyAcquisitionDropType.DutyDrop));
                    return group.Where(source => (int)(source.Metadata?.DropType
                        ?? DutyAcquisitionDropType.DutyDrop) == priority);
                })
                .DistinctBy(source => new
                {
                    source.Name,
                    ContentType = source.Metadata?.ContentType,
                    BossName = source.Metadata?.BossName,
                    DropType = source.Metadata?.DropType,
                })
                .OrderBy(source => source.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(source => source.Metadata?.BossName, StringComparer.OrdinalIgnoreCase)
                .ToImmutableArray());
        return new ItemAcquisitionSupplementLoad(
            Clean(document.GameBuild),
            immutable,
            immutable.Sum(pair => pair.Value.Length),
            warnings.ToImmutable());
    }

    private static ItemAcquisitionSupplementLoad Failed(string warning)
        => ItemAcquisitionSupplementLoad.Empty with
        {
            Warnings = [warning],
        };

    private static string Clean(string? value)
        => value?.Trim() ?? string.Empty;

    private static bool TryParseEvidence(
        DutySourceDocument entry,
        DutyAcquisitionDropType dropType,
        ImmutableArray<string>.Builder warnings,
        out ItemAcquisitionEvidence evidence)
    {
        evidence = null!;
        if (entry.Evidence is null)
        {
            warnings.Add($"Ignored duty source for item {entry.ItemId}: structured evidence is required.");
            return false;
        }

        var source = Clean(entry.Evidence.UpstreamSource);
        var revision = Clean(entry.Evidence.UpstreamRevision);
        var sourceHash = Clean(entry.Evidence.SourceHash);
        var generatorVersion = Clean(entry.Evidence.GeneratorVersion);
        if (!Enum.TryParse<ItemAcquisitionEvidenceOrigin>(
                Clean(entry.Evidence.Origin),
                ignoreCase: false,
                out var origin))
        {
            warnings.Add($"Ignored duty source for item {entry.ItemId}: evidence origin is required and must be recognized.");
            return false;
        }

        if (!Enum.TryParse<ItemAcquisitionEvidenceClassification>(
                Clean(entry.Evidence.Classification),
                ignoreCase: false,
                out var classification)
            || classification is not (ItemAcquisitionEvidenceClassification.VerifiedObserved
                or ItemAcquisitionEvidenceClassification.Corroborated))
        {
            warnings.Add(
                $"Ignored duty source for item {entry.ItemId}: evidence classification must be accepted and verified.");
            return false;
        }

        if (source.Length == 0 || revision.Length == 0 || generatorVersion.Length == 0)
        {
            warnings.Add(
                $"Ignored duty source for item {entry.ItemId}: upstream source, revision, and generator version are required.");
            return false;
        }

        if (!IsSha256(sourceHash))
        {
            warnings.Add($"Ignored duty source for item {entry.ItemId}: a valid SHA-256 source hash is required.");
            return false;
        }

        if (entry.Evidence.ItemId != entry.ItemId
            || entry.Evidence.ContentFinderConditionId != entry.DutyId)
        {
            warnings.Add($"Ignored duty source for item {entry.ItemId}: evidence IDs do not match the source record.");
            return false;
        }

        if (entry.DutyId == 0)
        {
            warnings.Add($"Ignored duty source for item {entry.ItemId}: a stable duty ID is required.");
            return false;
        }

        var bossProvenance = Clean(entry.Evidence.BossAssociationProvenance);
        if (Clean(entry.BossName).Length > 0 && bossProvenance.Length == 0)
        {
            warnings.Add($"Ignored duty source for item {entry.ItemId}: boss attribution requires separate provenance.");
            return false;
        }

        var bossNpcNameIds = (entry.Evidence.BossNpcNameIds ?? [])
            .Where(value => value > 0)
            .Distinct()
            .Order()
            .ToImmutableArray();
        switch (origin)
        {
            case ItemAcquisitionEvidenceOrigin.ObservedChest:
                if (classification != ItemAcquisitionEvidenceClassification.VerifiedObserved
                    || entry.Evidence.ObservationCount < 2
                    || entry.Evidence.TerritoryTypeId == 0
                    || entry.Evidence.TreasureId == 0
                    || entry.Evidence.MapId == 0)
                {
                    warnings.Add($"Ignored duty source for item {entry.ItemId}: observed chest evidence requires two observations and stable territory, treasure, and map IDs.");
                    return false;
                }

                break;
            case ItemAcquisitionEvidenceOrigin.LuminaSupplemental:
                if (classification != ItemAcquisitionEvidenceClassification.Corroborated
                    || entry.Evidence.SupplementalRowId == 0
                    || !source.Equals("Critical-Impact/LuminaSupplemental", StringComparison.Ordinal))
                {
                    warnings.Add($"Ignored duty source for item {entry.ItemId}: LuminaSupplemental evidence requires corroborated classification, an exact source row, and the pinned upstream identity.");
                    return false;
                }

                if (dropType is DutyAcquisitionDropType.BossDrop or DutyAcquisitionDropType.BossChest)
                {
                    if (Clean(entry.BossName).Length == 0
                        || bossProvenance.Length == 0
                        || bossNpcNameIds.IsEmpty)
                    {
                        warnings.Add($"Ignored duty source for item {entry.ItemId}: boss sources require a boss name, stable BNpcName IDs, and boss provenance.");
                        return false;
                    }
                }
                else if (Clean(entry.BossName).Length > 0 || !bossNpcNameIds.IsEmpty)
                {
                    warnings.Add($"Ignored duty source for item {entry.ItemId}: generic duty/chest sources cannot claim a boss.");
                    return false;
                }

                if (dropType == DutyAcquisitionDropType.DutyChest
                    && (entry.Evidence.TerritoryTypeId == 0
                        || entry.Evidence.TreasureId == 0
                        || entry.Evidence.MapId == 0))
                {
                    warnings.Add($"Ignored duty source for item {entry.ItemId}: duty chest evidence requires stable territory, treasure, and map IDs.");
                    return false;
                }

                break;
            default:
                warnings.Add($"Ignored duty source for item {entry.ItemId}: unsupported evidence origin.");
                return false;
        }

        evidence = new ItemAcquisitionEvidence(
            classification,
            source,
            revision,
            sourceHash,
            entry.Evidence.ObservationCount,
            entry.Evidence.ItemId,
            entry.Evidence.ContentFinderConditionId,
            entry.Evidence.TerritoryTypeId,
            entry.Evidence.TreasureId,
            entry.Evidence.MapId,
            generatorVersion,
            bossProvenance,
            origin,
            entry.Evidence.SupplementalRowId,
            entry.Evidence.FightNumber,
            bossNpcNameIds);
        return true;
    }

    private static bool IsSha256(string value)
    {
        const string prefix = "SHA256:";
        if (!value.StartsWith(prefix, StringComparison.Ordinal) || value.Length != prefix.Length + 64)
        {
            return false;
        }

        return value.AsSpan(prefix.Length).IndexOfAnyExcept(HexDigits) < 0;
    }

    private sealed class SupplementDocument
    {
        public int SchemaVersion { get; init; }

        public string? GameBuild { get; init; }

        public List<DutySourceDocument>? DutySources { get; init; }
    }

    private sealed class DutySourceDocument
    {
        public uint ItemId { get; init; }

        public uint DutyId { get; init; }

        public string? DutyName { get; init; }

        public string? SourceTable { get; init; }

        public string? ContentType { get; init; }

        public string? Difficulty { get; init; }

        public string? BossName { get; init; }

        public string? RaidSeries { get; init; }

        public string? Tier { get; init; }

        public string? Expansion { get; init; }

        public string? Location { get; init; }

        public string? Detail { get; init; }

        public string? DropType { get; init; }

        public string? Provenance { get; init; }

        public EvidenceDocument? Evidence { get; init; }
    }

    private sealed class EvidenceDocument
    {
        public string? Classification { get; init; }

        public string? UpstreamSource { get; init; }

        public string? UpstreamRevision { get; init; }

        public string? SourceHash { get; init; }

        public int ObservationCount { get; init; }

        public uint ItemId { get; init; }

        public uint ContentFinderConditionId { get; init; }

        public uint TerritoryTypeId { get; init; }

        public uint TreasureId { get; init; }

        public uint MapId { get; init; }

        public string? GeneratorVersion { get; init; }

        public string? BossAssociationProvenance { get; init; }

        public string? Origin { get; init; }

        public uint SupplementalRowId { get; init; }

        public uint FightNumber { get; init; }

        public List<uint>? BossNpcNameIds { get; init; }
    }
}
