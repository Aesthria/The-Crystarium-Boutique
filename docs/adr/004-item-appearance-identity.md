# ADR 004: Item and Appearance Identity

Decision: Use a hybrid model: deduplicated appearance tiles with source-item metadata.

Context: Multiple FFXIV items can share a visual model, but item metadata, restrictions, and acquisition context remain useful.

Options Considered: Every item as a tile; appearance-only deduplication; hybrid appearance tile plus source items.

Chosen Approach: Keep `ItemId` distinct from `AppearanceKey`. The initial key used only installed Lumina `Item.ModelMain` and `Item.ModelSub`; that hid legitimate shared-geometry variants. The current key uses the complete packed `ModelMain` and `ModelSub` values plus `Item.Icon` and `Item.DyeCount`. Deduplicate only within a selected slot/content group and retain every source item on the tile.

Why: The packed models preserve available set/variant and linked weapon information, while the item icon is the available rendered proxy for baked color/material differences and the dye count preserves distinct zero-, one-, and two-channel behavior. This favors visual completeness while still collapsing exact model/icon/dye matches.

Tradeoffs: Lumina exposes no separate authoritative material, texture, dye-region, or VFX fingerprint on `Item`. Packed model differences continue to distinguish known glow/matte and geometry variants; icon differences conservatively split baked visual variants. This can retain visually equivalent records with different icons, which is preferable to hiding a legitimate appearance. Items sharing a complete key can retain different restrictions or provenance, so the source list remains authoritative for item-level details.

Fallback: If packed-model semantics change or a row has no model key, exclude it from deduplicated browsing rather than merging by name or icon alone. The adapter can later add a more authoritative verified visual fingerprint without changing item identity.

Patch Risk: Moderate; Lumina schema and game-data interpretation can change.
