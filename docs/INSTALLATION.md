# Installation

The Crystarium Boutique is being prepared for public distribution through a custom Dalamud plugin repository. The public repository URL is not live yet and will be supplied when release infrastructure is enabled.

Do not use a guessed URL, a private GitHub URL, or a development build directory as a public installation source.

## Public installation flow

When the repository URL is announced:

1. Launch FINAL FANTASY XIV through XIVLauncher.
2. Open Dalamud Settings.
3. Open the Experimental or Custom Plugin Repositories section.
4. Add the published The Crystarium Boutique repository URL and save.
5. Open `/xlplugins`.
6. Find **The Crystarium Boutique** and choose **Install**.
7. Ensure separately installed Glamourer is enabled for appearance previews. Penumbra is optional but recommended for emergency redraw recovery.
8. Open TCB with `/boutique`, `/tcb`, or `/cb`.

## Updating

After public custom-repository distribution is active, Dalamud will surface TCB updates through its normal plugin update flow. Keep TCB, Dalamud, Glamourer, and Penumbra current. Do not merge files from unrelated or older development builds into an installed plugin directory.

## Requirements

- Windows and FINAL FANTASY XIV through XIVLauncher with current Dalamud.
- Glamourer installed separately for preview and restoration. Without Glamourer, TCB remains browse-only.
- Penumbra installed separately if the optional emergency redraw fallback is wanted.

TCB does not bundle Glamourer, Penumbra, or their API assemblies.

## Development builds

Dalamud Dev Plugin Location is a developer/testing mechanism, not the planned public installation method. Source-build instructions are in the root [README](../README.md).

The former invited-tester artifact process is retained only as [historical private-beta documentation](PRIVATE-BETA-TESTING.md).
