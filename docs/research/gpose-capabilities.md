# GPose Research Notes

Installed Dalamud API 15 XML documentation verifies `IClientState.IsGPosing`, `ITargetManager.GPoseTarget`, `IGameObject.ObjectIndex`, `IPlayerState.CharacterName`, `IPlayerState.HomeWorld`, and `IUiBuilder.DisableGposeUiHide`. Installed Glamourer API documentation verifies index-based appearance operations and a `GPoseChanged` event.

No supported API was identified for automatic GPose entry, camera positioning, framing, or lighting initialization, and those features remain permanently outside scope. The v0.8.8 amendment makes manual compatibility continuous: the plugin caches normal-world player identity, preserves the active Boutique session across `IClientState.IsGPosing` transitions, and prefers a local clone resolved from Dalamud's public target/object-table surfaces. Native v0.8.7 logs proved those surfaces can expose no mutable clone even while GPose is active. The guarded fallback therefore binds Glamourer's documented persisted player-name state and transfers the typed Boutique overlay through the name variants of `RevertState` and `SetItem`; capture, rollback, and refresh use their matching name operations. Exit still performs typed replay to normal index 0. There is no opaque cross-context snapshot application or GPose control behavior.

No reverse engineering is authorized or required for the Foundation sprint.
