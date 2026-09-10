# Penumbra API Research Notes

Audit target: installed `Penumbra 1.7.1.1` (revision `ed20fe006c6eba3bc27f8bc380d04b961f5d4a49`, Penumbra.Api revision `2343b3c12997160ffbd1f283c5e076059a8e4377`), Dalamud API 15. The 2026-09-09 comparison found the Boutique-consumed IPC unchanged and not obsolete at breaking/feature API `5.19`.

Evidence used:

- installed manifest and assembly version metadata;
- installed `Penumbra.Api.dll`;
- complete installed `Penumbra.Api.xml` subscriber/interface documentation;
- upstream repository metadata at https://github.com/xivdev/Penumbra.

Verified subscriber families potentially relevant later: breaking/feature API version, initialized/disposed events, feature negotiation, object collection lookup, changed-item queries, redraw, resource resolution, and temporary collection/mod state.

Deferred research questions for any future expansion beyond redraw:

- which feature strings must be negotiated for future Boutique requirements;
- reliable and inexpensive modded/replaced indicators for appearance tiles;
- whether a Boutique-specific temporary collection adds enough value to justify mutation risk.

The installed provider reports breaking/feature API `5.19`. Boutique uses only the verified `Penumbra.RedrawObject.V5` action with `(int objectIndex, int redrawType) -> void`, where `Redraw = 0` and `AfterGPose = 1`, as an optional game-state redraw fallback when Glamourer unloads during an applied preview. The object index remains a Dalamud game-object-table index and the call is queued rather than applied synchronously. Native FFXIV validation passed for immediate local-player restoration; contract compatibility was reconfirmed on installed Penumbra `1.7.1.1` on 2026-09-09. No Penumbra collection, mod, path, or temporary-state mutation is used, and Boutique does not reference or distribute `Penumbra.Api.dll` or Luna.
