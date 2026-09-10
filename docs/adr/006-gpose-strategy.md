# ADR 006: GPose Strategy

Decision: Do not implement a GPose workflow; allow a narrow continuous appearance-session handoff to either the identified local GPose clone or Glamourer's persisted local-player state.

Context: The Phase 0 audit found public GPose state, target, and object-table surfaces but no supported automatic entry/camera/lighting APIs. Product discussion rejected a forced GPose workflow. Later native use established a narrower requirement: players who independently enter GPose should keep using the ordinary Boutique and see previews on their local clone without manually selecting it.

Options Considered: Native hooks; automatic entry/game commands; full GPose workflow; no GPose behavior; target-only compatibility; exact local-clone resolution through the public object table.

Chosen Approach: Keep the Boutique normal-world-first and add no GPose-specific UI or controls. Cache loaded-player identity in normal play. When GPose begins, preserve the ordinary window, session, selections, and typed changed-slot state; release index 0; prefer a valid matching `ITargetManager.GPoseTarget`; otherwise enumerate `IObjectTable.PlayerObjects` for exactly one matching nonzero local clone. If neither index exists, bind Glamourer's documented persisted player-name state. Revert the selected index/name target and replay the Boutique overlay through the matching Boutique-owned IPC call gates. On exit, bind normal index 0 and replay the current overlay so GPose changes carry back. Do not apply opaque clone snapshots to the normal actor, redirect after binding, enter GPose, or control camera/lighting.

Why: This preserves the accepted unforced workflow while allowing the same preview experience in a state the player chose independently. Exact name/home-world matching plus one-time actor binding prevents target dependence and uses only supported public surfaces.

Tradeoffs: The player must enter GPose themselves. Glamourer's name-based API intentionally manipulates all current or persisted player states sharing the exact name, so the fallback is used only for the cached local identity and only when no mutable clone index exists. Only typed Boutique-changed slots/dyes are portable across contexts; no camera, lighting, pose, or actor-management assistance is provided.

Fallback: If an exact local GPose clone is unavailable but cached local identity exists, use Glamourer's persisted name target instead of leaving the handoff permanently pending. If replay fails after binding, return the destination state to game state, retain the Boutique's typed session state, and report the error without applying an opaque cross-context snapshot.

Patch Risk: Low. `IClientState.IsGPosing`, `ITargetManager.GPoseTarget`, `IObjectTable.PlayerObjects`, `IGameObject.ObjectIndex`, and Glamourer's documented index/name APIs are public surfaces; no signatures, pointers, or raw structures are used.
