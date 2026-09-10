using System.Collections.Immutable;

namespace CrystariumBoutique.Core.Catalog;

public enum ItemAcquisitionKind
{
    Vendor,
    MarketBoard,
    Crafting,
    Quest,
    Achievement,
    SeasonalEvent,
    OnlineStore,
    DutyDrop,
}

public enum ItemAcquisitionDetailLevel
{
    SourceOnly,
    Standard,
    Detailed,
}

public enum ItemAcquisitionEvidenceClassification
{
    VerifiedObserved,
    Corroborated,
    NeedsReview,
    Rejected,
}

public enum ItemAcquisitionEvidenceOrigin
{
    ObservedChest,
    LuminaSupplemental,
}

public enum DutyAcquisitionDropType
{
    DutyDrop,
    DutyChest,
    BossChest,
    BossDrop,
}

public sealed record ItemAcquisitionEvidence(
    ItemAcquisitionEvidenceClassification Classification,
    string UpstreamSource,
    string UpstreamRevision,
    string SourceHash,
    int ObservationCount,
    uint ItemId,
    uint ContentFinderConditionId,
    uint TerritoryTypeId,
    uint TreasureId,
    uint MapId,
    string GeneratorVersion,
    string BossAssociationProvenance = "",
    ItemAcquisitionEvidenceOrigin Origin = ItemAcquisitionEvidenceOrigin.ObservedChest,
    uint SupplementalRowId = 0,
    uint FightNumber = 0,
    ImmutableArray<uint> BossNpcNameIds = default);

public sealed record ItemAcquisitionMetadata(
    uint SourceId = 0,
    string SourceTable = "",
    string ContentType = "",
    string Difficulty = "",
    string BossName = "",
    string RaidSeries = "",
    string Tier = "",
    string Provenance = "",
    string Expansion = "",
    ItemAcquisitionEvidence? Evidence = null,
    DutyAcquisitionDropType DropType = DutyAcquisitionDropType.DutyDrop);

public sealed record ItemAcquisitionSource(
    ItemAcquisitionKind Kind,
    string Name = "",
    string Location = "",
    string Cost = "",
    string Detail = "",
    ItemAcquisitionMetadata? Metadata = null);

public static class ItemAcquisitionFormatter
{
    public static ImmutableArray<ItemAcquisitionSource> GetLocalSources(EquipmentItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        var result = ImmutableArray.CreateBuilder<ItemAcquisitionSource>();
        var sources = item.AcquisitionSources.IsDefault
            ? ImmutableArray<ItemAcquisitionSource>.Empty
            : item.AcquisitionSources;
        result.AddRange(sources);

        if (item.IsMogStationExclusive
            && !sources.Any(source => source.Kind == ItemAcquisitionKind.OnlineStore))
        {
            result.Add(new ItemAcquisitionSource(ItemAcquisitionKind.OnlineStore));
        }

        if (item.IsMarketable
            && !sources.Any(source => source.Kind == ItemAcquisitionKind.MarketBoard))
        {
            result.Add(new ItemAcquisitionSource(ItemAcquisitionKind.MarketBoard));
        }

        return result
            .Distinct()
            .OrderBy(source => GetSortOrder(source.Kind))
            .ThenBy(source => source.Name, StringComparer.OrdinalIgnoreCase)
            .ToImmutableArray();
    }

    public static string Format(ItemAcquisitionSource source, ItemAcquisitionDetailLevel detailLevel)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (source.Kind == ItemAcquisitionKind.DutyDrop)
        {
            return FormatDutySource(source, detailLevel);
        }

        var label = GetLabel(source.Kind);
        if (detailLevel == ItemAcquisitionDetailLevel.SourceOnly)
        {
            return label;
        }

        var parts = new List<string>(4) { label };
        AppendName(parts, source);
        if (detailLevel == ItemAcquisitionDetailLevel.Detailed)
        {
            Append(parts, "In", source.Location);
            Append(parts, "Cost", source.Cost);
            AppendMetadata(parts, source.Metadata);
            if (!string.IsNullOrWhiteSpace(source.Detail))
            {
                parts.Add(source.Detail.Trim());
            }
        }

