# Changelog

All notable changes to The Crystarium Boutique are recorded here. Entries under **Unreleased** describe the current development state and do not represent a published release.

## [0.1.1] - 2026-09-10

- Fixed the public release package layout so The Crystarium Boutique can be installed correctly through Dalamud's custom plugin repository.

## [0.1.0] - 2026-09-10

### Added

- Visual equipment browsing with slot, search, expansion, role, and dye-capability filters.
- Crystal Wardrobe with synchronized equipment, dual dye slots, Vertical/Horizontal orientation, Reset Character, and optional automatic gearset synchronization.
- Persistent Favorites, named Favorite lists, search/type filtering, a Favorite badge, and an optional configurable frame highlight.
- Saved Designs with custom names and notes, management actions, movable item-reference lists, and portable Design-string import/export sharing.
- Supported Eorzea Collection glamour URL import, with an explicit Save Design step and normal post-import modification.
- Persistent dual-dye preview and Clear All Dye Slots for active session dyes.
- Revert Item and Reset Character controls for safely undoing temporary appearance previews.
- Deterministic visor controls for supported headgear.
- Packaged offline How to Obtain information with conservative exact-item duty and boss attribution.
- Simple, The Crystarium Boutique, and Simple Crystarium Boutique themes.

### Changed

- Preserved meaningful visual variants across shared models, including baked colors, dye capability, relic progression, replicas, and Matte appearances where applicable.
- Added truthful Level and Role filtering, including Beastmaster and other Limited-role equipment support.
- Adopted the actual valid active appearance after loading a Design, retaining compatible current equipment when a cross-job entry cannot apply.
- Refined theme presentation, responsive layouts, tooltip controls, and Crystal Wardrobe sizing/orientation behavior.

### Fixed

- Kept active dyes persistent across compatible item changes and made dye clearing independent from equipment clearing.
- Prevented Clear All Dye Slots from altering stored Saved Designs while allowing a loaded Design to reapply its saved dyes.
- Corrected cross-job Design weapon state, linked weapon handling, and restoration after preview, close, combat, zoning, GPose, and dependency lifecycle changes.
- Restored Other Items Sharing This Model information without allowing shared models to inherit acquisition claims.
- Stabilized Crystal Wardrobe ordering, orientation swaps, and settled gearset auto-synchronization.

## Historical development notes — 2026-09-01

### Boutique-owned Glamourer IPC migration

- Replaced all eleven `Glamourer.Api` subscriber wrappers with one narrowly scoped Boutique-owned Dalamud `ICallGateSubscriber` adapter verified against Glamourer.Api revision `ea569211a7c3500f2ce0b1b9223df85d8cb85f1a`.
- Preserved API 1.8+ negotiation, index/name capture, item and two-stain application, snapshot replay, refresh, revert, lifecycle detection, and the accepted GPose name-target fallback without copying Glamourer implementation code.
- Removed the direct/runtime `Glamourer.Api.dll` and `Luna.dll` references. The three Microsoft.Extensions assemblies formerly present only through those wrappers are also absent from Boutique's assembly graph and package.
- Added plugin-level IPC contract tests and automated assembly, dependency-manifest, package, and forbidden-dependency validation. Missing or disabled Glamourer now has no CLR dependency path and remains browse-only.
- Added a separate Boutique-owned Penumbra API `5` redraw adapter using only `ApiVersion.V5`, lifecycle events, and `RedrawObject.V5`. If Glamourer unloads after an applied preview, Boutique queues a local actor redraw from game state; if Penumbra is absent, restoration is retained and retried when Glamourer returns. No `Penumbra.Api.dll` or Luna runtime reference is introduced.
- Native FFXIV regression passes normal-world mutation/restoration, linked weapons, dual dyes, reset, close/combat/zone cleanup, hot Glamourer enable, GPose behavior, and immediate Penumbra redraw after Glamourer is disabled.
- Final automated verification passes 230 tests (167 Core and 63 plugin IPC), the x64 Release build with zero warnings/errors, a 16-file private-beta package, deterministic double-generation, and forbidden `Luna.dll`, `Glamourer.Api.dll`, and `Penumbra.Api.dll` checks.

### Closed private beta preparation

- Reset the closed-beta package identity to `0.1.0-beta.1`, with numeric assembly/manifest version `0.1.0.1`, while preserving the existing configuration schema and accepted v0.17.8 feature baseline.
- Added an exact runtime/distribution allowlist, deterministic private-beta ZIP generator, SHA-256 package manifest, and archive validator that rejects source, debug symbols, local state, and unexpected files.
- Added a manually dispatched, read-only GitHub Actions workflow for a trusted self-hosted Windows x64 Dalamud build environment. It produces only an authenticated 30-day private artifact and cannot create a tag, release, Pages site, or public repository feed.
- Added closed-beta installation/update/uninstall guidance, a focused feedback checklist, and private bug/visual issue forms.
- Added exact third-party binary provenance and license texts for the pre-migration baseline; the subsequent Boutique-owned IPC migration removes those binaries and their license files from the package.
- Moved the manual private-beta build to a clean GitHub-hosted Windows 2025 runner using a SHA-256-pinned official Dalamud API 15 developer bundle, with no FFXIV or XIVLauncher installation required.
- Expanded the deterministic package manifest with source-repository, exact commit, build identity, SDK, platform, configuration, Dalamud-version, and plugin-DLL hash provenance, all enforced by the archive validator.

### Complete local acquisition pipeline

- Added structured acquisition metadata for duty/content type, difficulty, boss, raid series, tier, source identity, and provenance.
- Added a separate offline .NET 10 acquisition generator that accepts pinned Tracky `ChestDropsV2` input, validates exact stable IDs against current local Lumina/game data, quarantines low-confidence/conflicting evidence, and emits a deterministic schema-2 supplement plus a development-only review report and generation manifest.
- Added a six-record reviewed sample covering Dungeon, Trial, Extreme Trial, legacy direct-drop Normal Raid, direct-drop Savage Raid, and Alliance Raid. It preserves exact Item/CFC/Territory/Treasure/Map IDs and source hashes; no boss is asserted without a separately verified chest mapping, and the plugin performs no runtime download.
- Added explicit `VerifiedObserved`, `Corroborated`, `NeedsReview`, and `Rejected` evidence states. Runtime loading accepts only the first two with complete structured provenance and two or more observations. Coffers/tokens are rejected under their exact IDs rather than expanded into gear, and shared appearances cannot inherit sources.
- Expanded local recipe sources with ingredients, yield, recipe requirements, specialization, quest, level, stars, and master book data. Expanded quest sources with issuer location, quest giver, and reward quantity.
- Removed the source loader's four-location vendor truncation and the tooltip's general six-source truncation while preserving exact-item and shared-appearance separation.
- Kept direct acquisition tooltips readable by displaying at most three vendor rows and restored shared appearances to one compact availability summary such as `[Vendor, Crafted, Marketboard]`; players can hover the named item itself for its full acquisition card.
- Limited direct quest acquisition rows to two per tooltip while retaining the complete indexed quest data.
- Restricted acquisition indexing to mapped Boutique equipment instead of constructing discarded source strings for every game item row.

### Verification status

- The focused generator build, all 164 automated tests, and the final x64 Release solution build pass with zero warnings/errors after the Glamourer `1.7.0.12` / Penumbra `1.7.0.11` dependency audit. Local staging and native validation remain pending review of the sample pipeline.

## 0.17.8 - 2026-08-31

### Crystarium gold window chrome

- Changed the first-use/reset Boutique typography default from Antique Gold to Soft Gold `#FFD48D`. Schema 21 migrates the untouched `#998C57` default while preserving any other custom font color.
- Applied the same configurable gold to all five primary menu-tab labels. Inactive tabs now use 50% text, background, and gold-frame opacity; the active tab remains fully opaque.
- Recolored the Crystarium title bar from neutral black-charcoal to a deep blue that blends with the stained-glass sheet. Window titles and title-bar controls use the same configured gold, and a responsive three-layer gold rule now spans the title bar's lower edge.
- Scoped the title color so Designs, Settings, Help, and Info retain their existing content palettes. The Boutique page follows the configured gold while Crystal Wardrobe and Save continue using their independent button/font colors.

### Verification status

- Debug and Release builds pass with zero warnings/errors and all 144 automated tests pass in both configurations. The local v0.17.8 Release candidate is staged; native title-bar and tab visual validation remains required.

## 0.17.7 - 2026-08-31

### Complete tooltip rails and Crystarium typography

- Replaced reliance on the supplied tooltip texture's nearly transparent center-side pixels with opaque, layered dark-gold side rails drawn safely inside the popup clip rectangle. The supplied textured top, bottom, and beveled corners remain in place while both vertical sides now connect continuously at every tooltip height.
- Changed the horizontal-bar first-use/reset default to dark bronze `#3F2F0F`. Schema 20 migrates the former untouched Steel Blue default while preserving custom bar colors.
- Expanded the former Boutique slot/page font-color option into a full **Boutique tab font color** setting. It defaults to Antique Gold `#998C57` and applies to normal Boutique text, filter labels and values, slot labels, Designs selector, and page controls; Crystal Wardrobe and Save retain their independent colors.
- Matched the horizontal rules immediately above and below the item grid to the thicker primary-tab underline and corrected footer space reservation for the larger Crystarium beam.

### Verification status

- Debug and Release builds pass with zero warnings/errors and all 144 automated tests pass in both configurations. The local v0.17.7 Release candidate is staged; native visual validation remains required.

## 0.17.6 - 2026-08-31

### Polished responsive defaults and Crystarium controls

- Changed the first-use and reset defaults to 30% stained-glass darkness, 20% background-only transparency, 125% Boutique interface scale, and 100% Jupiter tooltip scale. Schema 19 migrates former zero-value defaults while preserving established nonzero custom transparency/darkness choices.
- Changed the item-hover default to `#0FFCCD` at 15% transparency. Existing configurations using the former untouched Crystal Blue default migrate automatically, while custom hover colors remain unchanged.
- Applied Boutique-owned scaling directly inside the nested Boutique workspace so its slot filters, controls, labels, buttons, and page interface respond to the same 75-200% setting already used by the remaining tabs, Crystal Wardrobe, and Design List.
- Removed the native tooltip popup border in the Crystarium profile and reinforced both textured gold side rails, preserving the supplied responsive top, bottom, and beveled corners.
- Vertically centered equipment-slot filter labels, restored the SIMPLE Steel Blue and Eucalyptus defaults for Crystal Wardrobe and Save in both themes, and retained custom button-color overrides.
- Made the horizontal-bar color setting drive both structural bars and normal separators in the Crystarium theme. The primary tab underline is thicker and overlaps the former gap so it meets the bottom of the tabs cleanly.

