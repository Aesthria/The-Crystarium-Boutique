# Phase 7E Refreshed Publication Snapshot Inventory

This inventory classifies the current native-validated worktree, including the Penumbra 1.7.1.1 compatibility audit, for the refreshed history-free Phase 7E publication candidate. The inventory itself does not stage, commit, publish, or alter Git history.

Classification:

- **A — INCLUDE:** maintained public source, tests, runtime data/assets, build tooling, legal/provenance material, repository support, and current public documentation.
- **B — PUBLIC HISTORICAL / DEVELOPER:** maintained research, ADR, historical beta, and design records that are safe and useful to publish.
- **C — EXCLUDE:** private planning material, obsolete assets, local/generated review output, build artifacts, caches, or other material not required for a public source snapshot.
- **D — OWNER DECISION REQUIRED:** none for this candidate.

## A — Include in the clean publication candidate

| Path(s) | Reason |
| --- | --- |
| `.editorconfig`, `.gitattributes`, `.gitignore` | Repository formatting, attributes, and safe ignore policy. |
| `.github/ISSUE_TEMPLATE/*.yml`, `.github/workflows/private-beta.yml` | Approved issue intake and retained historical/manual validation workflow. |
| `LICENSE`, `COPYRIGHT.md`, `CONTRIBUTING.md`, `SECURITY.md`, `SUPPORT.md`, `THIRD-PARTY-NOTICES.md` | Required public legal, contribution, security, support, and notice material. |
| `README.md`, `CHANGELOG.md` | Current project entry point and historical change record. |
| `CrystariumBoutique.sln`, `Directory.Build.props`, `global.json` | Required solution and deterministic build metadata. |
| `scripts/*.ps1`, `scripts/RuntimePackageFiles.psd1` | Maintained build, test, staging, generation, and package-validation tools. |
| `src/CrystariumBoutique.Core/**` | Maintained domain source and lock file. |
| `src/CrystariumBoutique/**` except the obsolete frame named below | Maintained plugin source, manifest, lock file, acquisition runtime data, and allowlisted runtime images. This includes current Paladin/Beastmaster handling and the shared Favorites Revert control. |
| `tests/**` | Complete maintained Core and plugin regression suites, including current Favorites Revert, Paladin, and Beastmaster coverage. |
| `tools/CrystariumBoutique.AcquisitionGenerator/**` except `supplemental-output/generation-review.json` | Generator source/project, lock file, sample evidence, six pinned LuminaSupplemental CSV inputs, upstream GPL license, `source.json`, and deterministic generation manifest. |
| `third-party/**` | Canonical notice pointer and retained third-party license texts used by dependency audits. |
| `docs/ASSET-PROVENANCE.md`, `docs/DATA-SOURCES.md`, `docs/DESIGN-IMPORT.md`, `docs/FEATURES.md`, `docs/INSTALLATION.md`, `docs/PUBLICATION-SNAPSHOT.md`, `docs/PUBLIC_RELEASE_LEGAL_CHECKLIST.md`, `docs/TROUBLESHOOTING.md` | Current public/release-maintainer documentation. |

## B — Public historical / developer material

| Path(s) | Reason |
| --- | --- |
| `ARCHITECTURE.md`, `BROWSER_UX.md`, `DATA_MODEL.md`, `DYE_STUDIO.md`, `GLAMOURER_INTEGRATION.md`, `GPOSE_RESEARCH.md`, `LOADOUTS.md`, `OUTFIT_CONTEXT.md`, `PATCH_COMPATIBILITY.md`, `PENUMBRA_INTEGRATION.md`, `TEST_PLAN.md` | Maintained architecture, design, integration, compatibility, and validation history. |
| `docs/adr/*.md` | Maintainer architecture decisions. |
| `docs/research/*.md` | Maintainer research and verified contract/data records. |
| `docs/PRIVATE-BETA-TESTING.md` | Explicitly labeled historical private-beta workflow; not current public installation guidance. |

## C — Exclude

| Path(s) | Reason |
| --- | --- |
| `The Crystarium Boutique - Foundation.doc` | Private historical planning/specification material with document-author metadata; not needed to build, test, run, license, or maintain the public source. |
| `src/CrystariumBoutique/images/crystarium-item-frame.png` | Obsolete, unused, and absent from the runtime package allowlist. The approved current asset is `crystarium-item-frame-cornered.png`. |
| `tools/CrystariumBoutique.AcquisitionGenerator/supplemental-output/generation-review.json` | Large generated review/quarantine output; not required to reproduce accepted output or run the generator. |
| `.git/**` | Existing private history, including the prohibited superseded Revert blob; a clean candidate must contain no inherited Git metadata. |
| `.codex-spec/**`, `.dotnet-cli-home/**`, `.packages/**`, `.vs/**`, `bin/**`, `obj/**`, `artifacts/**`, `TestResults/**`, `tmp/**` | Local tooling state, caches, builds, test output, staged packages, and temporary audit output. |
| `*.zip`, `*.patch`, backup directories, local staging output, local configuration, logs, dumps, credentials, keys, and secrets | Generated/private material not part of the reviewed public source snapshot. |

## D — Owner decision required

None. The owner explicitly cleared all current runtime images for redistribution. The cornered item frame is additionally supported by repository records describing it as original project-generated artwork.

## Candidate rules

The clean Phase 7E candidate copies only classes A and B from the current worktree, contains no `.git` directory, and excludes every class C entry. The original dirty private development repository is preserved unchanged except for reviewed publication and dependency-audit documentation; no tracked or untracked work is discarded.
