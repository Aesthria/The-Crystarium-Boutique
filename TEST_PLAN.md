# Test Plan

## Automated coverage

Run `.\scripts\Build.ps1` or:

```powershell
dotnet test .\tests\CrystariumBoutique.Core.Tests\CrystariumBoutique.Core.Tests.csproj
```

The 164-test suite covers the v0.1 session/dependency/restore paths and restore-failure retry; normal/GPose/missing-target resolution including generic-clone identity matching, targetless local-clone discovery, transition-time index-zero rejection, selected-local preference, and unrelated-target fallback; successful, empty, and failed-cleanup cross-context preview transfer; centralized slot mapping and linked-weapon metadata detection; known, future, Mogstation, and All groups, including Mogstation membership in All and All-first defaults; newest-first item/source ordering; 15/20/18/24-item pagination; page clamping; runtime page-size changes; slot/group/item-ID and normalized slot/name indexes; appearance deduplication with retained source items and cross-content shared-appearance lookup; marketability; typed acquisition formatting, vendor limits, compact shared-source labels, schema-2 evidence/provenance loading, invalid/unproven rejection, observed-equipment generation, coffer/token non-expansion, shared-appearance isolation, conservative boss enrichment, single-observation/conflict quarantine, and deterministic output; local race, gender, and Grand Company equip-restriction evaluation; normalized search; source-alias promotion; combined level/item-level/dye/role filtering; exact active-class weapon filtering and non-weapon behavior; compact class/job masks; filtered pagination; query-cache reuse; render-loop page memoization; browser navigation/selection guards including same-slot page preservation; packed stain colors; Shade grouping; dye search; bounded recent dyes; two-channel stain transformations; missing-item/channel guards; failed dye-state preservation; bounded item-preview undo, original-state return, dye-history exclusion, linked-weapon restoration, and undo rollback; changed-slot context; game-state slot/reset-all orchestration; replay/capture rollback; failure-state preservation; single and linked equipment/dye preview/restoration orchestration; linked-component replacement, synchronized dyes, paired reset, second-component failure rollback; loadout schema validation, JSON round-tripping, migration, isolation/quarantine, recoverable deletion, library CRUD/import/export, unique naming, persistence-failure isolation, load replay, and load rollback; and strict Eorzea Collection URL conversion, JSON parsing, local item/dye resolution, linked off-hand propagation, omitted-slot Emperor normalization, and catalog-missing Emperor fallback.

Native FFXIV behavior is not mocked. Dalamud lifecycle and rendering require the in-game smoke test.

## v0.17.8 Crystarium-chrome acceptance test

1. Reload the staged plugin and confirm Info reports `v0.17.8 · Crystarium gold window chrome`.
2. Confirm **Boutique tab font color** defaults/resets to `#FFD48D`. Verify normal Boutique-page text and all five primary tab labels use this color while Crystal Wardrobe and Save retain their independent font colors.
3. Select each primary tab. Confirm the active tab is fully opaque and every inactive tab uses 50% opacity for its label, background, and supplied gold frame. Hovering and switching must remain clear and clickable without opacity becoming stuck.
4. Inspect the main title bar at compact, default, and large window sizes. Confirm its dark blue blends with the stained-glass background, `The Crystarium Boutique` and the menu/collapse/close controls are gold, and a continuous gold rule touches the full lower edge without clipping the title or controls.
5. Change the Boutique-tab font color and confirm Boutique text, tab labels, title text, and title controls update together. Confirm normal Designs, Settings, Help, and Info content colors remain unchanged.
6. Switch to SIMPLE and confirm its accepted title, tabs, and content palette remain unchanged. Recheck tooltip rails, structural beams, scaling, selections, dyes, Designs, Wardrobe, GPose handoff, combat lock, close/unload, and restoration.

Report any white Crystarium tab/title element, inactive tab above 50% opacity, transparent active tab, title-rule gap, title/control clipping, color leaking into other tab content or Wardrobe/Save, SIMPLE regression, or appearance/restoration regression.

## v0.17.7 tooltip-rail and Boutique-type acceptance test

1. Reload the staged plugin and confirm Info reports `v0.17.7 · Crystarium framing and typography`.
2. Hover short and long item names at several UI and tooltip scales. Confirm the gold frame now has continuous opaque left and right rails connected to the supplied upper/lower bevels, with no native thin popup border and no clipped content.
3. Open Settings > UI Options and confirm the horizontal-bar default/reset is dark bronze `#3F2F0F`. Change it while Crystarium is active and confirm the tab underline, the beams above/below the item grid, and remaining structural rules update immediately.
4. Confirm the beam above the grid and the beam below it match the thickness of the beam directly beneath the primary tabs. Confirm page controls remain clear of the lower beam in both 3x6 and 4x6 layouts.
5. Confirm **Boutique tab font color** defaults/resets to Antique Gold `#998C57`. Change it and verify normal Boutique labels, filter text, slot labels, Designs selector, and page controls update, while Crystal Wardrobe and Save retain their independent font colors and item tooltips retain their acquisition/rarity palette.
6. Recheck scaling, SIMPLE presentation, selection, paging, dyes, Designs, Wardrobe, GPose handoff, combat lock, close/unload, and game-state restoration.

Report any missing tooltip side, duplicate thin border, mismatched beam thickness, footer overlap, unchanged Boutique text, Wardrobe/Save color leak, or appearance/restoration regression.

## v0.17.6 responsive-defaults acceptance test

1. Reload the staged plugin and confirm Info reports `v0.17.6 · polished responsive defaults`. Confirm the first-use/reset values are 125% interface scale, 100% Jupiter tooltip size, 30% stained-glass darkness, 20% background-only transparency, and a `#0FFCCD` hover highlight at 15% transparency.
2. Move Boutique interface scale through 75%, 100%, 125%, 150%, and 200%. Confirm the Boutique tab's slot buttons, filter controls, labels, actions, page controls, and fitted text now resize along with Designs, Settings, Help, Info, Crystal Wardrobe, and Design List. Repeated changes must not crash XIV or trigger a font-atlas rebuild.
3. Inspect all twelve equipment-slot buttons at several scales and window sizes. Confirm each label is vertically centered inside its gold frame, remains clear of its slot icon, and fits without clipping.
4. Confirm Crystal Wardrobe uses the SIMPLE Steel Blue default and Save uses the SIMPLE Eucalyptus default in both themes. Apply custom colors, reload, and confirm the overrides persist; revert them and confirm the shared defaults return.
5. Hover short and long item names around every screen edge. Confirm the native thin tooltip popup border is absent in Crystarium and the supplied gold frame has continuous left/right rails, top/bottom rails, and intact bevels around the complete background.
6. Change the horizontal-bar color while Crystarium is active. Confirm all structural bars and separators update, and the thicker bar below the main tabs touches their bottom edge without obscuring labels or content.
7. Recheck SIMPLE presentation, grids, filters, selection, dyes, designs, Wardrobe, GPose handoff, combat lock, close/unload, and game-state restoration.

Report any scale-inert Boutique control, font drift, crash, missing tooltip side, duplicate border, bottom-heavy slot label, wrong action-button default, unchanged Crystarium bar, tab overlap, or appearance/restoration regression.

## v0.17.5 responsive interface-scale acceptance test

1. Reload the staged plugin and confirm Info reports `v0.17.5 · responsive interface scale`. Open Settings > UI Options and confirm **Boutique interface scale** defaults to 125%, changes immediately from 75% through 200%, persists after reload, and returns to 125% with Reset.
2. At 125%, resize the main Boutique slowly from its minimum practical size through a large/full-screen window. Confirm tabs, slot filters, filter controls, labels, page navigation, and buttons grow progressively with the window instead of remaining tiny; shrinking must retain a readable floor without clipping or overlap.
3. Repeat resizing with Crystal Wardrobe and a generated Design List. Confirm their text, buttons, item cards, fitted dye labels, and tooltips follow the same preference and responsive growth.
4. Test interface settings at 100%, 125%, 150%, and 200% in both grid layouts. Confirm filters and gold frames keep their shape, text remains legible, and the layout remains usable at the supported window sizes.
5. With the Boutique open, slowly cycle Dalamud UI scale through 100%, 230%, 300%, and back, allowing each host rebuild to finish. Repeat once with Wardrobe open. XIV must remain stable; no custom font-atlas code is present.
6. Recheck exact gold frames, tooltip clearance, the unframed previous-item control, SIMPLE theme, acquisition output, selections, dyes, Designs, GPose handoff, combat lock, close/unload, and game-state restoration.

Report any crash, persistent tiny UI, failure to grow, excessive growth, size drift, clipping, font warning, frame regression, or appearance/restoration regression.

## v0.17.4 scale-safety hotfix acceptance test

1. Keep the Boutique disabled until the locally staged DLL reports assembly `0.17.4.0`, then re-enable it and confirm Info reports `v0.17.4 · scale-safety hotfix`.
2. Open the Boutique and move Dalamud's global UI-scale slider slowly through 100%, 150%, 230%, and 300%, then back to 100%. Repeat the full cycle at least three times, allowing each font rebuild to finish before the next cycle. XIV must remain stable with no plugin/font exception.
3. Repeat one cycle with the Boutique closed and one with Crystal Wardrobe open. Confirm Boutique, Wardrobe, generated design windows, tooltips, fitted dye text, and control spacing retain their established relative sizing.
4. Confirm the exact dark-gold frames, tooltip top clearance, and intentionally unframed previous-item button remain unchanged. Switch to SIMPLE and confirm no gold frames appear.
5. Recheck item application, restrictions, paging, dyes, Designs, GPose handoff, combat lock, close/unload, and game-state restoration after the final scale cycle.

Report any crash, freeze, font-atlas warning, missing glyph, progressive size drift, clipped control, frame regression, or appearance/restoration regression immediately.

## v0.17.3 focused native acceptance test

1. Reload the staged plugin and confirm Info reports `v0.17.3 · exact gold frames` with no font-build or missing-texture warning.
2. Select the Crystarium theme and inspect tabs, all twelve slot filters, every filter/input, Crystal Wardrobe, Save, First/Previous/Next/Last, and clickable controls across Designs, Settings, Help, Wardrobe, and dialogs. Confirm each uses the supplied dark-gold texture with intact concave corners and weathering, without stretching, seams, clipping, or foreground overlap.
3. Confirm the previous-item/revert icon has no gold frame. Resize the main and Wardrobe windows continuously and verify every other frame follows its live control rectangle while the revert icon remains evenly positioned and fully clickable.
4. Hover short and long item names at every tooltip font setting. Confirm the supplied tall gold frame follows the complete tooltip size, its corners meet the rounded background, the former 15px gap above the name/facts row is gone, and no header, wrapped acquisition line, or shared appearance touches or escapes the border.
5. Set Dalamud global UI scale to 100%, 230%, and 300%, reloading fonts if Dalamud requests it. Confirm Boutique, Crystal Wardrobe, generated design windows, titles, controls, tooltip text, and plugin spacing retain the same established on-screen sizing; confirm other plugins still follow Dalamud's setting.
6. Hover Aetherial Steel Chainmail (item 3097) or another item without a direct local source. Confirm no boss/chest claim is invented. When a locally sourced same-appearance alias exists, the tooltip explains that the exact direct relation is absent and lists the verified alias source below.
7. Confirm Marketboard output still reports eligibility only, no live price appears, and plugin load/hover produces no live-network activity or per-hover hitching.
8. Switch to SIMPLE and confirm the supplied gold frames are absent and its accepted presentation is unchanged. Recheck item application, restrictions, paging, dyes, Designs, Wardrobe, GPose handoff, combat lock, close/unload, and game-state restoration.

Report a missing/warped frame, gold on the revert icon, top tooltip gap, content clipping, any global-scale drift, a guessed acquisition claim, a live request, a SIMPLE regression, or an appearance/restoration regression.

## v0.17.2 focused native acceptance test

1. Reload the staged plugin and confirm Info reports `v0.17.2 · gold control frames`.
2. Hover short and long equipment names. Confirm the rarity-colored name is 20% smaller than v0.17.1, still wraps directly beneath its own starting edge, and never enters the fixed gap or the right-aligned facts column.
3. Inspect the tooltip at minimum and maximum content sizes and beside every screen edge. Confirm its thick shadow/gold/highlight surround follows the live tooltip size, all four 15px bevels meet cleanly, the rounded background stays inside it, and no text touches the frame.
4. Resize the Boutique continuously in both grid modes. Inspect all twelve slot buttons, expansion/role/dye/design dropdowns, search/clear, Crystal Wardrobe, Save, First/Previous/Next/Last, and undo. Confirm the compact bevels remain symmetric and no icon, arrow, or fitted label clips into the wider frame.
5. Inspect buttons, tabs, text fields, sliders, color controls, and dropdowns throughout Designs, Settings, Help, Crystal Wardrobe, save/import/edit dialogs, and dye dialogs. Confirm normal, hovered, active, selected, and disabled controls remain readable and clickable.
6. Switch to SIMPLE and confirm the gold overlay is absent and its accepted presentation is unchanged.
7. Recheck acquisition content, tooltips, paging, selections, dyes, designs, Wardrobe, GPose handoff, close/unload, and game-state restoration.

