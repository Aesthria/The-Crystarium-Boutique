# Feature Reference

This page describes the user-facing features available in The Crystarium Boutique 0.1.0 release candidate.

## Boutique browsing

Browse the local FFXIV equipment catalog by equipment slot. Search by item text, narrow results by expansion, role or job-relevant categories, filter by dye-slot capability, and set a required/equip-level upper bound. **All** applies no level restriction; choosing **60**, for example, shows equipment requiring levels 1–60. This filter is not Item Level/iLvl. The browser keeps separate display entries for meaningful visual differences instead of collapsing every shared model into one result.

Meaningful variants can include baked/default colors, dye support, materials or presentation, relic progression, standard and glowing replicas, Matte replicas, and other shared-model variants that remain visually distinct.

### Item controls

- **Left Click:** preview the item in Crystal Wardrobe and on the character.
- **Right Click:** manage the item in All Favorites or named Favorite lists.
- **Head Left Click:** preview with the optional visor piece hidden.
- **Head Ctrl + Left Click:** preview the full or alternate visor appearance when supported.

Headgear without an alternate visor still applies normally. TCB does not guess whether a particular item supports a visor.

## Crystal Wardrobe

Crystal Wardrobe displays the equipment and stains currently synchronized with the active Boutique session. It provides cards for supported equipment slots and separate controls for Dye Slot 1 and Dye Slot 2.

- Choose Vertical or Horizontal orientation.
- Changing orientation swaps the current Wardrobe window dimensions.
- Left Click a dye slot to open Dye Studio.
- Right Click an equipped Wardrobe card to remove the item from the current Boutique appearance.
- Use **Reset Character** to synchronize equipment and dyes from the current FFXIV character state.
- Hide or reopen the Wardrobe without closing the Boutique.

When **Automatically Sync Wardrobe** is enabled, TCB waits for FFXIV to finish applying a genuine local-player gearset change and then adopts the settled equipment and stains. It does not automatically select a Saved Design. When disabled, no automatic resync occurs and Reset Character remains available.

## Dyes

TCB supports Dye Slot 1 and Dye Slot 2 where the item supports them. User-selected dyes persist across compatible item swaps. Stains synchronized from real game equipment become the active Boutique dye values instead of being discarded on the next preview.

**Clear All Dye Slots** clears current active Boutique/session dyes while leaving equipment intact. It does not alter stored Saved Designs. Loading a saved Design can intentionally apply that Design's saved dyes again.

## Favorites

Right Click a Boutique item to add or remove it and manage named-list membership. Favorites persist locally, can belong to multiple named lists, and remain available through All Favorites.

The Favorites view supports named-list selection, equipment-slot filtering, and text search. Filtering changes only what is displayed. Favorited items show a badge in the normal Boutique grid. An additional Favorite frame highlight is optional, defaults to off, and has a configurable color. Its bottom-right Revert control uses Boutique's shared previous-item state rather than a separate Favorites history.

## Saved Designs

**Save Design** stores the active Boutique/Wardrobe appearance with a custom name and optional notes. The Designs tab supports loading, renaming, duplicating, overwriting, deleting, and generating a movable item-reference list.

Loading a Design adopts the actual appearance that was validly applied. If a Design contains equipment that cannot be used by the current job, TCB retains the valid active equipment for that slot rather than falsely showing incompatible equipment as active. A cross-job Design may therefore apply compatible armor while retaining the current valid weapon.

### Design-string sharing

**Export Design** copies a portable Design string from the selected saved Design. Share that string with another TCB user. **Import Design** validates a compatible shared string and saves a new copy in the receiving user's local Designs list, ready to load, try, rename, or modify.

### Eorzea Collection URL import

Choose **Eorzea Collection Import from URL** from the Boutique Designs selector and submit a supported public glamour URL. TCB retrieves and resolves the compatible equipment against current local game data, then applies it to the active session. Choose **Save Design** to keep the result locally and use normal Boutique controls to modify it afterward.

See [Design Import](DESIGN-IMPORT.md) for exact differences and limitations.

## Item information and visual relationships

Item tooltips show equipment facts, supported local acquisition information, and a compact list of other items sharing the same model. Model-sharing entries are visual references only: they do not inherit the hovered item's acquisition source.

## How to Obtain

Packaged acquisition data covers supported duty-obtained equipment from Dungeons, Trials, Raids, Savage content where represented, Alliance Raids, Treasure Hunt, Boss Chests, Duty Chests, and Duty Drops. Boss names appear only when verified stable IDs establish the exact relationship. Core acquisition lookup is local and offline.

Missing acquisition data does not mean an item is invalid or unobtainable. See [Data Sources](DATA-SOURCES.md).

## Themes and interface options

TCB provides:

- Simple
- The Crystarium Boutique
- Simple Crystarium Boutique

Fresh configurations default to The Crystarium Boutique, while saved choices remain intact. Settings include interface and tooltip scaling, transparency, Favorite highlighting, Wardrobe auto-sync, and other presentation or safety controls.
