# Penumbra Integration

Status: implemented as an optional emergency restoration fallback through direct Dalamud IPC.

## Verified local baseline

- Installed Penumbra: `1.7.1.1` on Dalamud API 15. The 2026-09-09 comparison found the Boutique-consumed contract unchanged and not deprecated at breaking/feature API `5.19`.
- Verified Penumbra revision: `ed20fe006c6eba3bc27f8bc380d04b961f5d4a49`, with installed Penumbra.Api revision `2343b3c12997160ffbd1f283c5e076059a8e4377`.
- The exact Penumbra.Api project declares the `MIT` license through its `PackageLicenseExpression`. Boutique neither references nor redistributes `Penumbra.Api.dll`.
- Upstream project: https://github.com/xivdev/Penumbra

## Verified public surface relevant to the Boutique

The installed API documentation confirms typed subscribers/interfaces for:

- `ApiVersion`, expressed as breaking/feature version parts;
- `Initialized` and `Disposed` lifecycle events;
- `SupportedFeatures` and `CheckSupportedFeatures`;
- `GetCollectionForObject`;
- `GetChangedItemsForCollection`;
- `RedrawObject`;
- player/game-object path resolution and reverse resolution;
- temporary collection and temporary mod-setting APIs.

These APIs establish that collection context, changed-item metadata, safe redraw requests, and resource resolution can be implemented through a versioned public surface. Boutique uses only API-version/lifecycle detection and the redraw action described below; it does not manipulate Penumbra collections or mod state.

## Current boundary

`IPenumbraService` exposes availability, detected-version state, and a queued local-player redraw request. `PenumbraRedrawService` invokes Boutique-owned Dalamud IPC for `Penumbra.ApiVersion.V5`, Penumbra lifecycle events, and `Penumbra.RedrawObject.V5` only. The redraw action uses `(int objectIndex, int redrawType)` with verified wire values `Redraw = 0` and `AfterGPose = 1`.

The redraw is requested only as an emergency game-state restoration fallback when Glamourer unloads during an applied Boutique preview. If Penumbra is unavailable, Boutique retains the interrupted session and retries restoration when Glamourer returns. No Penumbra collection, path, temporary-mod, or appearance-state mutation is performed.

## Future adapter gate

Before any broader Penumbra feature is enabled:

1. Verify its public IPC contract against the exact upstream revision being targeted.
2. Query the breaking/feature API tuple and required feature flags at runtime.
3. Subscribe to initialization/disposal so dependency loss degrades gracefully.
4. Prefer read-only collection/changed-item queries.
5. Require a separate ADR and native test plan before temporary collections or mod settings are changed.
6. Treat redraw as an explicit, rate-limited request, not a per-frame operation.

Native FFXIV validation passed for the emergency redraw path after Glamourer unload. Contract compatibility was reconfirmed against installed Penumbra `1.7.1.1` / API `5.19` on 2026-09-09; this pre-publication revalidation does not replace the earlier native behavior test. Collection-aware and mod-state features remain intentionally out of scope.