### Verification status

- Debug and Release builds pass with zero warnings/errors and all 144 automated tests pass in both configurations. The local v0.17.6 Release candidate is staged; native visual validation remains required.

## 0.17.5 - 2026-08-31

### Responsive Boutique-owned interface scale

- Removed v0.17.4's inverse Dalamud scale and scoped spacing division after native testing showed that the safe hotfix made Boutique elements extremely small and prevented them from growing with a larger window.
- Added a persisted **Boutique interface scale** control under Settings > UI Options with a 75-200% range, a readable 125% default, explanatory text, and a reset action. Existing configurations migrate to schema 18 and receive the new default.
- Added responsive current-window scaling driven by both width and height. The main Boutique, Crystal Wardrobe, and generated Design List begin at their chosen interface scale, grow progressively as their windows are enlarged, retain a readable floor when compact, and cap growth to protect layouts.
- Tooltip scale and fitted dye/card labels now apply relative to the responsive Boutique scale. The implementation uses only `ImGui.SetWindowFontScale` on the current window, standard Dalamud font handles, and no global style mutation or font-atlas rebuild.
- Exact supplied gold frames, tooltip clearance, the unframed previous-item button, truthful local acquisition output, and session/restoration behavior remain unchanged.

### Verification status

- Debug and Release builds pass with zero warnings/errors and all 144 automated tests pass in both configurations. The local v0.17.5 Release candidate is staged; native responsive-size and repeated Dalamud-slider validation remain required.

## 0.17.4 - 2026-08-31

### Dalamud UI-scale crash hotfix

- Withdrew v0.17.3 after native testing showed that repeatedly moving Dalamud's global UI-scale slider could terminate XIV. The local logs show v0.17.3 loading normally and the process ending abruptly during the scale test without a managed plugin exception, consistent with the new custom font-atlas rebuild path.
- Removed every Boutique-created font-atlas delegate and all `FontScaleMode.UndoGlobalScale` usage. Boutique now uses only Dalamud's standard `NewGameFontHandle` lifecycle, leaving font creation, rebuild, and disposal entirely with the supported host path.
- Preserved Boutique-owned visual sizing with a current-window inverse scale applied during drawing, plus the existing scoped spacing normalization. Tooltip and fitted card/dye text now use the same helper, so they retain their configured relative sizes without touching global ImGui state or rebuilding the atlas.
- The exact supplied gold frame assets, tooltip clearance, unframed previous-item button, truthful local acquisition fallback, and all accepted appearance/session behavior remain unchanged.

### Verification status

- Debug and Release builds pass with zero warnings/errors and all 144 automated tests pass in both configurations. The corrected local v0.17.4 Release candidate is staged; repeated native slider testing is required before this hotfix can be accepted.
- **Superseded:** the crash-safe inverse scaling made Boutique elements too small and non-responsive. v0.17.5 replaces it with a Boutique-owned responsive scale without restoring custom atlas code.

## 0.17.3 - 2026-08-31

### Exact supplied frames and independent UI scale

- Replaced the programmatically drawn Crystarium gold outlines with the exact dark-gold artwork supplied in `Correct these.pdf`. The locally bundled transparent texture uses separate control and tooltip source regions with nine-slice rendering, preserving the original weathered texture and corner geometry while controls and tooltips resize.
- Applied the image frame through the existing shared wrappers for tabs, buttons, dropdowns, inputs, sliders, color controls, checkboxes, dialogs, Crystal Wardrobe, Save, and page navigation across every tab. The previous-item/revert control is intentionally unframed.
- Replaced the former tooltip vector outline with the supplied taller frame, reduced Crystarium tooltip top padding from 15px to 6px, and retained responsive width/height constraints so the background, corners, header, and wrapped content remain inside the frame.
- Rebuilt every Boutique-owned typeface through Dalamud's supported `FontScaleMode.UndoGlobalScale` path and normalize global spacing values only inside Boutique windows. Dalamud's 230-300% UI scale therefore no longer overrides the plugin's established font and layout sizes or affects other plugins.
- Audited the installed Lumina sheets again. They expose recipes, quests, achievements, shops, festivals, marketability, and Online Store membership, but no reliable item-to-boss/chest loot relation. Unknown direct sources now state that precise limitation and explicitly direct players to verified same-appearance sources when those are locally available; no source is guessed and no network/dependency was added.

### Verification status

- Debug and Release builds pass with zero warnings/errors and all 144 automated tests pass in both configurations. The local v0.17.3 Release candidate is staged; native visual inspection of exact frames, tooltip clearance, scale independence, and existing appearance/restoration behavior remains required.
- **Withdrawn:** native testing subsequently found an XIV crash while repeatedly changing Dalamud's UI scale. v0.17.4 removes the custom font-atlas path introduced here.

## 0.17.2 - 2026-08-31

### Responsive gold control framing

- Reduced the dedicated acquisition-tooltip item-name face by exactly 20%, from 32px to 25.6px, while retaining independent wrapping and the separated facts column.
- Added a large three-layer antique-gold tooltip surround with dark shadow, gold body, highlight line, responsive dimensions, 15px beveled corners, matching window rounding, and additional inner padding so content and background remain inside the frame.
- Added responsive gold frames with compact beveled corners to every primary tab and standard action button across Boutique, Designs, Settings, Help, Crystal Wardrobe, design dialogs, and dye dialogs.
- Applied the same control treatment to equipment-slot buttons, Crystal Wardrobe, Save, First/Previous/Next/Last, undo, all Boutique filter fields/dropdowns, remaining dropdowns, text inputs, numeric sliders, and color controls. Increased Crystarium frame padding keeps text and icons clear of the wider borders.
- The gold frames are drawn as resolution-independent geometry from each live control rectangle, so they grow and shrink with responsive controls without stretching a bitmap. SIMPLE remains unchanged.

### Verification status

- Debug and Release builds pass with zero warnings/errors and all 144 automated tests pass in both configurations. The local v0.17.2 Release candidate is staged; native inspection of frame joins, responsive sizing, content clearance, tooltip wrapping, and every clickable state remains required.

## 0.17.1 - 2026-08-31

### Tooltip header and stained-glass controls

- Doubled the acquisition tooltip's item-name font from 16px to 32px while retaining the persisted whole-tooltip scale control.
- Rebuilt the header as a wrapping name column, a fixed 28px breathing gap, and an independent right-aligned facts column so long names continue directly below their own starting edge and cannot collide with `LV | ILV`, item ID, or dye-slot text.
- Added a persisted 0-100% **Stained-glass background darkness** slider and reset action under Settings > UI Options. The shade is drawn with the shared backdrop renderer, so it applies consistently to every main tab, Crystal Wardrobe, and generated design-list windows without dimming controls, icons, frames, or text.
- Migrated configuration schema 16 to 17 with the accepted original stained-glass brightness as the zero-darkness default.

### Verification status

- Debug and Release builds pass with zero warnings/errors and all 144 automated tests pass in both configurations. The local v0.17.1 Release candidate is staged; native tooltip wrapping, readability, and cross-window stained-glass darkness acceptance remain required.

## 0.17.0 - 2026-08-31

### Acquisition tooltip rework

- Replaced the category/status tooltip with a compact acquisition card using plugin-owned Jupiter typography, local rarity color, right-aligned level/item-level/ID/dye facts, content-sized framing, and icon-corner anchoring.
- Added startup-only reverse indexes over installed recipes, quest rewards, achievement rewards, Gil and special exchanges, Grand Company shops, NPC shop associations/areas, festivals, FittingShop rows, and marketability. Hovering performs immutable dictionary lookups only.
- Added locally available vendor NPC/area/cost, crafting job/recipe, quest, achievement, seasonal-event, Marketboard-eligibility, and Mogstation/Online Store output without any remote price or store request.
- Added an all-expansions appearance lookup so the tooltip can list other item names sharing the model and each alias's first enabled acquisition source across Boutique, Designs, generated references, and changed Wardrobe cards.
- Expanded Item Tooltip settings with Source only/Standard/Detailed modes, independent source-category toggles, shared-appearance visibility/limit, and a persisted 75-200% Jupiter font range. Existing configurations migrate to a readable 100% minimum.
- Kept Duty Drop as a typed forward-compatible source while explicitly omitting unsupported boss/chest claims when the installed base game sheets do not contain a reliable item relationship. Live Marketboard and Online Store prices remain intentionally absent under the local-only/no-remote policy.

### Verification status

- Debug and Release builds pass with zero warnings/errors and all 144 automated tests pass in both configurations. The local v0.17.0 Release candidate is staged; focused native tooltip/data acceptance remains required before approval.

## 0.16.3 - 2026-08-31

### Corner-fitted steel frame

- Replaced the straight-opening frame asset with a new non-destructive sibling variant containing four compact, rotationally identical forged-steel corner caps inspired by the supplied architectural reference.
- Each cap extends inward only at the diagonal, then tapers quickly into the unchanged straight inner edge, covering the game icon's exposed rounded-corner wedges without thickening the complete border or obscuring useful icon artwork.
- Updated responsive frame geometry to the new asset's measured 1106/1254 straight-centerline opening while retaining the corner caps' intentional local overlap.
- Updated build output and local development staging to use `crystarium-item-frame-cornered.png`; the prior frame source remains available locally as a rollback asset.

### Verification status

- Debug and Release builds pass with zero warnings/errors and all 139 automated tests pass in both configurations. The local v0.16.3 Release package is staged for focused native acceptance.

## 0.16.2 - 2026-08-31

### Responsive frame fit

