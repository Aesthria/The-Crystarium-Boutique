# Acquisition Supplement Generator

This .NET 10 development tool creates Boutique's packaged, offline acquisition supplement from pinned local datasets. It contains no HTTP client and performs no download. The released plugin references neither this project nor Lumina's standalone `GameData` reader.

## Production LuminaSupplemental generation

The production path consumes only these pinned Critical-Impact/LuminaSupplemental `5.1.4` tables from commit `e9bf1495bf8588d2cf21441ef03f0d9dfe0f790a`:

- `DungeonBoss.csv`
- `DungeonBossChest.csv`
- `DungeonBossDrop.csv`
- `DungeonChest.csv`
- `DungeonChestItem.csv`
- `DungeonDrop.csv`

The source descriptor pins every file by SHA-256 and records the upstream GPL-3.0-only license. Boutique's parser/importer is original code; no LuminaSupplemental implementation code or binary is linked into the plugin.

The generator validates exact Item, ContentFinderCondition, TerritoryType, Treasure, Map, and BNpcName IDs against the current local FFXIV sqpack through installed Lumina 7 assemblies. Non-equipment IDs, invalid IDs, conflicts, and incomplete boss relationships are rejected. It never expands coffers or tokens, transfers acquisition across a shared appearance/model, or guesses a boss.

Source precision is ordered per exact Item and duty:

1. `BossDrop`
2. `BossChest`
3. `DutyChest`
4. `DutyDrop`

Only the highest available precision is retained for each Item/duty pair. Alternative duties and distinct exact bosses remain separate records. Boss attribution requires an exact supplemental Item/duty/fight relationship, a matching `DungeonBoss` row, and a valid current Lumina BNpcName ID. A duty chest remains bossless even when the upstream dataset carries a derived boss field.

Run from PowerShell:

```powershell
.\scripts\Generate-AcquisitionSupplement.ps1 -GamePath '<path-to-your-FFXIV-installation>'
```

Outputs:

- `src/CrystariumBoutique/acquisition-data/item-acquisition-supplement.json`: normalized schema-3 data packaged with the plugin.
- `supplemental-output/generation-manifest.json`: game/Lumina version, generator/source revisions and hashes, licensing, coverage, and rejection counts.
- `supplemental-output/generation-review.json`: rejected relationships for development review; never packaged.

With the pinned input and local game build, the acquisition JSON is deterministic. Generation timestamps are confined to the development manifest.

## Tracky/XIVStats sample path

The earlier Tracky `ChestDropsV2` path remains as a six-record development fixture. It validates observed Item, duty, territory, treasure, and map IDs and requires at least two observations for `VerifiedObserved` evidence. It does not populate the production dataset.

```powershell
.\scripts\Generate-AcquisitionSample.ps1 -GamePath '<path-to-your-FFXIV-installation>'
```

The tool also supports read-only Tracky and Item inspection:

```powershell
dotnet run --project .\tools\CrystariumBoutique.AcquisitionGenerator -- inspect `
  --tracky <local-ChestDropsV2.json> --game-path <local-FFXIV-path> --duties 4,59,65

dotnet run --project .\tools\CrystariumBoutique.AcquisitionGenerator -- inspect-items `
  --game-path <local-FFXIV-path> --items 1636,11085,18112
```
