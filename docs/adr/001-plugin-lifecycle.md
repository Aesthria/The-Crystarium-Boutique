# ADR 001: Plugin Lifecycle

Decision: Use synchronous `IDalamudPlugin` construction and deterministic `Dispose()` for v0.1.

Context: Dalamud API 15 supports both `IDalamudPlugin` and `IAsyncDalamudPlugin`. The Foundation performs no long-running startup work.

Options Considered: `IDalamudPlugin`; `IAsyncDalamudPlugin`; custom background startup from a synchronous constructor.

Chosen Approach: Constructor-inject Dalamud services, compose small owned objects, register command/UI callbacks, and unregister them in idempotent reverse order during `Dispose()`.

Why: It is the smallest current supported lifecycle for a synchronous shell and makes ownership obvious.

Tradeoffs: v0.2 asynchronous indexing may motivate migration to `IAsyncDalamudPlugin` or a cancellable service started after load.

Fallback: Add a cancellable initialization service without changing domain interfaces; migrate the entry point if load gating becomes necessary.

Patch Risk: Low; public Dalamud API, reviewed on API bumps.