Report any open frame seam, uneven bevel, stretched edge, foreground overlap, lost click target, popup frame drawn in the wrong window, SIMPLE regression, or restoration regression.

## v0.17.1 focused native acceptance test

1. Reload the staged plugin and confirm Info reports `v0.17.1 · tooltip and glass controls`.
2. Hover short and very long equipment names at tooltip scales 75%, 100%, 150%, and 200%. Confirm the item name is twice its former base size, wraps directly beneath its own starting edge, and retains a visible gap before the right-aligned `LV | ILV`, ID, and dye-slot column at every window position.
3. Open Settings > UI Options under the Crystarium theme. Move **Stained-glass background darkness** from 0% through 100% and confirm only the glass sheet darkens while text, controls, icons, steel frames, and overlays retain their brightness.
4. Confirm the darkness updates immediately and consistently on Boutique, Designs, Settings, Help, Info, Crystal Wardrobe, and a generated design-list window. Reload once to confirm persistence, then use the reset button and confirm the original brightness returns.
5. Switch to SIMPLE and confirm the stained-glass setting has no visual effect there, then return to Crystarium and confirm the saved value is restored.
6. Recheck acquisition content, tooltip edge anchoring, paging, selection, Wardrobe, Designs, GPose handoff, close/unload, and game-state restoration.

Report any header overlap, clipped or incorrectly indented wrapped name, dimmed foreground element, inconsistent window darkness, lost setting, or restoration regression.

## v0.17.0 focused native acceptance test

1. Reload the staged plugin and confirm Info reports `v0.17.0 · acquisition tooltips`. Check the log for the local acquisition item/source counts and report any per-sheet warning.
2. Hover crafted, ordinary-vendor, special-exchange, quest, achievement, seasonal, marketable, unmarketable, and Mogstation items. Confirm each tooltip uses the Jupiter font, rarity-colored name, right-aligned `LV`, `ILV`, `ID`, and dye-slot facts, then lists only truthful locally available acquisition sources.
3. For a normal vendor and token exchange, select Detailed under Settings > Item Tooltip. Confirm available NPC/shop name, area, cost/currency, recipe level/book, quest/achievement/event name, and duty requirement appear without clipped or blank separators. Missing fields must be omitted rather than guessed.
4. Find an appearance shared by multiple item rows. Confirm **OTHER ITEMS USING THIS APPEARANCE** lists each configured alias on its own line with a compact acquisition summary. Change the shared-item limit and source toggles and verify the tooltip updates immediately.
5. Test Source only, Standard, and Detailed modes; every source toggle; default and Shift-hidden visibility; overlay transparency; and Jupiter sizes 75%, 100%, 125%, 150%, 175%, and 200%. Move/resize the Boutique and confirm the bordered card grows with content, stays in front, and uses its edge fallback without following the pointer.
6. Repeat on a Boutique icon, Designs visual card, generated Design reference icon, and changed Crystal Wardrobe item. Confirm the acquisition format is shared and generated-reference Marketboard hover publication still works.
7. Confirm Marketboard output reports eligibility only and no live price is shown. Exact boss/chest drop text must not appear unless backed by a reliable local relation; no remote request or new dependency is permitted.
8. Recheck item application, restrictions/outer warning contour, paging, dyes, save/load/import/export, GPose handoff, combat lock, close, unload, and game-state restoration.

Report wrong acquisition claims, startup warnings, missing shared aliases, per-hover hitching, clipped/oversized cards, incorrect rarity or Jupiter font, lost tooltip settings, live-network activity, or any appearance/restoration regression.

## v0.16.3 focused native acceptance test

1. Reload the staged plugin and confirm Info reports `v0.16.3 · corner-fitted steel frame`. Select the Crystarium theme and inspect icons with clearly rounded game-art corners.
2. Confirm each former stained-glass corner wedge is covered by one compact inward steel cap. The four caps must be identical rotations, meet the icon cleanly, and taper back into the existing straight inner edges.
3. Confirm the caps do not obscure meaningful icon artwork, widen the full straight border, add large fan panels, create seams, or disturb the accepted outer frame silhouette.
4. Resize continuously from compact through maximum size in both 3 × 6 and 4 × 6 layouts. Confirm straight edges remain flush, corner coverage scales proportionally, frames remain centered, and no new gap or overlap appears.
5. Hover eligible and restricted items. Confirm their single outer contours still follow the complete frame without restoring inner borders or icon wash. Switch to SIMPLE and confirm it remains unchanged.
6. Recheck item application, tooltips, restrictions, paging, Wardrobe, Designs, GPose handoff, combat lock, normal close, command close, and unload restoration.

Report any exposed corner wedge, excessive icon coverage, asymmetric cap, straight-edge thickening, frame drift, doubled interaction outline, SIMPLE regression, or behavior/restoration regression.

## v0.16.2 focused native acceptance test

1. Reload the staged plugin and confirm Info reports `v0.16.2 · responsive frame fit`. Select the Crystarium theme and begin at the normal compact window size shown in the supplied reference.
2. Inspect all 18 or 24 icons. Confirm the steel frame's inner edge sits directly against every icon edge and rounded corner, with no visible stained-glass gap, icon overlap, or inconsistent side.
3. Slowly enlarge and shrink the window through its full supported range. Confirm the frame remains flush continuously rather than fitting only near maximum size; the frame thickness should scale proportionally with the icon.
4. Repeat in both 3 × 6 and 4 × 6 layouts and across several equipment slots. Confirm row/column spacing, centering, hover contour, restricted contour, tooltips, and paging remain correct.
5. Switch to SIMPLE and confirm its accepted geometry is unchanged. Recheck left/right item application, GPose handoff, combat lock, normal close, command close, and unload restoration.

Report any inner glass gap, icon clipping, frame drift during resizing, asymmetric corner, outline misalignment, SIMPLE regression, or appearance/restoration regression.

## v0.16.1 focused native acceptance test

1. Reload the staged plugin and confirm Info reports `v0.16.1 · Crystarium frame refinement`. Confirm SIMPLE still matches the accepted screenshot: its base item outline, blue hover treatment, and bright-red restricted treatment are visually unchanged.
2. Select **THE CRYSTARIUM BOUTIQUE THEME** and inspect all icon corners at wide/narrow sizes and in both 3 × 6 and 4 × 6 layouts. Confirm each icon is surrounded by one continuous near-black weathered-steel piece with no seams, stacked bands, detached corner shapes, bright metal, or icon overlap.
3. Hover multiple eligible items. Confirm exactly one colored contour appears outside the complete steel frame, follows all four rounded corners, and disappears immediately when hover ends. There must be no cyan inner border, icon wash, second ring, or persistent selection.
4. Browse multiple race-, gender-, or Grand Company-restricted items. Confirm exactly one muted dark-red (`#8A2A2D`) contour appears outside the steel frame, with no red inner border or icon wash; verify the tooltip reason remains correct.
5. Open Settings > **UI OPTIONS**. Change the hovered-item highlight RGB and transparency from 0% through 100%, confirming live response in SIMPLE and Crystarium. At 100% the hover treatment should be invisible; reset and confirm Crystal Blue `#7AD6FF` at 0% transparency returns.
6. Reload the plugin and confirm the custom hover RGB/transparency persists. Recheck Crystarium frame asset availability after reload and after switching themes repeatedly.
7. Recheck item application with both mouse buttons, tooltips/default-Shift modes, restriction evaluation, paging/wheel navigation, responsive resizing, Wardrobe, Designs, GPose handoff, combat lock, normal close, command close, and unload restoration.

Report any doubled outline, inner colored ring, icon wash, bright-red Crystarium warning, malformed corner, frame/icon overlap, missing frame texture, lost hover setting, SIMPLE regression, or behavior/restoration regression.

## v0.16.0 focused native acceptance test

1. Reload the staged plugin and confirm Info reports `v0.16.0 · Run With It interface pass`. Existing installations must retain their saved theme; a genuinely new configuration must begin on **THE CRYSTARIUM BOUTIQUE THEME**.
2. Open Settings and confirm **THEMES** is first, with **SIMPLE** and **THE CRYSTARIUM BOUTIQUE THEME**; confirm the remaining renamed sections include **BOUTIQUE ITEM GRID SIZE** and **UI OPTIONS**.
3. In the Crystarium profile, confirm the Boutique grid has no black square workspace shading, icons are approximately 25% larger than v0.15.4, mural frames are approximately 25% thinner, and horizontal/vertical gaps are approximately 28% tighter while glass remains evenly visible at every edge and between rows.
4. Switch to SIMPLE and confirm its established grid geometry remains intact. In both themes, hover an eligible icon and confirm a blue glow follows only the hover; click with left and right mouse buttons and confirm application succeeds without leaving any persistent blue selection box or frame.
5. Browse race-, gender-, and Grand Company-restricted items. Confirm incompatible rows receive a persistent red frame plus soft red icon wash and their tooltip names the applicable restriction. Confirm compatible items are never falsely marked.
6. Open Crystal Wardrobe in both 2 × 6 and 6 × 2 layouts. Confirm the v0.15.4 mural wrapper is gone, the accepted earlier card geometry is restored, and its black card/workspace backing is about 60% more transparent without clipping slot names, item names, icons, or dyes.
7. Under **UI OPTIONS**, customize Wardrobe button background/text and Save button background/text independently, reload to verify persistence, then use each reset and confirm it returns to the active theme's own default.
8. Open Designs. Confirm there is no redundant top DESIGNS heading, saved-design space is redistributed, and Crystarium Rename/Duplicate/Overwrite follow the Boutique slot-button color. Hover Design Item List icons and verify the same glow and tooltip mode as Boutique; clicking must never apply equipment or publish a market-hover item.
9. Open Help and confirm every section starts collapsed, expands/collapses cleanly, lists right-click instant application before left-click, and documents Designs exchange, Eorzea import, themes, layouts, tooltips, safety, and restricted-item feedback.
10. Compare the native title, primary tabs, slot buttons, filter headings/controls, navigation, Wardrobe, Designs, Settings, Help, cards, helper copy, and numeric readouts against the requested FFXIV font roles. Change Dalamud's global font and confirm the plugin-owned typography remains unchanged.
11. Recheck responsive resizing, 3 × 6 and 4 × 6 grids, all filters, paging/wheel navigation, previous item, dyes, save/load/import/export, Eorzea import, GPose handoff, combat lock, normal close, command close, unload restoration, and both theme/transparency profiles.

Report any opaque grid square, incorrect spacing/frame reserve, persistent blue selection, false/missing restriction warning, Wardrobe clipping, nonpersistent color, interactive Design card, expanded Help section, font-role mismatch, global-font coupling, or appearance/restoration regression.

## v0.15.4 focused native acceptance test

1. Reload the staged plugin and confirm Info reports `v0.15.4 · framed Crystal Wardrobe`. Select Crystarium UI and open Crystal Wardrobe.
2. Compare a Wardrobe equipment-card frame with a Boutique item-icon frame. Confirm both use the same near-black backing, dark outer band, mid-steel body, recessed seam, worn inset edge, rounded contour, and stable patina/scuff details.
3. Confirm the black square shading is gone from the complete Wardrobe workspace, every equipment card, and every nested icon well. The uninterrupted stained-glass sheet should remain visible behind and between all 12 cards while text, icons, and dye controls stay readable.
4. Inspect both horizontal 2 × 6 and vertical 6 × 2 layouts at wide, narrow, short, and tall sizes. Confirm each frame remains inside its table cell and the added inset prevents slot names, wrapped item names, icons, and dye controls from touching or crossing the metal.
5. Select every slot from the Wardrobe and Boutique. Confirm the selected Wardrobe card receives its Crystarium outer glow, browsing changes slots normally, and Settings > Chosen item highlight color still recolors only the selected Boutique grid appearance.
6. Exercise Wardrobe item selection, Ctrl + click reset, Reset Slot, Reset All, both supported dye buttons/popups, unsupported-channel marks, tooltips, scrolling where applicable, layout persistence, GPose handoff, combat close, normal close, and unload restoration.
7. Switch to Default UI and confirm the accepted Wardrobe workspace fill, card fill/border, selected-card shading, icon region, spacing, behavior, and all saved presentation colors remain unchanged.

Report any mismatched frame layer, black card square, obscured glass, frame/content overlap, clipped glow, unreadable label, broken interaction, unwanted Wardrobe recoloring from the grid highlight setting, or Default-profile regression.

