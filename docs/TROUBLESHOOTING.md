# Troubleshooting

## Boutique does not open

- Try `/boutique`, `/tcb`, or `/cb`.
- Confirm the plugin is enabled in `/xlplugins` and current for the installed Dalamud API.
- If combat safety is enabled, wait until combat ends before reopening.
- Reload the plugin and capture the focused Dalamud error if it still fails.

## Glamourer is unavailable or items do not preview

Glamourer must be installed and enabled separately for appearance capture and preview. TCB remains browse-only without it. Enable or update Glamourer, then allow TCB to detect it; if necessary, reload TCB after Glamourer is active.

## Penumbra is unavailable

Penumbra is optional and separately installed. Its TCB integration is limited to an emergency local-player redraw after Glamourer unloads during an applied preview. Normal browsing does not require it. Without Penumbra, TCB retains pending restoration and retries when Glamourer returns.

## Crystal Wardrobe does not match the character

- If **Automatically Sync Wardrobe** is disabled, enable it for future genuine gearset changes.
- Wait for FFXIV to finish applying the gearset before checking the Wardrobe.
- Choose **Reset Character** to manually resynchronize current FFXIV equipment and dyes.

Auto Sync tracks settled game gearset changes; it does not automatically choose a Saved Design.

## Dyes did not appear

- Confirm the current item supports the selected dye slot.
- Unknown stains from an external import resolve to No Dye.
- **Clear All Dye Slots** clears active session dyes while keeping equipment; loading a saved Design can reapply its saved dyes.
- Use **Reset Character** to recapture the character's current real-game stains.

## Headgear visor click has no visible effect

Some head items have no alternate visor appearance. Left Click previews with the optional visor piece hidden; Ctrl + Left Click requests the full or alternate appearance. No visible difference on unsupported headgear is expected.

## A cross-job Design weapon does not apply

This can be correct compatibility behavior. When a Design entry cannot validly apply to the active job, TCB retains the valid active equipment for that slot rather than displaying incompatible gear as active. Compatible armor can still apply while the current weapon remains.

## An imported Design string is rejected

- Copy and paste the entire string, including the `TCB-DESIGN-1:` prefix.
- Ensure the sender and recipient use compatible TCB versions.
- Do not edit, wrap, or truncate the string.
- Import validates the current Design schema and rejects malformed or unproven data.

## An Eorzea Collection URL cannot be imported

- Use a full HTTPS URL on `ffxiv.eorzeacollection.com` in the `/glamour/{id}/...` form.
- Confirm the page is public and the network request is available.
- An import fails if a returned equipment item cannot be resolved in current local game data.
- Eorzea Collection URL import is separate from the **Import Design** string workflow.

See [Design Import](DESIGN-IMPORT.md) for exact limitations.

## Acquisition information is missing

Not every item is duty-obtained, and missing acquisition data does not mean an item is invalid. TCB shows exact bosses only when verified evidence proves the item-to-source chain. Report a suspected omission with the Item ID and a reputable source.

## Theme, scaling, or layout looks wrong

- Note the selected theme, Boutique interface scale, Dalamud global scale, display resolution, and window dimensions.
- Try another built-in theme to determine whether the issue is theme-specific.
- Reopen the affected window after changing its orientation or scale.
- Attach a tightly cropped, redacted screenshot to a Visual/UI issue.

## Reloading the plugin

Before reloading, close Boutique normally so it can restore the captured game appearance. If a preview persists after a dependency unload, re-enable Glamourer; Penumbra can provide the optional immediate redraw fallback. Include only focused, redacted log excerpts in reports—never credentials, complete configuration folders, private paths, or unrelated player information.
