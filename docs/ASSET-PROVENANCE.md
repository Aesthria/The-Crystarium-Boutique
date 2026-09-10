# Runtime Asset Provenance

This ledger covers every image in the runtime package allowlist in `scripts/RuntimePackageFiles.psd1`. It records provenance supported by project records and explicit owner direction.

The plugin loads FFXIV equipment icons from the user's installed game through Dalamud at runtime. Those game icons are not stored in this repository or copied into the Boutique package.

| Asset | Purpose | SHA-256 | Dimensions | Creator/source and method | Third-party/reference relationship | Redistribution basis | Public-release status | Owner confirmation required? |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `icon.png` | Plugin icon | `7E608A25B08CB4C2BE4924DC9CEDC43140BCF7D755EE107A7EA2F4DFF1E14AA1` | 512×512 | Project-generated asset. Owner-confirmed as created specifically for The Crystarium Boutique. | Owner-confirmed independent of Square Enix artwork. | Approved by the owner for redistribution with the project. | CLEARED — PROJECT-GENERATED | No |
| `equipment-slots.png` | Equipment-slot glyph sheet | `B4DDE89EDD1528754B684114376243B9C65CC27BBCE686775E5458573D5C0B41` | 1448×1086 | Project-generated asset. Owner-confirmed as created specifically for The Crystarium Boutique. | Owner-confirmed independent of Square Enix artwork. | Approved by the owner for redistribution with the project. | CLEARED — PROJECT-GENERATED | No |
| `previous-item.png` | Revert/Previous Item control | `C7DB684A87F51D9FD668C571D48DD6334E93B1BE08E3831BE0DB9E64E46C6076` | 96×96 | Project-generated replacement created specifically for The Crystarium Boutique. | Owner-confirmed independent of Square Enix artwork; it replaces the prohibited historical asset. | Approved by the owner for redistribution with the project. | CLEARED — CURRENT WORKTREE ONLY | No |
| `crystal-wardrobe.png` | Crystal Wardrobe button | `8DE166874CA7BF85C38777C845A198D7695FE816CA904CA295D44BF81F2F4FA7` | 1254×1254 | Project-generated asset. Owner-confirmed as created specifically for The Crystarium Boutique. | Owner-confirmed independent of Square Enix artwork. | Approved by the owner for redistribution with the project. | CLEARED — PROJECT-GENERATED | No |
| `unavailable-dye.png` | Unavailable dye-slot indicator | `B7802F5401F272092EBE7346A0437132151805A48A20D732057F607A7B56DB8C` | 32×32 | Project-generated asset. Owner-confirmed as created specifically for The Crystarium Boutique. | Owner-confirmed independent of Square Enix artwork. | Approved by the owner for redistribution with the project. | CLEARED — PROJECT-GENERATED | No |
| `crystarium-stained-glass.png` | Crystarium-theme background sheet | `A2A10E31C54992638638FE20BBC079EEE192F2487FAA9775EEB457439F6A2A09` | 1672×941 | Project-generated asset. Owner-confirmed as created specifically for The Crystarium Boutique. | Owner-confirmed independent of Square Enix artwork. | Approved by the owner for redistribution with the project. | CLEARED — PROJECT-GENERATED | No |
| `crystarium-item-frame-cornered.png` | Weathered-steel item and Wardrobe frame | `61CC47F8322E9E0A8F96FDF4A08E33BD773EA95EC87A4AACEF6EF3A5364F3262` | 1254×1254 | Project records describe an original transparent forged-steel frame and later original corner-cap variant generated for Boutique. | Inspired by a supplied architectural reference; project records identify no copied game texture or incorporated third-party artwork. | Repository provenance establishes original project generation; approved for redistribution under the project license. | CLEARED — PROJECT-GENERATED | No |
| `crystarium-gold-frames.png` | Nine-slice control and tooltip frames | `94A51AA09058CF0E0ABE9310722D9EA191BFD01614A394FE168E085F53EDD8A2` | 1536×1024 | Project-generated asset. Owner-confirmed as created specifically for The Crystarium Boutique. | Owner-confirmed independent of Square Enix artwork. | Approved by the owner for redistribution with the project. | CLEARED — PROJECT-GENERATED | No |
| `favorite-badge.png` | Top-right Favorite marker | `62BC5EFCC77BA6E9A8F3C91DB5F16DF82115F7F16EF444E3BA0BD42F9EC608AB` | 64×64 | Project-generated asset. Owner-confirmed as created specifically for The Crystarium Boutique. | Owner-confirmed independent of Square Enix artwork. | Approved by the owner for redistribution with the project. | CLEARED — PROJECT-GENERATED | No |

## Superseded Revert icon

The current working tree does not contain the old lightly modified Square Enix-derived Revert icon. The historical blob `095469e60cd9d63d1ec90ae01a6c34731bdbfd0d` at `src/CrystariumBoutique/images/previous-item.png` is **DO NOT SHIP** and is not approved for public redistribution. It remains reachable from the current private Git history and must be removed from the history selected for public publication before repository visibility changes.

## Non-runtime image source

`src/CrystariumBoutique/images/crystarium-item-frame.png` is not in the runtime allowlist, is unused by the project, and is excluded from the clean publication candidate. It remains only in the private development worktree as an obsolete rollback asset.

## Release status

Every allowlisted runtime image is cleared for the clean publication candidate. This ledger does not transfer ownership of FFXIV or Square Enix material and does not treat visual similarity alone as proof of permission.
