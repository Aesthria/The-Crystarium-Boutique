# Architecture

## v0.1 structure

| Layer | Location | Responsibility |
| --- | --- | --- |
| Domain | `src/CrystariumBoutique.Core` | Immutable appearance/catalog values, indexed repository, browser controller, pagination, error/result model, dependency contracts, explicit session state machine, and restoration orchestration. Has no Dalamud dependency. |
| Plugin adapter | `src/CrystariumBoutique` | Dalamud lifecycle, Lumina row translation, game-icon access, command registration, configuration persistence, logging, dependency composition, and WindowSystem UI. |
| Tests | `tests/CrystariumBoutique.Core.Tests`, `tests/CrystariumBoutique.Plugin.Tests` | Fast tests for game-independent state/recovery behavior and the Boutique-owned Glamourer IPC contract. |
| Research/decisions | `docs/research`, `docs/adr` | Verified API evidence and durable design decisions. |
| Local tooling | `scripts`, `tools/CrystariumBoutique.AcquisitionGenerator` | Repeatable restore/build/test and dev-artifact staging plus offline, build-time acquisition-supplement generation. |

## v0.17 acquisition-tooltip boundary

`BoutiqueFontSet` uses only Dalamud's standard `NewGameFontHandle` lifecycle. `BoutiqueScaleStyle` combines the persisted 75-200% Boutique interface preference with a bounded geometric window-size ratio, then applies that result only through `ImGui.SetWindowFontScale` on the current top-level window. Relative tooltip and fitted-label scales multiply that top-level value. It never creates custom atlas delegates, rebuilds fonts, divides Dalamud's global style, or mutates ImGui state for other plugins.

`CrystariumGoldFrameRenderer` owns one shared transparent texture containing the exact supplied short-control and tall-tooltip frames. Controls and tooltips draw only the eight perimeter slices, so source corners retain their shape and straight spans resize without stretching the center or painting over the underlying control/window background.

`LuminaItemAcquisitionLoader` is the sole acquisition-data translation boundary. During catalog startup it constructs immutable item-ID reverse indexes from the installed `Recipe`, `Quest`, `Achievement`, `GilShop`/`GilShopItem`, `SpecialShop`, `GCScripShopItem`, `ENpcBase`/`ENpcResident`/`Level`, `Festival`, and existing `FittingShopItemSet` data. Each sheet family fails independently and reports a warning rather than making the equipment catalog unavailable. No sheet is queried from a hover/render path.

`CrystariumBoutique.AcquisitionGenerator` is a separate development executable and is not referenced by the released plugin. It consumes a pinned local Tracky `ChestDropsV2` file, a hash/revision/license descriptor, and independently reviewed duty metadata. The tool validates exact Item, ContentFinderCondition, TerritoryType, Treasure, and Map IDs against the current local sqpack through standalone Lumina, quarantines uncertain/conflicting evidence, and writes deterministic schema-2 supplement records plus a development-only manifest/review report. Curated duty metadata can only enrich an observed Item-to-chest relationship; it cannot create one. The runtime receives only the generated local JSON and performs no acquisition network request.

`EquipmentItem.AcquisitionSources` carries typed local source records into the game-independent catalog. Marketability and Online Store classification are normalized by `ItemAcquisitionFormatter`, while `EquipmentCatalog.TryGetAppearance` exposes every alias sharing an appearance across content groups. `ItemTooltipRenderer` consumes only these immutable records, configuration, local rarity colors, and the shared appearance list. It renders the same Jupiter acquisition card for Boutique, Designs, generated references, and changed Wardrobe cards.

The settings contract separates visibility from detail: source-only, standard, or detailed output; category toggles; shared-model visibility/limit; transparency; and 75-200% font scale. Base-sheet data does not comprehensively encode exact boss/chest drops, and live Marketboard/Online Store prices require remote services. Those values are never guessed, scraped, or requested. A typed Duty Drop reaches the runtime only through the strict packaged supplement: exact stable IDs, accepted structured evidence, two or more observations, source revision/hash, and separate boss-association provenance whenever a boss name is present.

## v0.16 interface and restriction boundaries

