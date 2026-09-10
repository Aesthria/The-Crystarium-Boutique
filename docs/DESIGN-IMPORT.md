# Design Import and Sharing

The Crystarium Boutique supports two distinct import workflows. A TCB Design string is for sharing a saved Design directly between TCB users. An Eorzea Collection URL asks TCB to retrieve and resolve a supported public glamour page. They are not interchangeable.

## TCB Design-string sharing

### Export a Design

1. Open the Designs tab and select a saved Design.
2. Choose **Export Design**.
3. TCB copies a portable Design string to the clipboard.
4. Share that complete string with another TCB user.

Export does not upload the Design or contact a service.

### Import a Design

1. Copy a compatible Design string shared by another user.
2. Open the Designs tab and choose **Import Design**.
3. Paste the full string if it is not already in the import field.
4. Confirm **Import Design** in the dialog.

TCB validates the string and saves the imported result as a new entry in the local Designs list. Importing does not immediately or permanently overwrite the character. Select and load the saved Design when ready, then use normal Boutique controls to try or modify it.

### Compatibility and validation

The current public exchange prefix is `TCB-DESIGN-1`. Imports must use the supported format and current Design schema, contain valid supported slots and appearance data, and pass the same Design validation used by local storage. Invalid, incomplete, corrupted, or unsupported-version strings are rejected rather than partially trusted.

Imported Designs resolve against the recipient's current catalog and appearance rules. If a saved item cannot validly apply to the active job, TCB retains valid active equipment for that slot. Compatible portions can still apply.

## Eorzea Collection URL import

### Supported URL form

TCB accepts an HTTPS public glamour URL on `ffxiv.eorzeacollection.com` whose path begins with `/glamour/` followed by a positive numeric glamour ID, for example:

```text
https://ffxiv.eorzeacollection.com/glamour/12345/example-name
```

Other hosts, non-HTTPS links, non-glamour pages, and URLs without a valid glamour ID are rejected.

### Import workflow

1. Copy the supported public glamour URL.
2. On the Boutique tab, open the Designs selector.
3. Choose **Eorzea Collection Import from URL**.
4. Paste the URL and choose **Import and Apply**.
5. Review the applied compatible appearance.
6. Choose **Save Design** to keep it in the local Designs list.
7. Modify the saved or active appearance with the normal Boutique equipment and dye tools.

The URL import applies to the current Boutique session first; it does not silently create a saved Design. Saving is an explicit follow-up action.

### Resolved information and limitations

- TCB resolves supported equipment slots by the item names returned by the Eorzea Collection glamour response and checks them against current local game data.
- Up to two returned dye names are resolved against local stain data. An unknown or missing dye becomes No Dye.
- A linked off-hand component is supplied when the resolved main-hand item requires one and the source does not provide it separately.
- Omitted supported armor/accessory slots use the matching Emperor's New item so older visible gear does not blend into the imported glamour.
- If one or more named items cannot be found in current local game data, the import is rejected with an error instead of silently producing a misleading partial result.
- The importer handles compatible equipment appearance data; it does not promise support for every Eorzea Collection page or every non-equipment customization field.

### Network and privacy behavior

This workflow requires network access only after the user explicitly submits a supported Eorzea Collection URL. TCB converts that URL to the site's public glamour data endpoint and downloads the requested response with a size limit and timeout. Normal Boutique browsing, TCB Design-string import/export, Favorites, saved Designs, and packaged acquisition lookup do not require that request.

Eorzea Collection is an independent service and is not bundled with or operated by The Crystarium Boutique.
