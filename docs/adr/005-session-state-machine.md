# ADR 005: Boutique Session State Machine

Decision: Represent session lifecycle as an explicit guarded state machine.

Context: Original state, preview state, saved loadouts, restoration, and failure states must never be conflated.

Options Considered: Boolean flags; event-driven mutable state; explicit state transitions.

Chosen Approach: One active session with a unique ID, defined states, validated transitions, and bounded transition history.

Why: Prevents overlapping initialization and makes restoration/recovery testable.

Tradeoffs: More domain code than ad-hoc flags.

Fallback: Unsupported transitions return typed errors and leave the current state unchanged.

Patch Risk: Low; plugin-owned domain logic.
