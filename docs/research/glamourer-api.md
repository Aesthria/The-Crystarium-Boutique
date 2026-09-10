# Glamourer API Research Notes

Audit target: installed `Glamourer 1.7.1.0` (implementation revision `b25174f5e41006b5e5f3a7c17e08b0220105c588`), unchanged Glamourer.Api revision `ea569211a7c3500f2ce0b1b9223df85d8cb85f1a`, and Dalamud API 15. The 2026-09-09 revalidation found every Boutique-consumed label, signature, enum wire value, lifecycle event, and semantic contract unchanged and not obsolete.

Evidence used:

- installed manifest and assembly version metadata;
- installed `Glamourer.Api.dll` assembly metadata and public contract surface;
- complete installed `Glamourer.Api.xml` subscriber/interface documentation;
- upstream repository/release metadata at https://github.com/Ottermandias/Glamourer.

Verified subscriber families relevant to Boutique: API version, state capture, state apply, single-item/stain apply, reapply, revert, and lifecycle events. `UnlockState`, state-change events, and Glamourer's GPose event are not consumed. Boutique detects GPose through Dalamud and never requests a persistent state lock.

Verified implementation details for v0.3:

- `Glamourer.ApiVersion.V2` uses `ICallGateSubscriber<(int major, int minor)>`; the installed implementation exposes `1.8`.
- Required operational labels are `Glamourer.GetStateBase64`, `Glamourer.GetStateBase64Name`, `Glamourer.ApplyState`, `Glamourer.ApplyStateName`, `Glamourer.ReapplyState`, `Glamourer.ReapplyStateName`, `Glamourer.RevertState`, `Glamourer.RevertStateName`, `Glamourer.SetItem.V3`, and `Glamourer.SetItemName.V2`.
- `GetStateBase64` uses object index and key and returns `(int errorCode, string state)`; its name form replaces the index with a player name.
- `SetItem.V3` uses `(int objectIndex, byte slot, ulong itemId, IReadOnlyList<byte> stains, uint key, ulong flags) → int`; its name form uses the exact corresponding player-name signature.
- `ApplyState` deliberately receives the opaque Base64 string boxed as `object`. `SetItem` receives a concrete two-element `List<byte>` to retain the verified stain serialization behavior.
- Boutique accepts API major 1, minor 8 or newer only after every operational call gate reports `HasFunction`. It re-runs detection on `Glamourer.Initialized` and marks the provider unavailable on `Glamourer.Disposed`.
- Automatic Wardrobe Sync uses the retained historical IPC label `Penumbra.StateFinalized` with `Action<IntPtr, StateFinalizationType>`. `Gearset` remains wire value `9`; Boutique filters to the current local-player pointer outside GPose and defers generation-tracked synchronization to a safe update pass.
- The runtime adapter is original Boutique code built on Dalamud `ICallGateSubscriber`; `Glamourer.Api.dll` and `Luna.dll` are neither referenced nor packaged.
- The installed `ApiEquipSlot` documentation identifies Main Hand as the main/two-handed weapon slot and Off Hand as the shield slot or the off-hand component of certain weapons. v0.7.5 uses this contract only when the installed Item row maps the same source item to both visible weapon slots and exposes a nonzero `ModelSub`.
- `ApplyState.Invoke(string, int, uint, ApplyFlag)` is used for restore with `Once | Equipment | Customization`.
- `ReapplyState.Invoke(int, uint, ApplyFlag.Once)` is used once after a successful head-slot mutation to request a supported redraw without changing visor/hat meta state.
- v0.7.5 also issues one `ReapplyState(..., Once)` after successful whole-snapshot application so reset, rollback, and close/unload restoration refresh weapon/equipment redraw state without acquiring a lock.
- Installed `ApplyFlagEx` values are `DesignDefault=7`, `StateDefault=14`, and `RevertDefault=6`; Boutique deliberately avoids the `Lock` bit.
- `Initialized.Subscriber` and `Disposed.Subscriber` return disposable event subscribers with explicit enable/disable support.
- The API assembly references only .NET 10, Dalamud 15.0.3, and Newtonsoft.Json 13.

Repeated-preview and exact close/unload restoration passed native validation. Repeated AFK camera returns also passed the focused v0.4.1 head-reapply regression without requiring visor or hat-visibility toggles. Multi-channel dye editing remains assigned to v0.5.

The v0.5 re-audit found no separate dye-only subscriber in the complete installed IPC type list. `SetItem.V3` is the verified public mutation accepting an item/custom ID and stain payload. Boutique therefore retains the selected item and both stain bytes in game-independent session state, updates one validated channel, and reissues the same endpoint with the numeric one-shot flag. It does not infer an endpoint or patch the opaque state payload.
