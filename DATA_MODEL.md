# Data Model

## Appearance values

- `AppearanceId`: the visual identity used for deduplication and preview operations.
- `SourceItemId`: optional provenance for an appearance; it must not replace appearance identity.
- `StainId`: a dye/stain identifier.
- `AppearanceSelection`: an appearance plus optional source item and zero or more stain channels.
- `AppearanceSnapshot`: an opaque serialized external appearance state, its format, and optional source version. The core does not parse integration-owned payloads.
- `AppearanceKey`: the local catalog deduplication key formed from verified `Item.ModelMain`, `Item.ModelSub`, `Item.Icon`, and `Item.DyeCount` values. It preserves shared-geometry baked-color and dye-capability variants while still grouping exact visual-key matches.

## Dyes

- `StainDefinition`: one named local dye with byte ID, RGB color, stable color group, sort order, and metallic flag.
- `StainCatalog`: an immutable, ID-indexed collection of named stains with normalized name/group search.
- `StainGroup`: a stable UI grouping translated from the installed sheet's Shade values; unknown future values degrade into Other.
- `RecentStainHistory`: a bounded, duplicate-free, most-recent-first list. It is memory-only in v0.5 and survives Boutique session close/reopen for the current plugin lifetime.
- `PreviewEquipmentState`: the current Boutique-selected item, display name, icon provenance, supported channel count, and complete two-channel stain state for one changed equipment slot. A linked weapon is represented truthfully by matching Main Hand and Off Hand records, so dyes and loadouts preserve both components without adding integration-specific fields.

`StainId(0)` means no dye. The Lumina adapter excludes row zero from the swatch catalog, while the Dye Studio exposes it explicitly as **Clear channel**. `AppearanceSelection.WithStain` always materializes two channel positions so the verified Glamourer item request receives a deterministic complete stain list.

## Catalog

- `EquipmentItem`: actual FFXIV item identity and metadata: row ID, localized name/category, equip and item levels, icon, dye-channel count, model key, content group, compatible visible slots, role flags, and locally verified Mogstation membership.
- `AppearanceEntry`: one visual-variant tile plus every source item sharing its complete model/icon/dye key within a slot/content group. A deterministic primary source prefers a current normal/original item over replica and excluded legacy aliases.
- `ContentGroup`: data-driven group identity, display order, and equip-level range.
- `CatalogFilter`: normalized text intent plus optional equip-level/item-level bounds, dye-capability mode, and class/role selection.
- `CatalogPage`: selected slot/group, zero-based internal page index, total pages/results, a caller-selected positive page size (18 or 24 in the Boutique UI), measured query duration, and filter-cache status.

The installed `Item` row has no explicit expansion-introduction property. v0.2 therefore uses clearly documented expansion-era equip-level bands (1–50, 51–60, and so on) as content groups and generates future ten-level bands dynamically. This is not a claim that every low-level item was introduced in ARR.

## Session

`BoutiqueSession` owns a generated `SessionId`, original appearance, current preview appearance, dirty flag, current state, and a bounded transition history. `BoutiqueSessionController` additionally owns the complete set of Boutique-changed per-slot `PreviewEquipmentState` values during the active session so dye transformations and reset replay never require parsing the opaque snapshot. Items mapped to both weapon slots create matching Main Hand/Off Hand records and are applied, dyed, replaced, and reset as one logical pair. Missing slot entries explicitly mean the session-opening appearance. Original, preview, and persisted loadouts are separate concepts.

Current states are `Closed`, `Initializing`, `WaitingForCharacter`, `Active`, `Restoring`, `Committing`, `DependencyUnavailable`, `RestoreFailed`, and `Suspended`. The v0.1 controller exercises the load/degraded/restore paths; later milestones may add guarded transitions without bypassing the state machine.

## Loadouts

- `LoadoutEquipmentState`: one replayable changed slot containing the slot, appearance/model ID, required source item ID, complete stain values, supported dye-channel count, display name, and icon provenance.
- `BoutiqueLoadout`: a stable GUID, schema version, unique display name, UTC created/updated timestamps, one or more unique equipment-slot records, optional tags, and optional notes.
- `LoadoutStoreSnapshot`: all recoverable validated records plus nonfatal warnings and migration/quarantine counts.
- `LoadoutLibrary`: the in-memory ordered view and persistence-first CRUD boundary. It mutates memory only after a store write/archive succeeds.

A loadout is a typed Boutique overlay, not a complete opaque character snapshot: absent slots intentionally mean the current session-opening appearance. Loading restores that exact opening snapshot and replays every saved slot. This makes item/dye data portable between local sessions while preserving the rule that Glamourer's serialized state remains integration-owned.

## Results and errors

Operations return `Result` or `Result<T>`. `BoutiqueError` contains a stable `BoutiqueErrorCode`, player-readable message, optional technical context, and a recoverability flag. Categories follow the authoritative specification, including dependency, player/game-state, item/dye, capture/apply/restore, data/configuration, and unknown-integration failures. Legacy GPose error values remain inert for schema/source compatibility, but no GPose feature is planned.

## Persistence boundary

`PluginConfiguration` persists small UI settings: technical-status visibility, tooltip transparency defaulted to 50%, tooltip font scale defaulted to 75%, grid rows defaulted to 4 and clamped to 3 or 4, and Outfit Context visibility defaulted on. `JsonLoadoutStore` separately persists one schema-versioned, GUID-named JSON document per loadout beneath Dalamud's plugin configuration directory. Writes go through a temporary file; deleted records move to a recoverable local archive; invalid/unsupported records move to quarantine without blocking valid records; schema-zero records migrate to schema one. No loadout contains an absolute path or machine/user identity.

Search/filter state, recent dyes, session snapshots, active per-slot previews, and transitions remain memory-only. Broader configuration migrations, undo/redo entries, and catalog indexes remain intentionally non-persistent until their milestones define their invariants.