- Replaced the fixed 15-pixel Crystarium frame reserve with geometry derived from the generated asset's measured 1113/1254 transparent-opening ratio.
- The grid now solves the available icon size through the full framed-size scale, then derives the exact symmetric outer extent from the resulting icon size.
- Frame bitmap, hover contour, restriction contour, centering, and table reservation now share that same dynamic extent, keeping the metal's inner edge flush with the icon at normal, compact, and maximum window sizes.

### Verification status

- Debug and Release builds pass with zero warnings/errors and all 139 automated tests pass in both configurations. The local v0.16.2 Release package is staged for focused native acceptance.

## 0.16.1 - 2026-08-31

### Crystarium frame and interaction refinement

- Replaced the code-drawn multi-layer Crystarium grid frame with one original transparent PNG: a continuous near-black weathered forged-steel surround whose rounded corners and inner opening map directly around the square game icon.
- Removed the Crystarium-only inner hover/restriction stroke and icon wash. Hover and incompatible-item feedback now use one outside contour around the complete metal frame; the accepted SIMPLE treatment is unchanged.
- Changed the Crystarium incompatible-item outline from neon red to muted dark red `#8A2A2D` to match the supplied reference.
- Added persisted 0–100% hovered-item highlight transparency alongside the existing RGB control, with a combined Crystal Blue/opaque reset and configuration migration to schema 15.
- Added the generated frame asset to build output and deterministic local development staging.

### Verification status

- Debug and Release builds pass with zero warnings/errors and all 139 automated tests pass in both configurations. The local v0.16.1 Release package is staged for focused native visual acceptance.

## 0.16.0 - 2026-08-31

### Run With It interface pass

- Made the Crystarium Boutique grid workspace transparent over the stained-glass sheet, increased its preferred icons by 25%, reduced the weathered-steel frame thickness by 25%, and tightened grid spacing to 72% of the previous value while retaining even row and column distribution.
- Restored Crystal Wardrobe cards to the accepted pre-mural layout and retained their dark backing at 40% opacity, removing the shared mural frame that clipped card content.
- Reorganized Settings around top-level **THEMES**, **BOUTIQUE ITEM GRID SIZE**, and **UI OPTIONS** sections; renamed the profiles **SIMPLE** and **THE CRYSTARIUM BOUTIQUE THEME** and made the Crystarium profile the default for new installations while preserving existing profile choices.
- Added independent persisted background and font colors for the Boutique **Crystal Wardrobe** and **Save** actions, each with an active-theme reset.
- Added local Lumina race, gender, and Grand Company equipment restrictions. Restricted appearances receive a persistent red frame/glow and a matching tooltip explanation.
- Removed persistent selected-tile highlighting. Eligible icons now use the configurable highlight only while hovered, and both left-click and right-click apply immediately.
- Removed the redundant Designs page heading, added Crystarium-aware action colors, and gave read-only Design Item List icons hover glow plus the same configurable item tooltips without making the cards apply equipment.
- Rebuilt Help as collapsed-by-default sections and documented instant right-click application, left-click application, themes, layouts, tooltips, safety, and Design exchange.
- Added plugin-owned FFXIV font handles for native titles and consistent Jupiter, Axis, Miedinger Mid, and Trump Gothic typography across Boutique, Wardrobe, Designs, Settings, Help, navigation, cards, and tooltips independently of Dalamud's global font choice.
- Advanced configuration schema to 14 with compatible migration, normalization, and theme-safe defaults.

### Verification status

- Debug builds pass with zero warnings/errors and all 139 automated tests pass. Native in-game visual and interaction acceptance remains required for the local v0.16.0 candidate.

## 0.15.4 - 2026-08-31

### Shared Crystal Wardrobe mural frames

- Extracted the accepted five-layer weathered-steel mural frame into one shared renderer used by both Boutique appearance icons and Crystal Wardrobe equipment cards.
- Wrapped each Wardrobe card in the same near-black backing, dark outer band, mid-steel body, recessed seam, worn edge, and stable patina/scuff treatment as the Boutique grid.
- Added a frame-aware card inset so slot names, wrapped item names, high-resolution icons, and both dye controls remain inside the steel without clipping or overlap.
- Removed the Crystarium profile's black child fill from the Wardrobe workspace, equipment cards, and nested icon wells so the uninterrupted stained-glass background remains visible behind and between them.
- Kept a fixed Crystarium slot-selection glow on Wardrobe cards while preserving the separately configurable chosen-item highlight's documented Boutique-grid-only scope.
- Left the Default profile's Wardrobe card backgrounds, borders, selection fill, layout, and behavior unchanged.
- Refreshed the local compile-time Glamourer/Luna reference paths to the installed `1.7.0.10` development environment; runtime API compatibility remains version-checked.

### Verification status

- The Debug and Release builds pass with zero warnings/errors and all 136 automated tests pass in both configurations. The local v0.15.4 Release package is staged for focused native visual validation.

## 0.15.3 - 2026-08-30

### Layered weathered steel and configurable selection

- Increased the Crystarium mural frame's principal metal band from `10` to `14` logical pixels and added an `18`-pixel near-black structural backing.
- Split the frame into five stable visual treatments: black backing, dark outer band, mid-steel body, recessed dark seam, and worn inset edge.
- Added restrained fixed patina and scuff marks along different frame sides to create weathering without flicker, randomness, animation, or extra textures.
- Recalculated the frame reserve from the outermost backing, retaining the complete frame outside the item image and preserving centered layout at every supported window size.
- Expanded and repositioned selected-item feedback to wrap the full weathered-steel assembly with a solid outer line and softer secondary halo inside the tile's existing safety inset.
- Added persisted RGB **Chosen item highlight color** under Settings > Plugin Appearance, with a Crystal Blue `#7AD6FF` reset and explanatory text. It affects only Boutique grid selection in both profiles.
- Advanced configuration schema to 13 with clamped backward-compatible Crystal Blue values that preserve the accepted Default selected border.

### Verification status

- The Debug and Release builds pass with zero warnings/errors and all 136 automated tests pass in both configurations. The local v0.15.3 Release package is staged for focused native visual validation.

## 0.15.2 - 2026-08-30

### Metal support beams and mural item frames

- Replaced every profile-aware horizontal divider with a Crystarium-only three-tone steel support beam at exactly three times its Default thickness. Default UI retains its original line color and size.
- Added a dark structural core, upper steel bevel, and near-black lower edge so section dividers read as supports set into the glass rather than luminous painted lines.
- Increased the Crystarium browser icon frame's principal metal stroke from `2.5` to `10` logical pixels, with a broader near-black backing and inset steel highlight.
- Reserved the complete frame footprint outside each icon before centering, so the enlarged rounded frame follows the image perimeter without covering or clipping any icon pixels.
- Removed per-tile Child background and border shading in Crystarium UI, exposing the continuous glass around every framed appearance.
- Moved Crystarium selection feedback from a shaded square tile to a restrained outer glow around the metal mural frame. Default tile selection remains unchanged.

### Verification status

- The Debug and Release builds pass with zero warnings/errors and all 136 automated tests pass in both configurations. The local v0.15.2 Release package is staged for focused native visual validation.

## 0.15.1 - 2026-08-30

### Frameless glass and raised dark metal

- Replaced the architectural stained-glass background with a new original `1672 x 941` edge-to-edge mottled glass sheet containing no arches, frames, mullions, straight divisions, ornaments, margins, or embedded UI elements.
- Increased direct glass visibility and reduced the top-level readability veil so the Boutique, Crystal Wardrobe, and generated Design List clearly read as one glass surface.
- Made child workspaces lightly shaded instead of opaque so the continuous background remains visible between and beneath controls.
- Reworked the Crystarium palette around dark graphite metal: opaque metal buttons, fields, tabs, popups, scrollbars, dividers, item frames, Wardrobe cards, and saved Design cards use near-black shadows and restrained steel bevels to stand above the glass.
- Restricted luminous cyan to small state feedback such as checks, active slider grabs, and selected controls rather than using it as the default structural border.
- Replaced the single luminous outer stroke with a thick near-black frame and a subtle inset steel highlight drawn by the interface, not baked into the glass asset.

### Verification status

- The Debug and Release builds pass with zero warnings/errors and all 136 automated tests pass in both configurations. The local v0.15.1 Release package and revised glass asset are staged for focused native visual validation.

### Native result

- Passed on 2026-08-30.

## 0.15.0 - 2026-08-30

### Default and Crystarium UI profiles

- Replaced the future-UI placeholder with equal **Default UI** and **Crystarium UI** profile buttons and a green active-choice checkmark.
- Preserved every existing presentation setting as the non-destructive Default profile. Selecting Crystarium stores only the active profile, so returning to Default restores the player's exact saved background, border, bar, button, filter, tooltip, transparency, and layout preferences.
- Added an original packaged `1672 x 941` stained-glass background derived from the reference material's cobalt/cyan glass, dark tracery, and pointed-arch language without copying a screenshot composition.
- Added cover-cropped, aspect-safe stained-glass rendering with a dark readability veil and participation in existing whole-window and background-only transparency settings.
- Added a coordinated cool-glass ImGui palette across the main Boutique, Crystal Wardrobe, generated Design List, tabs, panels, popups, fields, scrollbars, controls, text, and separators.
- Added Crystarium-specific graphite-and-cyan item/icon framing, tighter architectural card corners, luminous structural rules, themed saved-design cards, and coordinated Wardrobe/Save actions while leaving danger and dye-color semantics intact.
- Advanced configuration schema to 12 with a backward-compatible Default-profile migration and invalid-value normalization.
- Added the theme asset to project output and local development staging.

### Verification status

- The Debug and Release builds pass with zero warnings/errors and all 136 automated tests pass in both configurations. The local v0.15.0 Release package and all six packaged image assets are staged for focused native visual validation.

## 0.14.0 - 2026-08-30

### Design polish and UI customization

