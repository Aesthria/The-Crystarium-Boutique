# Third-Party Notices

This notice describes third-party software, data, and interoperability relevant to The Crystarium Boutique. Boutique itself is licensed under `AGPL-3.0-only`; each third-party project remains governed by its own license.

The release package contains Boutique assemblies, Boutique data/assets, and legal/testing documents only. It does not bundle an FFXIV installation, XIVLauncher/Dalamud, Glamourer, Penumbra, Lumina, Newtonsoft.Json, player configuration, or local Design data.

## Runtime platform and data

### Dalamud implementation

- Project: Dalamud, maintained by XIVLauncher and Dalamud contributors.
- Purpose: independently installed plugin host and runtime API.
- Compatibility: Boutique targets Dalamud API 15. The exact installed implementation commit is not pinned by this repository.
- License: the current official Dalamud repository publishes the implementation under GNU Affero General Public License v3. The exact license at the installed implementation revision remains **MANUAL VERIFICATION REQUIRED** before v0.1.0.
- Distribution: Dalamud is separately installed by the user; its assemblies are not copied into the Boutique release package.
- Source: <https://github.com/goatcorp/Dalamud>

### Dalamud.NET.Sdk and DalamudPackager

- Project: `Dalamud.NET.Sdk` / `DalamudPackager`, maintained by the Dalamud project.
- Purpose: MSBuild plugin references, manifest generation, and packaging during development.
- Version: `15.0.0`, pinned by the project SDK declaration and lock files.
- License: `Dalamud.NET.Sdk` `15.0.0` declares `MIT` in its official NuGet metadata. The exact `DalamudPackager` package notice should be reconfirmed with the final locked package during release validation.
- Distribution: build-time only; neither package is copied into the Boutique runtime ZIP.
- Source: <https://github.com/goatcorp/Dalamud.NET.Sdk> and <https://www.nuget.org/packages/Dalamud.NET.Sdk/15.0.0>

### Lumina

- Purpose: host-provided local FFXIV game-data access at runtime and standalone game-data validation in the development acquisition generator.
- Observed assembly version: `7.0.0.0` in the validated Boutique runtime dependency inspection. The exact source commit used by the installed Dalamud bundle is not pinned by Boutique.
- License: the current official Lumina repository publishes `WTFPL`; the exact license/revision corresponding to the host-provided `7.0.0.0` assembly remains **MANUAL VERIFICATION REQUIRED** before v0.1.0.
- Distribution: Lumina assemblies are marked non-private or resolved from the development environment and are not copied into the Boutique release package.
- Source: <https://github.com/NotAdam/Lumina>

### Newtonsoft.Json

- Purpose: JSON support exposed by the installed Dalamud development/runtime environment.
- Observed assembly version: `13.0.0.0` in the validated Boutique runtime dependency inspection. The exact package/source revision supplied by the installed host is not pinned by Boutique.
- License: the official Newtonsoft.Json source publishes the library under `MIT`; the exact host-supplied revision remains **MANUAL VERIFICATION REQUIRED** before v0.1.0.
- Distribution: the reference is marked non-private and `Newtonsoft.Json.dll` is not copied into the Boutique release package.
- Source: <https://github.com/JamesNK/Newtonsoft.Json>

### .NET

- Project owner: .NET Foundation and contributors.
- Purpose: target framework, compiler, runtime, and build tooling.
- License: MIT; the retained notice is [`third-party/licenses/dotnet-runtime-MIT.txt`](third-party/licenses/dotnet-runtime-MIT.txt).
- Distribution: the .NET runtime is not bundled in the Boutique plugin package.

## Optional plugin interoperability

### Glamourer

- Project: Glamourer, maintained in the Ottermandias organization.
- Purpose: optional appearance capture, preview, dye, linked-equipment, and restoration operations through Dalamud IPC.
- Contract revision: Glamourer `1.7.1.0` revision `b25174f5e41006b5e5f3a7c17e08b0220105c588`; unchanged Glamourer.Api `ea569211a7c3500f2ce0b1b9223df85d8cb85f1a`; protocol `1.8`.
- License: Apache License 2.0; retained text: [`third-party/licenses/Glamourer-APACHE-2.0.txt`](third-party/licenses/Glamourer-APACHE-2.0.txt).
- Distribution: Glamourer and Glamourer.Api are not bundled. Boutique contains an original IPC adapter that reproduces only endpoint labels, signatures, and numeric wire values required for interoperability.
- Source: <https://github.com/Ottermandias/Glamourer> and <https://github.com/Ottermandias/Glamourer.Api/tree/ea569211a7c3500f2ce0b1b9223df85d8cb85f1a>

### Penumbra

