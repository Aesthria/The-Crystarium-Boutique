# Release infrastructure

This document describes the local and GitHub release flow for The Crystarium Boutique. The tooling does not publish anything unless an approved stable version tag is pushed. A private raw `repo.json` URL cannot be used by Dalamud because custom repository URLs do not support authentication.

## Authoritative feed contract

The index follows Dalamud's current [custom repository documentation](https://dalamud.dev/plugin-publishing/custom-repositories/) and the current [`IPluginManifest`](https://github.com/goatcorp/Dalamud/blob/15352a3e235a893e097e8f3e998818124a278416/Dalamud/Plugin/Internal/Types/Manifest/IPluginManifest.cs) contract reviewed for Phase 8.

The stable entry intentionally omits all testing-channel fields. `DownloadLinkInstall` and `DownloadLinkUpdate` point to the same immutable, versioned GitHub Release ZIP. `RepoUrl` and `IconUrl` require the repository to be public.

## Version model

`Package-Release.ps1`, `Generate-RepoIndex.ps1`, and `Invoke-ReleaseDryRun.ps1` receive one semantic version. Supported inputs are:

- `major.minor.patch-rc.N` for a local or private release-candidate dry run;
- `major.minor.patch` for a final release.

The scripts derive the four-part CLR/Dalamud assembly version as `major.minor.patch.N` for an RC and `major.minor.patch.0` for a final release. An RC used only for infrastructure testing must never be published if its numeric assembly version could conflict with a later stable release.

The checked-in runtime is `0.1.0`, with numeric assembly/file version `0.1.0.0` and the neutral `v0.1.0` UI label.

## Local dry run

From a reviewed working tree:

```powershell
.\scripts\Invoke-ReleaseDryRun.ps1 `
  -Version 0.1.0-rc.0 `
  -AllowDirty
```

`-AllowDirty` is only for an explicitly reviewed local infrastructure test. Final packaging fails on a dirty tree.

The dry run:

1. restores locked dependencies unless `-NoRestore` is supplied;
2. builds x64 Release using the supplied version;
3. runs the complete test suite and runtime-dependency inspection;
4. packages only `ReleasePackageFiles.psd1` entries;
5. validates the ZIP and generates its SHA-256;
6. regenerates the ZIP and requires the same hash;
7. generates and validates `repo.json`;
8. checks package/feed version, filename, internal name, and Dalamud API consistency.

No step contacts GitHub or publishes an artifact.

## Maintainer release procedure

1. Choose the RC or final semantic version.
2. Verify that the reviewed runtime version, assembly/file version, and neutral UI label match the chosen release.
3. Run the local dry run without `-AllowDirty` after committing the reviewed release-preparation changes.
4. Inspect every ZIP entry, the package manifest, ZIP SHA-256, and generated `repo.json`.
5. Commit the exact reviewed `repo.json` and release-preparation changes, then push the reviewed commit while the repository is still private.
6. With explicit owner approval, configure public security/reporting protections and make the clean repository public.
7. Verify anonymous source, license, README, icon, and `repo.json` access.
8. Create and push the exact stable version tag, such as `v0.1.0`.
9. The tag-only workflow rebuilds and validates the release, requires its generated feed to match committed `repo.json`, and then creates the GitHub Release with the ZIP asset.
10. Verify the release asset, package provenance, anonymous feed access, Dalamud installation, and update behavior.

The workflow does not run on ordinary pushes to `main`. Manual dispatch is dry-run-only and has read-only repository permission. Only an explicit `v*.*.*` tag enters the job with `contents: write` permission.

The workflow downloads the official Dalamud developer bundle, verifies its reviewed SHA-256 before extraction, and then verifies the exact `Dalamud.dll` assembly version. Updating that mutable upstream bundle requires a new local hash/version audit and a reviewed workflow change.

## Package allowlist

The public ZIP contains:

- `CrystariumBoutique.dll`, `CrystariumBoutique.Core.dll`, `CrystariumBoutique.deps.json`, and `CrystariumBoutique.json`;
- both runtime acquisition/availability supplements;
- the nine cleared runtime PNG assets;
- `LICENSE`, `COPYRIGHT.md`, `THIRD-PARTY-NOTICES.md`, and the LuminaSupplemental GPL license;
- generated `PACKAGE-MANIFEST.json`.

The validator rejects every unexpected ZIP entry, API/plugin binaries owned by Glamourer or Penumbra, Luna, test/source/debug material, raw generator inputs, review output, private-beta documentation, obsolete assets, local configuration, credentials, and logs.

## Public URLs

- Feed: `https://raw.githubusercontent.com/Aesthria/The-Crystarium-Boutique/main/repo.json`
- Repository: `https://github.com/Aesthria/The-Crystarium-Boutique`
- Release ZIP example: `https://github.com/Aesthria/The-Crystarium-Boutique/releases/download/v0.1.0/CrystariumBoutique-0.1.0.zip`
- Icon: `https://raw.githubusercontent.com/Aesthria/The-Crystarium-Boutique/main/src/CrystariumBoutique/images/icon.png`

The repository, feed, and icon endpoints require a public repository. The release ZIP additionally requires the corresponding tagged GitHub Release and asset.

## Repository security and protection checklist

- Enable and verify GitHub Private Vulnerability Reporting.
- Add a lightweight `main` ruleset that blocks force pushes and branch deletion. Requiring pull requests may be enabled after publication if it remains practical for a solo maintainer.
- Keep Actions default permissions read-only. Grant `contents: write` only to the tag-gated release job.
- Protect version tags from update/deletion after the release process is proven.
- Complete the public-release legal, asset, data-provenance, installation, update, and rollback checklist before changing visibility.

## Rollback

If a serious problem is discovered after publication, preserve the published tag and release as evidence, temporarily remove or disable the affected feed entry only when necessary to protect users, and publish a corrected patch release such as `0.1.1`. Do not rewrite `main`, move or replace a published version tag, or silently overwrite an existing release asset. Record the issue and corrective release in the changelog and security advisory process as appropriate.
