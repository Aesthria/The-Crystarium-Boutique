# Development Environment

Initial audit date: 2026-08-25; current dependency baseline reviewed: 2026-09-09 (America/Los_Angeles)

This file intentionally omits user-profile paths, account data, character names, credentials, and machine-specific identifiers. The historical Foundation specification is retained only in the private development workspace and is intentionally excluded from the clean publication snapshot.

## Status

| Component | Audited status |
| --- | --- |
| Windows | Windows 11 25H2-compatible build `26200.9168`, x64. The legacy registry product label reports Windows 10 Pro; the build number is the authoritative OS indicator. |
| .NET SDK | `10.0.401` selected under the `10.0.400` `latestPatch` policy; `10.0.400` is also installed. |
| .NET runtime | `Microsoft.NETCore.App 10.0.11` installed. |
| dotnet MSBuild | `18.9.11` from SDK `10.0.401`. |
| Visual Studio | Visual Studio Community 2026 `18.9.1`, complete and launchable. |
| Visual Studio MSBuild | `18.9.1.35102`. |
| Git | `2.48.1.windows.1`. |
| FFXIV | Configured installation is present on a non-system volume; local game build marker is `2026.08.11.0000.0000`. |
| XIVLauncher | Installed and active. |
| Dalamud | Installed version `15.0.3.4`; API level 15; dev mode enabled. |
| Dalamud dev-plugin locations | None configured at initial audit; the user registered the staged local v0.1 DLL for native validation. Repository tooling does not modify this setting. |
| Glamourer | Compatibility baseline `1.7.1.0` (revision `b25174f5e41006b5e5f3a7c17e08b0220105c588`), protocol `1.8`, on Dalamud API 15. Boutique uses its own narrow IPC adapter and has no Glamourer assembly build/runtime dependency. |
| Penumbra | Compatibility baseline `1.7.1.1` (revision `ed20fe006c6eba3bc27f8bc380d04b961f5d4a49`; Penumbra.Api revision `2343b3c12997160ffbd1f283c5e076059a8e4377`), protocol `5.19`, on Dalamud API 15. Boutique uses its own narrow IPC adapter for the emergency redraw fallback and has no Penumbra assembly build/runtime dependency. |
| Workspace at start | One authoritative `.doc` specification; no source project and no Git repository. |

## Verified target

- Target framework: `net10.0-windows`.
- Dalamud API: 15.
- Build SDK: `Dalamud.NET.Sdk/15.0.0`.
- UI: Dalamud Windowing API with `Dalamud.Bindings.ImGui`.
- Unit-testable domain code remains independent of the running game.

Official Dalamud documentation identifies v15 as API level 15 on .NET 10, and the NuGet gallery publishes `Dalamud.NET.Sdk` 15.0.0. The installed Dalamud and plugin manifests agree with that baseline.

## Development and publication boundaries

- The canonical repository is `Aesthria/The-Crystarium-Boutique`. Repository visibility, public feeds, tags, and releases are owner-controlled publication actions and are not changed by local build tooling.
- The retained private-beta workflow is a historical/manual artifact path, not the final public-release channel. It uses a clean GitHub-hosted Windows 2025 runner and a SHA-256-pinned official Dalamud API 15 developer bundle; it does not install or require FFXIV or XIVLauncher.
- .NET telemetry is disabled in repository build scripts.
- Build caches and artifacts remain inside ignored workspace directories.
- Local XIVLauncher, Dalamud, FFXIV, Glamourer, and Penumbra settings are read for audit only and are not modified by build scripts.

## Native validation status

The v0.1 load/open/configuration/unload/reload gate passed on 2026-08-25. Local log inspection confirmed clean initialization and disposal with no Boutique exceptions. Each later milestone still requires its focused user-performed in-game smoke test.
