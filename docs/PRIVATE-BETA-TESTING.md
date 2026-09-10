# Historical Private Beta Workflow

> **HISTORICAL PRIVATE BETA WORKFLOW — NOT THE CURRENT PUBLIC INSTALLATION METHOD**

This file preserves the former invited-tester artifact workflow for project history. It is not an active download, installation, update, or support path. For the intended public custom-repository flow, see [Installation](INSTALLATION.md).

During the closed private beta, builds were available only to invited collaborators of the private repository. The retained instructions below describe that completed testing phase and must not be presented as current public guidance.

## Requirements

- A GitHub account invited to the private `Aesthria/The-Crystarium-Boutique` repository.
- Windows with Final Fantasy XIV launched through XIVLauncher and current Dalamud.
- Current Glamourer and Penumbra plugins installed and enabled separately.
- The complete `CrystariumBoutique-0.1.0-beta.1.zip` package from an approved private workflow run.

## Download and install

1. Accept the GitHub repository invitation and sign in with that account.
2. Open the private repository, select **Actions**, then **Private Beta Package**.
3. Open the approved successful workflow run identified by the project owner.
4. Download the `CrystariumBoutique-0.1.0-beta.1` artifact.
5. Extract the downloaded Actions artifact. It contains `CrystariumBoutique-0.1.0-beta.1.zip`.
6. Extract the entire `CrystariumBoutique` folder from that ZIP to a permanent location. Keep every included file together.
7. Open FFXIV through XIVLauncher.
8. Open Dalamud Settings with `/xlsettings` and enable developer options if needed.
9. Add the extracted `CrystariumBoutique` folder as a **Dev Plugin Location**. If Dalamud requests a file, select `CrystariumBoutique.dll` inside that folder.
10. Open `/xlplugins`, scan for dev plugins, and enable **The Crystarium Boutique**.
11. Use `/CB` or `/Boutique` to open it.

Never add random DLLs or copy files from FFXIV, Dalamud, Glamourer, Penumbra, or another Boutique build into the package folder.

## Update

1. Disable or unload Boutique and close FFXIV.
2. Download and extract the newly approved beta artifact.
3. Replace the entire old `CrystariumBoutique` folder with the new folder.
4. Never merge files from different beta versions.
5. Launch FFXIV and re-enable Boutique. Confirm the expected version in Boutique's Info tab.

## Uninstall

1. Disable or unload Boutique and close FFXIV.
2. Remove the Boutique Dev Plugin Location from Dalamud Settings.
3. Delete the extracted `CrystariumBoutique` folder.

Boutique's saved configuration and Designs are managed separately by Dalamud. State whether you retained them when reporting an uninstall/reinstall problem.

## Troubleshooting

- Confirm Glamourer is installed, enabled, and current.
- Confirm Penumbra is installed, enabled, and current.
- Confirm Boutique shows version `0.1.0-beta.1`.
- Confirm the entire package was extracted and no files were merged from an older build.
- Confirm the Dev Plugin Location points to the extracted Boutique folder or its `CrystariumBoutique.dll`.
- If loading fails, capture the exact Dalamud error. Do not upload complete configuration folders, credentials, or unredacted logs.

## Focused beta checklist

- `/CB` and `/Boutique` open and close the plugin.
- All tabs, both themes, the Crystal Wardrobe, window resizing, and 3 x 6 / 4 x 6 layouts render correctly.
- Slot, expansion, role, dye-slot, and item-name filters work.
- Tooltips remain readable and show appropriate local acquisition information.
- Equipment, linked weapons, both dye slots, Designs, and Eorzea Collection imports work.
- Reset, previous item, closing, unloading, combat lock, zoning, logout/login, and character changes restore the expected state.
- GPose entry/exit preserves the active session and permits preview/reset operations.
- Disabling Glamourer during an applied preview restores the local actor through Penumbra; re-enabling Glamourer starts a fresh usable session.
- No unexplained FPS spikes, retained appearances, exceptions, hangs, or crashes occur.

## Report feedback

Use the private repository's **Bug report** or **Visual feedback** issue form. Include the exact beta version, minimal reproduction steps, expected and actual behavior, applicable Item ID, game state, dependency state, and a focused screenshot or log excerpt.

Remove character/account information, chat, tokens, credentials, and private filesystem paths before uploading anything.

## Repository-owner steps

The private workflow uses a GitHub-hosted `windows-2025` runner. No self-hosted runner, FFXIV installation, XIVLauncher installation, or repository secret is required.

One time:

1. Invite each tester to the private repository with read access and have them accept the invitation.
2. Confirm GitHub Actions is enabled for the repository.

For each approved beta build:

1. Open **Actions** → **Private Beta Package** → **Run workflow** on `main`.
2. Wait for every validation step to pass.
3. Send testers the workflow run name or number privately. Do not create a public release or external download.

If the pinned Dalamud download hash or version check fails after a Dalamud update, do not bypass it. Re-audit the new official bundle locally, update the reviewed hash/version in the workflow, and rerun the full validation suite.