## v0.15.3 focused native acceptance test

1. Reload the staged plugin and confirm Info reports `v0.15.3 · weathered mural frames`. Select Crystarium UI.
2. Confirm each item frame is darker and visibly deeper than v0.15.2, with a near-black backing, dark outer band, mid-steel body, recessed seam, worn edge, and subtle stable patina/scuff marks.
3. Inspect every edge and rounded corner at wide and narrow sizes. Confirm the complete 18/14-pixel assembly remains outside the icon image, centered, unclipped, and contained within its tile.
4. Select multiple appearances and confirm the highlight wraps the outermost backing with a clear primary line and softer outer halo without intersecting the metal or icon.
5. Open Settings > Plugin Appearance and locate **Chosen item highlight color** immediately after the item/card border color. Change it several times and confirm only the chosen Boutique grid appearance updates in both Default and Crystarium UI.
6. Click **Reset chosen-item highlight to Crystal Blue** and confirm the value and selected outline return to `#7AD6FF`. Reload and confirm the selected custom/reset value persists.
7. Confirm the setting's information explains its exact scope and that Wardrobe selection, tabs, tooltips, structural beams, and unrelated borders do not inherit the chosen-item color.
8. Recheck all icon slots, 3 x 6 and 4 x 6 layouts, paging/wheel navigation, selection/application, tooltips, transparency, profile switching, GPose handoff, combat lock, close, and unload restoration.

Report any flat/unweathered frame, unstable markings, icon intrusion, clipped halo, incorrect setting placement, nonpersistent color, reset mismatch, unrelated recoloring, lost Default appearance, or behavior/restoration regression.

## v0.15.2 focused native acceptance test

1. Reload the staged plugin and confirm Info reports `v0.15.2 · metal beams and mural frames`. Select Crystarium UI.
2. Confirm every custom horizontal divider across Boutique, Designs, Settings, Info, and Crystal Wardrobe is approximately three times thicker than Default UI and reads as a dark steel beam with a lighter upper bevel and near-black lower edge.
3. Switch to Default UI and confirm every divider immediately returns to its original thickness and saved color; switch back to Crystarium UI.
4. Confirm every browser appearance has a substantially thicker rounded metal frame: the main steel stroke should be four times the prior `2.5`-pixel stroke, backed by a dark outer support and a narrow inset highlight.
5. Inspect all four icon edges and corners. Confirm no metal stroke overlaps, masks, or clips the item image and each frame remains centered around its actual icon at wide and narrow window sizes.
6. Confirm the old grey square tile background/border is absent and continuous blue glass remains visible around each framed icon.
7. Select several items and confirm selection is shown by a restrained outer frame glow without restoring the square tile shading. Recheck hover tooltips and item application.
8. Recheck 3 x 6 and 4 x 6 layouts, all slots, wheel/page navigation, previous item, Wardrobe, Designs, transparency, profile switching, GPose handoff, combat lock, close, and unload restoration.

Report any thin or luminous beam, unchanged frame thickness, frame intrusion, clipped icon, grey tile square, missing selection feedback, layout overflow, lost Default styling, or behavior/restoration regression.

## v0.15.1 focused native acceptance test

1. Reload the staged plugin and confirm Info reports `v0.15.1 · stained-glass and dark-metal UI`. Select Crystarium UI under Settings.
2. Confirm the Boutique background is only one uninterrupted mottled blue-glass sheet. It must contain no baked-in arches, frames, columns, mullions, panel divisions, straight lines, ornamental border, margin, or white space.
3. Open Crystal Wardrobe and Generate List and confirm the same frameless glass fills each window independently.
4. Confirm tabs, work areas, buttons, filter fields, popups, scrollbars, horizontal dividers, item borders, Wardrobe cards, saved Design cards, and the outer window edge read as raised dark graphite metal placed over the glass.
5. Confirm the glass remains visible between and beneath UI elements while all labels, item icons, rarity colors, tooltips, dyes, and state indicators remain readable.
6. Confirm cyan is limited to restrained hover/selection/check/slider feedback rather than appearing as the default metal border.
7. Resize all three windows through wide and narrow shapes. Confirm the glass cover-crops without stretching, seams, margins, or repeated patterns and the metal elements remain aligned.
8. Recheck whole-window and background-only transparency, then switch to Default UI and confirm the accepted Default appearance and saved presentation settings are still exact.
9. Recheck browsing, filters, paging, Wardrobe, Designs, dyes, GPose handoff, combat lock, command close, normal close, and unload restoration in both profiles.

Report any baked-in architecture, luminous default border, insufficient glass visibility, floating/unframed control, unreadable content, crop distortion, missing auxiliary-window background, lost Default setting, or behavior/restoration regression.

## v0.15.1 native result

Passed on 2026-08-30.

## v0.15.0 focused native acceptance test

1. Reload the staged plugin and confirm Info reports `v0.15.0 · Crystarium UI profiles`. Open Settings > **UI: DEFAULT & CRYSTARIUM BOUTIQUE PROJECT** and confirm equal **Default UI** and **Crystarium UI** buttons with one green active checkmark.
2. Start on Default UI and compare the main Boutique, Crystal Wardrobe, generated Design List, tabs, filters, cards, pagination, and settings with the accepted v0.14 appearance. Confirm no Default color, spacing, control, icon, tooltip, transparency, or layout regression.
3. Record several non-default presentation values, including custom background, item border, horizontal bar, Boutique button/text, filter background/text, whole-window transparency, background-only transparency, tooltip mode, and grid/Wardrobe layouts.
4. Select Crystarium UI. Confirm the main Boutique immediately gains the original blue stained-glass backdrop, dark graphite framing, cyan structural borders, cool-glass buttons/fields/tabs, pale-blue typography, and themed scrollbars without obscuring icons or text.
5. Open Crystal Wardrobe and Generate List. Confirm both independently movable/resizable windows use the same stained-glass background and palette, Wardrobe and saved Design cards carry the tighter luminous architectural framing, and all content remains readable.
6. Resize every themed window from wide landscape through its supported narrow/tall sizes. Confirm the background uses centered cover-cropping without stretching, empty bands, white margins, tile seams, or misplaced borders.
7. Exercise all 12 slot buttons, filters/popups, search/clear, Designs selector, Save, Crystal Wardrobe, paging/wheel navigation, previous item, dye buttons/popups, Settings controls, and Design actions. Confirm the profile changes presentation only and preserves all behavior.
8. Adjust whole-window and background-only transparency while Crystarium UI is active. Confirm the stained glass responds to background transparency, controls remain legible, and the Settings tab remains recoverable at allowed limits.
9. While Crystarium UI is active, revisit Plugin Appearance and confirm its notice explains that the shown color controls belong to Default UI. Switch back to Default UI and verify every value recorded in step 3 returns exactly; switch between profiles several times to confirm neither overwrites the other.
10. Leave Crystarium UI selected, reload the plugin, and confirm the profile persists. Repeat with Default UI selected. Confirm schema migration starts existing v0.14 configurations on Default UI.
11. Recheck normal preview/reset, linked weapons, dyes, saved/imported Designs, Eorzea Collection import, GPose handoff, combat lock, command close, normal close, and unload restoration in both profiles.
12. Browse rapidly and resize each window while watching Info diagnostics. Report any sustained frame-time regression, repeated asset load, texture flicker, clipping, or visual draw-order problem.

Report any Default-profile visual change, lost presentation setting, incorrect active checkmark, missing background, distorted crop, unreadable content, unthemed auxiliary window, control behavior change, persistence failure, or appearance-restoration regression.

## v0.14.0 focused native acceptance test

1. Reload the staged plugin and confirm Info reports `v0.14.0 · Design polish and UI customization`. Confirm `/CB` and `/Boutique` open the Boutique when closed and close/restore it when entered again while open; retained `/cboutique` remains an open command.
2. Change multiple slots before command-closing. Confirm the main window, Crystal Wardrobe, and generated list close and the exact session-opening appearance returns. Reopen and confirm a fresh session. Repeat in GPose once.
3. Confirm Ctrl + Shift + Delete no longer resets anything in the Boutique or Wardrobe and is absent from Help. Confirm visible Reset All and Ctrl + click per-slot reset still work.
4. Open the Boutique Designs dropdown and confirm **Eorzea Collection Import from URL** is Antique Gold. Import one valid public glamour and recheck preview, linked weapons/dyes, saving, reset, and close restoration.
5. Open Designs and confirm Import/Export are Terracotta `#996D57`, Confirm Delete is Clear Red, Cancel Action is Steel Blue, and Rename/Duplicate Confirm name/Cancel edit are Dusty Rose. Exercise each successful and cancelled path.
6. Hover Export and confirm its tooltip explains that clicking copies the Design string for sharing. Click Export and confirm a visible clipboard-success message appears; paste/import the resulting `TCB-DESIGN-1` string and verify the imported copy.
7. Confirm the Design toolbar has the same three-pixel structural bar as Boutique. In Saved Outfit Info, confirm one line reads `Created | MM/dd/yyyy << | >> Updated | MM/dd/yyyy` and NOTES is enclosed by light-grey vertical/horizontal borders.
8. Confirm DESIGN ITEM LIST has matching structural bars above and below its heading and every read-only visual card has Warm Charcoal `#4D484B` background without becoming interactive.
9. Under Settings > Item Tooltip, confirm Default tooltips and Hidden tooltips buttons appear and the active choice carries a green drawn checkmark. In Default mode, browser, Wardrobe-card, and generated-list item tooltips appear normally.
10. Select Hidden tooltips, reload, and confirm the choice persists. Item tooltips must remain hidden until Shift is held while hovering, appear immediately while Shift remains held, and disappear when Shift is released. Non-item instructional controls such as Export and previous-item help must remain available normally.
11. Under Plugin Appearance, confirm a configurable structural rule appears below every option/description. Change the horizontal-bar color and confirm all structural bars update across tabs, Boutique, Designs, Info, Settings, and Crystal Wardrobe; reset to Steel Blue.
12. Change Boutique button background and font colors. Confirm all 12 slot buttons, First/Previous/Next/Last, Page text, and previous-item button respond. Confirm Crystal Wardrobe, Save, Search Clear, and Design action colors do not change; reset both colors.
13. Change filter background and font colors. Confirm Expansion, Role, Dye Slots, Search, and Designs fields update, including selected fitted text, popup choices, and search hint; reset to Warm Charcoal/White.
14. Confirm the bottom Settings section is named **UI: DEFAULT & CRYSTARIUM BOUTIQUE PROJECT**, contains the future custom-art work-in-progress notice, and makes no runtime UI switch yet.
15. Reload once more and verify every v0.14 setting persists. Recheck responsive resizing, paging/wheel navigation, previous-item history, saved Designs, combat lock, GPose handoff, normal close, and unload restoration.

Report command cleanup failure, unexpected shortcut reset, incorrect assigned color, missing clipboard notice, date/notes/list framing error, tooltip-mode leak, lost color persistence, excluded-control recoloring, or any appearance-restoration regression.

## v0.13.2 native result

Passed on 2026-08-30.

## v0.13.2 focused native acceptance test

1. Reload the staged local dev plugin and confirm Info reports `v0.13.2 · balanced footer controls`.
2. At a wide Boutique size, confirm Page/First/Previous/Next/Last are 30% smaller than v0.13.1 and the previous-item control is 15% smaller, without changing their labels, colors, images, or behavior.
3. Confirm the pagination row and previous-item control share the same vertical center between the steel-blue horizontal rule and the bottom of the Boutique content area.
4. Confirm the empty space above, to the right of, and below the previous-item control is visually equal, forming a consistent three-sided frame.
5. Narrow and widen the Boutique from every edge. Confirm pagination and the previous-item control continue scaling together without collision, pagination remains horizontally centered, and the equal top/right/bottom frame remains intact.
6. Recheck First/Previous/Next/Last, wheel paging, disabled end states, previous-item restoration, linked weapons, GPose handoff, combat close, window close, and plugin unload restoration.

Report any incorrect scale, footer drift, unequal revert inset, navigation/revert overlap, clipped control, paging failure, or restoration regression.

## v0.13.1 native result

Passed on 2026-08-30.

## v0.13.1 focused native acceptance test

