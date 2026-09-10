# Security Policy

## Supported version

Security fixes target the latest supported public version unless a release notice states otherwise.

## Reporting a vulnerability

Do not publish credentials, tokens, private account data, exploit details, or unredacted logs in a public issue.

When the repository's **Security** tab offers **Report a vulnerability**, use that route to submit a private report. If that control is unavailable, do not open a public issue containing sensitive details. A non-sensitive issue may state that a security concern exists and request a private contact path, without including the vulnerability details.

For ordinary crashes, incorrect item data, or visual defects that do not expose sensitive information, use the normal issue forms described in [Support](SUPPORT.md).

## Scope

Useful security reports may include unsafe file handling, unintended network requests, credential exposure, untrusted Design-string parsing problems, external URL validation issues, or packaging that includes unintended binaries or local data.

TCB's normal catalog and acquisition lookup are local/offline. The Eorzea Collection importer performs an external request only when a user explicitly submits a supported URL.
