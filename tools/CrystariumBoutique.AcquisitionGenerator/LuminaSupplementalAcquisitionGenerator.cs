using CrystariumBoutique.Core.Catalog;

namespace CrystariumBoutique.AcquisitionGenerator;

public static class LuminaSupplementalAcquisitionGenerator
{
    public static GenerationResult Generate(
        LuminaSupplementalGeneratorInput input,
        DateTimeOffset? generatedAtUtc = null)
    {
        ArgumentNullException.ThrowIfNull(input);
        var dataset = LuminaSupplementalDatasetReader.Read(input.DatasetDirectory, input.Source);
        var bossRows = dataset.Bosses
            .GroupBy(value => new BossFightKey(value.DutyId, value.FightNumber))
            .ToDictionary(group => group.Key, group => group.OrderBy(value => value.RowId).ToArray());
        var chests = dataset.Chests
            .GroupBy(value => value.RowId)
            .ToDictionary(group => group.Key, group => group.First());
        var candidates = new List<GeneratedDutySource>();
        var rejected = new List<ReviewedObservation>();

        foreach (var row in dataset.BossDrops)
        {
            if (!TryGetItemAndDuty(input.GameData, row.ItemId, row.DutyId, out _, out var duty, out var reason)
                || !TryResolveBosses(input.GameData, bossRows, row.DutyId, row.FightNumber, out var bosses, out reason))
            {
                rejected.Add(ToReview("DungeonBossDrop", row.RowId, row.ItemId, row.DutyId, reason));
                continue;
            }

            candidates.Add(CreateSource(
                input,
                dataset,
                row.ItemId,
                duty,
                DutyAcquisitionDropType.BossDrop,
                "DungeonBossDrop",
                row.RowId,
                row.FightNumber,
                bosses));
        }

        foreach (var row in dataset.BossChests)
        {
            if (!TryGetItemAndDuty(input.GameData, row.ItemId, row.DutyId, out _, out var duty, out var reason)
                || !TryResolveBosses(input.GameData, bossRows, row.DutyId, row.FightNumber, out var bosses, out reason))
            {
                rejected.Add(ToReview("DungeonBossChest", row.RowId, row.ItemId, row.DutyId, reason));
                continue;
            }

            candidates.Add(CreateSource(
                input,
                dataset,
                row.ItemId,
                duty,
                DutyAcquisitionDropType.BossChest,
                "DungeonBossChest",
                row.RowId,
                row.FightNumber,
                bosses));
        }

        foreach (var row in dataset.ChestItems)
        {
            if (!chests.TryGetValue(row.ChestId, out var chest))
            {
                rejected.Add(ToReview("DungeonChestItem", row.RowId, row.ItemId, 0, "InvalidDungeonChestId"));
                continue;
            }

            if (!TryGetItemAndDuty(input.GameData, row.ItemId, chest.DutyId, out _, out var duty, out var reason)
                || !ValidateChest(input.GameData, chest, duty, out reason))
            {
                rejected.Add(ToReview("DungeonChestItem", row.RowId, row.ItemId, chest.DutyId, reason));
                continue;
            }

            candidates.Add(CreateSource(
                input,
                dataset,
                row.ItemId,
                duty,
                DutyAcquisitionDropType.DutyChest,
                "DungeonChestItem",
                row.RowId,
                0,
                BossResolution.Empty,
                chest));
        }

        foreach (var row in dataset.DutyDrops)
        {
            if (!TryGetItemAndDuty(input.GameData, row.ItemId, row.DutyId, out _, out var duty, out var reason))
            {
                rejected.Add(ToReview("DungeonDrop", row.RowId, row.ItemId, row.DutyId, reason));
                continue;
            }

            candidates.Add(CreateSource(
                input,
                dataset,
                row.ItemId,
                duty,
                DutyAcquisitionDropType.DutyDrop,
                "DungeonDrop",
                row.RowId,
                0,
                BossResolution.Empty));
        }

        var accepted = SelectHighestPrecision(candidates);
        var review = new GenerationReviewDocument
        {
            GeneratorVersion = AcquisitionSupplementGenerator.GeneratorVersion,
            Rejected = rejected
                .OrderBy(value => value.ItemId)
                .ThenBy(value => value.ContentFinderConditionId)
                .ThenBy(value => value.SourceTable, StringComparer.Ordinal)
                .ThenBy(value => value.SourceRowId)
                .ToList(),
        };
        var supplement = new SupplementDocument
        {
            GameBuild = input.GameData.GameBuild,
            GeneratorVersion = AcquisitionSupplementGenerator.GeneratorVersion,
            DutySources = accepted,
        };
        var totalInput = dataset.BossDrops.Count
            + dataset.BossChests.Count
            + dataset.ChestItems.Count
            + dataset.DutyDrops.Count;
        var combinedHash = AcquisitionSupplementGenerator.ComputeSha256(string.Join(
            "\n",
            dataset.FileHashes.Select(pair => $"{pair.Key}={pair.Value}")));
        var itemCount = accepted.Select(value => value.ItemId).Distinct().Count();
        var exactBossItemCount = accepted
            .Where(value => value.DropType is DutyAcquisitionDropType.BossDrop or DutyAcquisitionDropType.BossChest)
            .Select(value => value.ItemId)
            .Distinct()
            .Count();
        var manifest = new GenerationManifest
        {
            GameBuild = input.GameData.GameBuild,
            LuminaVersion = input.GameData.LuminaVersion,
            GeneratorVersion = AcquisitionSupplementGenerator.GeneratorVersion,
            GeneratedAtUtc = generatedAtUtc ?? DateTimeOffset.UtcNow,
            UpstreamSource = input.Source.UpstreamSource,
            UpstreamRevision = input.Source.UpstreamRevision,
            InputSha256 = combinedHash,
            UpstreamDatasetSha256 = combinedHash,
            Licenses = [new GenerationLicense(input.Source.License, input.Source.LicenseUrl)],
            TotalInputRecords = totalInput,
            AcceptedRecords = accepted.Count,
            RejectedRecords = rejected.Count,
            SupersededRecords = totalInput - rejected.Count - accepted.Count,
            RejectionReasons = CountReasons(rejected),
            BoutiqueEquipmentItems = input.GameData.EquipmentItemCount,
            ItemsWithDutyAcquisition = itemCount,
            ItemsWithExactBossAcquisition = exactBossItemCount,
            ItemsWithoutDutyAcquisition = Math.Max(0, input.GameData.EquipmentItemCount - itemCount),
            BossDropRecords = accepted.Count(value => value.DropType == DutyAcquisitionDropType.BossDrop),
            BossChestRecords = accepted.Count(value => value.DropType == DutyAcquisitionDropType.BossChest),
            DutyChestRecords = accepted.Count(value => value.DropType == DutyAcquisitionDropType.DutyChest),
            GenericDutyDropRecords = accepted.Count(value => value.DropType == DutyAcquisitionDropType.DutyDrop),
            RecordsByContentType = new SortedDictionary<string, int>(accepted
                .GroupBy(value => value.ContentType, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal), StringComparer.Ordinal),
            SourceFiles = dataset.FileHashes,
        };
        return new GenerationResult(supplement, review, manifest);
    }

