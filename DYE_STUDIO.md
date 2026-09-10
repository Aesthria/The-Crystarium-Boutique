# Dye Studio

Status: v0.5.0 behavior was accepted; the old standalone bottom workspace was removed in v0.9.0 and its verified dye picker returns as card-integrated controls in v0.9.2.

## Verified data and integration boundaries

- Dye metadata comes only from the installed local Lumina `Stain` sheet. The loader retains byte ID, localized name, RGB color, Shade/SubOrder grouping, and metallic state.
- The installed Glamourer API 1.8 surface has no separate dye-only subscriber. The supported `SetItem` subscriber accepts the current item/custom ID plus a list of stain bytes.
- Boutique never parses or edits Glamourer's opaque appearance snapshot. The active session retains the selected source item and complete two-channel stain state for each Boutique-previewed slot.

## Interaction flow

`OutfitContextPanel → BoutiqueSessionController.PreviewDye → IAppearanceService.ApplyDye → Glamourer SetItem → recapture preview`

The controller rejects dye requests unless the session is Active, the slot has a Boutique-selected item, and the zero-based channel is below the item's verified dye-channel count. A valid request changes one channel, preserves the other, and reissues the current item with both stain bytes and one-shot flags.

## v0.9.2 presentation

The old standalone bottom-workspace cell remains removed. Every equipment card instead reserves a bottom row for two compact stain-name/color buttons. Unsupported channels are disabled with red X overlays; supported buttons open the accepted searchable popup with color groups, recent swatches, clear, metallic labels, and channel tabs. Local stain metadata and typed dye values remain part of the catalog, tooltip, saved-design, imported-design, replay, and restoration boundaries. Eorzea Collection dye names resolve through the same local stain index.

Applying from this popup preserves the Boutique browser's exact slot, expansion, filters, tile selection, and page. Popup clicks are excluded from card navigation, and selecting the already active equipment slot is a controller no-op.

## Recovery

Every successful mutation is recaptured as the current preview. Linked weapons update both matching main/off-hand components before recapture. The original opaque snapshot remains unchanged and is restored through the accepted close/unload path. A failed single-item or linked-pair dye request reapplies the prior preview and does not replace the controller's last successful channel state.
