# ADR 008: Configuration and Loadout Persistence

Decision: Use Dalamud configuration for small plugin settings and a separate versioned repository for loadouts.

Context: Settings and potentially dozens of named appearance documents have different migration and failure needs.

Options Considered: One configuration blob; separate JSON files; embedded database.

Chosen Approach: `IPluginConfiguration` for small settings. Beginning in v0.7, use `ILoadoutStore`/`JsonLoadoutStore` for one schema-versioned, GUID-named JSON record per loadout beneath Dalamud's plugin configuration directory. Write through a temporary file, archive deletions locally, migrate supported older schemas, and isolate invalid records per file.

Why: Keeps Foundation simple while avoiding an unbounded configuration object.

Tradeoffs: Two persistence paths require coordinated backup/migration later. Loadouts intentionally store typed Boutique-changed slots rather than opaque Glamourer state, so absent slots inherit the current session-opening appearance.

Fallback: Quarantine corrupt loadout data and preserve recoverable records; never block plugin load on one invalid loadout.

Patch Risk: Low; plugin-owned formats with explicit schema versions.