    private static bool TryGetItemAndDuty(
        IGameDataIndex gameData,
        uint itemId,
        uint dutyId,
        out GameItem item,
        out GameDuty duty,
        out string reason)
    {
        item = null!;
        duty = null!;
        if (itemId == 0 || !gameData.TryGetItem(itemId, out item!))
        {
            reason = "InvalidItemId";
            return false;
        }

        if (!item.IsEquipment)
        {
            reason = "NonEquipmentExactItem";
            return false;
        }

        if (dutyId == 0 || !gameData.TryGetDuty(dutyId, out duty!))
        {
            reason = "InvalidContentFinderConditionId";
            return false;
        }

        reason = "";
        return true;
    }

    private static bool TryResolveBosses(
        IGameDataIndex gameData,
        IReadOnlyDictionary<BossFightKey, DungeonBossRow[]> bossRows,
        uint dutyId,
        uint fightNumber,
        out BossResolution bosses,
        out string reason)
    {
        if (!bossRows.TryGetValue(new BossFightKey(dutyId, fightNumber), out var rows)
            || rows.Length == 0)
        {
            bosses = BossResolution.Empty;
            reason = "MissingDungeonBossRelationship";
            return false;
        }

        var names = new List<string>();
        foreach (var row in rows)
        {
            if (row.BNpcNameId == 0 || !gameData.TryGetBossName(row.BNpcNameId, out var name))
            {
                bosses = BossResolution.Empty;
                reason = "InvalidBNpcNameId";
                return false;
            }

            names.Add(name);
        }

        bosses = new BossResolution(
            string.Join(" / ", names.Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase)),
            rows.Select(value => value.BNpcNameId).Distinct().Order().ToArray(),
            rows.Select(value => value.RowId).Distinct().Order().ToArray());
        reason = "";
        return true;
    }

    private static bool ValidateChest(
        IGameDataIndex gameData,
        DungeonChestRow chest,
        GameDuty duty,
        out string reason)
    {
        if (chest.TerritoryTypeId == 0 || !gameData.ContainsTerritory(chest.TerritoryTypeId))
        {
            reason = "InvalidTerritoryTypeId";
            return false;
        }

        if (duty.TerritoryTypeId != chest.TerritoryTypeId)
        {
            reason = "ConflictingLuminaRelationship";
            return false;
        }

        if (chest.TreasureId == 0 || !gameData.ContainsTreasure(chest.TreasureId))
        {
            reason = "InvalidTreasureId";
            return false;
        }

        if (chest.MapId == 0 || !gameData.TryGetMapTerritory(chest.MapId, out _))
        {
            reason = "InvalidMapId";
            return false;
        }

        reason = "";
        return true;
    }

    private static GeneratedDutySource CreateSource(
        LuminaSupplementalGeneratorInput input,
        LuminaSupplementalDataset dataset,
        uint itemId,
        GameDuty duty,
        DutyAcquisitionDropType dropType,
        string table,
        uint rowId,
        uint fightNumber,
        BossResolution bosses,
        DungeonChestRow? chest = null)
    {
        var fileName = table + ".csv";
        var sourceHash = dataset.FileHashes[fileName];
        var provenance = $"{input.Source.UpstreamSource} {input.Source.UpstreamVersion} @ {input.Source.UpstreamRevision}; {fileName} {sourceHash}";
        var bossProvenance = bosses.NpcNameIds.Length == 0
            ? ""
            : $"LuminaSupplemental DungeonBoss.csv rows {string.Join(',', bosses.SourceRowIds)} @ {input.Source.UpstreamRevision}; {dataset.FileHashes["DungeonBoss.csv"]}";
        if (chest is not null)
        {
            provenance += $"; DungeonChest.csv row {chest.RowId} {dataset.FileHashes["DungeonChest.csv"]}";
        }

        return new GeneratedDutySource
        {
            ItemId = itemId,
            DutyId = duty.ContentFinderConditionId,
            DutyName = duty.Name,
            SourceTable = $"LuminaSupplemental/{table}",
            ContentType = duty.ContentType,
            Difficulty = duty.Difficulty,
            BossName = bosses.DisplayName,
            Expansion = duty.Expansion,
            Detail = GetDropLabel(dropType),
            DropType = dropType,
            Provenance = provenance,
            Evidence = new GeneratedEvidence
            {
                Classification = ItemAcquisitionEvidenceClassification.Corroborated,
                UpstreamSource = input.Source.UpstreamSource,
                UpstreamRevision = input.Source.UpstreamRevision,
                SourceHash = sourceHash,
                ItemId = itemId,
                ContentFinderConditionId = duty.ContentFinderConditionId,
                TerritoryTypeId = chest?.TerritoryTypeId ?? duty.TerritoryTypeId,
                TreasureId = chest?.TreasureId ?? 0,
                MapId = chest?.MapId ?? 0,
                GeneratorVersion = AcquisitionSupplementGenerator.GeneratorVersion,
                BossAssociationProvenance = bossProvenance,
                Origin = ItemAcquisitionEvidenceOrigin.LuminaSupplemental,
                SupplementalRowId = rowId,
                FightNumber = fightNumber,
                BossNpcNameIds = [.. bosses.NpcNameIds],
            },
        };
    }

    private static List<GeneratedDutySource> SelectHighestPrecision(
        IEnumerable<GeneratedDutySource> candidates)
        => candidates
            .GroupBy(value => new ItemDutyKey(value.ItemId, value.DutyId))
            .SelectMany(group =>
            {
                var priority = group.Max(value => (int)value.DropType);
                return group
                    .Where(value => (int)value.DropType == priority)
                    .DistinctBy(value => new SourceIdentity(value.DropType, value.BossName));
            })
            .OrderBy(value => value.ItemId)
            .ThenBy(value => value.DutyId)
            .ThenByDescending(value => value.DropType)
            .ThenBy(value => value.BossName, StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static ReviewedObservation ToReview(
        string sourceTable,
        uint sourceRowId,
        uint itemId,
        uint dutyId,
        string reason)
        => new()
        {
            Classification = ItemAcquisitionEvidenceClassification.Rejected,
            Reason = reason,
            ItemId = itemId,
            ContentFinderConditionId = dutyId,
            SourceTable = $"LuminaSupplemental/{sourceTable}",
            SourceRowId = sourceRowId,
        };

    private static SortedDictionary<string, int> CountReasons(IEnumerable<ReviewedObservation> values)
        => new(values
            .GroupBy(value => value.Reason, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal), StringComparer.Ordinal);

    private static string GetDropLabel(DutyAcquisitionDropType dropType)
        => dropType switch
        {
            DutyAcquisitionDropType.BossDrop => "Boss Drop",
            DutyAcquisitionDropType.BossChest => "Boss Chest",
            DutyAcquisitionDropType.DutyChest => "Duty Chest",
            _ => "Duty Drop",
        };

    private readonly record struct BossFightKey(uint DutyId, uint FightNumber);

    private readonly record struct ItemDutyKey(uint ItemId, uint DutyId);

    private readonly record struct SourceIdentity(DutyAcquisitionDropType DropType, string BossName);

    private sealed record BossResolution(
        string DisplayName,
        uint[] NpcNameIds,
        uint[] SourceRowIds)
    {
        public static BossResolution Empty { get; } = new("", [], []);
    }
}