1. Reload the staged local dev plugin and confirm Info reports `v0.13.1 · coordinated responsive controls`. Confirm `/CB`, `/Boutique`, and `/cboutique` open the same window and `/TCB` is no longer registered.
2. Narrow and widen the Boutique repeatedly. Confirm all 12 slot glyphs retain their current size and left-hand anchor while only the adjacent slot label shrinks as needed; no label may draw through a glyph or outside its button.
3. Select the longest Role value and other long Expansion, Dye Slots, and Designs values at a narrow width. Confirm only the selected field preview shrinks. Open each dropdown and confirm every popup choice uses the normal menu font size rather than inheriting the fitted preview size.
4. At the same narrow widths, confirm Crystal Wardrobe and Save contract with the filter row, keep their readable fitted labels/glyph, and never overlap Role, Dye Slots, Search, Clear, or Designs.
5. Confirm Page text and First/Previous/Next/Last are approximately 1.5× their former size at a wide window. Narrow and widen the window and confirm all pagination controls and the previous-item icon scale together without overlap, navigation remains centered, and the icon is evenly spaced from the steel-blue rule and the right/bottom edges.
6. Open Settings and collapse every section. Confirm no backward question-mark or unsupported arrow glyph appears. Recheck slot/filter selection, every paging route, wheel paging, previous-item restoration, Save/load, GPose handoff, combat close, window close, and plugin unload restoration.

Report any stale command, slot-icon drift, label/glyph collision, tiny popup entry, action/filter overlap, footer collision, uneven revert inset, unsupported Settings glyph, or appearance-restoration regression.

## v0.13.0 native result

Passed on 2026-08-30.

## v0.13.0 focused native acceptance test

1. Reload the staged local dev plugin and confirm Info reports `v0.13.0 · responsive controls and visual design cards`. Confirm the primary tab order is exactly Crystarium Boutique, Designs, Settings, Help, Info and that there is no separate Status or Debug tab.
2. In 3 × 6 and 4 × 6 Boutique layouts, inspect item borders at several window sizes. Both dark and configured-color strokes must follow each icon's rounded corners without projecting outside the icon image.
3. Narrow the Boutique until Expansion, Role, Dye Slots, and Designs would previously clip. Confirm each selected value shrinks only as needed, remains fully readable inside its field, and returns to normal size when widened. Open every dropdown and confirm its menu entries remain normal-sized and selectable.
4. Open Crystal Wardrobe, choose dyes with long names, and narrow both orientations. Confirm each active dye name shrinks to remain fully visible without changing button height, dye color, channel selection, or card behavior.
5. Confirm the previous-item control is approximately 35% smaller than v0.12, remains evenly inset at bottom-right, and no excess footer gap remains. Page controls must stay centered and wheel/button paging plus complete linked-item history must remain functional.
6. Open Designs and confirm the actions are exactly two rows: Save/Rename/Duplicate/Import, then Load/Overwrite/Delete/Export. Save must be eucalyptus, Load steel blue, Delete clear red, and the other five dusty rose. Exercise every action and both confirmation paths.
7. Confirm Generate List is absent from the top toolbar and remains visible at bottom-right while selecting designs and scrolling details. It must still open the separate movable/resizable reference window with working market-hover publication.
8. In Saved Outfit Info, confirm the selected name has a dusty-rose divider, metadata is limited to `Created | DD/MM/YYYY`, `Updated | DD/MM/YYYY`, and optional `NOTES | player text`, with no schema or timestamp text.
9. Confirm **DESIGN ITEM LIST** has a steel-blue divider and exactly two rows of six read-only cards in Boutique slot order. Saved cards must show slot, item icon/name, and chosen dye names/colors; omitted slots must be visibly empty. Clicking or hovering these embedded cards must never load a design, change equipment/dyes, publish a market item, or alter selection.
10. Open Info and confirm plugin/runtime and technical status remain at the top. Beneath their steel-blue divider, confirm all former Debug data is present and read-only. Recheck tab switching, Save/load, GPose handoff, combat lock, reset, close, and unload restoration.

Report any protruding border, clipped responsive label, resized field height, footer imbalance, wrong action order/color, missing/floating Generate List, stale metadata, interactive visual card, missing diagnostics, or appearance-restoration regression.

## v0.12.0 focused native acceptance test

1. Reload the staged local dev plugin and confirm Status reports `v0.12.0 · steel-blue Boutique and Crystal Wardrobe`. Resize the Boutique from every edge and confirm no side scrollbar appears and the 3 × 6 / 4 × 6 grids, wheel paging, and selection remain functional.
2. Confirm the active primary tab and the line beneath the tabs use Steel Blue `#578899`. Confirm the separators below the filters and below the item grid use the same color and are approximately three times the former thickness.
3. Confirm every filter field uses Warm Charcoal `#4D484B`; the header is **Role**, its aggregate choice is **All**, and the Designs selector reads **No Design**. Recheck every filter, partial search, and Clear behavior.
4. Confirm filter/action buttons use rounded corners. Save must use Eucalyptus `#579983` with white text. Previous/Next must use Dusty Rose, be centered with page text, and sit below the grid separator with visible breathing room.
5. Confirm the control formerly named Boutique Wardrobe is now **Crystal Wardrobe**, uses Steel Blue, has rounded corners, shows the new white dress-form glyph without clipping, and explains itself on hover.
6. Open Crystal Wardrobe and confirm all four toolbar buttons are rounded and a thick steel-blue separator spans below them. Resize in both orientations and confirm card borders remain responsive and aligned.
7. Confirm every Boutique item icon and every Crystal Wardrobe card has a visible Antique Gold `#998C57` border. Change the border under Settings > Plugin Appearance, verify both surfaces update together, reset it to Antique Gold, reload, and verify persistence.
8. Confirm each Wardrobe card has a Dusty Rose separator beneath the slot/item heading. Test no-, one-, and two-dye-slot items and confirm unsupported channels use the exact small supplied red-X artwork while supported dye controls and colors still work.
9. Confirm the previous-item image is roughly three to four times the prior size, does not overlap page controls, and has spacing around it. Recheck empty history, single-slot history, linked-weapon history, dye-history exclusion, and transaction rollback behavior.
10. Recheck Save/design loading, Crystal Wardrobe close/reopen, normal close, combat lock, GPose entry/exit, Reset Slot, Reset All, and plugin unload. Every lifecycle route must retain the accepted v0.11 behavior and restore game state when required.

Report any scrollbar, old label/color, thin/incomplete separator, square control, glyph/X clipping, missing/nonresponsive border, lost setting, grid/footer overlap, paging regression, or appearance-restoration leak.

Native result: passed on 2026-08-29.

## v0.11.0 focused native acceptance test

1. Reload the staged local dev plugin and confirm Status reports `v0.11.0 · slot glyphs and previous-item restore`.
2. Confirm every dusty-rose slot button has a readable white glyph: crossed swords for Main Hand; helmet, torso, glove, pants, and boots for armor; shield for Off Hand; and earring, necklace, bracelet, and rings for accessories. Verify the text remains readable and every button still selects its correct slot.
3. On any slot with at least two pages, move beyond page 1, select an item, open its Wardrobe dye popup, and apply a dye. Confirm the Boutique stays on the exact same page with the same expansion and filters instead of returning to page 1. Repeat after moving/resizing both windows.
4. Select item A and then item B. Click the supplied dusty-rose New Game+ button at the bottom-right and confirm the character and Wardrobe return to item A. Click it again and confirm the slot returns to its session-opening appearance.
5. Select item A, item B, then apply one or two dyes to B. Click previous once and confirm it returns directly to A; dye-only edits must not add extra previous-item steps.
6. Repeat with a linked Main Hand item and a standalone replacement. Confirm previous-item restores the complete matching main/off-hand pair and never leaves a stale sheath or secondary model.
7. Confirm the previous-item button is disabled before any Boutique item selection and after all available steps are consumed. Recheck close, unload, combat lock, and GPose transition restoration.

Report any missing/misaligned glyph, clipped slot text, page/filter jump after dye application, undo that records dyes, incomplete linked-weapon restore, enabled empty-history button, or restoration regression.

Native result: passed on 2026-08-29.

## v0.10.1 focused native acceptance test

1. Reload the staged local dev plugin and confirm Status reports `v0.10.1 · responsive wardrobe and help page`.
2. On the Boutique tab, confirm the slot filter is exactly 2 × 6: Main Hand, Head, Body, Hands, Legs, Feet; then Off Hand, Earrings, Necklace, Bracelets, Right Ring, Left Ring. Resize the main window horizontally and confirm the labels no longer clip.
3. Confirm **Boutique Wardrobe** is fully readable above Save, both controls share the same right edge, and the Wardrobe button extends left toward Designs. Confirm the horizontal separator below the filter row always spans the available width while resizing.
4. Open the Wardrobe and confirm its toolbar is 2 × 2: Horizontal/Vertical followed by Reset chosen slot/Reset all slots. Resize from both left and right edges and confirm the toolbar no longer imposes the former wide minimum.
5. In horizontal layout, resize the Wardrobe vertically and confirm both card rows shrink/expand with the available height. In vertical layout, confirm all six row heights respond too; scrolling should appear only after the 120-pixel-per-row readability floor is reached.
6. Apply common and higher-rarity items through direct browsing, a saved design, and an Eorzea import. Confirm each Wardrobe item name uses the same in-game rarity color as its Boutique tooltip instead of always green.
7. Confirm supported item-card buttons read **Dye 1** and **Dye 2**, existing stain names/colors still replace those labels after selection, and unavailable channels retain the accepted small centered red X.
8. Open **Help** as a normal tab. Confirm its controls/keybinds, saved-design exchange, and Eorzea Collection instructions are separately headed, readable, and vertically scrollable.
9. Under Settings > Plugin Appearance, enable the custom background color, choose a visibly different RGB color, and confirm the Boutique, detached Wardrobe, and generated design-list windows update. Confirm whole-window and background-only transparency still work independently, then disable the custom color and verify normal Dalamud styling returns.
10. Recheck the already-passed keybindings, paging, filter/Mogstation results, tooltips, GPose handoff, reset behavior, main close, combat close, and plugin unload restoration.

Report any clipped slot/action label, incorrect row order, incomplete divider, fixed-height Wardrobe row, excess horizontal minimum, green-only card name, old dye label, blank Help page, background setting that affects text/icons, or restoration regression.

## v0.10.0 focused native acceptance test

1. Reload the staged local dev plugin. Confirm Dalamud shows the new square blue-violet crystalline Boutique icon instead of `?`, and Status reports `v0.10.0 · detachable wardrobe and in-plugin help`.
2. Confirm the main Boutique contains 12 evenly spaced dusty-rose slot buttons above the filters and no equipment cards. Exercise all buttons and confirm the browser changes to the corresponding slot.
3. Confirm the red **Boutique Wardrobe** button is beside Designs and above the neon-pink **Save** button. Toggle it repeatedly; the separate Wardrobe must be movable/resizable and closing only that window must not close the session.
4. Confirm the default Wardrobe is horizontal 2 × 6 with the accepted v0.9.2 order and functionality. Switch to vertical and confirm six paired rows: Main/Off Hand, Head/Earrings, Body/Necklace, Hands/Bracelets, Legs/Feet, Left/Right Ring. Reload the plugin and confirm the last orientation persists.
5. Recheck wrapped names, high-resolution icons, one/two-channel dye selection, linked-weapon synchronization, and stain-colored labels. Unsupported dye buttons must show only a small centered red X rather than a large diagonal cross.
6. Change several slots. Ctrl + left click one corresponding Wardrobe card and confirm only that slot resets; then press Ctrl + Shift + Delete while either Boutique window is focused and confirm all changed slots reset to the session opening appearance. Repeat one reset in GPose and verify transition/restoration behavior remains accepted.
7. Open the trailing **Help** button. Confirm it overlays the current tab without selecting a new page and exposes three right-arrow submenus for controls/keybinds, saved-design sharing, and Eorzea Collection imports.
8. Open Expansion and confirm the first/default label is **All**, its results include at least one known Mogstation item, and the separate **Mogstation** group remains usable.
9. Hover common and higher-rarity items. Confirm only the item name is underlined and its color matches the in-game rarity; the equipment category must not be underlined. A store item must show orange `Mogstation exclusive - Online Store` text.
10. Recheck Save, design load/import/export, wheel and button paging, main-window close, combat close, GPose handoff, and plugin unload. Closing/restoring must also close the Wardrobe and leave no equipment or linked weapon state behind.

Report icon failure, layout/order/persistence error, card regression, a shortcut resetting the wrong state, Help replacing a tab, Mogstation missing from All, wrong tooltip line/color, or any restoration leak.

Native result: partially passed on 2026-08-28. Icon, keybindings, GPose, Wardrobe visibility/orientation/reset controls, every slot selector, All/Mogstation aggregation, tooltips, and the small unavailable-dye X worked. Testing identified the 1 × 12 selector/action clipping, fixed Wardrobe row heights, 1 × 4 toolbar minimum, green-only card names, old dye labels, nonfunctional Help overlay, and requested background-color control addressed by v0.10.1.

## v0.9.2 focused native acceptance test