This section supersedes the v0.15 presentation details retained in the composition history below.

`BoutiqueFontSet` owns plugin-private handles from Dalamud's game-font atlas: Trump Gothic 23 for native window titles, Jupiter 20/16 for navigation and section roles, Axis 14/12 for controls and helper text, and Miedinger Mid 14 for navigation/numeric readouts. Windows push the title handle during `PreDraw`, restore it during `PostDraw`, and scope every content handle locally; no global Dalamud font setting is read or changed.

`EquipmentItem.EquipRestrictions` carries only immutable local Lumina data translated from `EquipRaceCategory` and `Item.GrandCompany`. `ItemEquipRestrictions.Evaluate` compares that record with a narrow `CharacterEquipContext` assembled from `IPlayerState`. The UI consumes the resulting typed reason only for red warning framing and tooltip feedback; it does not infer restrictions from names and does not remove appearance-discovery rows.

`ItemTooltipRenderer` is shared by the Boutique grid and read-only Design Item List, so position, opacity, font scaling, rarity, metadata, marketability, restriction text, and Shift-reveal behavior cannot drift. Design cards expose hover state solely for visual feedback and remain outside every appearance-mutation and hovered-market-item path.

The Crystarium Boutique workspace is transparent over the packaged glass. `CrystariumMetalFrameRenderer` draws one generated transparent weathered-steel PNG around each enlarged grid icon; the measured 1106/1254 straight-centerline alpha opening defines the framed-size scale and dynamic symmetric extent, while four compact corner gussets intentionally overlap only the icon's rounded corners. Crystarium hover/restriction feedback reuses that extent as one outside contour with no inner stroke or wash. Crystal Wardrobe retains the accepted normal bordered-card geometry with a 40%-opaque black backing. Configuration schema 15 persists the active SIMPLE/Crystarium profile, hover RGB/transparency, and independent theme-aware Wardrobe/Save action colors; existing profile choices remain untouched while new installations start on Crystarium.

## Composition and flow

`Plugin` is the composition root. It constructs narrow Boutique-owned Glamourer and optional Penumbra IPC services, creates `BoutiqueSessionController`, registers `BoutiqueWindow`, registers `/boutique`, `/tcb`, and `/cb`, and subscribes to Dalamud UI callbacks. Penumbra is limited to a version-checked queued local-actor redraw if Glamourer unloads after a Boutique mutation; all normal preview and restore ownership remains in Glamourer. All three commands are session-aware open/close toggles. Disposal reverses those registrations and asks the controller to restore/close before releasing UI resources.

The plugin composes `BoutiqueSessionController → IAppearanceService → GlamourerAppearanceService → IGlamourerIpcClient → GlamourerIpcClient`. Only the final adapter knows Glamourer endpoint labels, raw `ICallGateSubscriber` signatures, `HasFunction`, `InvokeFunc`, API compatibility, and Initialized/Disposed events. It accepts API major 1 with minor 8 or newer and verifies every required endpoint before enabling mutations. Boutique has no `Glamourer.Api.dll` or `Luna.dll` reference; missing or disabled Glamourer leaves browsing available. Outside GPose the appearance service binds index 0 and caches the loaded player's name/home world. GPose handoff retains the accepted exact-actor resolution and persisted-name fallback, while `BoutiqueSessionController.TransferPreviewToCurrentActor` reverts the destination and replays the complete typed changed-slot overlay. No native hooks, offsets, or pointers are present.

`LuminaItemCatalogLoader` reads the installed `Item`, `ClassJob`, `ClassJobCategory`, and `FittingShopItemSet` sheets once at startup and composes the separate acquisition loader above. It derives Marketboard eligibility from the local Item row's nonzero `ItemSearchCategory` and tradability flag and retains local item rarity. `LuminaStainCatalogLoader` separately translates the installed `Stain` sheet into immutable dye records. These translation boundaries release UI code from Lumina schema knowledge; the tooltip's local rarity color is the narrow exception and uses Dalamud's installed `ItemUtil` color-row resolver plus the installed `UIColor` sheet. `EquipmentCatalog` builds slot/content/role indexes, normalized search keys, aggregate All entries (including Mogstation), all-group appearance aliases, and hybrid deduplicated appearance entries once; page reads do not rescan the sheet. A separate ID/normalized-name lookup retains compatible zero-model rows such as Emperor's New items for import without exposing them as browser appearances. Appearance entries and retained source aliases use descending local Item row ID as the available newest-first introduction-order proxy. `StainCatalog` precomputes its ID and normalized-name indexes. Equipment filter results use a bounded 64-query cache and browser state recomputes only on user changes.

