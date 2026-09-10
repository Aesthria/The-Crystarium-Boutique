# Dependencies

Status as of 2026-09-09. Versions are pinned for repeatable local and release-candidate builds.

## Build and runtime

| Dependency | Version/target | Purpose | Source of truth |
| --- | --- | --- | --- |
| .NET SDK | `10.0.400` | Compiler, restore, build, and test | Local `dotnet --info` |
| Dalamud | API 15; current staged assembly reference `15.0.3.3` | Separately installed plugin runtime, local item-rarity color lookup, and plugin icon discovery | Local runtime plus https://dalamud.dev/versions/v15/ |
| Dalamud.NET.Sdk / DalamudPackager | `15.0.0` | Build-time plugin references, manifest generation, and packaging | https://www.nuget.org/packages/Dalamud.NET.Sdk/15.0.0 |
| Lumina / Lumina.Excel | Observed assembly `7.0.0.0` | Host-provided local game-data access; standalone development generator validation | Installed Dalamud development/runtime environment |
| Newtonsoft.Json | Observed assembly `13.0.0.0` | Host-provided JSON interoperability | Installed Dalamud development/runtime environment |
| DotNet.ReproducibleBuilds | `1.2.39` | Deterministic build metadata; private build asset | https://www.nuget.org/packages/DotNet.ReproducibleBuilds/1.2.39 |
| xUnit.net v3 | `4.0.0` | Domain unit tests | https://www.nuget.org/packages/xunit.v3/ |
| Microsoft.NET.Test.Sdk | `18.9.0` | `dotnet test` integration | https://www.nuget.org/packages/Microsoft.NET.Test.Sdk/ |

`Dalamud.NET.Sdk` is the supported plugin SDK path. The development-only acquisition generator and plugin IPC test project resolve their compile-time Dalamud/Lumina assemblies through `BoutiqueDalamudHome`: `DALAMUD_HOME` when provided, otherwise the local XIVLauncher Hooks development directory. These references are never copied into the Boutique package. Automated clean builds may use the official API 15 developer bundle after verifying its reviewed SHA-256 and Dalamud assembly version; FFXIV and XIVLauncher are not build dependencies in that environment. The implementation, SDK, linked/host-provided libraries, IPC providers, and build-only packages retain separate licensing relationships documented in [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).

## Optional in-game integrations

| Integration | Installed version | Compile-time state in v0.10 |
| --- | --- | --- |
| Glamourer | Separately installed; API major `1`, minor `8` or newer | Offline Dalamud IPC provider for capture, preview, restore, linked equipment/dyes, GPose handoff, and version/lifecycle handling. |
| Penumbra | Separately installed; breaking API `5` | Optional offline `RedrawObject.V5` fallback after Glamourer unloads during an applied preview; no collection/mod mutation. |

Boutique owns narrow IPC clients built directly on Dalamud `ICallGateSubscriber`. Glamourer labels, signatures, and numeric wire values were reverified against installed Glamourer `1.7.1.0` (revision `b25174f5e41006b5e5f3a7c17e08b0220105c588`) and unchanged Glamourer.Api revision `ea569211a7c3500f2ce0b1b9223df85d8cb85f1a`; the provider still reports API `1.8`. Penumbra's `ApiVersion.V5`, lifecycle events, `RedrawObject.V5` signature, and redraw enum values were reverified against installed Penumbra `1.7.1.1` (revision `ed20fe006c6eba3bc27f8bc380d04b961f5d4a49`) / API `5.19` (Penumbra.Api revision `2343b3c12997160ffbd1f283c5e076059a8e4377`). The newer builds remain backward-compatible with Boutique's existing minimum-version policy, so no requirement was raised. Boutique references or redistributes neither API assembly. `Luna.dll` and the three Microsoft.Extensions assemblies formerly pulled into the package by those wrappers are not build or runtime dependencies. Missing or disabled Glamourer leaves Boutique in browse-only mode; missing Penumbra disables only immediate unload redraw recovery, with restoration retried when Glamourer returns.

The acquisition index uses Lumina sheets already exposed by the installed Dalamud API 15 runtime plus a packaged, versioned local JSON supplement. A separate .NET 10 development generator reads pinned Critical-Impact/LuminaSupplemental `5.1.4` CSV data (`e9bf1495bf8588d2cf21441ef03f0d9dfe0f790a`, `GPL-3.0-only`) and validates exact Item, ContentFinderCondition, TerritoryType, Treasure, Map, and BNpcName IDs against standalone Lumina `7.0.0.0` and local game build `2026.08.11.0000.0000`. The older six-record Tracky sample remains a development fixture under its MIT license. The generator, raw inputs, manifest, and review output are not runtime dependencies; the plugin performs no acquisition-data download or other runtime network access.

The v0.17.3 audit includes `InstanceContent`, `InstanceContentRewardItem`, `ContentFinderCondition`, `Treasure`, and battle-NPC sheet shapes. None provides a reliable reverse item-to-duty/boss/chest loot relation in the installed base data. Exact boss/chest rows can therefore come only from the verified local supplement, while separately indexed sources for an item sharing the same appearance remain explicitly labeled as aliases and are never presented as the hovered item's direct drop.

## Dependency policy

- Pin package versions; update deliberately after an environment audit.
- Prefer local game data and public Dalamud APIs.
- Keep optional plugin integrations behind narrow interfaces and runtime version checks.
- Keep third-party IPC labels and signatures in one adapter, verify them against an exact upstream contract revision, and gate every operation through runtime version/endpoint checks.
- Do not add native-hook packages or unsafe dependencies to the stable path without a dedicated ADR and fallback.