1. Reload the staged local dev plugin and confirm Status reports `v0.9.2 · enlarged equipment cards and integrated dye slots`.
2. Confirm the accepted 2 × 6 order remains intact and every card is approximately 1.5× taller than v0.9.1. Resize/move the window and confirm both rows remain even.
3. Preview several unusually long item names. Every complete name must wrap above the icon, remain readable, and never be covered by the icon. Confirm each icon is centered with visible space between it and the bottom controls.
4. On a nondyeable item, confirm both bottom buttons read **Dye slot 1/2**, are disabled, and carry clear red X overlays. On a one-slot item, confirm only the second button has the red X. On a two-slot item, confirm neither button has an X.
5. Click every supported dye-slot button. Confirm the local dye popup opens for the correct card/channel; exercise name search, color groups, recent colors, clear, and one metallic dye.
6. Choose visibly different dyes for channels 1 and 2. Confirm each applies immediately, preserves the other channel, and changes only its own button to the exact swatch color and dye name. Dark colors should use readable light text and light colors readable dark text.
7. Test a linked weapon dye and confirm its matching main/off-hand component remains synchronized. Confirm a manual dye change changes the Designs selector to **No Custom Design**, then save and reload the dyed custom design.
8. Recheck card slot navigation away from the dye buttons, grid paging, GPose carryover, close restoration, and unload restoration.

Report any clipped/covered name, card overflow, missing bottom space, wrong red-X state, dye popup/channel mismatch, wrong color/name, one channel overwriting the other, linked-weapon mismatch, or restoration regression.

Native result: passed on 2026-08-28. Enlarged card geometry, wrapped names, integrated dye-slot controls, and the accepted v0.9.2 behavior worked as requested.

## v0.9.1 focused native acceptance test

1. Reload the staged local dev plugin and confirm Status reports `v0.9.1 · two-row equipment cards and reliable design saving`.
2. Confirm the equipment cards form exactly two rows of six. Row one must be Main Hand, Head, Body, Hands, Legs, Feet; row two must be Off Hand, Earrings, Necklace, Bracelets, Right Ring, Left Ring.
3. Confirm both rows use equal card widths/heights, item names wrap without hiding the icon area, changed-item icons are larger than the prior 1 × 12 arrangement, and every card still selects its equipment slot.
4. Select at least two Boutique items, click the final **Save** button, and confirm **SAVE CURRENT DESIGN** opens immediately. Enter a custom name, optionally enter notes, and save it.
5. Confirm the new custom name becomes the active Designs selector value and appears in the Designs tab. Load another state, reselect the new design, and verify its saved items apply correctly.
6. Recheck the Eorzea import popup, 3 × 6 and 4 × 6 grids, mouse-wheel paging, close restoration, and unload restoration for focused regressions.

Report incorrect card order, uneven/clipped cards, Save still doing nothing, a name dialog that cannot save, a missing new design, or any preview/restoration regression.

## v0.9.0 native acceptance test

1. Reload the staged local dev plugin, open it with `/TCB` or `/Boutique`, and confirm Status reports `v0.9.0 · redesigned Boutique and Eorzea imports` with Glamourer available.
2. Confirm the default tab is **Crystarium Boutique**. All 12 evenly sized equipment cards must appear immediately below the tabs, span the window, wrap slot/current-item text, show large icons for changed items, highlight the active slot, and switch slots when clicked.
3. Confirm the old bottom Designs/Outfit Info/Dye Studio area is absent. The equipment-slot, minimum/maximum level, and minimum/maximum item-level controls must also be absent.
4. Confirm the single row order is **Expansion**, **Class / Role**, **Dye Slots**, **Search**, red square **X**, **Designs**, **Save**. Search must show the `Item` hint; the X must clear search/dye/class filters while retaining the active equipment slot and expansion.
5. Open Expansion and confirm entries contain names only, **All Expansions** remains first/default, and the cash-shop entry is exactly **Mogstation**. Recheck newest-first results on one expansion and All Expansions.
6. In Settings, confirm Layout offers only `3 x 6` and `4 x 6`; both render the expected 18/24 items, centered pagination, page buttons, and grid-local mouse-wheel paging after window movement/resizing.
7. In Designs, confirm **No Custom Design** is the normal label, choose an existing saved design and verify it applies, then change one item and confirm the label returns to **No Custom Design**. Use the final **Save** button with a custom name and confirm the new name appears in both the selector and Designs tab.
8. Start an active session, choose **Eorzea Collection Import from URL**, and submit a public `https://ffxiv.eorzeacollection.com/glamour/...` link. Confirm importing is explicit, the applied selector label becomes exactly **Eorza Col.**, resolved equipment/dyes appear on the cards and character, and any omitted armor/accessory slots use their corresponding Emperor's New item.
9. Modify one imported item, save under a custom name, apply another design, then reload the saved import. Confirm the customized equipment/dyes replay and the original source URL was never opened automatically or sent anywhere except the submitted read-only Eorzea request.
10. If the imported glamour contains a linked main/off-hand weapon, confirm its matching secondary component applies and restores with the main item. Otherwise perform the accepted linked-weapon regression with a local Boutique item.
11. Recheck continuous GPose behavior: carry a normal-world selection into GPose, change one slot there, exit, and confirm the latest state carries back. Then confirm No Custom Design/reset orchestration, window close, combat close, and plugin unload still restore game state without a persistent weapon component.
12. Confirm the local Dalamud log records v0.9.0 initialization, no missing Luna/Microsoft assembly, clean disposal, and no Boutique exception. Note any failed/unresolved Eorzea item or dye name exactly as reported.

Report card/filter clipping, wrong row order, a five-column layout, stale design labels, an import URL rejected incorrectly, an unresolved item/dye that exists locally, incorrect Emperor normalization, linked-weapon mismatch, page/wheel regression, GPose regression, or restoration leak. Do not test or authorize any remote publication; the only network action in scope is a user-submitted read-only Eorzea glamour import.

## v0.1 native result

Passed on 2026-08-25. Local logs confirmed successful initialization, two clean unload/reload cycles, expected dependency-unavailable warnings, and no Boutique exceptions.

## v0.2 native result

Passed on 2026-08-25. Native runs verified 52,801 examined Item rows, 28,962 catalog items, 23,731 indexed appearances, real icons, exact 6 × 3 geometry, button paging, reliable wheel paging, icon-sized 50%-transparent overlays before and after window movement, selection-only behavior, and clean unload/reload. Catalog construction measured 69.9–98.1 ms.

## v0.3 native result

Passed on 2026-08-25. API 1.8 discovery, original capture, repeated weapon/equipment previews, exact restoration, hover/paging regressions, plugin reload, and clean unload all worked in normal-world testing. The final local log contained clean initialization/disposal and no post-fix Boutique error. GPose was excluded; the later superseding product decision removed it permanently from plugin scope.

The completed procedure was:

1. Run `.\scripts\Build.ps1` and confirm the build/test summary passes.
2. Ensure Glamourer is enabled, then reload the staged dev plugin and run `/cboutique`.
3. Confirm the rail shows `Glamourer: Available`, `Glamourer API: 1.8`, and `Session: Active`.
4. Record the character's original appearance. Select a visible equipment slot and click three different tiles in succession. Each click must update that slot immediately, keep the original snapshot intact, and show `Previewing: <item name>` without an error.
5. Repeat step 4 for one weapon slot and one armor/accessory slot. Clicking the same tile twice is valid and must not produce an error.
6. Recheck the v0.2 regression points: move/resize the window, hover overlays, page buttons, and at least 15 wheel-page actions.
7. Close the Boutique with its window close button. The entire original appearance must return, including every previewed slot.
8. Reopen the Boutique, preview another item, then disable/reload the Boutique plugin while the preview is visible. The original appearance must return during unload.
9. Confirm the local Dalamud log contains API 1.8 detection, clean initialization/disposal, and no Crystarium Boutique exception.

Report any item/slot that fails, whether close and unload both restore exactly, and any visible status/error. Do not test dyes, loadouts, Penumbra collection changes, or GPose in v0.3.

## v0.4 initial native result

Partially passed on 2026-08-25. Search, dye/level/item-level filters, Clear, tooltip transparency, close restoration, and unload restoration worked. Query time was approximately 0.006 ms. The run exposed clipped filter/rail text, an inaccessible maximum-item-level control, overly large text on long overlays, a head-slot redraw failure after the AFK camera transition, and intermittent FPS drops from roughly 120 to 80–85 while browsing.

The focused v0.4.1 head-redraw regression subsequently passed: after several AFK camera transitions, head equipment continued to preview immediately without toggling visor or hat visibility.

## v0.4.1 revised native acceptance test

1. Run `.\scripts\Build.ps1` and confirm the build and all 46 tests pass, then reload the staged dev plugin and run `/cboutique`.
2. Confirm the brand reads `v0.4.1 · browser refinement`, the wider rail shows full group/status text, and all six labeled filter columns are visible. Numeric fields should be blank with an **Any** hint; no unexplained zero should appear.
3. Confirm the grid renders 6 columns × 4 rows / 24 positions. Exercise direct group selection, both **Group** arrows, buttons, and at least 15 grid-local wheel page actions.
4. Confirm **All Expansions** is last and combines multiple expansion eras. Confirm **Mogstation (Online Store)** is separate, populated, excluded from All Expansions, and several item overlays contain the Mogstation purchase note.
5. Exercise Tank, Healer, Melee DPS, Physical Ranged DPS, Caster / Magical Ranged DPS, Crafter, and Gatherer filters on appropriate slots. Universal cosmetic gear may correctly appear under multiple roles.
6. Recheck partial/reordered text search, minimum/maximum level, minimum/maximum item level, exact No/Single/Two dye-slot choices, combined filters, and **Clear**.
7. Hover several unusually long item names at the default 75% tooltip font setting. Test the font slider at 50%, 75%, 100%, and 150% and the background slider at 0%, 50%, and 100%; reload and confirm both last values persist.
8. Select and preview three head items. Allow/reproduce the AFK camera pan, return without toggling visor or hat visibility, then select three more head items. Every selection must redraw immediately. Report any status/error if it does not.
9. Browse at least ten dense pages while watching FPS and technical status. Query time must remain below 50 ms. If FPS drops, capture the simultaneous **UI draw** average and peak values; this distinguishes Boutique draw work from asynchronous game texture loading.
10. Preview several weapon and armor items, then verify close restoration and unload restoration still return exactly to game state.
11. Confirm the local Dalamud log records v0.4.1 initialization, local FittingShop counts, clean disposal, and no Boutique exception.

Report any incorrect role/store classification, clipped control, missing 24th tile, long-overlay clipping at 75%, head redraw loss, query at or above 50 ms, UI draw spike correlated with FPS loss, or restoration regression. Dye application, loadouts, Penumbra collection changes, and GPose remain outside v0.4.1.

## v0.4.2 revised native acceptance test

1. Run `.\scripts\Build.ps1` and confirm the build and all 50 tests pass, then reload the staged dev plugin and run `/cboutique`.
2. Confirm the brand reads `v0.4.2 · browser navigation`. The left rail should contain brand, status, and technical diagnostics only. Confirm the workspace shows **Expansion**, **Equipment Slot**, **Search**, **Clear**, and **Tooltip** above **Dye Support**, **Class / Role**, **Min Level**, **Max Level**, **Min iLvl**, and **Max iLvl** with no clipping at the normal window size.
3. Confirm **All Expansions** is the first expansion choice and is selected on initial open. Select several other expansions, then change equipment slots; every slot change must return to **All Expansions** rather than A Realm Reborn.
4. Open the **Tooltip** dropdown. Confirm background transparency and font size remain adjustable, close the dropdown, reload the plugin, and confirm their last values persist.
5. On the character's current combat job, select **Main Hand** and confirm only weapons usable by that active class/job appear. If the job supports an off-hand item, repeat for **Off Hand**. Change to a materially different job while the Boutique remains open and confirm weapon results refresh automatically without pressing Clear or reopening the window. Zero Off Hand results are valid for jobs without an off-hand appearance.
6. Confirm the automatic weapon restriction does not narrow Head, Body, Hands, Legs, Feet, or accessories. Exercise the manual Class / Role selector independently, then press **Clear** and confirm only manual search/filter state resets while current-job weapon compatibility remains enforced.
7. Recheck 6 columns × 4 rows / 24 positions, previous/next buttons, and at least 15 grid-local wheel page actions. Check multiple expansion and Mogstation choices, including Mogstation purchase notes.
8. Preview several weapons, armor items, and three head items. Recheck an AFK camera return, then verify close restoration and plugin-unload restoration return exactly to game state.
9. Browse at least ten dense pages while watching FPS and technical status. Query time must remain below 50 ms. If FPS drops, capture simultaneous UI average/peak timing.
10. Confirm the local Dalamud log records v0.4.2 initialization, local FittingShop counts, clean disposal, and no Boutique exception.

Report any incompatible weapon, missing compatible weapon, stale results after a job change, dropdown/control clipping, incorrect default expansion, tooltip-setting regression, paging failure, redraw/restoration regression, or correlated performance spike. Dye application, loadouts, Penumbra collection changes, and GPose remain outside v0.4.2.