        return string.Join(" | ", parts);
    }

    private static string FormatDutySource(
        ItemAcquisitionSource source,
        ItemAcquisitionDetailLevel detailLevel)
    {
        var metadata = source.Metadata;
        var heading = GetDutyHeading(metadata);
        if (detailLevel == ItemAcquisitionDetailLevel.SourceOnly)
        {
            return heading;
        }

        var dropLabel = GetDropTypeLabel(metadata?.DropType ?? DutyAcquisitionDropType.DutyDrop);
        var sourceLabel = string.IsNullOrWhiteSpace(metadata?.BossName)
            ? dropLabel
            : $"{metadata.BossName.Trim()} — {dropLabel}";
        return string.IsNullOrWhiteSpace(source.Name)
            ? $"{heading} | {sourceLabel}"
            : $"{heading} | {source.Name.Trim()} | {sourceLabel}";
    }

    private static string GetDutyHeading(ItemAcquisitionMetadata? metadata)
    {
        var type = metadata?.ContentType.Trim() ?? string.Empty;
        var difficulty = metadata?.Difficulty.Trim() ?? string.Empty;
        var heading = type switch
        {
            var value when value.Contains("Alliance", StringComparison.OrdinalIgnoreCase) => "ALLIANCE RAID",
            var value when value.Contains("Dungeon", StringComparison.OrdinalIgnoreCase) => "DUNGEON",
            var value when value.Contains("Trial", StringComparison.OrdinalIgnoreCase) => "TRIAL",
            var value when value.Contains("Raid", StringComparison.OrdinalIgnoreCase) => "RAID",
            _ => "DUTY",
        };
        if (difficulty.Length == 0
            || difficulty.Equals("Normal", StringComparison.OrdinalIgnoreCase)
            || difficulty.Equals("Normal Raid", StringComparison.OrdinalIgnoreCase)
            || difficulty.Equals("Alliance Raid", StringComparison.OrdinalIgnoreCase))
        {
            return heading;
        }

        return $"{heading} — {difficulty.ToUpperInvariant()}";
    }

    private static string GetDropTypeLabel(DutyAcquisitionDropType dropType)
        => dropType switch
        {
            DutyAcquisitionDropType.BossDrop => "Boss Drop",
            DutyAcquisitionDropType.BossChest => "Boss Chest",
            DutyAcquisitionDropType.DutyChest => "Duty Chest",
            _ => "Duty Drop",
        };

    public static string GetCompactSummary(ItemAcquisitionSource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var label = GetLabel(source.Kind);
        return string.IsNullOrWhiteSpace(source.Name)
            ? label
            : $"{label}: {source.Name.Trim()}";
    }

    public static string GetCompactLabel(ItemAcquisitionKind kind)
        => kind switch
        {
            ItemAcquisitionKind.Vendor => "Vendor",
            ItemAcquisitionKind.MarketBoard => "Marketboard",
            ItemAcquisitionKind.Crafting => "Crafted",
            ItemAcquisitionKind.Quest => "Quest",
            ItemAcquisitionKind.Achievement => "Achievement",
            ItemAcquisitionKind.SeasonalEvent => "Seasonal",
            ItemAcquisitionKind.OnlineStore => "Mogstation",
            ItemAcquisitionKind.DutyDrop => "Duty drop",
            _ => "Source",
        };

    public static string GetLabel(ItemAcquisitionKind kind)
        => kind switch
        {
            ItemAcquisitionKind.Vendor => "Vendor",
            ItemAcquisitionKind.MarketBoard => "Marketboard",
            ItemAcquisitionKind.Crafting => "Crafted",
            ItemAcquisitionKind.Quest => "Quest",
            ItemAcquisitionKind.Achievement => "Achievement",
            ItemAcquisitionKind.SeasonalEvent => "Seasonal Event",
            ItemAcquisitionKind.OnlineStore => "Mogstation - Online Store",
            ItemAcquisitionKind.DutyDrop => "Duty Drop",
            _ => "Source",
        };

    private static int GetSortOrder(ItemAcquisitionKind kind)
        => kind switch
        {
            ItemAcquisitionKind.DutyDrop => 0,
            ItemAcquisitionKind.SeasonalEvent => 1,
            ItemAcquisitionKind.Quest => 2,
            ItemAcquisitionKind.Achievement => 3,
            ItemAcquisitionKind.Crafting => 4,
            ItemAcquisitionKind.Vendor => 5,
            ItemAcquisitionKind.MarketBoard => 6,
            ItemAcquisitionKind.OnlineStore => 7,
            _ => int.MaxValue,
        };

    private static void AppendName(List<string> parts, ItemAcquisitionSource source)
    {
        if (string.IsNullOrWhiteSpace(source.Name))
        {
            return;
        }

        parts.Add(source.Kind == ItemAcquisitionKind.Vendor
            ? $"NPC: {source.Name.Trim()}"
            : source.Name.Trim());
    }

    private static void Append(List<string> parts, string label, string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            parts.Add($"{label}: {value.Trim()}");
        }
    }

    private static void AppendMetadata(
        List<string> parts,
        ItemAcquisitionMetadata? metadata)
    {
        if (metadata is null)
        {
            return;
        }

        Append(parts, "Type", metadata.ContentType);
        Append(parts, "Difficulty", metadata.Difficulty);
        Append(parts, "Boss", metadata.BossName);
        Append(parts, "Series", metadata.RaidSeries);
        Append(parts, "Tier", metadata.Tier);
    }
}