The UI reads session, browser, outfit, loadout, and dependency state and sends tile/reset/load requests to `BoutiqueSessionController`; it never calls appearance IPC or game memory directly. The controller applies typed `PreviewEquipmentState` records through `IAppearanceService`, recaptures the preview, and keeps the original snapshot owned by `BoutiqueSession` for state comparison and transactional rollback. Baseline replacement uses `RevertToGame`, then replays remaining typed Boutique changes. `BoutiqueWardrobeWindow` and `OutfitContextPanel` own the movable/resizable 2 × 6 or 6 × 2 Wardrobe, integrated dyes, direct navigation, and controller-backed resets. `LoadoutPanel` owns Design selection, persistence, exchange, and its read-only 2 × 6 visual projection; that projection exposes visual hover/tooltips but no mutation or market publication. `DesignReferenceWindow` remains the explicit movable/resizable market reference. `BoutiqueWindow` composes Crystarium Boutique, Designs, Settings, Help, and Info; adaptive control labels do not mutate popup typography, and the responsive footer keeps navigation and previous-item history separate. `PluginBackgroundStyle` cover-crops the stained-glass sheet, `CrystariumProfileStyle` supplies dark-metal controls, and `CrystariumMetalFrameRenderer` maps the single transparent steel-frame texture around grid icons. Browser and Design overlays share `ItemTooltipRenderer`, immutable marketability/rarity/restriction metadata, and plugin-owned game fonts. Runtime assets use `ITextureProvider` and local packaged files only.

`EorzeaCollectionImportService` is the only external-design transport boundary. It runs only after an explicit URL submission, accepts HTTPS URLs on the exact `ffxiv.eorzeacollection.com` host and `/glamour/{id}` route, fetches the corresponding read-only JSON endpoint with a 20-second timeout and 2 MiB cap, and owns no upload/authentication surface. The game-independent `EorzeaCollectionImport` parser resolves item/dye names through local indexes, supplies Emperor's New items for omitted armor/accessory slots, preserves linked weapon components, and returns a transient typed `BoutiqueLoadout`. Applying it reuses the same rollback-safe controller path as a local saved design.

`ILoadoutStore` keeps persistence independent of Dalamud and UI code. `JsonLoadoutStore` implements one versioned GUID-named document per look under the plugin configuration directory, atomic temporary-file replacement, schema migration, per-record validation/quarantine, and recoverable deletion. `LoadoutExchangeCodec` maps only portable typed fields to/from a bounded, versioned `TCB-DESIGN-1` clipboard payload. `LoadoutLibrary` owns sorted in-memory CRUD/import state and never updates it before persistence succeeds. A load request uses only validated typed item/dye records: the controller returns the actor to game state, replays the saved overlay, recaptures, and rolls back to the prior preview snapshot on failure.

Successful direct item selections append the prior complete typed equipment overlay to a 32-entry history owned by `BoutiqueSessionController`; dye-only mutations do not. The bottom-right previous-item action returns the actor to game state, replays the selected historical overlay in deterministic slot order, recaptures it, and removes the history entry only after success. This includes complete linked weapon pairs and uses the prior opaque preview for transactional rollback.

The main slot selector and previous-item action use packaged local PNG assets through `ITextureProvider`. The generated white 4 × 3 equipment-glyph sheet is sampled by UV for the requested 2 × 6 slot order, while the supplied New Game+ image is rendered directly. The Crystal Wardrobe toggle uses a generated local white dress-form glyph, and unsupported dye channels use the exact supplied red-X image. All assets are copied into local build and development staging output; none requires a network request.

## Safety properties

