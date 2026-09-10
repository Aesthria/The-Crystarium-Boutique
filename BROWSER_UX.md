# Browser UX

Status: browser, grids, designs, linked weapons, settings, GPose flow, integrated card dyes, saving, detached Wardrobe behavior, corner-conforming borders, visual saved-design cards, responsive controls, and the balanced footer are accepted through v0.13.2; v0.14.0 adds command toggling, tooltip visibility modes, Design polish, and persisted interface colors; v0.15.x adds SIMPLE/Crystarium profiles, frameless stained glass, steel supports, and mural framing; v0.16.x adds the tightened mural grid, readable Wardrobe, local restriction warnings, plugin-owned FFXIV typography, one custom corner-fitted steel frame, and single-contour feedback; v0.17.x adds configurable Jupiter acquisition tooltips, exact resize-safe supplied gold frames, and plugin-owned sizing that is independent of Dalamud's global UI scale.

## Implemented filter surface

- The selected equipment card and expansion remain the primary indexed filters.
- Text search matches normalized source-item names and equipment categories. Case, repeated whitespace, punctuation, and diacritics are folded; every query term must match, but term order does not matter.
- Dye capability supports None ↔ All, exact no-dye, exact single-slot, and exact two-slot filtering.
- Class / Role supports Tank, Healer, Melee DPS, Physical Ranged DPS, Caster / Magical Ranged DPS, Crafter, and Gatherer from local `ClassJobCategory` flags.
- Clear resets search, dye-slot, and class/role filters without changing the selected slot/expansion or current preview selection.

Appearance deduplication remains intact. When a query matches a non-primary source item for a shared model, that source becomes the tile's visible/clickable item while the entry retains every source identity.

Appearance entries and their retained source aliases are ordered by descending local Item row ID. This is the available game-data proxy for introduction order, so each selected expansion and **All** presents newer additions before older ones without a hand-maintained patch table.

## Performance design

- The Lumina Item sheet and slot/content/appearance indexes are constructed once at plugin startup.
- Normalized search keys are precomputed once per source item.
- The controller recomputes a page only after slot, group, filter, or page state changes—not once per ImGui frame.
- Selecting the already active slot is a state-preserving no-op. Wardrobe dye popup clicks are excluded from card navigation, so a dye application cannot reset the browser's page, expansion, filters, or tile selection.
- Filtered appearance arrays use a FIFO-bounded 64-query cache; paging a filtered result does not rescan its group. Matching entries are reused unless a non-primary source alias must be promoted.
- Technical status exposes filtered-result count, measured query time, and cache hit/miss state.
- Full pages and rendered geometry use the persisted layout choice: 24 items / 6 × 4 or 18 items / 6 × 3. Legacy five-column settings migrate to six columns. Switching layouts resets pagination to page 1 while preserving the active item preview.
- Technical status includes rolling Boutique UI draw average/peak timing alongside query timing.

## Special content groups

- **All** aggregates expansion and Mogstation equipment and remains the first/default choice, including after slot changes.
- **Mogstation** remains a separate focused choice built exclusively from item IDs referenced by the installed `FittingShopItemSet` sheet. Those items also appear in All and receive the orange Online Store note in their overlay.

## Eligibility feedback and deferred filters

Current-class/job compatibility is enforced automatically for Main Hand and Off Hand through exact local class/job masks. The plugin now evaluates locally verified `EquipRaceCategory` race/gender flags and `Item.GrandCompany` against the loaded player; incompatible entries stay visible for appearance discovery but receive a persistent red frame/glow and an explicit tooltip reason. Owned/unowned still requires a reliable local ownership source. Favorites and recently previewed remain optional roadmap enhancements. None are inferred from item names or hard-coded tables.

An item row compatible with both Main Hand and Off Hand and carrying a nonzero local `ModelSub` is treated as one linked weapon pair. The same verified source item is sent to both Glamourer slots so the API can resolve its main model and matching off-hand component; standalone main-hand weapons and true off-hand items keep their independent behavior.

## Dye state

- Previewed items, saved designs, and imported designs retain only their verified `Item.DyeCount` channels.
- The standalone Dye Studio workspace remains removed. Two compact channel controls live in each equipment card; unsupported channels carry a small centered red X, while supported controls open the searchable local stain picker and display the selected stain's exact color/name.

## Equipment cards

