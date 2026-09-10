# The Crystarium Boutique

**Browse FFXIV's wardrobe like a boutique.**

The Crystarium Boutique (TCB) is a visual equipment-discovery and appearance-preview plugin for FINAL FANTASY XIV. Browse equipment, preview items and dyes on your character, organize Favorites, build and share Designs, work from the synchronized Crystal Wardrobe, and inspect supported acquisition information without leaving the plugin.

## Features

- Browse equipment visually by slot, expansion, role, dye capability, required/equip-level upper bound, or text search. Choosing level 60, for example, shows required levels 1–60; this is not an Item Level filter.
- Explore meaningful visual variants, including baked colors, dye support, material differences, relic stages, glowing or standard replicas, and Matte replicas where the appearance differs.
- Left Click an item to preview it immediately. Right Click manages its Favorite status and named Favorite lists.
- For Head items, Left Click hides the optional visor piece; Ctrl + Left Click shows the full or alternate visor appearance when the item supports one.
- Preview two dye slots and keep chosen dyes while trying compatible equipment.
- See other items that share the same model without treating them as the same obtainable item.
- View packaged, offline How to Obtain details for supported duty-obtained equipment.

## Crystal Wardrobe

Crystal Wardrobe is the synchronized equipment-and-dye workspace beside the Boutique. It displays the active appearance, including Dye Slot 1 and Dye Slot 2, and supports Vertical and Horizontal layouts. Changing orientation swaps the current Wardrobe window dimensions so the workspace stays usable.

When **Automatically Sync Wardrobe** is enabled, the Wardrobe resynchronizes after FFXIV finishes applying a genuine local-player gearset change. **Reset Character** provides a manual resync. Right Click an equipped Wardrobe card removes that item from the current Boutique appearance; Right Click on a Boutique item remains Favorites management.

**Clear All Dye Slots** clears the current Boutique session's active dyes while keeping its equipment intact. It does not edit stored Saved Designs, and loading a saved Design can deliberately reapply that Design's saved dyes.

## Favorites

Favorites persist locally and can belong to multiple named lists. The Favorites view supports list selection, item-type filtering, and search while preserving newest-first Favorite ordering. Favorited Boutique items show a badge; an optional configurable frame highlight is available and defaults to off. The bottom-right Revert control uses the same shared previous-item state as Boutique browsing.

## Saved Designs, sharing, and imports

Save the current appearance as a Design with a custom name and optional notes. The Designs tab can load, rename, duplicate, overwrite, delete, and generate movable item-reference lists for saved Designs.

**Export Design** copies a portable Design string for sharing with another TCB user. **Import Design** accepts a compatible shared string, saves it to the user's own Designs list, and lets the user load, try, or modify it.

TCB can also import a supported public Eorzea Collection glamour URL. The compatible appearance is applied to the current Boutique session; use **Save Design** to keep it, then modify it with the normal Boutique tools. This is separate from TCB Design-string sharing. See [Design Import](docs/DESIGN-IMPORT.md) for the workflows and limitations.

If a saved Design contains equipment that cannot validly apply to the current job, TCB retains the valid active equipment for that slot instead of presenting incompatible equipment as active.

## How to Obtain

TCB packages acquisition information for supported duty-obtained equipment, including Dungeons, Trials, Raids, Savage content where represented, Alliance Raids, Treasure Hunt, Boss Chests, Duty Chests, and Duty Drops. Exact bosses are named only when an exact item-to-source evidence chain proves the relationship.

Not every item comes from a duty, and missing acquisition information does not mean an item is invalid. Core catalog and acquisition lookup are local and offline. See [Data Sources](docs/DATA-SOURCES.md).

## Themes

The current themes are:

- Simple
- The Crystarium Boutique
- Simple Crystarium Boutique

Fresh configurations default to **The Crystarium Boutique**. Existing saved theme choices are preserved.

## Commands