Native result: passed on 2026-08-25. All Expansions defaulting, the reorganized controls, active-class weapon filtering, paging, previews, restoration, and repeated AFK head behavior worked. The only requested follow-up was reducing the Tooltip settings popup text to prevent its long background-transparency label from clipping.

## v0.4.3 focused native acceptance test

1. Reload the staged dev plugin and confirm the brand reads `v0.4.3 · tooltip polish`.
2. Open **Tooltip** and confirm its popup labels and slider values render at the smaller 75% scale without clipping.
3. Move the transparency and item-overlay font sliders and confirm both still work; the item-overlay font should continue following its own slider rather than the popup's fixed presentation scale.

Report any remaining popup clipping or slider regression. No broader v0.4.2 regression rerun is required for this isolated presentation change.

Native result: passed on 2026-08-25. The smaller Tooltip settings popup rendered without the reported clipping and the focused v0.4.3 gate closed.

## v0.5.0 native acceptance test

1. Run `.\scripts\Build.ps1` and confirm the build and all 63 tests pass, then reload the staged dev plugin and run `/cboutique`.
2. Confirm the brand reads `v0.5.0 · dye studio`, the log reports 125 named dyes, and technical status shows `Dyes: 125` without a catalog error.
3. Select a nondyeable item and confirm the rail says it has no dye slots. Select a one-slot dyeable item and confirm one dye-slot button appears. Select a two-slot item and confirm two buttons appear.
4. Open a channel. Confirm the popup displays colored swatches grouped into the expected color families; hover several standard and metallic swatches and verify name, dye ID, and metallic label where applicable.
5. Search by partial dye name, select individual color groups, return to **All color groups**, and confirm the swatch results respond without page/FPS stalls.
6. Apply three different dyes to channel 1. Every click must update the character immediately and add/move the dye in **Recent**. Click a Recent swatch again and confirm it reapplies without an exception.
7. On a two-channel item, set visibly different dyes on channels 1 and 2. Change channel 1 again and confirm channel 2 remains unchanged; then change channel 2 and confirm channel 1 remains unchanged.
8. Use **Clear channel** on each channel independently and confirm only that channel returns to no dye.
9. Select a different item in the same slot and confirm its Boutique dye state starts clear and its own channel count replaces the previous controls.
10. Close the Boutique while dyes are visible and confirm the exact original appearance returns. Reopen, preview/dye again, disable or reload the plugin, and confirm unload also restores the original appearance.
11. Recheck one head-item dye after an AFK camera return and confirm the accepted head redraw behavior remains intact.
12. Confirm the local log records Glamourer API 1.8, the item and stain catalog counts, v0.5 initialization, clean disposal, and no Boutique exception.

Report any incorrect/missing swatch color or name, wrong channel count, one channel overwriting the other, failed clear/reset, Recent-row exception, delayed mutation, restoration regression, clipping, or visible error/status text. Loadouts, full outfit-context/reset, Penumbra collection changes, and GPose remain outside v0.5.

Native result: passed on 2026-08-25. All Dye Studio controls, one- and two-channel application, recent/clear behavior, AFK head behavior, and exact close/unload restoration worked as intended.

## v0.5.1 focused native acceptance test

1. Reload the staged dev plugin and confirm the brand reads `v0.5.1 · layout options`.
2. Confirm a horizontal separator appears between the Dye Studio area and **Show technical status** in the left rail.
3. Open **Tooltip → Adjust**, then select `6 columns x 3 rows (18)`. Confirm exactly three rows and six columns render, full pages contain 18 appearances, page totals update, empty positions remain aligned, and previous/next plus wheel paging work.
4. Return to **Tooltip → Adjust** and select `6 columns x 4 rows (24)`. Confirm exactly four rows and six columns render, full pages contain 24 appearances, page totals update, empty positions remain aligned, and both paging methods still work.
5. Leave either layout selected, reload the plugin, and confirm that layout persists. Switch to the other layout and repeat one reload if convenient.
6. Preview one item and apply one dye after changing layouts; confirm preview, Dye Studio, close restoration, and unload restoration remain unchanged.

Report any clipped grid selector, incorrect tile/page count, stale page total, missing/extra blank position, paging regression, persistence failure, or preview/restoration regression. No broader v0.5 Dye Studio rerun is required.

Native result: passed on 2026-08-25. The rail separator, both grid geometries, 18/24-item pagination, paging controls, persistence, preview, Dye Studio, and restoration checks passed.

## v0.6.0 native acceptance test

1. Reload the staged dev plugin, run `/cboutique`, and confirm the brand reads `v0.6.0 · outfit context`.
2. Confirm **Outfit Context** shows all 12 supported slots in two columns. Untouched cards should read **Original appearance** and the summary should read **Current: Original appearance**.
3. Click several cards and confirm each selects the corresponding browser slot. Preview visibly different items in Head, Body, Hands, and one weapon slot; each changed card should show the correct icon/name and the summary count should update without losing changes in other slots.
4. Apply one dye and one two-channel dye where supported. Hover the changed cards and confirm Before shows the session opening appearance while Current shows the item and dye names.
5. Select one changed slot and click **Reset slot**. Only that slot should return to its session-opening appearance; every other changed item/dye should remain. Its card should return to **Original appearance**, its tile highlight should clear, and the changed count should decrease by one.
6. Click **Reset all**. Every remaining changed slot should return exactly to the session-opening appearance, all cards should return to **Original appearance**, the count should become zero, and both reset buttons should disable.
7. Open **Tooltip → Adjust**, turn off **Show equipment cards**, reload, and confirm the panel remains hidden. Re-enable it and reload once more to confirm persistence in both directions.
8. Make several new changes, close the Boutique, and confirm exact restoration. Reopen, make changes again, unload/reload the plugin, and confirm unload restoration remains exact.
9. Confirm the local log records v0.6 initialization, clean disposal, and no Boutique exception.

Report any missing/incorrect card, wrong icon/name/dye, slot navigation mismatch, reset affecting an unintended slot, other changed slots disappearing during slot reset, stale changed counts, visibility persistence failure, or restoration regression. Loadouts, Penumbra collection mutation, undo/redo, and GPose remain outside this focused gate.

Native result: passed on 2026-08-25. All 12 cards, direct slot navigation, multi-slot item/dye state, slot reset, Reset All, show/hide persistence, and exact close/unload restoration worked as intended.

## v0.7.0 native acceptance test

1. Reload the staged dev plugin, run `/cboutique`, and confirm the brand reads `v0.7.0 · loadouts`, technical status reports `Loadouts: 0` on a new library, and no loadout-library error appears.
2. Preview a multi-slot look containing Main Hand, Off Hand where supported, Head, Body, and at least one accessory. Apply one single-channel dye and one visibly different two-channel dye. Confirm the loadout rail reports the same current-change count as Outfit Context.
3. Click **Save current**, enter a distinctive name and notes, save it, then open **Manage**. Confirm the name, notes, timestamps, schema, saved slots, and both dye values are shown. A saved look must include every Boutique-changed slot and no untouched slots.
4. Change the current preview materially or use Reset All, then select the saved look and click **Load**. Confirm every saved weapon/off-hand/equipment/dye value returns, while unsaved slots use the appearance captured when this Boutique session opened.
5. Rename the look, duplicate it, and confirm both names appear. Reload the plugin and confirm both records survive, retain their details, and can still be loaded.
6. Change the current preview and click **Overwrite**. Confirm the first click changes nothing and presents **Confirm overwrite**; cancel once, retry, confirm it, reset/change the preview, then load and verify the replacement state.
7. Click **Delete**. Confirm the first click changes nothing and presents **Confirm delete**; cancel once, retry, confirm it, reload the plugin, and verify only the selected visible record is gone.
8. Create enough additional named looks to exercise scrolling in **Manage**. Confirm the manager remains responsive and does not impose a visible capacity error.
9. With a loaded or newly previewed look visible, close the Boutique and confirm the exact session-opening game state returns. Repeat with plugin unload/reload and confirm restoration remains exact while saved loadouts persist.
10. Confirm the local Dalamud log records the loadout count plus migrated/quarantined counts, v0.7 initialization, clean disposal, and no Boutique exception.

Report any missing slot/dye, unexpected change to an unsaved slot, load failure, rollback/restoration regression, stale manager entry, confirmation bypass, reload data loss, clipping, warning, or exception. Penumbra collection changes and GPose remain outside v0.7.

Native result: passed on 2026-08-25. Persistent loadout creation/details, typed multi-slot and dye replay, Rename, Duplicate, confirmed Overwrite/Delete, plugin-reload persistence, and exact close/unload restoration all worked as intended.

## v0.7.1 focused native acceptance test

1. Reload the staged dev plugin, run `/cboutique`, and confirm the brand reads `v0.7.1 · bottom workspace`. The left rail must contain brand, status, feedback, settings, and diagnostics only; Loadouts, Outfit Context, slot cards, and Dye Studio must no longer appear there.
2. Confirm the full-width bottom strip is visible beneath the browser in this order: Loadouts; Outfit Context/current/reset summary; Main Hand over Off Hand; Head over Earrings; Body over Necklace; Hands over Bracelets; Legs over Left Ring; Feet over Right Ring; Dye Studio.
3. Click each of the 12 bottom slot cards and confirm the browser selects the matching slot. Preview several items and dyes, confirm changed cards show the item name and selected highlighting, and hover both changed and unchanged cards to verify complete Before/Current/dye details.
4. Exercise **Reset slot** and **Reset all** from the bottom summary. Confirm their accepted v0.6 behavior and tile-highlight clearing remain unchanged.
5. Open **Manage** and **Save current** from the bottom Loadouts cell. Load an existing v0.7 record, create another, and confirm management, confirmations, persistence, and popup sizing remain unchanged.
6. Select nondyeable, one-channel, and two-channel items. Confirm the bottom Dye Studio status/buttons update for the selected slot, each channel opens the existing visual dye popup, and both channels still apply independently.
7. Use **Tooltip → Adjust → Show equipment cards** to hide and re-enable the six paired slot columns. Confirm Loadouts, the Outfit Context summary/reset controls, and Dye Studio remain available, then reload once to confirm the preference still persists.
8. Test both 6 × 3 and 6 × 4 grids, buttons, and wheel paging at the default window size. Resize reasonably narrower and wider; report any bottom-cell overlap, clipped control, missing card, vertical scrollbar, grid loss, or popup-placement failure.
9. Close the Boutique with changes visible, then repeat with plugin unload/reload. Both paths must restore the exact session-opening game state and saved loadouts must persist.

Report any ordering error, wrong slot navigation, card/state mismatch, control clipping, hidden-setting regression, popup failure, paging regression, or restoration/data-loss issue. GPose is permanently outside plugin scope and requires no testing.

Native result: revision required on 2026-08-25. The bottom strip appeared in the requested order, but the 150-pixel cells were too small, changed-slot icons were not shown, item text clipped, and Loadout Management actions could be pushed below the visible details.

## v0.7.2 revised native acceptance test

1. Reload the staged dev plugin, run `/cboutique`, and confirm the brand reads `v0.7.2 · expanded workspace`. Confirm the bottom workspace is nearly twice the v0.7.1 height and remains below—not over—the browser.
2. Preview short- and very-long-named items in at least six different slots. Every changed card must show its verified game icon and wrap the item name inside the card. Untouched slots should show the intentional `--` original-state placeholder rather than a guessed icon.
3. Click all 12 enlarged cards and hover several changed/unchanged cards. Verify exact slot navigation, selected highlighting, and complete Before/Current/dye hover details without internal card scrollbars.
4. Test both 6 × 3 and 6 × 4 browser layouts at the default window size. Confirm all rows resize into the upper region, page controls remain visible, and buttons/wheel paging still work. Resize reasonably narrower/wider and report any overlap or unreachable control.
5. Open **Loadouts → Manage** and confirm three panes appear in this order: **Actions**, saved Boutique list, and details. Select a loadout containing many slots; Load, Rename, Duplicate, Overwrite, Delete, confirmations, and Close must remain visible on the far left without scrolling to the bottom of details.
6. Exercise each management operation once, including cancelling and confirming Overwrite/Delete. Confirm the middle list and right details update correctly and persistence across plugin reload remains intact.
7. Recheck Reset Slot/Reset All, one- and two-channel Dye Studio buttons/popups, equipment-card visibility persistence, and exact close/unload restoration.

Report any missing/wrong icon, unwrapped or clipped item text, card scrollbar, grid/page-control loss, incorrectly ordered manager pane, hidden action, broken confirmation, persistence failure, or restoration regression.

Native result: focused revision required on 2026-08-25. The selected item name in the rightmost Dye Studio cell remained a clipped single line when a long equipment name was previewed.