- Made `/CB` and `/Boutique` safe open/close toggles while retaining `/cboutique` as the compatibility open command; command closure uses the same deterministic appearance restoration as the window close path.
- Removed the nonworking Ctrl + Shift + Delete reset-all shortcut from both windows and Help while retaining visible Reset All controls and Ctrl + click per-slot reset.
- Styled the Boutique Eorzea Collection import entry Antique Gold and the Designs Import/Export actions Terracotta `#996D57`.
- Styled Confirm Delete Clear Red, Cancel Action Steel Blue, and Rename/Duplicate confirm/cancel actions Dusty Rose; replaced the thin toolbar separator with the same configurable three-pixel structural rule used by Boutique.
- Added an Export hover explanation and persistent success notice confirming the portable Design string was copied to the clipboard.
- Combined Created/Updated into one `MM/dd/yyyy` line, framed NOTES with a light-grey border, framed DESIGN ITEM LIST between two structural rules, and set its read-only cards to Warm Charcoal `#4D484B`.
- Added Default tooltips and Hidden tooltips modes. Hidden mode shows item tooltips only while Shift is held, with a drawn green checkmark on the active choice; browser, Wardrobe, and generated-list item tooltips share the setting.
- Added persisted RGB controls and resets for structural horizontal bars, Boutique slot/page/revert button backgrounds and text, and filter-field backgrounds/text. Existing Steel Blue, Dusty Rose, White, and Warm Charcoal colors remain migration defaults; Crystal Wardrobe, Save, Search Clear, and assigned dialog colors remain independent.
- Added structural rules below every Plugin Appearance option and the requested collapsed future-UI notice for a later Crystarium-art project interface.
- Advanced configuration schema to 11 with clamped backward-compatible defaults.

### Verification status

- The Debug and Release builds pass with zero warnings/errors and all 136 automated tests pass in both configurations. The local v0.14.0 Release package is staged for focused native validation.

## 0.13.2 - 2026-08-30

### Balanced footer controls

- Reduced the full-width Page/First/Previous/Next/Last scale by 30%, from 1.5× to 1.05×, while retaining the coordinated narrow-window scaling introduced in v0.13.1.
- Reduced the previous-item control by 15%, from 78 to 66.3 logical pixels.
- Removed the footer's explicit ImGui spacer and positioned its control row from the horizontal rule's exact lower edge.
- Included the grid-to-rule item spacing in the calculated footer reserve, vertically centered pagination and the previous-item control inside one shared footer row, and retained one equal 12-pixel top/right/bottom inset around the previous-item control.

### Verification status

- All 136 automated tests and the final local Release build pass with zero warnings or errors. The staged DLL and manifest are version `0.13.2`, all five staged image assets hash-match their local sources, and focused native validation passed on 2026-08-30.

## 0.13.1 - 2026-08-30

### Coordinated responsive controls

- Replaced the `/TCB` alias with `/CB`; retained `/Boutique` and `/cboutique`.
- Replaced window-wide combo font scaling with clipped, field-local fitted preview text so a long selected Role, Expansion, Dye Slots, or Designs value never changes the font size of the opened popup choices.
- Preserved each equipment-slot glyph at its established anchor while fitting and clipping only the adjacent label inside its remaining button space.
- Made the filter-row action column proportional and added fitted Crystal Wardrobe and Save labels/icons so the action buttons contract with the window instead of colliding with neighboring filters.
- Increased page text and First/Previous/Next/Last controls to 1.5× at full width, then coordinated their down-scaling with the previous-item icon at narrower widths.
- Recalculated the footer reserve from its live responsive geometry, kept navigation centered, and evenly inset the previous-item control from the upper rule and the window's right/bottom edges.
- Removed the unsupported closed-section triangle glyphs from Settings headers.

### Verification status

- All 136 automated tests and the final local Release build pass with zero warnings or errors. The staged DLL and manifest are version `0.13.1`, and focused native validation passed on 2026-08-30.

## 0.13.0 - 2026-08-29

### Responsive controls, visual design cards, and consolidated Info

- Corrected browser icon framing so both border strokes stay inside each icon's rounded bounds instead of projecting beyond its corners.
- Added adaptive selected-text scaling to the Boutique Expansion, Role, Dye Slots, and Designs dropdowns, and to long Crystal Wardrobe dye names, while preserving normal-size menu entries and control height.
- Reduced the previous-item control from 120 to 78 logical pixels (35%) and reduced its reserved footer space while retaining bottom-right alignment and centered page navigation.
- Rebuilt Designs actions as the requested two-row 4-column layout: Save/Rename/Duplicate/Import followed by Load/Overwrite/Delete/Export. Save is eucalyptus, Load steel blue, Delete clear red, and the remaining actions dusty rose.
- Anchored Generate List at the bottom-right of Designs and retained its existing separate movable market-reference window.
- Simplified selected-design metadata to `Created | DD/MM/YYYY`, `Updated | DD/MM/YYYY`, and optional `NOTES | ...`; removed schema/time detail from the visible summary.
- Replaced the text-only equipment summary with a noninteractive 2 × 6 Design Item List showing all slots, saved item icons/names, and saved dye names/colors. These cards cannot load, mutate, publish hover state, or change appearance.
- Renamed Status to **Info**, moved the complete read-only Debug surface beneath runtime/technical status with a steel-blue divider, removed the standalone Debug tab, and reordered tabs to Crystarium Boutique, Designs, Settings, Help, Info.

### Verification status

- All 136 automated tests and the final local Release build pass with zero warnings or errors. The staged DLL and manifest are version `0.13.0`, all five staged image assets hash-match their local sources, and focused native validation passed on 2026-08-30.

## 0.12.0 - 2026-08-29

### Steel-blue Boutique and Crystal Wardrobe polish

- Reworked the active tab, top/content separators, and grid/footer divider around the requested Steel Blue `#578899`, with three-pixel rules and no Boutique-side scrollbar during resizing.
- Changed filter fields to Warm Charcoal `#4D484B`, shortened **Class / Role** to **Role**, shortened the aggregate role choice to **All**, and renamed **No Custom Design** to **No Design**.
- Standardized rounded controls, changed Save to Eucalyptus `#579983`, changed page controls to Dusty Rose, centered paging above a substantially larger, spaced previous-item control, and preserved the accepted paging/session behavior.
- Renamed **Boutique Wardrobe** to **Crystal Wardrobe**, added a locally generated white dress-form glyph and explanatory hover text, and restyled its toolbar/separator.
- Added Antique Gold `#998C57` framing to browser icons and Wardrobe cards, with a persisted Settings color control and configuration schema 10 migration.
- Added Dusty Rose card-header separators and replaced unsupported dye text marks with the exact supplied red-X artwork. All new assets remain packaged and staged locally.

### Verification status

- All 136 automated tests and the final local Release build pass with zero warnings or errors. The staged DLL and manifest are version `0.12.0`, all five staged image assets hash-match their local sources, and native validation passed on 2026-08-29.

## 0.11.0 - 2026-08-28

### Slot navigation, dye-page continuity, and previous-item restore

- Added compact white equipment glyphs to all 12 dusty-rose Boutique slot selectors. Main Hand uses crossed swords, Off Hand uses a shield, and every armor/accessory button uses its matching slot silhouette.
- Preserved the exact active browser slot, expansion, filters, tile selection, and page when a Wardrobe dye is applied. Re-selecting the already active card no longer resets pagination to page 1, and an open dye popup cannot trigger the card's navigation click path.
- Added the supplied dusty-rose New Game+ control at the bottom-right of the appearance browser. It walks back through the last 32 successful Boutique item selections, restores the complete earlier typed equipment overlay (including linked weapon pairs), and leaves dye-only changes out of item history.
- Undo replay is transactional: failed replay or recapture reapplies the prior complete preview and retains both current typed equipment state and undo history.
- Added the two local runtime image assets to build output and development staging. No remote asset or publication path was introduced.

### Verification status

- All 136 automated tests and the final local Release build pass with zero warnings or errors. Native v0.11.0 glyph, page-preservation, and previous-item-control validation passed on 2026-08-29.

## 0.10.1 - 2026-08-28

### Responsive Boutique/Wardrobe corrections

- Replaced the clipping 1 × 12 main equipment-slot selector with the requested 2 × 6 order: Main Hand, Head, Body, Hands, Legs, Feet; then Off Hand, Earrings, Necklace, Bracelets, Right Ring, Left Ring.
- Reserved the complete measured width of **Boutique Wardrobe** so its right edge remains aligned with Save while the control grows left toward Designs instead of clipping.
- Added a full-width responsive separator beneath the filter/design/action row and above the appearance grid.
- Reworked the Wardrobe toolbar from 1 × 4 to 2 × 2: Horizontal/Vertical followed by Reset chosen slot/Reset all slots. Reduced the safe minimum window width accordingly.
- Wardrobe card row heights now derive from the available vertical space. Horizontal cards shrink/expand with the window; vertical cards do the same until the 120-pixel readability floor, after which scrolling remains available.
- Wardrobe item names now use the same installed local Dalamud/`UIColor` item-rarity resolver as browser tooltips, including saved/imported items resolved through their local source item IDs.
- Shortened card/picker channel labels from **Dye slot 1/2** to **Dye 1/2** while preserving the accepted small centered unavailable X.
- Replaced the nonfunctional Help popup button with a normal, scrollable **Help** tab containing separate controls/keybinds, design exchange, and Eorzea Collection sections.
- Added an optional persisted RGB plugin-background color under Settings > Plugin Appearance. It applies to the Boutique, detached Wardrobe, and generated design-list windows while retaining existing independent transparency controls.
- Migrated configuration to schema 9; prior users retain the normal Dalamud background unless they explicitly enable the custom color.

### Verification status

- All 130 automated tests and the local Debug build pass with zero warnings or errors. Native v0.10.1 responsive-layout, Help-page, rarity-color, dye-label, background-color, and restoration validation is pending.

## 0.10.0 - 2026-08-28

### Detachable Wardrobe, local help, and catalog polish

