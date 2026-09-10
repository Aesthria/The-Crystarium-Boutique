# Security Policy

## Supported version

Before the first public release, only the current development line is maintained. After release, security fixes will target the latest supported public version unless a release notice states otherwise.

## Reporting a vulnerability

Do not publish credentials, tokens, private account data, exploit details, or unredacted logs in a public issue.

No private security-reporting address or verified private vulnerability-reporting channel is currently designated. Until the repository owner configures one, do not open a public issue containing sensitive details. A non-sensitive issue may state that a security concern exists and request a private contact path, without including the vulnerability details.

**Required owner action before v0.1.0:** configure and verify a private vulnerability-reporting mechanism, then document its exact supported entry point here. This statement does not claim that GitHub private vulnerability reporting is currently enabled.

For ordinary crashes, incorrect item data, or visual defects that do not expose sensitive information, use the normal issue forms described in [Support](SUPPORT.md).

## Scope

Useful security reports may include unsafe file handling, unintended network requests, credential exposure, untrusted Design-string parsing problems, external URL validation issues, or packaging that includes unintended binaries or local data.

TCB's normal catalog and acquisition lookup are local/offline. The Eorzea Collection importer performs an external request only when a user explicitly submits a supported URL.
