# ADR 002: Glamourer Integration

Decision: Isolate Glamourer behind `IAppearanceService` and a Boutique-owned, narrowly scoped Dalamud IPC client after runtime compatibility checks.

Context: Appearance capture, per-slot preview, dyes, full-state apply, and restoration belong to Glamourer, but v0.1 must not invent IPC.

Options Considered: Redistributed `Glamourer.Api`/Luna wrappers; a centralized Boutique-owned Dalamud IPC adapter; direct character memory manipulation; no integration.

Chosen Approach: Domain-owned result types and opaque snapshots; a valid unavailable fallback; and `GlamourerAppearanceService → IGlamourerIpcClient → GlamourerIpcClient`. Only `GlamourerIpcClient` contains the verified endpoint labels, `ICallGateSubscriber` signatures, version policy, and provider lifecycle subscriptions. It uses local-player index operations, a guarded name fallback for GPose, one-shot mutation, no persistent lock, and full game-state restoration.

Why: Keeps the UI testable, makes dependency failure/recovery explicit, permits browse-only startup without Glamourer, and avoids linking to or redistributing `Glamourer.Api.dll` and its Luna runtime.

Tradeoffs: Boutique owns a small wire contract verified against Glamourer.Api revision `ea569211a7c3500f2ce0b1b9223df85d8cb85f1a`. It accepts API major 1, minor 8 or newer only when every required versioned endpoint is present. Contract changes still require re-audit and native validation. Avoiding a persistent lock reduces crash residue but means Boutique does not claim exclusive ownership against simultaneous external edits.

Fallback: Browsing remains available with preview disabled and a clear dependency status.

Patch Risk: Moderate; versioned third-party public API.