- A separately movable/resizable **Crystal Wardrobe** projects all 12 supported equipment slots into even cards. Horizontal 2 × 6 is the persisted default; vertical 6 × 2 pairs related equipment slots. Missing changed-slot state means the exact session-opening appearance, not an unknown item guess. The Crystarium profile uses the accepted pre-mural card geometry with a 40%-opaque black backing so long text, icons, and dyes remain readable without clipping while stained glass remains visible around and through the cards; SIMPLE retains its established card treatment.
- Each card gives its slot/current item natural wrapped space above the image, colors that item name from the exact local game rarity row, centers a verified high-resolution icon, and reserves its bottom edge for **Dye 1/2** controls. Row height follows available vertical window space down to a 120-pixel readability floor. Clicking outside the dye controls selects the browser slot; Ctrl + click resets that slot. The visible 2 × 2 Wardrobe toolbar retains selected-slot and reset-all actions through the same rollback-safe controller; the nonworking Ctrl + Shift + Delete reset-all shortcut is removed.
- The old bottom Outfit Info summary/count/reset UI and visibility setting are no longer rendered.

## Designs

- The compact control row ends with **Designs** and **Save**. Designs defaults to **No Design**, applies saved designs directly, and exposes **Eorzea Collection Import from URL** as its first action. Saving is disabled until at least one slot has changed through the Boutique.
- Save captures all and only the current typed changed-slot overlay, with a unique name and optional notes. The manager shows timestamps, schema, saved slots, item names, and stain values.
- The dedicated Designs tab begins directly with its two color-coded action rows: Save Design/Rename/Duplicate/Import Design and Load/Overwrite/Delete/Export Design. Overwrite and Delete expose an inline second confirmation and can be cancelled without mutation; Generate List remains fixed at bottom-right. In the Crystarium profile, Rename, Duplicate, and Overwrite follow the Boutique slot-button color.
- Designs keeps the saved-design list on the left and independently scrollable Saved Outfit Info on the right. The selected summary shows only creation/update dates and prefixed notes, followed by a read-only 2 × 6 Design Item List with slot names, saved icons, item names, and dye names/colors. Eligible icons receive the same hover-only glow and configurable item tooltip as Boutique icons, including Shift-reveal mode, but never apply equipment or publish an interaction click.
- Loading intentionally replaces the current preview overlay: it returns to this session's opening appearance, replays the selected saved slots/dyes, and clears stale tile highlighting after success.
- Nonfatal migration/quarantine warnings remain visible in the Designs tab while recoverable loadouts remain usable.

## Primary tabs, layout, and settings

- The shared header is followed by **Crystarium Boutique**, **Designs**, **Settings**, **Help**, and **Info** tabs. Help is a scrollable page of collapsed-by-default guidance sections covering controls, Designs exchange, Eorzea Collection, and themes/UI; Crystarium Boutique remains the default after load or window close.
- Crystarium Boutique has no permanent left rail or bottom workspace: a white-glyph 2 × 6 slot selector, warm-charcoal controls, steel-blue dividers, a framed appearance grid, centered dusty-rose paging, and a larger bottom-right previous-item control use the complete width. A measured-width steel-blue Crystal Wardrobe toggle with a local white dress-form glyph and a eucalyptus Save action follow Designs without clipping.
- SIMPLE browser icons retain the accepted configured outline and glow. Crystarium icons use one transparent, continuous near-black weathered-steel PNG whose rounded inner opening maps around the icon; hover is one configurable outer contour with persisted RGB and 0–100% transparency, while restricted items use one muted dark-red outer contour. Neither Crystarium state draws an inner outline or icon wash, and clicking leaves no selected outline behind.
- Settings offers only `3 x 6` and `4 x 6` layouts. Tooltip controls remain in Settings rather than the filter strip.
- Info contains plugin/version, dependency feedback, the technical-status preference, counts, and performance timing. Its steel-blue-separated Debug section is deliberately read-only and reports current local session/browser state, changed equipment, errors, and bounded transitions without any remote communication.
- Settings owns item-overlay transparency/font size, hovered-item highlight color/transparency, 0–90% whole-window transparency, background-only transparency, grid layout, and the default-on combat lock. The combat lock restores and closes the active session on combat entry, then refuses open requests until combat ends.
- Settings begins with **THEMES**, offering **SIMPLE** and **THE CRYSTARIUM BOUTIQUE THEME** with an active checkmark. New installations start on Crystarium; migrations preserve existing profile choices. Crystarium uses a packaged frameless blue stained-glass sheet beneath raised graphite metal without modifying SIMPLE values. **UI OPTIONS** also owns independent, resettable Crystal Wardrobe and Save button/background colors. Whole-window and background-only transparency continue to affect both profiles.
- The save and Eorzea import popups remain available; saved-design management occupies its own full tab.
