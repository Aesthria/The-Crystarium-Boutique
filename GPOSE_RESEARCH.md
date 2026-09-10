# GPose Capability Research

Status: Phase 0 research with a narrow v0.8.8 continuous-handoff amendment. GPose entry/workflow features remain removed; an active Boutique session follows the logged-in player into and out of GPose.

## Capability matrix

| Capability | Classification | Foundation decision |
| --- | --- | --- |
| Detect whether GPose is active | Supported public Dalamud API | Used to hand the same bound Boutique session between normal index 0 and the GPose clone. |
| Access GPose target | Supported public Dalamud API | Preferred when it already identifies the local player, but not required for the handoff. |
| Receive GPose enter/leave from Glamourer | Third-party public API | Audited only; no subscriber will be created. |
| Keep Boutique UI visible in GPose | Supported public Dalamud API | Enabled so the normal Boutique window can be opened manually. |
| Initiate GPose | Unknown/requires research | Out of scope and unnecessary for the accepted workflow. |
| Detect successful transition | Supported after manual entry | Out of scope. |
| Identify the GPose actor | Supported target, player state, and object table | Cache the local identity, prefer a matching current GPose target, otherwise enumerate `IObjectTable.PlayerObjects` for exactly one nonzero player with the same name and compatible home world, then bind once. Generic GPose clones do not always expose `IPlayerCharacter` world metadata. |
| Initialize camera/framing | Unknown/requires research | Out of scope. |
| Initialize lighting | Unknown/requires research | Out of scope. |
| Preserve normal GPose controls | Plugin-owned behavior | Guaranteed by adding no input, camera, lighting, pose, or control hooks. |
| Detect GPose transition | Supported public Dalamud API | Existing UI callback compares `IClientState.IsGPosing`, releases the obsolete actor binding, and starts a guarded destination handoff. |
| Transfer Boutique session | Plugin state machine + verified appearance API | Prefer a destination actor index; when none exists, bind Glamourer's persisted player-name state. Call the matching revert operation and replay all typed Boutique-changed slots/dyes without closing the session. Exit targets normal index `0`. |

## Final product decision

The Boutique remains a normal-world-first workflow and never initiates or configures GPose. v0.8.8 keeps the existing window and session open when the player independently enters GPose, prefers an exact local clone index, and falls back to Glamourer's documented persisted player-name state when Dalamud exposes no mutable clone. Changes made through the Boutique in GPose are applied to Glamourer's current/persisted states and replayed to normal index `0` on exit. The handoff occurs only on the observed context transition; later target changes cannot redirect it. No native actor scanning, automatic GPose entry, signatures, offsets, memory writes, game commands, camera hooks, or lighting hooks are introduced.
