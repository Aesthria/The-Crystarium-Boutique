# Glamourer Integration

Status: v0.3 equipment and v0.5 dye adapters accepted; v0.6 reset, v0.7 loadouts, v0.7.5 linked weapons, v0.8.8 GPose handoff, and v0.9.0 imported-design replay use verified typed mutation operations.

## Verified local baseline

- Installed audit target: Glamourer `1.7.1.0` (implementation revision `b25174f5e41006b5e5f3a7c17e08b0220105c588`) on Dalamud API 15.
- Verified contract source: Glamourer.Api revision `ea569211a7c3500f2ce0b1b9223df85d8cb85f1a`, retained unchanged by the installed Glamourer `1.7.1.0` build.
- Boutique implements an original narrow Dalamud `ICallGateSubscriber` client. It does not reference or redistribute `Glamourer.Api.dll`, `Luna.dll`, or the Microsoft.Extensions assemblies formerly required by those wrappers.
- Installed implementation API tuple: `1.8` (`GlamourerApi.ApiVersion`).
- Upstream project: https://github.com/Ottermandias/Glamourer
- Glamourer requires Penumbra to be installed and active for its appearance behavior.

## Verified public surface relevant to the Boutique

The installed API documentation confirms typed subscribers/interfaces for:

- `ApiVersion` with major/minor compatibility semantics;
- `GetState`/`GetStateBase64` by game-object index and their player-name variants;
- `ApplyState` and `ApplyStateName` for a validated Glamourer state;
- `SetItem` and `SetItemName` for a single equipment slot, item/custom ID, and stain list;
- `ApiEquipSlot.MainHand` for the main or two-handed model and `ApiEquipSlot.OffHand` for shields or the off-hand component of certain weapons;
- `RevertState` and `UnlockState`;
- `StateChanged`, `StateChangedWithType`, and `GPoseChanged` events;
- typed `GlamourerApiEc` result codes including actor, item, key, state, and unknown failures.

Index-based operations are preferred over player-name operations. The adapter caches the current local player's name only for the lifetime of the plugin instance and uses it as a guarded GPose fallback because native v0.8.7 testing proved that Dalamud may expose no mutable clone index. The Boutique domain does not persist that identity or retain raw pointers.

## v0.3 implementation

`IAppearanceService` remains free of Glamourer types. `GlamourerAppearanceService` owns actor/GPose/session behavior and calls `IGlamourerIpcClient`. `GlamourerIpcClient` is the sole owner of verified labels, raw subscriber signatures, `HasFunction`/`InvokeFunc`, compatibility policy, and Initialized/Disposed subscriptions.

