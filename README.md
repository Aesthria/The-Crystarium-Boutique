# The Crystarium Boutique

Browse and preview gear like a transmog browser for FFXIV. Search by slot, filter by armor family, preview dyes, and save/share outfits.

Status: Early preview. Try On native interop is optional; a stub logs previews unless the symbol is enabled.

## Features
- In-game window with tabs per gear slot
- Search by name
- Filters
 - Armor family (Fending, Maiming, Striking, Scouting, Aiming, Casting, Healing, DoW/DoM, Crafting/Gathering, Universal)
 - Job filter for weapons (restricts to current job only)
 - Race exclusive tab to show race-locked models
- Preview item via Try On
 - Dye selection per slot; preview updates automatically
- Save/load outfits
 - Share outfits as JSON to clipboard

## Commands
- `/tcb` or `/boutique` — toggle the main window
- `/tcbdiag` — print Try On diagnostics and symbol state

## Build
- Requirements: .NET8 SDK, Dalamud dev environment
- Project: `TheCrystariumBoutique/TheCrystariumBoutique.csproj`
- Output includes `plugin.json` next to the DLL

To enable native Try On interop, define the symbol in the project file:

```xml
<!-- Enable native Try On calls -->
<DefineConstants>$(DefineConstants);ENABLE_TRYON</DefineConstants>
```

Without the symbol, previews log to chat as a stub.

## Installation (Dev)
1. Build the project in Debug or Release
2. Point Dalamud Dev Plugin Locations at the built `TheCrystariumBoutique.dll`
3. Enable the plugin from `/xlplugins`

## Notes
- Data is read from Lumina sheets at runtime.
- Icons are cached for performance and disposed properly.
- Job classification is cached to reduce reflection overhead.

## License
AGPL-3.0-or-later
