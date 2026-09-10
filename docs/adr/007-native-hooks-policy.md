# ADR 007: Native Hooks Policy

Decision: No native hooks or unsafe memory access in the Foundation; future hooks require a dedicated ADR, feature flag, validation, and fallback.

Context: The product can deliver value through supported Dalamud and third-party APIs.

Options Considered: Hooks-first implementation; supported APIs only; selective isolated hooks.

Chosen Approach: Dalamud API, typed third-party API, stable game data, plugin-owned UI workaround, then omit/redesign before considering hooks.

Why: Minimizes patch risk and protects deterministic restoration.

Tradeoffs: Some aesthetic automation may remain unavailable.

Fallback: Manual or UI-based workflow.

Patch Risk: None in v0.1; high for any future native feature.