- Compatibility requires API major `1` and minor `8` or newer within that major.
- `ApiVersion`, index- and name-based `GetStateBase64`, `ApplyState`, `ReapplyState`, `RevertState`, and `SetItem` call gates must all report `HasFunction` before preview is enabled.
- Outside GPose, the original local-player state is captured from object index `0` as opaque Glamourer Base64 before a session becomes Active, and the loaded player's name/home world are cached locally for later clone validation. Inside GPose, the adapter prefers a valid local `GPoseTarget.ObjectIndex`, otherwise it enumerates Dalamud's public `IObjectTable.PlayerObjects` and accepts exactly one nonzero player object matching the cached character name and home world. Generic GPose clones without `IPlayerCharacter` world metadata are accepted only after exact name matching. Ambiguous or missing matches remain pending and never fall back to another actor.
- The resolved object index is bound for the complete Boutique session. A tile click calls `SetItem` for that bound actor with the selected source item ID, two explicit stain bytes, no lock key, and `ApplyFlag.Once`; later target changes cannot redirect an active session.
- A catalog item mapped by local game data to both weapon slots with a nonzero `ModelSub` is applied with the same source item ID to Main Hand and Off Hand. This follows the installed API's documented off-hand-component contract and avoids class/item-name heuristics.
- The installed API exposes no distinct dye-only subscriber. A v0.5 dye click updates one validated channel in the controller's selected `AppearanceSelection`, preserves the other channel, and calls the same typed `SetItem` operation with the current item ID and both stain bytes.
- A successful head-slot mutation is followed by one `ReapplyState` call with `ApplyFlag.Once` to refresh the actor after the reported AFK/visor redraw loss. It does not force hat or visor meta state.
- Preview state is recaptured after every successful mutation while the original snapshot remains unchanged.
- The installed API exposes whole-state `RevertState` but no per-slot revert. Slot reset returns equipment/customization to game state, then replays all other explicit Boutique-changed slots through `SetItem`. A replay/recapture failure reapplies the prior preview snapshot before returning the error.
- Reset All uses `RevertState` and clears explicit changed-slot state only after success. No opaque Base64 is parsed or patched.
- A v0.7 loadout persists the Boutique controller's typed changed-slot records, including source item IDs and complete stain values. It never persists, interprets, or edits a `GetState` object/Base64 payload.
- Loading returns the actor to game state, replays every validated saved slot through `SetItem`, and recaptures the resulting opaque preview. Any replay/capture failure reapplies the previous preview before returning an error; changed-slot state is replaced only after complete success.
- Reset and lifecycle restore call `RevertState` with `Equipment | Customization`, matching Glamourer's own **Revert to Game** operation. Captured snapshots are retained for session comparison and transactional rollback, not used as the game-state reset baseline.
- If a close-time restore fails, the session remains `RestoreFailed`; a later close or deterministic plugin-disposal cleanup retries the same captured original instead of clearing the failed session.
- Entering or leaving GPose does not close the window or session. The adapter immediately releases the obsolete actor binding and prefers an automatically resolved destination index. If no mutable GPose index is exposed, it binds the cached local player through Glamourer's documented name surface instead. The controller calls the matching index/name `RevertState` operation and replays every typed Boutique-changed equipment/dye record through `SetItem` or `SetItemName`. Exit performs the inverse operation against normal local-player index `0`. A handoff failure reverts the destination state again and never applies the prior clone snapshot.
- While the fallback is active, capture, transactional snapshot rollback, head refresh, item/dye changes, Reset Slot, Reset All, close, and unload all use the matching typed name endpoints. Glamourer's API explicitly applies name mutations to all current or persisted player states with that name, which lets the GPose clone and normal persisted state stay synchronized even when no usable clone index exists.
- `Success` and idempotent `NothingDone` are accepted for mutation; all other `GlamourerApiEc` values are mapped into Boutique errors.
- Lifecycle Initialized/Disposed subscriptions and all appearance operations use Boutique-owned call gates verified against the exact upstream API revision. All subscriptions are disposed during plugin shutdown.
- Automatic Wardrobe Sync consumes the unchanged `Penumbra.StateFinalized` event as `Action<IntPtr, StateFinalizationType>`. Only exact `Gearset = 9` notifications for the nonzero current local-player pointer outside GPose are accepted. They queue a generation-tracked request for the next safe update pass; active previews are first relinquished and captured only after the matching revert finalization. Other actors, GPose, unrelated finalization types, duplicate lifecycle subscriptions, superseded generations, and disabled synchronization are rejected without polling, timers, or arbitrary delays.
- Glamourer unregisters its operational IPC providers before invoking `Glamourer.Disposed`, so `RevertState` is no longer callable at dependency-loss notification time. When a Boutique mutation is active, the adapter therefore queues the local actor through Penumbra's independently versioned `RedrawObject.V5` action. This redraws from game state on the following framework cycle. If Penumbra is absent, the interrupted session remains recoverable and is reverted first when Glamourer is re-enabled.
- Glamourer and Penumbra remain separately installed providers. Missing or disabled Glamourer produces browse-only mode and cannot cause a CLR assembly-load failure.

## Native acceptance gate

Repeated normal-world equipment previews, one/two-slot dyes, close/unload restoration, AFK-return head redraw, earlier selectable grids, reset, Designs, linked weapons, Settings, combat safety, and the v0.8.2 browser passed in game with the former typed-wrapper implementation. v0.8.7 established that session/UI carry-in and linked weapons work but the clone index can remain unavailable. v0.8.8 passed focused native verification of name-targeted GPose behavior. The Boutique-owned raw-IPC migration passes automated build/contract/package validation. The unchanged contracts and full Boutique regression baseline were native-validated against Glamourer `1.7.1.0` on 2026-09-09.
