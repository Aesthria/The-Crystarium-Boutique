# Public Release Legal Checklist

Use this checklist before publishing source code, a plugin ZIP, a repository feed, or a public release.

## Project identity and license

- [ ] Root `LICENSE` contains the complete GNU Affero General Public License v3 text.
- [ ] All project metadata uses the exact SPDX identifier `AGPL-3.0-only`.
- [ ] Public author/maintainer is consistently identified as Aesthria.
- [ ] Repository links target `https://github.com/Aesthria/The-Crystarium-Boutique`.
- [ ] Preserve the reviewed Git history used to identify the corresponding source.
- [ ] Source corresponding to every distributed Boutique binary is publicly available as required by the AGPL.
- [ ] Any network-facing deployment provides users the corresponding source as required by AGPL section 13.

## Copyright and contributions

- [ ] `COPYRIGHT.md` reflects the current year and known copyright holders.
- [ ] Contributions were submitted under the inbound-equals-outbound `AGPL-3.0-only` policy.
- [ ] No contribution requires an unrecorded CLA, assignment, employer consent, or third-party permission.
- [ ] Generated or assisted work is not credited as a separate author unless a human rights holder has actually claimed copyright.
- [ ] Review AI-assisted portions carefully before making representations in any formal copyright-registration filing.
- [ ] Consider U.S. Copyright Office registration separately; repository notices do not register copyright.
- [ ] Obtain trademark clearance separately before seeking registration or claiming rights in the project name or logo.

## Dependencies and interoperability

- [ ] Re-run dependency and runtime-assembly inspection from a clean build.
- [ ] Confirm no Luna, Glamourer.Api, or Penumbra.Api binary or CLR reference is present.
- [ ] Re-verify the exact Dalamud, Dalamud.NET.Sdk, Lumina, Newtonsoft.Json, Glamourer contract, and Penumbra contract revisions and licenses.
- [ ] Confirm Glamourer and Penumbra remain separately installed and only accessed through documented Dalamud IPC contracts.
- [ ] Update `DEPENDENCIES.md` and `THIRD-PARTY-NOTICES.md` for every changed dependency, data source, or IPC contract.
- [ ] Include any license or notice file required by a redistributed dependency.

## Assets and game data

- [ ] Reverify that every allowlisted runtime image remains `CLEARED` in [`ASSET-PROVENANCE.md`](ASSET-PROVENANCE.md), including its current hash and dimensions.
- [ ] Manually verify the origin and redistribution permission for every packaged PNG, including icon, wardrobe, dye, stained-glass, and frame artwork.
- [ ] Confirm project copyright claims cover only original portions of project-specific artwork.
- [ ] Confirm no proprietary FFXIV font, executable, sheet, icon archive, or other game file is redistributed.
- [ ] Retain the independent-project and Square Enix/FINAL FANTASY XIV disclaimer.

## Acquisition data

- [ ] Verify the pinned Critical-Impact/LuminaSupplemental version, commit, per-file hashes, GPL-3.0-only notice, and generated provenance manifest.
- [ ] Confirm the LuminaSupplemental GPL-3.0 license text is present in the release ZIP.
- [ ] Verify the pinned Tracky/XIVStats revision, hash, MIT notice, and generated provenance manifest.
- [ ] Confirm the runtime supplement contains only reviewed exact-item relationships and no prohibited scraped content.
- [ ] Confirm the plugin performs no background acquisition-data download or other unintended runtime network access.

## Package and publication

- [ ] Build and test from a clean checkout with locked dependencies.
- [ ] Validate the release allowlist and inspect every ZIP entry.
- [ ] Confirm `LICENSE`, `COPYRIGHT.md`, and `THIRD-PARTY-NOTICES.md` are present in the release ZIP.
- [ ] Scan tracked files and the release ZIP for credentials, private keys, tokens, machine-specific paths, player configuration, and local Design data.
- [ ] Run `git diff --check` and review the final staged diff before committing.
- [ ] Create and verify the intended versioned tag/release while preserving its source revision.
- [ ] Verify release version, changelog, install instructions, repository manifest/feed metadata, and rollback instructions.
- [ ] Record the final ZIP SHA-256 and preserve the source revision used to build it.

Copyright registration and trademark registration are external legal processes; repository documentation does not accomplish either one. This checklist is a release-maintenance aid, not legal advice. Obtain qualified legal review when the project's ownership, distribution model, or dependencies materially change.
