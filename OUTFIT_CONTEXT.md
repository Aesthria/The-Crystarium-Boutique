# Outfit Info

Status: v0.6.0 state/reset behavior passed; v0.9.2 card/dye behavior passed; v0.10.0 detachable Wardrobe/reset behavior passed; v0.10.1 made both orientations vertically responsive; v0.11.0 preserves browser state during dye interaction; v0.12.0 renames and visually frames the Crystal Wardrobe.

## State boundary

The original and current appearance remain complete opaque snapshots owned by `BoutiqueSession`. `BoutiqueSessionController` separately tracks every slot changed through the Boutique as a typed `PreviewEquipmentState`, including source item, icon, supported channels, and both stain values. A missing changed-slot entry means that slot remains at the session-opening appearance.

This deliberately avoids parsing Glamourer's Base64 state. The cards can truthfully show detailed Boutique changes and an explicit **Original appearance** state without guessing the game's original item metadata.

## Slot reset

The installed Glamourer API 1.8 exposes no per-slot revert subscriber. A Boutique slot reset therefore:

1. reapplies the exact session-opening snapshot;
2. replays every other changed slot in deterministic slot order through the verified typed item endpoint;
3. recaptures the resulting complete preview;
4. removes the reset slot from changed state only after success.

If replay or recapture fails, the controller reapplies the prior complete preview snapshot and retains all changed-slot records. A rollback failure is surfaced as nonrecoverable instead of being hidden.

## Reset all and presentation

Reset All reapplies the exact session-opening snapshot, marks the session clean, and clears changed-slot state only after the appearance request succeeds.

The detachable **Crystal Wardrobe** displays all 12 supported slots. Its default horizontal 2 × 6 order uses Main Hand, Head, Body, Hands, Legs, Feet followed by Off Hand, Earrings, Necklace, Bracelets, Right Ring, Left Ring. Its vertical 6 × 2 order pairs Main/Off Hand, Head/Earrings, Body/Necklace, Hands/Bracelets, Legs/Feet, and Left/Right Ring. Each rounded card keeps the accepted height, complete naturally wrapped name, centered high-resolution icon, configurable Antique Gold border, Dusty Rose header separator, and reserved dye-control row. An untouched slot is labeled **Original appearance** because the plugin intentionally does not parse the opaque session-opening snapshot for item metadata.

Each dye control represents one channel. Unsupported channels are disabled with the small centered supplied red-X image. A supported no-stain channel reads **Dye 1/2**; after selection it displays the exact stain name on the local stain RGB color, using black or white text according to contrast. Clicking a supported control opens the searchable grouped stain picker and applies through the same transactional controller used by earlier Dye Studio validation.

The Wardrobe's 2 × 2 toolbar reduces its horizontal minimum. Card height is recalculated from the current child-region height for two horizontal rows or six vertical rows, with a 120-pixel minimum before scroll fallback. Item-name color is resolved from the same local rarity-color service as browser tooltips by looking up the preview state's retained source item ID in the catalog.

The Wardrobe toolbar exposes selected-slot and reset-all buttons. Ctrl + left click on a card invokes slot reset. The former Ctrl + Shift + Delete reset-all shortcut is removed; visible reset actions reuse the same controller methods described above and clear stale active-design/tile selection state only after success.

The old bottom Outfit Info block, counts, and Reset Slot/Reset All controls were removed by the v0.9.0 interface specification. The rollback-safe controller operations remain available to design loading, lifecycle restoration, and internal orchestration; removing those controls did not weaken close/unload/combat cleanup.