- `/boutique`
- `/tcb`
- `/cb`

All three commands toggle the same Boutique experience.

## Requirements

- Windows and FINAL FANTASY XIV running through XIVLauncher with current Dalamud.
- Glamourer installed separately for appearance capture, preview, and restoration. Without it, TCB remains available in browse-only mode.
- Penumbra installed separately for the optional emergency redraw fallback used if Glamourer unloads during an active preview.

Glamourer and Penumbra are independent projects. Neither plugin nor either API assembly is bundled with TCB.

## Installation and updating

Public distribution is planned through a custom Dalamud plugin repository. The repository URL is not live yet and will be supplied at release; no current URL should be treated as an installation feed.

Once enabled, installation will follow the normal custom-repository flow: add the supplied URL under Dalamud's Experimental/Custom Plugin Repositories settings, save, open `/xlplugins`, find **The Crystarium Boutique**, and install. Updates will then be delivered through Dalamud. See [Installation](docs/INSTALLATION.md).

## Privacy and local-first behavior

The equipment catalog uses local game data. Acquisition lookup uses packaged data and runs offline. Favorites, settings, and Designs are stored locally through the plugin's current persistence systems.

TCB does not contact a live price service or acquisition website during normal browsing. The explicit Eorzea Collection URL import action is the exception: when the user submits a supported URL, TCB accesses that external resource to retrieve the requested glamour data.

## Dependencies and data sources

TCB is built with the Dalamud plugin SDK and uses Lumina-backed game data exposed by Dalamud. It interoperates with separately installed Glamourer and Penumbra through narrow Dalamud IPC contracts; it does not bundle `Glamourer.Api.dll`, `Penumbra.Api.dll`, or `Luna.dll`.

The offline acquisition supplement is generated deterministically from pinned, reviewed sources and validated stable IDs. See [Dependencies](DEPENDENCIES.md), [Data Sources](docs/DATA-SOURCES.md), and [Third-Party Notices](THIRD-PARTY-NOTICES.md).

## Support

Consult [Troubleshooting](docs/TROUBLESHOOTING.md) first. Once the repository is public, reproducible bugs, incorrect item/acquisition data, visual issues, and feature requests can be filed with the provided GitHub issue forms. See [Support](SUPPORT.md) and [Security Policy](SECURITY.md).

## Building from source

The validated build targets .NET 10, Windows x64, and Dalamud API 15. Install the .NET SDK selected by `global.json` and provide a compatible Dalamud development environment. `DALAMUD_HOME` may be set to the directory containing the required Dalamud development assemblies.

```powershell
dotnet restore CrystariumBoutique.sln --locked-mode
.\scripts\Build.ps1 -Configuration Release -SkipStage
```

The build script compiles the solution, validates runtime dependencies, and runs both automated test projects. Omit `-SkipStage` only for local native development staging.

## License and credits

The Crystarium Boutique is free software licensed under the **GNU Affero General Public License, version 3 only** (`AGPL-3.0-only`). Copyright © 2026 Aesthria. See [LICENSE](LICENSE), [COPYRIGHT.md](COPYRIGHT.md), and [CONTRIBUTING.md](CONTRIBUTING.md).

TCB uses or interoperates with work from the Dalamud/XIVLauncher ecosystem, Lumina, Glamourer, Penumbra, Critical-Impact/LuminaSupplemental, Newtonsoft.Json, .NET, xUnit.net, and other listed development dependencies. Each project remains independently maintained and governed by its own license. Detailed acknowledgements and distribution boundaries are in [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md). Runtime artwork sources and redistribution clearance are recorded in [Asset Provenance](docs/ASSET-PROVENANCE.md).

## Disclaimer

The Crystarium Boutique is an unofficial third-party project and is not affiliated with, endorsed by, or sponsored by Square Enix. FINAL FANTASY XIV and related names, trademarks, game data, icons, and assets belong to their respective rights holders.