- Only one session may initialize at a time.
- Original and preview appearance snapshots are distinct values.
- Closing an active session routes through restoration.
- Restore failure remains visible as `RestoreFailed`; it is not silently discarded.
- A later close or plugin-disposal cleanup retries restoration from `RestoreFailed` before the session can be cleared.
- Transition history is bounded to 32 entries.
- Previous-item history is separately bounded to 32 complete typed overlays. Failed undo replay/recapture retains the current overlay and history, and dye-only edits never add history entries.
- Optional dependencies may be unavailable without preventing plugin load or window display.
- Closing the main Boutique, entering default combat lock, or unloading also closes the detached Wardrobe before session restoration; closing only the Wardrobe never ends the session.
- Every full catalog page follows the persisted 18-item / 6 × 3 or 24-item / 6 × 4 layout selected by the user; legacy five-column preferences migrate to six columns.
- Tile selection can mutate equipment only through the active session controller. A data-verified linked weapon row is the sole multi-slot case and applies the same item to Main Hand and Off Hand as one rollback-safe transaction.
- Dye selection requires an active session, a Boutique-selected item in that slot, and a channel supported by that item. Dye popup interaction cannot re-select the current slot or reset browser pagination.
- A dye request preserves the other channel and reuses the verified item IPC endpoint; opaque Glamourer snapshots are never parsed or patched.
- Linked weapon dyes update both matching components and commit only after both item calls and appearance recapture succeed.
- Recent dye history is bounded to 12 unique nonzero stains.
- Each successful preview is recaptured without replacing the original session snapshot.
- Glamourer preview uses one-shot apply flags and no persistent state lock.
- Boutique-owned Glamourer lifecycle subscribers and Dalamud callbacks are disposed deterministically.
- Duplicate model keys retain every source item rather than discarding item identity.
- Search/filter matches are evaluated against one source item at a time; a matching alias becomes the tile's primary source without discarding the complete source list.
- Filtered pages preserve the selected 18/24-entry invariant and cache growth is bounded.
- Slot reset calls Glamourer's documented game-state revert and deterministically replays other changed slots; failed replay/recapture attempts restore the prior preview snapshot without discarding the controller's changed-slot state.
- Replacing or resetting either component of a linked weapon reverts/replays both weapon slots so a stale sheath or secondary component cannot remain in the typed Boutique state.
- Reset All clears changed-slot state only after the game-state revert succeeds.
- Loadout writes and archives must succeed before the visible library mutates.
- One corrupt or unsupported loadout is quarantined independently and cannot block plugin initialization or valid records.
- Loadout apply clears/replaces changed-slot state only after game-state revert, complete item replay, and recapture all succeed; failure reapplies the prior preview.
- Loadouts never persist opaque Glamourer state, absolute paths, character names, or machine identity.
- External design import is user-triggered, host/path allow-listed, HTTPS-only, response-bounded, read-only, and resolved entirely through installed local catalogs before any appearance mutation.
- Mogstation classification comes only from the game's local Online Store fitting catalog; no price, name, or tradability heuristic is used.
- Head-slot preview performs one one-shot Glamourer state reapply after the item mutation; no timer, polling loop, persistent lock, or native hook is introduced.
- Whole-snapshot application is reserved for transactional preview rollback and performs one one-shot Glamourer state reapply; reset and lifecycle restoration use `RevertState` instead.
- Closed UI has no polling loop or background worker.
- The default combat lock uses Dalamud's supported condition service during the existing UI callback. Its first combat frame closes the window and routes through game-state restoration; subsequent combat frames suppress drawing/open requests without repeated mutation attempts.
- All command and UI event registrations are removed deterministically and idempotently.
- GPose compatibility rebinds the same active session only at an observed GPose context transition. Target changes within a context cannot redirect mutations; cached identity protects generic clone matching; typed changed-slot replay carries state in both directions; and failure cleanup never applies a clone snapshot to the normal actor.

Future catalog, persistence, and verified integration components should remain behind narrow interfaces and be introduced with the corresponding ADR/test updates. Automatic GPose entry, camera/lighting, actor scanning/switching, and GPose-specific UI remain explicitly out of scope under ADR 006; only manual current-target compatibility is allowed.