- Added an exact 512 × 512 local Dalamud plugin icon at `images/icon.png`, copied into both build output and the staged development-plugin directory without a remote URL.
- Moved the 12 equipment cards out of the main Boutique into a separately movable/resizable **Boutique Wardrobe** window. Its persisted default is the accepted horizontal 2 × 6 arrangement; an alternate vertical 6 × 2 arrangement pairs Main/Off Hand, Head/Earrings, Body/Necklace, Hands/Bracelets, Legs/Feet, and Left/Right Ring.
- Added four dusty-rose Wardrobe controls for horizontal layout, vertical layout, selected-slot reset, and reset-all. Card behavior, wrapped names, high-resolution icons, integrated dye controls, and linked-weapon safety remain intact.
- Added Ctrl + left-click card reset and Ctrl + Shift + Delete reset-all shortcuts through the existing rollback-safe session controller.
- Replaced the removed card strip on the main Boutique with 12 evenly spaced dusty-rose slot buttons above the filters. Added a red **Boutique Wardrobe** toggle above a neon-pink **Save** button beside Designs.
- Added a trailing **Help** overlay with sideways-opening local guides for general controls/keybinds, saved-design exchange, and the explicitly invoked Eorzea Collection importer.
- Replaced large unsupported-dye crosses with a small centered red X.
- Item tooltip names now use Dalamud's installed local item-rarity color lookup and are underlined; equipment-category text is no longer underlined. The Mogstation note is the requested orange `Mogstation exclusive - Online Store` text.
- Renamed the aggregate selector display to **All** and included Mogstation equipment in that aggregate while retaining the separate **Mogstation** filter.
- Added item rarity to immutable catalog metadata, migrated configuration to schema 8 for persisted Wardrobe orientation, and staged no network-backed asset or publication path.

### Verification status

- All 130 automated tests and the local Debug build passed with zero warnings or errors. Native testing passed the icon, keybinds, GPose, Wardrobe toggle/layout/reset, slot selection, All/Mogstation aggregation, tooltip, and red-X behavior; it exposed the layout, Help-page, card-rarity, dye-label, and background customization corrections addressed by v0.10.1.

## 0.9.2 - 2026-08-28

### Enlarged equipment cards and integrated dyes

- Increased every equipment card's row height from 158 to 237 logical pixels, exactly 1.5×, while retaining the accepted 2 × 6 order.
- Replaced the fixed-height item-name header with natural wrapped text followed by a separator. The complete name now owns space above the icon instead of clipping inside a fixed child or being obscured by the image.
- Split each card vertically into text, a centered high-resolution icon region, and reserved bottom controls so icons no longer sit flush against the card bottom.
- Added two side-by-side **Dye slot** buttons to every card. Unsupported channels are disabled and crossed by prominent red X marks; zero-, one-, and two-slot items therefore expose the requested visual states.
- A supported button opens the searchable local dye menu with color groups, recent dyes, clear, metallic metadata, and channel switching. Applying a dye uses the existing rollback-safe typed controller/Glamourer path, updates linked weapon components, and immediately changes the card button to the exact local stain color and name with contrast-aware text.
- Manual card-dye changes clear any stale active-design label so the selector truthfully returns to **No Custom Design** until the modified outfit is saved.

### Native status

- All 130 automated tests and the local Debug build passed with zero warnings or errors; the user subsequently passed the v0.9.2 visual and dye validation.

## 0.9.1 - 2026-08-28

### Equipment cards and design saving

- Replaced the single 1 × 12 equipment strip with the requested 2 × 6 block. The first row is Main Hand, Head, Body, Hands, Legs, Feet; the second is Off Hand, Earrings, Necklace, Bracelets, Right Ring, Left Ring.
- Kept every card evenly sized with its wrapped slot/item header, large high-resolution icon, selection border, tooltip, and direct slot navigation.
- Fixed the Boutique Save button opening its popup from the table's transient ImGui ID scope. It now defers opening until the shared parent scope, matching the already reliable Eorzea importer popup.
- The Save dialog suggests a name, accepts a custom name and optional notes, persists the current typed changed-slot design locally, and selects the new saved name after success.

### Native status

- v0.9.0 passed native validation before this focused layout/save revision.
- Local automated/build verification passed; v0.9.1 was superseded by the requested v0.9.2 card/dye revision before a separate native result.

## 0.9.0 - 2026-08-28

### Full-width Boutique redesign

- Renamed the default tab to **Crystarium Boutique** and moved all 12 equipment cards into one full-width strip immediately below the tabs. Each evenly sized card now gives its slot and selected-item name a wrapped header and uses the remaining area for a large high-resolution game icon.
- Removed the old bottom Designs, Outfit Info, and Dye Studio workspace plus the redundant equipment-slot, minimum/maximum level, and minimum/maximum item-level filters.
- Replaced the filter area with one compact row ordered **Expansion**, **Class / Role**, **Dye Slots**, **Search**, red square clear button, **Designs**, and **Save**. Expansion choices now use names only and the store group is exactly **Mogstation**.
- Added a Boutique Designs selector with **No Custom Design** as its normal state, direct saved-design application, an **Eorzea Collection Import from URL** action at the top, and a final Save control for a user-supplied name.

### Eorzea Collection import

- Added an explicitly user-invoked, read-only HTTPS importer for public `ffxiv.eorzeacollection.com/glamour/...` links. The importer accepts only that exact host/path, converts to the public glamour JSON endpoint, enforces a 20-second timeout and 2 MiB response limit, and never uploads data.
- Resolved imported item and dye names exclusively through the installed local catalogs, applied the result through the existing rollback-safe Boutique/Glamourer session, and left the imported `Eorza Col.` design transient until the user chooses Save.
- Filled omitted armor/accessory slots with the matching Emperor's New item and preserved data-verified linked weapon/off-hand behavior.

### Local toolchain compatibility

- Updated the exact-machine Glamourer compile target to installed `1.7.0.6` / API `1.8` and adapted lifecycle subscriptions to its current wrapper surface.
- Added the version-matched local `Luna.dll` and required Microsoft.Extensions assemblies to the ignored development artifact bundle. No remote service, repository, or publication path was added.
- All 130 automated tests and the local Debug build passed with zero warnings or errors. Native v0.9.0 validation passed.

## 0.8.8 - 2026-08-27

### Persisted GPose actor fallback

- Fixed the native `v0.8.7` failure where the Boutique stayed open and the incoming appearance was visible, but Dalamud exposed neither a valid local GPose target nor a matching mutable clone in `IObjectTable.PlayerObjects`. The handoff remained `PlayerUnavailable`, so item clicks, Reset Slot, Reset All, and unload restoration could not reach an actor.
- Added Glamourer's documented typed `GetStateBase64Name`, `ApplyStateName`, `ReapplyStateName`, and `SetItemName` endpoints to the adapter and compatibility gate. `RevertStateName` remains the matching cleanup operation.
- Exact object-index binding remains preferred. If no mutable GPose index is available but the cached local-player identity is known, the active session now binds Glamourer's persisted player state by name. GPose item/dye mutations and resets therefore update every current/persisted Glamourer state for that player, matching Glamourer's own cross-GPose behavior.
- GPose close/unload now uses the bound name target directly, so cleanup no longer retries the unavailable clone resolver and the pre-GPose Boutique overlay is returned to game state.
- The opaque rollback path also uses name-based apply/reapply while the fallback is active; it never redirects through a missing object index.
- Local build passes with zero warnings/errors and all 126 automated tests pass. Focused native validation passed.

### Native status

- `v0.8.7` partially passed: the window/session and pre-GPose appearance carried in, linked weapons were fixed, and normal-world behavior returned on exit. Revision was required because all live GPose mutations/resets and GPose unload cleanup were blocked by the unresolved clone index.
- `v0.8.8` passed native validation: live equipment changes and resets work inside GPose, the continuous session carries its latest state back to normal play, linked weapons remain correct, and disabling the plugin inside GPose restores game state without Glamourer's manual revert.

## 0.8.7 - 2026-08-27

### Continuous GPose handoff

- Removed GPose entry/exit as a Boutique close/reset boundary. The active window, session, filters, selected tiles, bottom equipment cards, dyes, and typed changed-slot state now remain intact across both transitions.
- Cached the loaded normal-world player's identity before GPose can make live `IPlayerState` identity unavailable, resolving the remaining false rejection seen in the v0.8.6 local log.
- Added explicit actor rebinding: entry releases normal index `0`, automatically discovers one exact name/home-world-matched nonzero player clone from Dalamud's public object table (while preferring a valid selected local GPose target), then binds it; exit binds normal index `0` again.
- Added `TransferPreviewToCurrentActor`, which returns the destination actor to game state and replays the complete typed Boutique equipment/dye overlay. Normal-world changes therefore carry into GPose, and subsequent Boutique changes made in GPose carry back out.
- A handoff replay/capture failure returns the destination actor to game state without applying an opaque snapshot captured from the other context. The controller retains its typed changed-slot state for diagnosis/recovery.
- Closing or unloading while still inside GPose also uses Glamourer's supported `RevertStateName` endpoint to clear the persisted normal-player state, preventing the pre-GPose weapon overlay from surviving after the session ends before an exit handoff.
- Explicit Reset Slot, Reset All, close, unload, and combat safety behavior remain true game-state cleanup operations.
- Added successful, empty-state, failed-cleanup, automatic local-clone resolution, and transition-time index-zero rejection tests. All 126 local automated tests pass; native validation remains pending.

### Native status

- v0.8.6 required revision: opening in GPose still degraded because live player identity was unavailable during clone validation, and the close/reset transition model was superseded by the requested Glamourer-like continuous carryover behavior.

## 0.8.6 - 2026-08-27

### GPose and game-state recovery

- Fixed the observed GPose browse-only failure. Dalamud can expose the selected GPose clone as a generic `IGameObject`; the Boutique now performs exact character-name validation without requiring `IPlayerCharacter`, while retaining the home-world check whenever that subtype is available.
- Added Glamourer's verified typed `RevertState` endpoint to the adapter compatibility gate.
- Replaced original-snapshot application as the reset/lifecycle baseline with Glamourer's documented equipment/customization game-state revert. Slot reset, Reset All, design loading, linked-weapon replacement, normal close, combat close, plugin unload, and GPose exit now use that baseline.
- GPose exit redirects game-state reversion to normal local-player index `0` after the transient clone disappears. It never applies the clone's captured snapshot to the normal actor.
- Retained opaque snapshot application only for rollback when a multi-step replay or recapture fails.
- Added identity matching and controller game-state-revert regression coverage.

### Tooltip and Settings revision