- Project: Penumbra, maintained by the Penumbra/xivdev contributors.
- Purpose: optional emergency object-redraw fallback through Dalamud IPC after Glamourer unloads during an active preview.
- Contract revision: Penumbra `1.7.1.1`, revision `ed20fe006c6eba3bc27f8bc380d04b961f5d4a49`, API `5.19`; Penumbra.Api revision `2343b3c12997160ffbd1f283c5e076059a8e4377`.
- License: the verified Penumbra.Api contract revision declares `MIT` in its project metadata. Re-check the independently installed Penumbra implementation's license at release time if it is to be described beyond this IPC boundary.
- Distribution: Penumbra and Penumbra.Api are not bundled. Boutique contains an original IPC adapter that reproduces only the verified API/version/lifecycle/redraw endpoint contract and numeric wire values.
- Source: <https://github.com/xivdev/Penumbra/tree/ed20fe006c6eba3bc27f8bc380d04b961f5d4a49> and <https://github.com/Ottermandias/Penumbra.Api/tree/2343b3c12997160ffbd1f283c5e076059a8e4377>

Glamourer and Penumbra are independently installed, independently maintained projects. Their names are used only to identify compatibility. They are neither part of Boutique nor covered by Boutique's copyright claim, and their developers do not automatically endorse or maintain Boutique.

## Acquisition-data tooling

### LuminaSupplemental acquisition data

- Project: Critical-Impact/LuminaSupplemental.
- Purpose: pinned build-time data for exact Item-to-duty, duty-chest, boss-chest, and boss-drop relationships. Boutique uses only `DungeonBoss.csv`, `DungeonBossChest.csv`, `DungeonBossDrop.csv`, `DungeonChest.csv`, `DungeonChestItem.csv`, and `DungeonDrop.csv`.
- Version/revision: `5.1.4`, commit `e9bf1495bf8588d2cf21441ef03f0d9dfe0f790a`.
- License: `GPL-3.0-only`; the complete retained and distributed text is [`tools/CrystariumBoutique.AcquisitionGenerator/supplemental-data/LuminaSupplemental-5.1.4/LICENSE`](tools/CrystariumBoutique.AcquisitionGenerator/supplemental-data/LuminaSupplemental-5.1.4/LICENSE).
- Integration: the pinned CSV inputs are consumed only by Boutique's original offline generator. The released plugin does not load LuminaSupplemental assemblies, source code, or network services. Its packaged normalized JSON preserves the exact upstream revision, per-file hash, source row, stable Item/ContentFinderCondition IDs, and boss provenance used to produce each relationship.
- Source: <https://github.com/Critical-Impact/LuminaSupplemental/tree/e9bf1495bf8588d2cf21441ef03f0d9dfe0f790a>

### XIVStats/Tracky sample data

- Copyright: Copyright © 2023 Infi `<infiziert@protonmail.ch>`.
- Purpose: a pinned six-record `ChestDropsV2` development sample used to exercise the offline acquisition-supplement generator.
- Revision: `97134b09bb13e062262fb3522f6835e0bd123e62`.
- License: MIT; retained text: [`tools/CrystariumBoutique.AcquisitionGenerator/sample-data/TRACKY-LICENSE.txt`](tools/CrystariumBoutique.AcquisitionGenerator/sample-data/TRACKY-LICENSE.txt).
- Distribution: no Tracky client or full dataset is included in the runtime package. The packaged supplement preserves reviewed provenance for validated factual item/source relationships.
- Source: <https://github.com/Infiziert90/FFXIVGachaSpreadsheet/tree/97134b09bb13e062262fb3522f6835e0bd123e62>

## Development and test tooling

- `DotNet.ReproducibleBuilds` `1.2.39`: .NET Foundation project; official package metadata declares `MIT`; build-only with `PrivateAssets=All`, and not included in the runtime package.
- Microsoft.NET.Test.Sdk `18.9.0`: Microsoft; local NuGet metadata declares `MIT`; development/test only.
- xUnit.net v3 `4.0.0`: xUnit.net contributors; local NuGet metadata declares `Apache-2.0`; development/test only.
- Transitive Microsoft Testing Platform, TestHost, CodeCoverage, Application Insights, BCL, registry/access-control, and xUnit component packages are locked for testing only and are not included in the Boutique runtime package.
- Standalone Lumina/Serilog references used by the acquisition generator are resolved from the local Dalamud development environment and are not included in the plugin package. Their exact revisions and license notices remain **MANUAL VERIFICATION REQUIRED** before distributing the generator as a separate binary.

## Historical audit material

The retained [`third-party/licenses/Luna-AGPL-3.0.txt`](third-party/licenses/Luna-AGPL-3.0.txt) records the license of a dependency removed before this release. Boutique does not reference or redistribute Luna. That historical license file is not the basis for Boutique's own license selection; Boutique independently uses `AGPL-3.0-only` as stated in the root [LICENSE](LICENSE).

## FINAL FANTASY XIV and Square Enix

FINAL FANTASY XIV names, trademarks, game data, icons, and other game assets are the property of Square Enix and their respective owners. Their presence or runtime use does not transfer ownership to Aesthria or Boutique contributors.

The Crystarium Boutique is an independent, unofficial fan project. It is not affiliated with, endorsed by, sponsored by, or otherwise associated with Square Enix Holdings Co., Ltd., Square Enix Co., Ltd., the FINAL FANTASY XIV development team, XIVLauncher/Dalamud, Glamourer, or Penumbra.
