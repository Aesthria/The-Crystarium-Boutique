# Data Sources

The Crystarium Boutique separates local game-data indexing from reviewed acquisition supplements. Runtime browsing and acquisition lookup are offline; the plugin does not query an acquisition website while a tooltip is open.

## FFXIV game data through Lumina

The installed Dalamud API 15 environment exposes Lumina-backed FFXIV sheets used for item identity, equipment slots, models, stains, recipes, quests, shops, duties, territories, maps, and related display metadata. Build-time acquisition validation uses standalone Lumina `7.0.0.0` against the pinned local game-data build recorded by the generator manifest.

Base sheets do not reliably provide every reverse Item → Duty → Chest → Boss relationship. TCB therefore does not infer a boss from item names, duty ordering, generic duty membership, coffers, tokens, or shared appearance models.

## Critical-Impact/LuminaSupplemental

- Upstream: `Critical-Impact/LuminaSupplemental`
- Version: `5.1.4`
- Commit: `e9bf1495bf8588d2cf21441ef03f0d9dfe0f790a`
- License: `GPL-3.0-only`
- Pinned inputs: `DungeonBoss.csv`, `DungeonBossChest.csv`, `DungeonBossDrop.csv`, `DungeonChest.csv`, `DungeonChestItem.csv`, and `DungeonDrop.csv`

These files are build-time inputs to Boutique's original generator. Exact stable Item, ContentFinderCondition, TerritoryType, Treasure, Map, and BNpcName identifiers are validated against the targeted game data. The normalized runtime supplement preserves upstream revision, hashes, source rows, evidence classification, and boss provenance.

The retained license is at [`tools/CrystariumBoutique.AcquisitionGenerator/supplemental-data/LuminaSupplemental-5.1.4/LICENSE`](../tools/CrystariumBoutique.AcquisitionGenerator/supplemental-data/LuminaSupplemental-5.1.4/LICENSE).

## Tracky/XIVStats development fixture

A pinned six-record `ChestDropsV2` sample from the XIVStats/Tracky ecosystem remains a development fixture for generator behavior. It is not a runtime service or a full production data dependency. Its revision and MIT notice are recorded in [Third-Party Notices](../THIRD-PARTY-NOTICES.md).

## Deterministic generation and validation

The acquisition generator:

1. reads pinned upstream inputs,
2. preserves exact stable IDs and source provenance,
3. validates those IDs against the targeted Lumina/game-data version,
4. rejects invalid, conflicting, coffer-expansion, token-expansion, and shared-model inheritance claims,
5. enriches an established item/source relationship with separately reviewed duty metadata,
6. accepts only evidence classifications allowed by the strict runtime schema,
7. sorts output deterministically, and
8. packages the resulting `item-acquisition-supplement.json` with TCB.

Questionable records are quarantined for review rather than silently promoted. Boss names are populated only for an exact verified Item → source/chest → duty → boss chain. An Item → Duty relation without verified boss evidence leaves the boss empty.

## Runtime behavior

The released plugin reads its packaged supplement and local game data. It does not contact LuminaSupplemental, Tracky, XIVAPI, Teamcraft, a wiki, or another acquisition service at runtime. The explicit user-requested Eorzea Collection URL importer is a separate feature and is not part of acquisition lookup.
