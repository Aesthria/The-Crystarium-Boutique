# Installed Item Data API Research

Audit target: installed Dalamud 15.0.3.4 / Lumina.Excel assembly 7.0.0.0 / local FFXIV build marker 2026.09.01.0000.0000.

Reflection over the installed generated `Lumina.Excel.Sheets.Item` row verified the v0.2 fields used by the adapter:

- `RowId`, `Name`, and `ItemUICategory` for item identity and localized display metadata;
- `EquipSlotCategory` with `MainHand`, `OffHand`, `Head`, `Body`, `Gloves`, `Legs`, `Feet`, `Ears`, `Neck`, `Wrists`, `FingerR`, and `FingerL` slot flags;
- `LevelEquip` and `LevelItem.RowId`;
- `Icon` and `DyeCount`;
- `ModelMain` and `ModelSub`.

The v0.4.1 and v0.4.2 audits additionally verified:

- `Item.ClassJobCategory` and every current generated class/job Boolean through Pictomancer and Viper;
- `ClassJob.Role` as corroborating job-role metadata;
- `FittingShopItemSet.Item` as the game's local Online Store fitting catalog.

The v0.5 audit verified the generated `Lumina.Excel.Sheets.Stain` row fields `RowId`, localized `Name`, packed RGB `Color`, `Shade`, `SubOrder`, and `IsMetallic`. The installed sheet contains 125 named nonzero stains. Packed color `0x00E4DFD0` for Snow White confirms red/green/blue extraction from bits 16/8/0. Shade values 2, 4, 5, 6, 7, 8, 9, and 10 provide stable Neutral, Red, Brown/Orange, Yellow, Green, Blue, Purple, and Special groups; unknown future values fall back to Other.

For v0.4.2, Boutique resolves every current class/job abbreviation to its local `ClassJob.RowId` once during catalog loading, then converts each `ClassJobCategory` into a compact two-word eligibility mask. Main Hand and Off Hand queries compare that mask with the active `IPlayerState.ClassJob.RowId`; `IClientState.ClassJobChanged` refreshes the browser immediately after an in-game job change. The broader manual role selector remains independent.

The installed `FittingShopItemSet` sheet contains 505 rows and 925 unique referenced item IDs. Boutique intersects those IDs with eligible equipment rows; it does not infer Online Store membership from item names, price, untradability, or external data.

The installed `Item` row does not expose an explicit expansion or introduction-patch field. v0.2 uses equip-level content bands named for the corresponding expansion era and documents that limitation in the data model and UI. Future bands are generated in ten-level increments rather than assuming a permanent expansion count.

Rows are excluded when the name is empty, the visible slot map is empty, or both model values are zero. The repository groups `(ModelMain, ModelSub, Icon, DyeCount)` within each slot/content group and retains all contributing source items. `Icon` is the installed API's conservative rendered proxy for baked color/material differences; the sheet exposes no separate authoritative material, texture, dye-region, or VFX fingerprint. Complete packed model values continue to distinguish geometry, linked weapon, and known glow/matte variants.

Game icons use the public `ITextureProvider.TryGetFromGameIcon(GameIconLookup, out ...)` and frame-local `GetWrapOrDefault()` path verified in the installed Dalamud XML documentation. v0.8.3 makes `itemHq: false, hiRes: true` explicit: high-resolution asset lookup was already the API default, while the separate HQ flag represents item quality rather than a larger pixel tier. FFXIV provides no additional official HD appearance-thumbnail tier, so softness at the 196-pixel Boutique tile size is source-art scaling rather than a lower-quality lookup. The shared texture result is not cached, matching the API's explicit recommendation.

v0.8.5 additionally retains Marketboard eligibility on each immutable equipment item. Local Lumina `ItemSearchCategory.RowId > 0` establishes that the item has a market search category, while `IsUntradable == false` confirms it is tradable. Both conditions are required before the tooltip displays the green Marketboard state; all other items display the red state. No network query or remote market service is involved.

The first native v0.2 run examined 52,801 Item rows and produced 28,962 eligible equipment items across 23,731 indexed slot/group appearance entries. Two clean plugin loads measured catalog construction at 98.1 ms and 83.7 ms. These values are diagnostic baselines, not hard-coded expectations.

The v0.17.8 acquisition audit verified the installed `Item`, `InstanceContent`, `InstanceContentRewardItem`, `ContentFinderCondition`, `ContentType`, `InstanceContentType`, `TerritoryType`, `Treasure`, `Map`, `BNpcBase`, and `BNpcName` shapes. `ContentFinderCondition` can describe a known duty, but `InstanceContentRewardItem` exposes no generated item reference and the remaining sheets do not provide a reliable reverse item-to-duty/boss/chest relation. Boutique therefore keeps installed-sheet acquisition resolvers separate from a packaged, versioned local supplement generated from pinned Critical-Impact/LuminaSupplemental tables. Supplemental duty entries require an exact item ID, stable duty/source evidence, and provenance and are never inferred from a shared model, named gear set, coffer/token contents, or vendor prerequisite.

Local recipe enrichment uses generated `Recipe.Ingredient`, `AmountIngredient`, `AmountResult`, `IsSpecializationRequired`, `Quest`, `ItemRequired`, `RecipeLevelTable`, and `SecretRecipeBook` fields. Quest enrichment uses `Reward`, `ItemCountReward`, `OptionalItemReward`, `OptionalItemCountReward`, `IssuerStart`, `IssuerLocation`, `PlaceName`, and `Festival`. Acquisition sources are resolved only for mapped Boutique equipment items and then attached immutably before catalog construction.