## v0.7.3 focused native acceptance test

1. Reload the staged dev plugin, run `/cboutique`, and confirm the brand reads `v0.7.3 · dye text wrap`.
2. Preview several long-named nondyeable, one-channel, and two-channel items. Confirm each full selected-item name wraps downward within the Dye Studio column.
3. Confirm the no-channel message or Channel 1/Channel 2 controls remain directly below the wrapped name, do not overlap it, and remain fully visible and clickable.
4. Open both dye channels on a two-channel item and apply a dye to each. Confirm the existing swatch popup and independent channel application remain unchanged.
5. Resize the Boutique reasonably narrower and wider. Confirm wrapping follows the available column width without horizontal clipping, an internal scrollbar, or text escaping the cell.

Report any clipped name, overlap, hidden channel control, unexpected cell scrollbar, dye regression, or restoration regression.

## v0.7.4 focused native acceptance test

1. Reload the staged dev plugin, run `/cboutique`, and confirm the brand reads `v0.7.4 · wider outfit cards`.
2. Confirm the leftmost bottom column shows Loadouts first and Outfit Context directly below it. Manage, Save current, Reset slot, and Reset all must all remain visible and clickable without an internal scrollbar.
3. Confirm the former standalone Outfit Context column is gone and all six paired equipment columns are evenly wider and slightly taller. Preview changed items in every column and verify their centered game icons are visibly larger, with wrapped slot/item text remaining inside each card.
4. Click all 12 cards and confirm exact slot navigation and selected highlighting. Recheck Reset slot, Reset all, Loadout Save/Manage, and the Loadout manager's three persistent panes.
5. Test both 6 × 3 and 6 × 4 browser layouts at the default window size, then resize reasonably narrower and wider. Confirm the combined control cell, all six equal card columns, Dye Studio, grid, and page controls remain visible without overlap or unintended scrollbars.
6. Recheck the v0.7.3 long-name Dye Studio wrapping case, one- and two-channel dye application, equipment-card visibility persistence, and exact close/unload restoration.

Report any reversed control order, clipped/hidden control, unequal slot width, icon regression, card-text overflow, wrong slot navigation, grid/page loss, Dye Studio regression, or restoration failure.

Native result: passed on 2026-08-26. The combined controls, six enlarged paired columns/icons, both grid modes, resizing, Dye Studio wrapping, navigation, and restoration checks worked as intended.

## v0.7.5 focused native acceptance test

1. Reload the staged dev plugin and confirm `/TCB`, `/Boutique`, and retained `/cboutique` each open the same Boutique window. Confirm the brand reads `v0.7.5 · linked weapons`.
2. On Samurai, preview at least five different katanas in sequence. Each selection must immediately show the matching sheath; the Off Hand card must update to the same source item without manually browsing Off Hand.
3. Repeat paired-component checks on Monk and Machinist using several weapons each. Confirm fists/secondary components match every selected main weapon and no prior component remains after repeated swaps.
4. On a dyeable paired weapon, apply and clear Channel 1 and Channel 2 where supported. Confirm both visible weapon components stay synchronized. Reset either Main Hand or Off Hand and confirm the complete linked pair returns to the session-opening state while unrelated changed equipment remains.
5. In Dye Studio, confirm Channel 1 occupies the upper bordered card and Channel 2 the lower card. Each card must show `Active: <dye name>` without opening the popup, update immediately after apply/clear, wrap long dye names, and remain fully clickable.
6. Preview paired and standalone weapons repeatedly, then close the Boutique. Reopen and repeat before disabling/reloading the plugin. Every close and unload path must restore the exact game/session-opening main and off-hand state without requiring Glamourer's **Revert to Game** control.
7. Save and reload a loadout containing a paired weapon and dyes. Confirm both main/off-hand records and dye values return together, then close and confirm exact restoration again.

Report any mismatched/missing sheath or secondary component, stale prior weapon part, partial dye/reset/loadout behavior, persistent weapon after close/unload, command failure, channel-card clipping, stale active dye name, or restoration failure.

Native result: passed on 2026-08-26. Linked Samurai, Monk, and Machinist components, repeated swaps, paired dyes/resets/design replay, all three commands, live channel names, and exact close/unload restoration worked as intended.

## v0.8.0 focused native acceptance test

1. Reload the staged dev plugin and open it with any supported command. Confirm the shared header reads `v0.8.0 · tabbed workspace`, the tabs appear in **Boutique**, **Designs**, **Status**, **Debug** order, and Boutique is selected by default. Select another tab, close the window, reopen it, and confirm Boutique is selected again.
2. In Boutique, confirm the old left rail is gone and the expansion, equipment-slot, search, tooltip, dye, class/role, level, icon grid, and paging controls use the former rail space. Confirm the bottom strip remains in place with **DESIGNS** above **OUTFIT INFO**, all paired slot cards, and Dye Studio.
3. Exercise both 6 × 3 and 6 × 4 grids, filters, buttons, wheel paging, tooltip controls, slot navigation, preview, and dyes. Resize the window reasonably narrower and wider; report any lost row, clipped filter, overlap, unexpected scrollbar, or bottom-workspace regression.
4. Choose **Open Designs** from the bottom strip and confirm the Designs tab opens. Verify the Actions pane remains on the left, every saved design appears in the middle list, and the selected design's name, notes, timestamps, schema, slots, item names, and dye values appear in Saved Outfit Info on the right.
5. From Designs, Save a new current design and exercise Load, Rename, Duplicate, cancel/confirm Overwrite, and cancel/confirm Delete. Confirm selection/details update immediately and records still persist after a plugin reload.
6. Open Status and confirm plugin/version, session/dependency state, preview feedback, and the **Show technical status** toggle live there and no longer appear in Boutique. Toggle technical details off/on and confirm counts/timing hide and return without affecting browsing or appearance state.
7. Open Debug and confirm it shows read-only current session/browser values, changed equipment, recent transitions, and any local errors without offering appearance mutation or upload controls. Switch among all tabs repeatedly and confirm the current filter/page/preview/dye state is preserved.
8. Close with changes visible, reopen, change equipment again, then disable/reload the plugin. Confirm exact session-opening restoration on both close and unload and that saved designs remain local and intact.

Report any wrong default tab, rail remnant, clipped tab/pane/control, missing design or action, stale design detail, technical-status leakage into Boutique, state loss while switching tabs, unexpected Debug mutation, persistence failure, or restoration regression.

Native result: passed on 2026-08-26. Tab order/defaulting, full-width Boutique layout, Designs management, Status/Debug separation, state preservation, and exact close/unload restoration worked as intended.

## v0.8.1 focused native acceptance test

1. Reload the staged dev plugin and confirm the shared header reads `v0.8.1 · settings and combat safety`. Verify the primary tab order is **Boutique**, **Designs**, **Status**, **Settings**, **Debug** and Boutique remains the default after closing/reopening the window.
2. In Boutique, confirm the former **Tooltip** dropdown is now **Layout** and contains only `3 rows x 6 columns (18)` and `4 rows x 6 columns (24)`. Select each option and confirm exact grid geometry, page counts, buttons, wheel paging, preview, and persistence after reload.
3. Open Settings and confirm item-tooltip **Overlay transparency** and **Font size** sliders are present there, no longer appear in Layout, and update hovered item overlays immediately across their full ranges. Confirm long overlay text and icon visibility remain usable.
4. Move **Whole-window transparency** through 0%, intermediate values, and 90%. Confirm the complete Boutique window changes opacity immediately, remains interactive/readable at 90%, and the value survives plugin reload.
5. Toggle **Show Outfit Info equipment cards** off/on in Settings, confirm the six paired card columns hide/return without losing current appearance state, and confirm the choice persists.
6. With **Close and disable the Boutique during combat** enabled, preview visibly different equipment/dyes and enter combat. The Boutique must close promptly and restore the exact session-opening appearance. During combat, `/TCB`, `/Boutique`, `/cboutique`, and Dalamud's main/config open buttons must not reopen or operate it. After combat ends, open it normally and confirm a fresh session begins.
7. Outside combat, disable the combat lock in Settings, enter combat, and confirm the Boutique remains open and its existing browsing/preview behavior remains available. Leave combat, re-enable the lock, reload, and confirm the enabled state persists.
8. Recheck normal close and plugin unload restoration after changing all new settings. Confirm saved Designs and all older tooltip/grid preferences remain intact after the configuration schema upgrade.

Report any extra Layout control, wrong grid label/count, missing or nonpersistent setting, fully unrecoverable window, combat-time reopening, failed combat restoration, repeated error spam, opt-out failure, design/config loss, or normal restoration regression.

## v0.8.2 focused native acceptance test

1. Reload the staged dev plugin and confirm the shared header reads `v0.8.2 · newest-first browser`. Open **Layout** and confirm its closed value and only two choices are exactly `3 x 6` and `4 x 6`, with no clipping or additional explanatory text.
2. Select a single expansion with many appearances. Hover the first several page-one tiles and compare their Item IDs with tiles on later pages; newer/higher local item rows should lead and older/lower rows should move later. Shared-model aliases may collapse into one appearance, but the newest source should be its default tile.
3. Repeat the ordering check under **All Expansions** and confirm the newest local additions lead rather than A Realm Reborn-era rows. Apply name, class/role, dye, and level filters and confirm the remaining results preserve newest-first order.
4. Switch between `3 x 6` and `4 x 6`; verify 18/24 pagination, arrows, wheel paging, selection, preview, and filters remain correct with the reordered catalog.
5. Complete the still-pending v0.8.1 Settings checks: tooltip sliders, whole-window transparency and persistence, Outfit Info visibility, default combat close/restoration and blocked reopening, combat opt-out, normal close, and unload restoration.

Report any clipped Layout text, oldest-first page, non-descending visible Item ID sequence, older duplicate chosen over a newer source, filter/pagination regression, or any v0.8.1 Settings/combat-safety failure.

Native result: passed on 2026-08-26. Compact Layout labels, newest-first expansion and All Expansions presentation, Settings persistence, combat safety/opt-out, and restoration regressions worked as intended.

## v0.8.3 focused native acceptance test

1. Reload the staged dev plugin and confirm the shared header reads `v0.8.3 · manual GPose compatibility`. Recheck one normal-world item preview and close restoration before entering GPose.
2. Enter GPose normally without using any Boutique command or automated transition. Confirm the Boutique closes if it was open, the game's GPose controls remain unchanged, and `/TCB` opens the ordinary five-tab window visibly inside GPose.
3. With your character selected as the GPose target, open the Boutique. Confirm the catalog populates, then preview Head, Body, Main Hand, and a linked weapon where practical. Every change must appear immediately on the visible GPose actor.
4. Apply one- and two-channel dyes, Reset Slot, Reset All, and one saved Design. Confirm the same accepted behavior operates on the bound GPose actor without affecting another target.
5. Change the GPose target after the Boutique session is active, then preview one more item. The original bound actor must remain the recipient; the newly targeted actor must not be modified. Close and reopen the Boutique to intentionally bind the new current target if desired.
6. Close the Boutique inside GPose and confirm the bound actor returns to its GPose-session opening appearance. Reopen, make another visible change, then exit GPose without closing first. The Boutique should close, normal-world appearance must remain correct, and `/TCB` should open a fresh normal-world session afterward.
7. Enter GPose once without a current target and attempt to open. Confirm preview activation fails safely with an instruction to select the character; close, select the actor, reopen, and confirm normal operation.
8. Inspect several browser and Outfit Info icons. They may look unchanged because high-resolution lookup was already the Dalamud default; confirm there are no missing/incorrect icons or HQ-quality markers.

Report any hidden Boutique UI, empty catalog after a valid target is selected, mutation of the wrong actor, target redirection during a bound session, GPose close/exit restoration failure, normal-world state contamination, missing icon, unexpected HQ marker, GPose control change, or exception.

## v0.8.4 focused native acceptance test

