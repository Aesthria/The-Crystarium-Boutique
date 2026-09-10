# Designs

Status: persistence, linked-weapon behavior, Boutique selection/saving, transient Eorzea Collection import, responsive selectors, and visual saved-design cards are accepted through v0.13.2. v0.14.0 adds Antique Gold Eorzea-import emphasis, Terracotta exchange actions, clipboard feedback/help, styled confirmation actions, US-format combined dates, bordered notes, framed list heading, and Warm Charcoal saved-item cards.

## Saved meaning

A Boutique design is the complete typed set of equipment slots changed through the Boutique when **Save** or **Overwrite** is confirmed. Each saved slot contains its appearance/model ID, required source item ID, slot, supported dye-channel count, complete stain values, display name, and icon provenance. The document also contains schema version 1, a stable GUID, unique name, created/updated UTC timestamps, optional tags, and optional notes. Internal types and the on-disk `loadouts` directory retain their established names for schema compatibility.

Untouched slots are deliberately absent. Loading a look restores the exact appearance captured when the current Boutique session opened, then replays the saved slots. The result is a portable Boutique overlay rather than a serialized whole-character snapshot.

## Library operations

- **Save Design** creates a new uniquely named record from all current Boutique changes.
- **Load** replaces the current preview overlay with the saved one.
- **Rename** changes only the display name.
- **Duplicate** creates a new GUID and timestamps while preserving the saved equipment, tags, and notes.
- **Overwrite** replaces equipment/dyes with the current Boutique changes after explicit confirmation.
- **Delete** removes the record from the visible library after explicit confirmation and moves its JSON document into a recoverable local archive.
- **Export Design** copies a portable Design string for sharing with another TCB user.
- **Import Design** validates a compatible shared Design string and saves a new local record without uploading it.

The dedicated Designs tab has no hard record limit. Its actions occupy two persistent rows, Generate List remains anchored at bottom-right, and Saved Outfit Info shows concise `Created | DD/MM/YYYY`, `Updated | DD/MM/YYYY`, and optional `NOTES | ...` metadata. The Design Item List is a read-only 2 × 6 visual projection of saved slots/icons/names/dyes and cannot apply or mutate appearance state.

The **Crystarium Boutique** tab also exposes a compact **Designs** selector. Its normal label is **No Design**; selecting a saved name applies it directly, while the last **Save** control opens the existing custom-name flow. Manual item changes return the selector label to **No Design** because the visible overlay is no longer identical to the selected record.

The selector's first action, **Eorzea Collection Import from URL**, accepts a public glamour URL after an explicit user submission. A successful import appears as transient **Eorza Col.**, applies through the normal rollback-safe load path, and can then be customized and saved locally under any user-chosen name. The imported source URL/name are local notes only. Omitted armor/accessory slots are represented by their corresponding Emperor's New items; linked weapon components and recognized dye names are retained.

## Local persistence

Designs live under the plugin's Dalamud configuration directory in the compatibility-preserved `loadouts` subdirectory. Active filenames are GUIDs and contain no character, user, or machine names. No remote path or account is used.

Each write serializes to a temporary file before replacing the active record. Supported schema-zero records migrate to schema one. Invalid, corrupt, mismatched-ID, or unsupported-schema files move independently into `quarantine`; other valid records still load and the UI/log reports the warning. Deleted records move into `deleted` for local recovery.

## Apply safety

The store and controller validate loadouts before application. A load first reapplies the current session-opening snapshot, then calls the verified typed equipment endpoint once per saved slot and recaptures the resulting preview. If reset, replay, or recapture fails, the controller attempts to restore the exact preview that was visible before Load and retains its prior changed-slot state.

Opaque Glamourer state is never parsed, patched, or stored in a loadout. Closing the Boutique or unloading the plugin continues to restore the current session's exact captured original appearance.
