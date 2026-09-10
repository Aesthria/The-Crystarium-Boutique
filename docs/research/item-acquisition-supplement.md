# Local Item Acquisition Supplement

Crystarium Boutique loads `acquisition-data/item-acquisition-supplement.json` once at startup after indexing acquisition relationships exposed directly by the installed Lumina sheets. The file is local and versioned by FFXIV game build. The released plugin has no Tracky, LuminaSupplemental, Teamcraft, XIVAPI, wiki, or other acquisition-network client.

The separate `tools/CrystariumBoutique.AcquisitionGenerator` executable creates schema 3 from pinned local inputs. The production path uses six Critical-Impact/LuminaSupplemental `5.1.4` CSV tables at commit `e9bf1495bf8588d2cf21441ef03f0d9dfe0f790a`. It verifies every input hash and validates stable IDs against the current local sqpack through installed standalone Lumina assemblies.

## Production evidence chain

The accepted mappings are:

- `DungeonBossDrop`: exact Item + duty + fight, enriched by exact `(duty, fight)` `DungeonBoss` rows → `BossDrop`.
- `DungeonBossChest`: exact Item + duty + fight, enriched by exact `(duty, fight)` `DungeonBoss` rows → `BossChest`.
- `DungeonChestItem` + `DungeonChest`: exact Item + chest + duty/territory/treasure/map → `DutyChest`, with no boss claim.
- `DungeonDrop`: exact Item + duty → `DutyDrop`, with no boss claim.

The importer applies `BossDrop > BossChest > DutyChest > DutyDrop` per exact Item/duty pair, deduplicates equivalent records, and retains legitimate alternative duties or bosses. `DungeonChest.DungeonBossId` is not sufficient boss evidence because upstream derives that field from item-set comparison; it is deliberately ignored for boss attribution.

Current Lumina provides Item names/equipment classification, ContentFinderCondition duty name/type/territory/expansion, TerritoryType/Treasure/Map ID validation, and BNpcName text. It does not independently establish the Item-to-duty or Item-to-boss relationship.

## Runtime schema 3

Each normalized duty source records:

- exact Item and ContentFinderCondition IDs;
- duty name, content type, difficulty, and expansion;
- `BossDrop`, `BossChest`, `DutyChest`, or `DutyDrop` precision;
- boss display name only for an exact verified boss relationship;
- upstream source, version/commit, per-file SHA-256, source row, generator version, and stable evidence IDs.

The runtime parser accepts two evidence origins:

- `ObservedChest`: a pinned Tracky relationship with `VerifiedObserved`, at least two observations, and valid territory/treasure/map IDs.
- `LuminaSupplemental`: a pinned exact supplemental relationship with `Corroborated`, a stable source row, and the expected upstream identity.

Boss sources additionally require nonempty boss-association provenance and stable BNpcName IDs. Duty-level sources cannot claim a boss. `NeedsReview`, malformed, and `Rejected` evidence cannot enter the runtime index. Acquisition is never inferred from a coffer/token, shared appearance/model, vendor prerequisite, localized name, or equipment-set membership.

## Generated development files

`generation-manifest.json` records game/Lumina and generator versions, generation timestamp, upstream revision and hashes, licensing, coverage, and rejection reason totals. `generation-review.json` preserves stable IDs and reasons for every rejected relationship. Neither file is packaged or displayed in normal tooltips.

The packaged plugin contains only the normalized runtime supplement and required notices. The raw source tables and generator stay in the source repository/build environment.