- Moved anchored item cards to ImGui's tooltip layer so they render above the Boutique window and icon children while retaining fixed icon-corner positioning and screen-edge fallbacks.
- Kept the item name bold, added spaces to `[ ID#... ]`, inserted a full-width divider below the name/ID row, and underlined the bold equipment category. Existing italic levels, dye-slot count, Marketboard color, and Mogstation warning remain.
- Removed Layout from the Boutique filter row and moved it into Settings.
- Removed the rejected `2 x 6` layout and added four button-driven choices in the requested order: `3 x 5`, `4 x 5`, `3 x 6`, and `4 x 6`. A disabled checkbox marks the active choice, and existing `2 x 6` preferences migrate to `3 x 6`.
- Reorganized Settings into collapsed-by-default **Crystarium Boutique Layout**, **Plugin Appearance**, **Item Tooltip**, and **Combat Safety** sections with explicit up/down toggle indicators.
- Expanded pagination coverage to all four 15/20/18/24-item page sizes. All 119 local automated tests pass; native validation remains pending.

### Native status

- v0.8.5 required revision: tooltips rendered behind the Boutique, GPose clones were rejected before preview, and serialized snapshot restoration could leave linked weapon models in Glamourer state after reset or unload.

## 0.8.5 - 2026-08-27

### Document-driven Boutique refinement

- Added a fully functional `2 x 6` / 12-item layout and ordered the Layout choices `2 x 6`, `3 x 6`, `4 x 6`. Existing saved 3-row and 4-row preferences remain valid.
- Removed the duplicate custom title/version row beneath Dalamud's native window title so the primary tabs occupy that space. The version remains available in Status.
- Removed the active-class weapon helper sentence; active-job weapon filtering remains unchanged.
- Simplified the bottom Designs cell by removing saved/changed counts and replacing the direct dropdown with a `Saved Designs` button that opens a bounded, scrollable mini-menu for immediate application. The full Designs tab count line was also removed.
- Reworked the centered page bar to `Page # / #`, `<< First`, `< Previous`, `Next >`, and `Last >>`, with safe disabled states at either end. Grid-local mouse-wheel paging remains functional without the helper sentence.
- Added local marketability metadata using Lumina's `ItemSearchCategory` plus `IsUntradable` fields.
- Replaced icon-covering overlays with non-interactive tooltips anchored corner-to-corner above/right of the hovered icon when space permits, with above/left and below fallbacks that never intentionally cover the source icon.
- Tooltips now use supported bold and italic game-font handles for the item name/category and bracketed level line, a smaller `[ID#...]`, exact dye-slot text, green/red Marketboard status, and the existing Mogstation warning. Duplicate-source count text was removed.
- Added marketability and 12/18-item pagination coverage. All 114 local automated tests pass; native validation remains pending for this build.

## 0.8.4 - 2026-08-26

### GPose recovery

- Fixed the native v0.8.3 restoration leak: leaving GPose now closes the Boutique and reapplies the session-opening appearance snapshot to normal local-player index `0` instead of discarding restoration after the GPose clone vanishes.
- GPose sessions now reject a selected actor whose player name and home world do not match the logged-in local player. The Boutique no longer risks binding its appearance session to another GPose actor.
- Added deterministic tests for own-target rejection and the GPose-clone-to-normal-player restoration target transition.

### Boutique cleanup and filtering

- Removed the redundant active-slot and expansion/result/page heading above the filters, removed the `CLEAR` heading, and centered the Previous/Page/Next/mouse-wheel navigation row.
- Renamed `DYE SUPPORT` to `DYE SLOTS`. Filters now provide `None <-> All`, exact `No dye slots`, exact `Single dye slot`, and `Two dye slots` choices.
- Added `Limited Classes` beneath caster roles. It indexes Blue Mage-specific equipment and supports Beastmaster-specific rows when that class is exposed by the installed local game data; ordinary shared caster/all-class gear is excluded.
- Renamed Dye Studio channel controls to `Dye slot 1` and `Dye slot 2`.
- Replaced the bottom `Open Designs` navigation button with a saved-design selector that applies the chosen design directly.

### Designs and settings

- Moved Save, Load, Rename, Duplicate, Overwrite, Delete, Import, Export, and Generate List actions into a top toolbar. The saved-design list is now the left column and displays names only.
- Added validated, local clipboard design exchange using the versioned `TCB-DESIGN-1` format. Imports always receive a fresh local identity and a conflict-free name.
- Added a movable, resizable generated design-list window with item icons, names, slots, dye IDs, and supported Dalamud hovered-item publication for market-search plugin interoperability.
- Added background-only transparency below whole-window transparency and applied both presentation settings to the main Boutique and generated-list windows.
- Local automated and native validation remain pending for this build.

## 0.8.3 - 2026-08-26

### Added after native v0.8.2 acceptance

- Added manual GPose compatibility without restoring the removed GPose workflow. The Boutique UI remains visible in GPose and a newly opened session binds through supported Dalamud APIs to the current `GPoseTarget` object index instead of the normal-world index 0.
- Bound the resolved appearance actor for the complete session so later GPose target changes cannot silently redirect item, dye, reset, design, or restoration operations to another actor.
- Entering or leaving GPose closes the current Boutique context and requires a fresh open in the new context. A session opened inside GPose restores its bound actor when closed; if GPose already ended and the transient clone no longer exists, the clone preview is safely discarded rather than blocking normal-world recovery.
- Missing GPose targets fail with an explicit instruction to select the character before opening the Boutique. No actor scanning, name persistence, pointers, native hooks, automatic GPose entry, camera, lighting, or control changes were introduced.
- Made the existing high-resolution, non-HQ game-icon lookup explicit for both browser tiles and Outfit Info cards. Dalamud's separate item-HQ flag describes item quality rather than a higher resolution and remains intentionally disabled.
- Added three target-resolution tests, bringing the suite to 97.

### Native status

- v0.8.2 passed on 2026-08-26, including compact Layout labels, newest-first expansion and All Expansions presentation, Settings persistence, combat safety/opt-out, and restoration regression coverage.
- Automated v0.8.3 validation is complete locally. Focused manual GPose open/preview/dye/restore/exit validation is pending.

## 0.8.2 - 2026-08-26

### Changed after v0.8.1 native layout feedback

- Shortened the Boutique **Layout** selector and its two choices to exactly `3 x 6` and `4 x 6`, removing redundant row/column/item-count text that clipped in the compact filter column.
- Changed appearance and retained-source ordering to newest-first using descending local `Item.RowId`, the available game-data proxy for introduction order. Selected expansion groups now lead with their latest item rows; **All Expansions** likewise leads with the newest local additions.
- When multiple source items share one appearance, the newest source row becomes the default visible/clickable representative. Search can still promote an older matching alias without losing the shared appearance identity.
- Added automated newest-first and duplicate-source ordering coverage, bringing the suite to 94 tests.

### Native status

- Passed on 2026-08-26, including compact Layout labels, newest-first expansion and All Expansions presentation, Settings persistence, combat safety/opt-out, and restoration regression coverage.

## 0.8.1 - 2026-08-26

### Changed after native v0.8.0 acceptance

- Renamed Boutique's former **Tooltip** dropdown to **Layout** and reduced it to the two supported choices: 3 rows × 6 columns and 4 rows × 6 columns.
- Added a primary **Settings** tab between Status and Debug. Item-overlay transparency and font sizing moved there without changing their persisted ranges or preview behavior.
- Moved the persisted Outfit Info equipment-card visibility preference into Settings.
- Added persisted whole-window transparency from 0–90%. The limit keeps the complete window faintly recoverable at its maximum setting.
- Added a persisted, default-on combat safety preference. When enabled, entering combat closes the Boutique, restores the session-opening appearance, and suppresses all Boutique drawing and open requests until combat ends. Players may opt out in Settings before combat.
- Bumped plugin configuration to schema 4 while preserving all existing settings and local design records.

### Native status

- v0.8.0 passed on 2026-08-26. Tab order/defaulting, full-width Boutique layout, Designs management, Status/Debug separation, state preservation, and close/unload restoration worked as intended.
- Automated v0.8.1 validation is complete locally. Focused native Layout, Settings persistence, whole-window transparency, and combat-close/restoration validation is pending.

## 0.8.0 - 2026-08-26

### Changed after native v0.7.5 acceptance

- Added a shared header with four primary tabs: **Boutique**, **Designs**, **Status**, and **Debug**. Boutique is selected whenever a newly loaded or closed window opens.
- Removed the permanent left rail from Boutique. The appearance browser, filters, and grid now use the full window width above the accepted bottom equipment workspace.
- Promoted the saved-look manager into the full-page **Designs** tab. All locally saved designs remain visible in persistent Actions, Saved Designs, and Saved Outfit Info panes with Save, Load, Rename, Duplicate, Overwrite, and Delete controls.
- Moved plugin/version, dependency feedback, performance counters, and the **Show technical status** preference into the dedicated **Status** tab so they no longer compete with browsing space.
- Added a local, read-only **Debug** tab showing current browser/session state, Boutique-changed equipment, performance timing, catalog/preview errors, and bounded recent transitions. It never uploads logs or mutates appearance state.
- Renamed the user-facing **Loadouts** and **Outfit Context** sections to **Designs** and **Outfit Info**. Persistence schemas and internal loadout type names remain unchanged for compatibility.

### Native status

- Passed on 2026-08-26. Tab order/defaulting, full-width Boutique layout, Designs management, Status/Debug separation, state preservation, and close/unload restoration worked as intended.

## 0.7.5 - 2026-08-26

### Added after native weapon and Dye Studio feedback

- Added verified linked-weapon handling for local item rows compatible with both Main Hand and Off Hand and carrying a nonzero `ModelSub`. Selecting such a weapon now applies its matching off-hand component automatically, covering Samurai sheaths, Monk secondary fists, Machinist secondary components, and future items represented by the same game-data rule without hard-coded job or item lists.
- Added `/TCB` and `/Boutique` as alternate menu commands while retaining `/cboutique` for compatibility. All registrations are removed during deterministic disposal.
- Split Dye Studio channels into bordered top/bottom cards matching the paired equipment-column presentation. Each card shows its swatch, channel control, and live `Active: <dye name>` text without opening the picker.

### Safety and recovery

