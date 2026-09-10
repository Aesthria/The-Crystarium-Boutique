# ADR 009: UI State Management

Decision: Windows render state and issue controller commands; they do not call game integrations directly.

Context: The required flow separates the equipment grid from Glamourer and native state.

Options Considered: UI-owned services; global mutable state; controller plus immutable/read-only views.

Chosen Approach: Compose a session controller and narrow services in the entry point; pass read-only status into reusable window sections.

Why: Keeps draw paths small, allocation-aware, and testable.

Tradeoffs: More explicit command/view-model plumbing as features grow.

Fallback: Add focused component controllers rather than expanding one monolithic `Draw()`.

Patch Risk: Low; plugin-owned architecture.