1. Reload the staged dev plugin and confirm the header reads `v0.8.4 · GPose recovery & design exchange`. In normal play, preview one item and close the Boutique; confirm the session-opening appearance still restores.
2. Enter GPose, select your own character, open the Boutique, and preview visibly different Head, Body, Main Hand, and dye choices. Exit GPose without closing the Boutique first. Confirm the Boutique closes and every Boutique-applied GPose item/dye disappears from the normal-world model immediately.
3. Reopen the Boutique after that exit. Confirm no stale tile or Outfit Info selection claims the old GPose choices. Reset Slot/Reset All must not resurrect any GPose-selected item. Close or unload the plugin and confirm the model remains at the correct game/session-opening state without using Glamourer's Revert command.
4. Re-enter GPose and target an actor other than your logged-in player character, then try to open. Confirm activation fails safely with an instruction to select your own character and no actor is changed. Select your own character, reopen, and confirm previewing works.
5. In Boutique, confirm the redundant slot/result headings and `CLEAR` heading are absent; navigation is centered; `DYE SLOTS` exposes `None <-> All`, `No dye slots`, `Single dye slot`, and `Two dye slots`; each exact filter works; and `Limited Classes` returns Blue Mage-specific (and Beastmaster-specific when present in local data) items without ordinary caster gear.
6. Confirm Dye Studio says `Dye slot 1` / `Dye slot 2`. In the bottom Designs selector, choose a saved name and confirm it applies directly without switching tabs.
7. In Designs, confirm all actions are in the top toolbar and the left saved list shows names only. Export a design, import the copied `TCB-DESIGN-1` text, and confirm a fresh local copy appears. Generate its item list, move/resize it, and verify icons/names. With a market-search plugin active, hover a reference icon and confirm it recognizes that item through Dalamud's shared hovered-item state.
8. In Settings, vary background-only transparency independently from whole-window transparency. Confirm icons, text, and controls retain their own opacity, the main background changes, the generated-list window follows both settings, and the values persist after reload.

Report any GPose state leak, stale selection, wrong reset baseline, failed own-character validation, filter mismatch, clipped toolbar/control, failed design round trip, stuck hovered item, transparency coupling, exception, or restoration requiring Glamourer's manual Revert command.

## v0.8.5 focused native acceptance test

1. Reload the staged dev plugin. Confirm the duplicate cyan title/version line is gone, the Boutique/Designs/Status/Settings/Debug tabs move directly beneath Dalamud's native title bar, and Status reports `v0.8.5 · layout & tooltip refinement`.
2. Open Layout and confirm the choices are ordered exactly `2 x 6`, `3 x 6`, `4 x 6`. Exercise all three layouts and verify 12/18/24 visible positions, correct page totals, preserved filtering/selection, working wheel paging, and persistence after reload.
3. Select a weapon slot on an active class and confirm incompatible weapons remain automatically filtered while the explanatory weapon sentence is no longer displayed.
4. In the bottom Designs cell, confirm saved/changed count text is absent and `Saved Designs` is a button rather than a closed-value dropdown. Open it with enough saved designs to scroll, select multiple names, and confirm each applies immediately without switching to the Designs tab. Verify Save current still works.
5. Confirm the centered navigation order is `Page # / #`, `<< First`, `< Previous`, `Next >`, `Last >>` with separators. Verify First/Previous disable on page one, Next/Last disable on the final page, direct first/last jumps are correct, and the removed wheel-help sentence does not affect wheel paging.
6. Hover short and very long item names in first, middle, and last columns/rows. Confirm the tooltip never follows the mouse, never intentionally covers the hovered icon, normally anchors above/right corner-to-corner, and safely falls back above/left or below near screen edges. Move the main window and repeat.
7. Confirm the tooltip renders a bold item name, smaller normal `[ID#...]`, bold equipment category, italic `[ Equip lvl # | Item lvl # ]`, `Dye slots none/1/2`, no Sources line, and the existing Mogstation warning where applicable. Recheck tooltip font/background sliders.
8. Compare known tradeable crafted equipment and known untradeable/reward equipment. Confirm `Marketboard` is green only for market-searchable tradable items and red otherwise. Spot-check the document examples if locally available: Mountain Chromite Musketoon should be green; Shadowhound Helm should be red.
9. Repeat the v0.8.4 GPose-exit restoration sequence before acceptance, since v0.8.4 did not receive a separate native pass before this combined candidate.

Report any header gap, wrong layout order/page size, incompatible weapon leak, clipped/non-scrollable Saved Designs popup, incorrect page jump, tooltip/icon overlap, mouse-following tooltip, typography/marketability mismatch, lost slider behavior, or GPose restoration regression.

Native result: revision required on 2026-08-27. The anchored tooltip rendered behind the Boutique's menu/icon child windows. GPose opening rejected the selected local clone as not matching the logged-in player, so previews and bottom equipment cards did not update. Reset Slot/Reset All could leave oddly colored Samurai, Gunbreaker, and Monk weapon models, and unloading the Boutique did not clear them; Glamourer's **Revert to Game** corrected the actor. The `2 x 6` layout was also rejected, and Settings/layout organization required revision.

## v0.8.6 focused native acceptance test

1. Reload the staged dev plugin and confirm Status reports `v0.8.6 · GPose recovery & settings refinement`.
2. Enter GPose, select your own character, and open the Boutique. Confirm the session is Active rather than degraded. Preview Head, Body, Main Hand, and dyes; every choice must change the GPose actor immediately and appear in the bottom equipment cards.
3. Exit GPose without closing the Boutique. Confirm the Boutique closes and the normal actor is immediately at true game state. Reopen and confirm no old GPose item is selected. Repeat once by unloading the Boutique while a GPose preview is active; after leaving GPose, no Glamourer manual revert should be necessary.
4. In normal play on Samurai, Gunbreaker, Monk, and Machinist where available, preview at least three weapons. For each job, exercise Reset Slot, Reset All, window close, and plugin unload. Main/off-hand models, sheaths, secondary components, dyes, and glow/color must return to actual equipped game state every time. Reopen Glamourer only to verify there is no retained Boutique state; do not press its manual revert unless reporting failure.
5. Hover tiles in the first, middle, and final columns/rows with the Boutique over both game scenery and other Boutique controls. The tooltip must always render in front, remain anchored beside rather than over the icon, and keep its edge fallback after moving/resizing the window.
6. Confirm each tooltip has a bold item name, spaced `[ ID#... ]`, a full-width divider, bold underlined equipment category, italic level line, exact dye-slot count, correct green/red Marketboard line, and Mogstation warning where applicable.
7. Open Settings. All sections must initially be collapsed with upward triangles and appear in this order: **Crystarium Boutique Layout**, **Plugin Appearance**, **Item Tooltip**, **Combat Safety**. Clicking a header must show a downward triangle and its existing controls; clicking again must hide them.
8. Expand Layout and confirm the four individual buttons are ordered `3 x 5`, `4 x 5`, `3 x 6`, `4 x 6`. Only the active choice has a checked, non-clickable checkbox. Exercise all four grids and verify 15/20/18/24 positions, page totals, First/Previous/Next/Last, wheel paging, selection, and persistence. `2 x 6` must not appear.
9. Return to Boutique and confirm no Layout control remains in the filter row; Expansion, Equipment Slot, Search, and Clear should use the freed width without clipping. Recheck Saved Designs, filtering, preview, Dye Studio, and bottom cards once.

Report any degraded GPose session for a correctly selected local actor, preview/card mismatch, transient GPose snapshot leaking into normal play, weapon component surviving reset/unload, need for Glamourer's manual revert, tooltip z-order/formatting failure, wrong collapse indicator/order, clickable active checkbox, wrong tile count, missing paging control, configuration loss, or exception.

Native result: revision required on 2026-08-27. GPose still opened in degraded mode and did not apply Boutique selections. Product direction superseded the hard close/reset boundary: Boutique state should now carry into GPose and GPose Boutique changes should carry back out without closing the window or starting a new session.

## v0.8.7 focused native acceptance test

1. Reload the staged dev plugin and confirm Status reports `v0.8.7 · continuous GPose handoff`.
2. In normal play, open Boutique and preview visibly different Head, Body, Main Hand, linked Off Hand where supported, and one or two dyes. Confirm bottom cards and Dye Studio show the active choices.
3. Leave Boutique open and enter GPose normally. The Boutique window must remain open with the same active selections, cards, filters, page, and dyes. Once the character clone is available, every pre-GPose Boutique change must appear on it without reopening the plugin or reselecting items.
4. While still in GPose, choose different Head, Body, weapon, and dye options. Each must update the visible clone immediately and update the existing bottom cards.
5. Exit GPose without closing Boutique. The window/session must remain open, and the normal character must now display the latest choices made in GPose. Bottom cards, selected tiles, dyes, filters, and page must remain unchanged.
6. Repeat entry and exit once more after changing another weapon in normal play. Confirm there is no accumulating sheath/off-hand mismatch, discoloration, missing model, degraded-session warning, or need to reopen Boutique.
7. After the final exit, use Reset Slot and Reset All. Confirm all carried Boutique changes return to true game state. Repeat with window close and plugin unload; Glamourer's manual **Revert to Game** must not be needed.
8. Enter GPose with Boutique open without manually targeting your character. Confirm the UI remains open and the handoff completes automatically when the exact local clone becomes available rather than applying changes to the obsolete normal actor. Selecting a different GPose actor must not redirect the active Boutique session.
9. Recheck the v0.8.6 tooltip-front-layer, formatting, four grid buttons, collapsed Settings sections, paging, Saved Designs, and filter-row checks once.

Report any Boutique close/reset during GPose entry or exit, lost UI/session state, missing pre-GPose appearance, GPose change not carried back, wrong actor mutation, perpetual handoff warning, reset/unload persistence, linked-weapon mismatch, manual Glamourer revert requirement, or exception.

Native result: revision required on 2026-08-27. The window/session, incoming appearance, and linked weapons worked, but the log remained `PlayerUnavailable` because no mutable GPose clone index was exposed. Item clicks highlighted only, Reset Slot/Reset All did nothing, and unloading inside GPose left the pre-GPose Boutique state active. Normal functionality returned after leaving GPose.

## v0.8.8 focused native acceptance test

1. Reload the staged dev plugin and confirm Status reports `v0.8.8 · persisted GPose handoff`.
2. In normal play, preview visibly different Head, Body, Main Hand/linked Off Hand, and dyes. Enter GPose without manually selecting a target. Confirm the window/session and incoming appearance remain intact.
3. Check the log once: the handoff should report either a local GPose object index or Glamourer's persisted local-player state. It must not remain `PlayerUnavailable`.
4. While in GPose, choose different items in at least Head, Body, Main Hand, and a linked weapon. Confirm every click changes the visible clone immediately rather than only highlighting the tile. Test both dye slots where supported.
5. Use Reset Slot on one changed slot while still in GPose, then reapply it. Confirm both operations update the visible clone. Use Reset All and confirm the visible clone returns to true game state; reapply two items for the exit test.
6. Exit GPose with Boutique still open. Confirm the latest two Boutique choices carry back to the normal character and the Boutique cards/selections remain synchronized.
7. Re-enter GPose, apply a visibly different weapon and armor item, then disable the Boutique through Dev Plugins while still in GPose. Confirm the visible clone returns to game state; after exiting GPose, the normal character must also be at game state without using Glamourer's manual revert.
8. Reload and repeat close-window cleanup once inside GPose, then repeat Reset All after returning to normal play. No main-hand/sheath/off-hand component may persist.

Report any `PlayerUnavailable` handoff, highlight-only item selection, inert reset, GPose-to-normal carry-back failure, state surviving close/unload, manual Glamourer revert requirement, linked-weapon mismatch, or exception.

Native result: passed on 2026-08-27. The persisted player-name fallback restored full Boutique mutation/reset functionality inside GPose, carried the latest Boutique state back to normal play, preserved linked-weapon correctness, and returned the character to game state when the plugin was disabled inside GPose.

## Acquisition enrichment focused native acceptance test

1. Reload the staged Release plugin and confirm the catalog loads without an acquisition warning. Record the new source/item counts and load duration from the local Dalamud log.
2. Hover a directly crafted item with acquisition detail set to Detailed. Confirm the tooltip shows crafter, recipe level/stars/book where applicable, yield/requirements, and the full ingredient list without a `+ more local sources` placeholder.
3. Hover a direct quest reward. Confirm the quest name, issuer location, quest giver when locally resolvable, and reward quantity appear. Recheck one seasonal-event quest reward.
4. Hover a SpecialShop item and a gil or Grand Company vendor item. Confirm every locally resolved vendor location and the complete currency/gil/seal or collectability requirement are present.
5. Hover an item with more than six direct local acquisition sources and confirm every source is rendered. Hover a shared appearance and confirm its sources remain under **OTHER ITEMS USING THIS APPEARANCE** rather than being labeled as direct sources for the selected item.
6. Confirm an item without an installed-sheet or supplemental duty relation still states that the exact source is unavailable; it must not infer a boss or duty from a shared model or vendor prerequisite.
7. Temporarily validate a known fixture only by adding a provenance-bearing record to the local staged supplement. Confirm duty, content type, difficulty, boss, series, and tier render for that exact item ID, then restore the shipped empty supplement.

Report any startup warning, incorrect quest/vendor/recipe association, missing locally indexed detail, truncated direct source, alias presented as a direct drop, unverified supplemental record accepted, tooltip overflow that makes sources inaccessible, or material catalog-load regression.

## Regression backlog

As milestones land, extend automated and native coverage for search/filtering, dyes, loadout persistence/migrations, undo/redo, territory/logout/character transitions, dependency loss/version mismatch, and performance targets.