- Linked main/off-hand application, replacement, dye changes, and reset are treated as one logical weapon pair. Each operation restores/replays from the captured session original when necessary, commits controller state only after recapture, and reapplies the prior opaque preview if any component or capture fails.
- Replacing a linked pair with a standalone weapon restores the no-longer-applicable counterpart instead of leaving a stale sheath or secondary component. Resetting either linked component resets both while replaying unrelated Boutique changes.
- Successful Glamourer snapshot application is followed by the same typed one-shot `ReapplyState` redraw used by the accepted head-slot mitigation, improving weapon restoration reliability without locks, timers, native hooks, or a revert-to-game operation that could destroy a pre-existing user appearance state.
- Plugin cleanup now retries exact-original restoration when an earlier window-close attempt left the session in `RestoreFailed`, rather than discarding that recoverable state during unload.
- Added eight linked-metadata, paired-weapon, and restore-retry success/failure tests, bringing the suite to 93.

### Native status

- Passed on 2026-08-26. Linked Samurai, Monk, and Machinist components, repeated weapon swaps, paired dyes/resets/loadout replay, all three commands, live Dye Studio channel names, and exact close/unload restoration worked as intended.

## 0.7.4 - 2026-08-25

### Changed after bottom-workspace layout feedback

- Combined Loadouts and Outfit Context in the leftmost bottom-workspace column, with the complete Outfit Context status and Reset Slot/Reset All controls directly below the Loadouts controls.
- Removed the former standalone Outfit Context column and divided its full width equally among the six paired equipment-slot columns. The Loadouts/Outfit controls and Dye Studio retain their existing proportional widths.
- Increased paired card height from 122 to 130 logical pixels and the responsive icon ceiling from 56 to 72 logical pixels so the wider cards visibly enlarge verified game icons while preserving centered placement and wrapped item names.

### Native status

- Passed on 2026-08-26. The stacked Loadouts/Outfit Context controls, evenly enlarged equipment columns/icons, both grids, resizing, long Dye Studio name wrapping, and restoration regressions worked as intended.

## 0.7.3 - 2026-08-25

### Changed after native Dye Studio feedback

- Changed the selected Dye Studio item name from a clipped single-line label to width-aware text that wraps downward within the rightmost bottom-workspace cell.
- Kept the dye-channel controls directly below the wrapped name with no changes to dye application, popup behavior, session state, or restoration.

### Native status

- Passed as part of the v0.7.4 gate on 2026-08-26. Long Dye Studio item names wrapped without hiding channel controls or introducing a scrollbar.

## 0.7.2 - 2026-08-25

### Changed after native bottom-workspace feedback

- Increased the full-width bottom workspace from 150 to 290 logical pixels. Each paired slot card grows from 52 to 122 pixels, providing roughly twice the usable presentation area requested in the approved follow-up mockup.
- Restored verified game icons for every Boutique-changed slot, centered them responsively from 36–56 pixels, and changed item-state text to wrap within the card. Untouched opaque original slots continue to show the truthful placeholder because the Boutique does not parse Glamourer's original snapshot to guess an item.
- Reduced the browser grid's hard minimum height from 600 to 260 pixels so both 6 × 3 and 6 × 4 layouts can adapt above the enlarged bottom workspace without forcing page controls off-screen at the default window size.
- Rebuilt Loadout Management as three persistent panes: Actions on the far left, the saved Boutique list in the middle, and independently scrollable loadout details on the right. Load, Rename, Duplicate, Overwrite, Delete, confirmations, and Close no longer sit below a potentially long equipment list.
- Expanded the manager popup from 680 to 860 pixels and stacked/wrapped action controls to remain usable with long loadout names and confirmations.

### Native status

- Focused revision required on 2026-08-25: the enlarged workspace loaded, but long selected-item names in the Dye Studio cell still used a single clipped line instead of wrapping downward.

## 0.7.1 - 2026-08-25

### Changed after native loadout acceptance

- Moved Loadouts, Outfit Context/reset controls, all 12 slot cards, and Dye Studio out of the left status rail into a full-width horizontal workspace beneath the browser.
- Matched the approved nine-column layout: Loadouts, current/reset summary, Main Hand/Off Hand, Head/Earrings, Body/Necklace, Hands/Bracelets, Legs/Left Ring, Feet/Right Ring, and Dye Studio.
- Kept slot-card navigation/highlighting and full hover details while using compact two-line cards so the bottom workspace preserves browser height. The accepted **Show equipment cards** preference hides only the six paired card columns; context/reset controls remain available.
- Kept the existing Save/Manage and visual dye popups attached to their relocated controls with no session, persistence, or appearance-integration changes.

### Product scope

- Removed all planned GPose-specific behavior. The Boutique will remain a normal-world appearance browser and will not detect, enter, target, or adapt its UI for GPose. Earlier capability research is retained only as an archived decision record.

### Native status

- Revision required on 2026-08-25. The bottom composition loaded, but its cells were too small, changed-slot icons were absent, item text clipped, and Loadout Management actions remained below long details where they could be cut off.

## 0.7.0 - 2026-08-25

### Added

- Added persistent named Boutique loadouts with Save, Load, Rename, Duplicate, Overwrite, and Delete operations. Overwrite and Delete require an explicit second confirmation.
- Saved loadouts retain every Boutique-changed slot's appearance/source item IDs, complete dye channels, display/icon provenance, schema version, stable GUID, timestamps, optional tags, and optional notes.
- Added a dedicated loadout manager and compact rail controls. The library has no hard item-count limit and remains scrollable beyond the roughly 20 looks expected by the Foundation specification.
- Added a separate versioned JSON repository under Dalamud's plugin configuration directory. GUID filenames avoid user/machine identity, writes use replaceable temporary files, deletions are archived locally, and schema-zero documents migrate to schema one.
- Added per-record validation and quarantine: one corrupt or unsupported document cannot block the plugin or other recoverable loadouts, and warnings remain visible in the rail/log.
- Added rollback-safe load orchestration. Loading first restores the current Boutique session's exact opening snapshot, replays the saved typed slot/item/dye overlay, recaptures the result, and restores the prior preview if replay or capture fails.
- Added 12 automated persistence, migration, quarantine, CRUD, validation, and load/rollback cases, bringing the suite to 85 tests.

### Scope and safety

- A v0.7 loadout is the complete set of slots changed through the Boutique, applied over the current session-opening appearance. It does not serialize or parse Glamourer's integration-owned opaque state.
- Loadout persistence is local to this PC. No remote, cloud path, account identity, or absolute machine path is stored in a loadout.

### Native status

- Passed on 2026-08-25. Persistence across reload, CRUD and destructive confirmations, multi-slot/dye replay, loadout management, and exact close/unload restoration all worked as intended.

## 0.6.0 - 2026-08-25

### Added

- Added a compact two-column Outfit Context panel covering all 12 supported equipment slots. Boutique-changed slots show their source icon/name and dye details; untouched slots explicitly remain at the session-opening appearance.
- Added Before / Current summary state, clickable slot cards, **Reset slot**, and **Reset all** controls. Card clicks switch the browser to that equipment slot.
- Added a persisted **Show equipment cards** setting under **Tooltip → Adjust** so the character-side context can be hidden without losing session state.
- Extended `PreviewEquipmentState` with icon provenance and exposed the complete changed-slot count for presentation and future loadouts.
- Added eight automated reset/state/navigation cases, bringing the suite to 73 tests.

### Safety

- The installed Glamourer API has no per-slot revert endpoint. Slot reset therefore reapplies the exact session-opening opaque snapshot, deterministically replays every other Boutique-changed slot through the verified typed item endpoint, and restores the prior preview snapshot if replay or recapture fails.
- Reset All applies the exact session-opening snapshot and clears changed-slot state only after success. Neither reset path parses Glamourer's opaque state or invents IPC.
- Optional undo/redo remains deferred; it is not required for the v0.6 gate.

### Native status

- Passed on 2026-08-25. Card presentation/navigation, multi-slot item/dye state, slot reset, Reset All, visibility persistence, and exact close/unload restoration all worked as intended.

## 0.5.1 - 2026-08-25

### Added after native Dye Studio acceptance

- Added a persisted **Grid Layout** selector under **Tooltip → Adjust** with `6 columns x 4 rows (24)` and `6 columns x 3 rows (18)` choices.
- Made catalog pagination, page counts, empty positions, tile geometry, previous/next buttons, and grid-local wheel paging follow the selected layout. Changing layouts returns safely to page 1 without clearing the current preview selection.
- Added a full-width rail separator between Dye Studio controls and **Show technical status**.
- Added automated 18-item catalog pagination and runtime browser page-size coverage, bringing the suite to 65 tests.

### Native status

- The complete v0.5.0 Dye Studio gate passed on 2026-08-25, including one- and two-channel dye behavior, clear/recent controls, AFK head behavior, and exact close/unload restoration.
- Passed on 2026-08-25. The separator, both selectable grid geometries, 18/24-item pagination, wheel/buttons, persistence, preview, dye, and restoration regressions all worked as intended.

## 0.5.0 - 2026-08-25

### Added

- Added a one-time local `Stain` sheet loader and immutable 125-dye catalog with verified RGB colors, game-provided Shade grouping/order, and metallic metadata.
- Added a visual Dye Studio popup reached through the selected item's available channel buttons in the status rail.
- Added normalized dye-name search; All, Neutral, Red & Pink, Brown & Orange, Yellow, Green, Blue, Purple, and Special & Metallic groups; hover names/IDs; clear-channel behavior; and a bounded 12-dye recent row.
- Added explicit per-slot preview equipment/dye state to the session controller. Selecting another item resets that slot's Boutique dye state; changing one channel preserves the other.
- Implemented one- and two-channel real-time dye requests through the installed typed Glamourer `SetItem` endpoint by reapplying the selected source item with the complete two-byte stain list.
- Added dye catalog and current-channel diagnostics without parsing Glamourer's opaque snapshots.
- Added 13 automated dye/color/group/search/recent/channel/orchestration cases, bringing the suite to 63.

### Safety and performance

- Glamourer 1.8 exposes no separate dye-only subscriber. The adapter does not invent one or edit opaque Base64/JSON; `IAppearanceService.ApplyDye` receives the controller's complete updated appearance selection and uses the already verified one-shot item endpoint.
- Stain metadata is loaded and normalized once. Swatch search recomputes only when search/group state changes, not each ImGui frame.
- Successful dye mutations are recaptured into preview state while the original session snapshot remains unchanged for close/unload restoration.

