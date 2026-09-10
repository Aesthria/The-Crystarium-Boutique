# Patch Compatibility

## Dependency classification

| Class | Current examples | Policy |
| --- | --- | --- |
| Relatively stable | Public Dalamud APIs, WindowSystem, injected services, versioned plugin APIs, Lumina sheets | Prefer these paths and verify versions after patches. |
| Moderately fragile | Undocumented plugin behavior or game-state assumptions | Isolate, document uncertainty, and provide a safe fallback. |
| Fragile | Signatures, offsets, raw structures, native calls | Absent from v0.1; require a dedicated ADR, runtime validation, feature flag, and independent disable path. |

## Current baseline

- FFXIV local build marker: `2026.09.01.0000.0000`.
- Dalamud: `15.0.3.4`, API 15.
- `Dalamud.NET.Sdk`: `15.0.0`.
- Target: .NET 10 / x64.
- Glamourer observed and native-validated: `1.7.1.0`, API 15 manifest, runtime API tuple `1.8`.
- Penumbra observed and native-validated: `1.7.1.1`, API 15 manifest, runtime API tuple `5.19`.
- Native hooks: none.

## Significant-patch procedure

1. Re-audit installed FFXIV, Dalamud API, SDK, Glamourer, and Penumbra versions.
2. Update pinned dependencies only when required and record the change.
3. Restore, build with zero warnings, and run all unit tests.
4. Verify each external adapter's runtime API/version handshake.
5. Validate GPose behavior and any patch-sensitive feature separately.
6. Run the native regression checklist in `TEST_PLAN.md`.
7. Disable only the incompatible optional feature while preserving safe browsing/UI behavior.

Never silently carry a signature, offset, IPC label, or undocumented assumption across a patch.
