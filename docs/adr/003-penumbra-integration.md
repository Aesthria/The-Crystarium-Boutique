# ADR 003: Penumbra Integration

Decision: Treat Penumbra as an optional, narrowly scoped redraw provider behind `IPenumbraService`.

Context: Penumbra can expose collection, changed-item, redraw, and path-resolution behavior, but Glamourer already owns appearance state.

Options Considered: Direct Penumbra manipulation; read-only augmentation; no Penumbra awareness.

Chosen Approach: Keep normal appearance ownership in Glamourer. Use Boutique-owned, version-checked Dalamud IPC for Penumbra's public `RedrawObject.V5` action only when Glamourer disappears during an applied Boutique preview. The queued redraw reconstructs the local actor from game state after Glamourer finishes unloading. No collection, mod, path, or appearance data is changed through Penumbra.

Why: Glamourer's operational endpoints are removed before its `Disposed` notification, so it cannot accept `RevertState` at that point. Penumbra's queued redraw provides immediate visual cleanup without unsafe game memory access or a runtime `Penumbra.Api`/Luna dependency. If Penumbra is unavailable, Boutique retains the interrupted session and retries game-state restoration when Glamourer returns.

Tradeoffs: Penumbra remains optional. Native FFXIV verification passed for the queued local-player redraw after Glamourer unload. Modded indicators and collection-specific preview remain deferred.

Fallback: Boutique continues without mod-awareness. If Penumbra is unavailable during a Glamourer unload, Boutique retains the interrupted session and retries game-state restoration when Glamourer returns.

Patch Risk: Moderate; versioned third-party public API.