### Native status

- Passed on 2026-08-25. Visual swatches, both dye channels, recents, clear/reset, AFK head behavior, and exact close/unload restoration worked without regression.

## 0.4.3 - 2026-08-25

### Fixed after native tooltip feedback

- Scaled only the Tooltip settings popup contents to 75%, matching the compact dropdown presentation and preventing the **Background Transparency** label from clipping.
- Kept the surrounding filter controls at their existing scale and left the independently adjustable item-overlay font size unchanged.

### Native status

- The v0.4.2 navigation, All Expansions default, active-class weapon filtering, layout, preview, restoration, and AFK head regressions passed.
- Passed on 2026-08-25. The reduced Tooltip settings popup scale eliminated the reported clipping without regressing either slider.

## 0.4.2 - 2026-08-25

### Changed after native navigation feedback

- Moved **All Expansions** to the top of expansion choices and made it the explicit default whenever the Boutique starts or the equipment slot changes.
- Replaced the rail's long equipment-slot and content-group lists with compact **Expansion** and **Equipment Slot** dropdowns above the grid.
- Reflowed the filter workspace into two responsive rows: expansion, slot, compact search, Clear, and tooltip settings above dye support, class/role, and the four level bounds.
- Moved tooltip background-transparency and font-size sliders into a dedicated **Tooltip** dropdown while retaining their persisted ranges and values.
- Reduced the now status-focused rail to 300 pixels so the 6 × 4 appearance grid receives more workspace.
- Added automatic exact class/job eligibility filtering for Main Hand and Off Hand items. Boutique builds compact eligibility masks from the installed `ClassJob` and `ClassJobCategory` sheets, initializes from `IPlayerState`, and refreshes immediately from Dalamud's class/job-change event.
- Kept the automatic weapon restriction separate from the manual Class / Role filter so **Clear** still resets only user-selected filters.
- Added active class/job and automatic-query details to technical status.

### Validation

- Repeated native AFK camera returns no longer interrupt head-item previews; the v0.4.1 `ReapplyState` mitigation passed this focused regression.
- Expanded automated coverage for All Expansions-first navigation, active-job refresh, exact weapon filtering, non-weapon behavior, and the two-range class/job mask. All 50 tests pass locally with zero build warnings.
- The new dropdown layout and live class/job switching subsequently passed the focused v0.4.2 native gate.

## 0.4.1 - 2026-08-25

### Changed after native browser feedback

- Replaced the cramped one-line filter strip with six labeled, responsive columns. Optional numeric bounds are blank with an **Any** hint instead of an unexplained `0`, and the dye selector receives 1.75× column weight.
- Added Class / Role filtering for Tank, Healer, Melee DPS, Physical Ranged DPS, Caster / Magical Ranged DPS, Crafter, and Gatherer using the installed `ClassJobCategory` job flags.
- Increased the navigation rail from 225 to 360 pixels and the default window from 1180 × 720 to 1500 × 980.
- Changed the grid to 6 columns × 4 rows / 24 items per page at the user's explicit request, superseding the Foundation document's original 6 × 3 requirement for this project.
- Added **All Expansions** at the bottom of chronological expansion navigation.
- Added a separate **Mogstation (Online Store)** content group from the installed `FittingShopItemSet` sheet and a prominent purchase note on matching item overlays. Mogstation items are excluded from **All Expansions** so the groups remain semantically distinct.
- Defaulted overlay font size to 75% and added a persisted 50–150% tooltip-font slider below background transparency.
- Added a Glamourer `ReapplyState` refresh after successful head-slot changes to address head items failing to redraw after the AFK camera transition; repeated AFK-return checks later passed this focused native verification.
- Added rolling average/peak Boutique UI draw timing beside the existing query timing to diagnose intermittent FPS drops without caching game texture handles against Dalamud guidance.
- Reduced the bounded filter cache to 64 queries and reused unchanged appearance entries to lower allocation/GC pressure in the new aggregate group.
- Added five role/group/pagination test cases, bringing the suite to 46.

### Initial v0.4 native result

- Search, dye/level/item-level filters, Clear, preview restoration on close/unload, and tooltip transparency passed.
- The first control strip clipped its dye text and maximum item-level field; the rail clipped content; long overlays needed smaller text; and head-slot redraw needed mitigation.
- Query timing remained approximately 0.006 ms during the reported intermittent 120-to-80/85 FPS drops, ruling out the catalog filter as the direct cause. The revised gate will compare FPS events with the new UI draw timing.

## 0.4.0 - 2026-08-25

### Added

- Immediate normalized text search across every retained source-item name and equipment category, including case, whitespace, punctuation, diacritic, and out-of-order term handling.
- Combined equippable-level, item-level, and dye-capability filters with one-click reset.
- Chronological previous/next content-group buttons in addition to direct expansion selection.
- Filtered-result count, measured query time, and cache hit/miss diagnostics.
- A persisted 0–100% tooltip-background transparency slider, defaulting to 50%.
- Nine search/filter/cache/navigation tests, bringing the suite to 41.

### Performance

- Precomputed normalized item search keys during the one-time catalog build.
- Memoized the controller's current page so ImGui frames do not repeat catalog queries.
- Added a bounded 128-query result cache; source aliases remain attached and a matching source becomes the visible/clickable primary item.
- Full pages remain exactly 18 entries and page changes reuse cached filtered results.

### Native status

- Debug build and all 41 automated tests pass with zero warnings.
- Native responsiveness, control layout, tooltip-slider persistence, and v0.3 preview/restoration regression remain the v0.4 acceptance gate.

## 0.3.2 - 2026-08-25

### Fixed

- Replaced the runtime `byte[]` stain argument with a concrete `List<byte>` payload. Dalamud IPC serialized `byte[]` as a Base64 JSON string (`"AAA="`), which Glamourer's `SetItem.V3` endpoint could not deserialize as `IReadOnlyList<byte>`. A list crosses the same verified typed endpoint as a JSON byte array.

### Native status

- Passed on 2026-08-25. Repeated weapon and equipment previews, exact restoration, hover/paging regressions, and reload behavior worked as intended with the corrected payload.
- The final local log recorded Glamourer API 1.8 detection, clean initialization/disposal, and no post-fix Boutique error.

## 0.3.1 - 2026-08-25

### Changed after native preview feedback

- Added structured `SetItem` success, result-code failure, and full exception logging with item, slot, stain, and flag context.
- Preserved nested IPC exception details in the domain error and exposed them under **Show technical status**.
- Recorded that GPose is not a v0.3 validation path; its actor/menu behavior remains assigned to the dedicated GPose milestone.

### Native status

- Glamourer API 1.8 discovery and original-state capture passed.
- The first v0.3 build failed the mutation gate because every `SetItem` invocation threw; the build did not expose the underlying exception.
- Normal-world preview and restoration require another native run with the new diagnostics before acceptance.

## 0.3.0 - 2026-08-25

### Added

- Typed adapter for the locally installed `Glamourer.Api` 1.7.0.2 assembly and runtime API 1.8 contract.
- Runtime endpoint/version detection with Available, Unavailable, and Incompatible diagnostics.
- Opaque Base64 original/preview appearance capture for local-player object index 0.
- One-shot, single-slot item preview using verified `SetItem.V3` signatures and exact equipment-slot mapping.
- Full original-snapshot restoration on window close and plugin unload.
- Deterministically disposed Glamourer initialized/disposed lifecycle subscribers.
- Four controller tests for repeated preview, failed preview, post-apply capture failure, and inactive-session rejection, bringing the suite to 32.

### Validation status

- Debug build, staging, and all 32 automated tests pass with zero warnings.
- Native repeated-preview and restoration validation remains the v0.3 acceptance gate.

## 0.2.0 - 2026-08-25

### Added

- Real local `Item` sheet loading through the installed Lumina/Dalamud APIs.
- Centralized visible equipment-slot mapping and immutable slot/content indexes.
- Hybrid model-key deduplication retaining all source-item identities.
- Expansion-era equip-level content groups with dynamic future bands.
- Responsive exact 6 × 3 icon-first appearance grid, 18-item pagination, page arrows, reliable grid-local wheel paging, selection state, tooltips, and verified game icons.
- Eighteen additional automated catalog/browser tests, bringing the suite to 28.

### Validated

- v0.1 loaded, opened, persisted configuration, unloaded, and reloaded cleanly in native Dalamud testing.
- Local logs contained expected degraded-integration warnings only and no Boutique exceptions.

### Changed after native browser feedback

- Enlarged icons toward the World of Warcraft appearance-menu scale and removed tile-body text in favor of hover tooltips.
- Removed per-tile scrollbars that intercepted mouse-wheel page input.
- Made the grid consume the available workspace height while preserving six columns and three rows.
- Replaced unsupported arrow glyphs in paging buttons with portable text labels.
- Replaced the oversized cursor-following tooltip with a non-interactive overlay matching the hovered icon's exact bounds and using 50% background transparency.
- Added a persisted tooltip-transparency percentage default so a later settings slider can expose the full 0–100% range without changing the renderer.
- Moved the overlay into the tile's ImGui child hierarchy so dragging/focusing the Boutique cannot place the main window above and hide it.
- Confirmed the final overlay behavior after window movement and the complete v0.2 native gate passed.

## 0.1.0 - 2026-08-25

### Added

- Phase 0 environment, dependency, Glamourer, Penumbra, and GPose audits.
- Architecture decisions for lifecycle, integrations, identity, session state, GPose, native hooks, persistence, and UI state.
- .NET 10 / Dalamud API 15 solution and minimal plugin manifest.
- `/cboutique` command, WindowSystem Foundation shell, configuration, structured logging, and deterministic disposal.
- Game-independent result/error, appearance, dependency, session, and restoration abstractions.
- Degraded unavailable-integration implementations for the Foundation milestone.
- Ten automated session/controller tests.
- Repeatable local build/test and development-artifact staging scripts.

### Deferred

- Catalog/grid, appearance application, dyes, loadouts, verified external adapters, and GPose assistance remain assigned to later milestones.
