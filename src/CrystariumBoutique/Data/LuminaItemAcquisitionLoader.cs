using System.Collections.Immutable;
using CrystariumBoutique.Core.Catalog;
using Dalamud.Plugin.Services;
using LuminaAchievement = Lumina.Excel.Sheets.Achievement;
using LuminaCraftType = Lumina.Excel.Sheets.CraftType;
using LuminaENpcBase = Lumina.Excel.Sheets.ENpcBase;
using LuminaENpcResident = Lumina.Excel.Sheets.ENpcResident;
using LuminaFestival = Lumina.Excel.Sheets.Festival;
using LuminaGCScripShopCategory = Lumina.Excel.Sheets.GCScripShopCategory;
using LuminaGCScripShopItem = Lumina.Excel.Sheets.GCScripShopItem;
using LuminaGilShop = Lumina.Excel.Sheets.GilShop;
using LuminaGilShopItem = Lumina.Excel.Sheets.GilShopItem;
using LuminaItem = Lumina.Excel.Sheets.Item;
using LuminaLevel = Lumina.Excel.Sheets.Level;
using LuminaQuest = Lumina.Excel.Sheets.Quest;
using LuminaRecipe = Lumina.Excel.Sheets.Recipe;
using LuminaSpecialShop = Lumina.Excel.Sheets.SpecialShop;

namespace CrystariumBoutique.Data;

public sealed record ItemAcquisitionLoad(
    ImmutableDictionary<uint, ImmutableArray<ItemAcquisitionSource>> SourcesByItem,
    int IndexedItemCount,
    int SourceCount,
    ImmutableArray<string> Warnings)
{
    public ImmutableArray<ItemAcquisitionSource> GetSources(uint itemId)
        => SourcesByItem.TryGetValue(itemId, out var sources)
            ? sources
            : ImmutableArray<ItemAcquisitionSource>.Empty;
}

public sealed class LuminaItemAcquisitionLoader
{
    private readonly IDataManager dataManager;
    private readonly HashSet<uint> validItemIds;
    private readonly Dictionary<uint, HashSet<ItemAcquisitionSource>> sourcesByItem = [];
    private readonly List<string> warnings = [];

    public LuminaItemAcquisitionLoader(
        IDataManager dataManager,
        IEnumerable<uint> validItemIds)
    {
        this.dataManager = dataManager ?? throw new ArgumentNullException(nameof(dataManager));
        ArgumentNullException.ThrowIfNull(validItemIds);
        this.validItemIds = validItemIds.ToHashSet();
    }

    public ItemAcquisitionLoad Load(
        IReadOnlySet<uint> onlineStoreItemIds,
        ItemAcquisitionSupplementLoad? supplement = null)
    {
        ArgumentNullException.ThrowIfNull(onlineStoreItemIds);
        TryLoad("Online Store", () => LoadOnlineStore(onlineStoreItemIds));
        TryLoad("crafting recipes", LoadRecipes);
        TryLoad("quest rewards", LoadQuestRewards);
        TryLoad("achievement rewards", LoadAchievementRewards);
        TryLoad("vendor locations and shops", LoadVendors);
        TryLoad("Grand Company shops", LoadGrandCompanyShops);
        if (supplement is not null)
        {
            warnings.AddRange(supplement.Warnings);
            TryLoad("local duty supplement", () => LoadSupplement(supplement));
        }

        var immutable = sourcesByItem.ToImmutableDictionary(
            pair => pair.Key,
            pair => pair.Value
                .OrderBy(source => source.Kind)
                .ThenBy(source => source.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(source => source.Location, StringComparer.OrdinalIgnoreCase)
                .ToImmutableArray());
        return new ItemAcquisitionLoad(
            immutable,
            immutable.Count,
            immutable.Sum(pair => pair.Value.Length),
            warnings.ToImmutableArray());
    }

    private void LoadOnlineStore(IReadOnlySet<uint> itemIds)
    {
        foreach (var itemId in itemIds)
        {
            Add(itemId, new ItemAcquisitionSource(
                ItemAcquisitionKind.OnlineStore,
                Metadata: new ItemAcquisitionMetadata(
                    SourceTable: "FittingShopItemSet",
                    Provenance: "Installed local game data")));
        }
    }

    private void LoadSupplement(ItemAcquisitionSupplementLoad supplement)
    {
        foreach (var pair in supplement.SourcesByItem)
        {
            foreach (var source in pair.Value)
            {
                Add(pair.Key, source);
            }
        }
    }

    private void LoadRecipes()
    {
        foreach (var recipe in dataManager.GetExcelSheet<LuminaRecipe>())
        {
            var itemId = recipe.ItemResult.RowId;
            if (!validItemIds.Contains(itemId))
            {
                continue;
            }

            var craft = recipe.CraftType.IsValid
                ? Clean(recipe.CraftType.Value.Name.ExtractText())
                : string.Empty;
            var detailParts = new List<string>(2);
            if (recipe.RecipeLevelTable.IsValid)
            {
                var level = recipe.RecipeLevelTable.Value;
                var stars = level.Stars == 0 ? string.Empty : $" {new string('\u2605', level.Stars)}";
                detailParts.Add($"Recipe Lv. {level.ClassJobLevel}{stars}");
            }

            if (recipe.SecretRecipeBook.IsValid)
            {
                var book = Clean(recipe.SecretRecipeBook.Value.Name.ExtractText());
                if (book.Length > 0)
                {
                    detailParts.Add(book);
                }
            }

            if (recipe.AmountResult > 1)
            {
                detailParts.Add($"Yields {recipe.AmountResult:N0}");
            }

            if (recipe.IsSpecializationRequired)
            {
                detailParts.Add("Specialist required");
            }

            if (recipe.Quest.IsValid)
            {
                var questName = Clean(recipe.Quest.Value.Name.ExtractText());
                if (questName.Length > 0)
                {
                    detailParts.Add($"Quest: {questName}");
                }
            }

            if (recipe.ItemRequired.IsValid)
            {
                var requiredItem = Clean(recipe.ItemRequired.Value.Name.ExtractText());
                if (requiredItem.Length > 0)
                {
                    detailParts.Add($"Required item: {requiredItem}");
                }
            }

            var ingredients = new List<string>(recipe.Ingredient.Count);
            for (var index = 0; index < recipe.Ingredient.Count; index++)
            {
                var ingredient = recipe.Ingredient[index];
                if (!ingredient.IsValid)
                {
                    continue;
                }

                var ingredientName = Clean(ingredient.Value.Name.ExtractText());
                if (ingredientName.Length == 0)
                {
                    continue;
                }

                var amount = index < recipe.AmountIngredient.Count
                    ? recipe.AmountIngredient[index]
                    : (byte)0;
                ingredients.Add(amount > 0
                    ? $"{amount:N0} {ingredientName}"
                    : ingredientName);
            }

            if (ingredients.Count > 0)
            {
                detailParts.Add($"Ingredients: {string.Join(" + ", ingredients)}");
            }

            Add(itemId, new ItemAcquisitionSource(
                ItemAcquisitionKind.Crafting,
                Name: craft,
                Detail: string.Join(" / ", detailParts),
                Metadata: new ItemAcquisitionMetadata(
                    recipe.RowId,
                    "Recipe",
                    Provenance: "Installed local game data")));
        }
    }

    private void LoadQuestRewards()
    {
        foreach (var quest in dataManager.GetExcelSheet<LuminaQuest>())
        {
            var questName = Clean(quest.Name.ExtractText());
            var rewards = new Dictionary<uint, uint>();
            for (var index = 0; index < quest.Reward.Count; index++)
            {
                var reward = quest.Reward[index];
                if (reward.GetValueOrDefault<LuminaItem>() is { } item)
                {
                    var amount = index < quest.ItemCountReward.Count
                        ? quest.ItemCountReward[index]
                        : (byte)0;
                    AddReward(rewards, item.RowId, amount);
                }
            }

            for (var index = 0; index < quest.OptionalItemReward.Count; index++)
            {
                var reward = quest.OptionalItemReward[index];
                if (reward.RowId > 0)
                {
                    var amount = index < quest.OptionalItemCountReward.Count
                        ? quest.OptionalItemCountReward[index]
                        : (byte)0;
                    AddReward(rewards, reward.RowId, amount);
                }
            }

            var festivalName = quest.Festival.IsValid
                ? Clean(quest.Festival.Value.Name.ExtractText())
                : string.Empty;
            var location = quest.IssuerLocation.IsValid
                ? ResolveLocation(quest.IssuerLocation.Value)
                : string.Empty;
            if (location.Length == 0 && quest.PlaceName.IsValid)
            {
                location = Clean(quest.PlaceName.Value.Name.ExtractText());
            }

            var issuer = quest.IssuerStart.GetValueOrDefault<LuminaENpcResident>() is { } resident
                ? Clean(resident.Singular.ExtractText())
                : string.Empty;
            foreach (var reward in rewards)
            {
                var detailParts = new List<string>(2);
                if (issuer.Length > 0)
                {
                    detailParts.Add($"Quest giver: {issuer}");
                }

                if (reward.Value > 0)
                {
                    detailParts.Add($"Reward quantity: {reward.Value:N0}");
                }

                var metadata = new ItemAcquisitionMetadata(
                    quest.RowId,
                    "Quest",
                    Provenance: "Installed local game data");
                if (festivalName.Length > 0)
                {
                    if (questName.Length > 0)
                    {
                        detailParts.Insert(0, $"Quest: {questName}");
                    }

                    Add(reward.Key, new ItemAcquisitionSource(
                        ItemAcquisitionKind.SeasonalEvent,
                        Name: festivalName,
                        Location: location,
                        Detail: string.Join(" / ", detailParts),
                        Metadata: metadata));
                }
                else
                {
                    Add(reward.Key, new ItemAcquisitionSource(
                        ItemAcquisitionKind.Quest,
                        Name: questName,
                        Location: location,
                        Detail: string.Join(" / ", detailParts),
                        Metadata: metadata));
                }
            }
        }
    }

    private void LoadAchievementRewards()
    {
        foreach (var achievement in dataManager.GetExcelSheet<LuminaAchievement>())
        {
            if (achievement.Item.RowId == 0)
            {
                continue;
            }

            Add(achievement.Item.RowId, new ItemAcquisitionSource(
                ItemAcquisitionKind.Achievement,
                Name: Clean(achievement.Name.ExtractText()),
                Metadata: new ItemAcquisitionMetadata(
                    achievement.RowId,
                    "Achievement",
                    Provenance: "Installed local game data")));
        }
    }

    private void LoadVendors()
    {
        var gilShops = dataManager.GetExcelSheet<LuminaGilShop>()
            .ToDictionary(shop => shop.RowId);
        var specialShops = dataManager.GetExcelSheet<LuminaSpecialShop>()
            .ToArray();
        var shopKeys = gilShops.Keys
            .Select(rowId => new ShopKey(ShopKind.Gil, rowId))
            .Concat(specialShops.Select(shop => new ShopKey(ShopKind.Special, shop.RowId)))
            .ToHashSet();
        var vendorDirectory = BuildVendorDirectory(shopKeys);

        foreach (var shopItem in dataManager.GetSubrowExcelSheet<LuminaGilShopItem>().Flatten())
        {
            var itemId = shopItem.Item.RowId;
            if (!validItemIds.Contains(itemId))
            {
                continue;
            }

            gilShops.TryGetValue(shopItem.RowId, out var shop);
            var shopName = shop.RowId > 0 ? Clean(shop.Name.ExtractText()) : string.Empty;
            var cost = shopItem.Item.IsValid && shopItem.Item.Value.PriceMid > 0
                ? $"{shopItem.Item.Value.PriceMid:N0} gil"
                : string.Empty;
            AddVendorSources(
                itemId,
                vendorDirectory,
                new ShopKey(ShopKind.Gil, shopItem.RowId),
                shopName,
                cost,
                detail: string.Empty,
                festivalName: shop.FestivalId > 0
                    ? ResolveFestivalName(shop.FestivalId)
                    : string.Empty);
        }

        foreach (var shop in specialShops)
        {
            var shopName = Clean(shop.Name.ExtractText());
            var festivalName = shop.RequiredFestival.IsValid
                ? Clean(shop.RequiredFestival.Value.Name.ExtractText())
                : string.Empty;
            var requiredDuty = shop.RequiredContentFinderCondition.IsValid
                ? Clean(shop.RequiredContentFinderCondition.Value.Name.ExtractText())
                : string.Empty;
            foreach (var offer in shop.Item)
            {
                var cost = FormatSpecialShopCost(offer);
                foreach (var received in offer.ReceiveItems)
                {
                    AddVendorSources(
                        received.Item.RowId,
                        vendorDirectory,
                        new ShopKey(ShopKind.Special, shop.RowId),
                        shopName,
                        cost,
                        requiredDuty.Length > 0 ? $"Requires: {requiredDuty}" : string.Empty,
                        festivalName);
                }
            }
        }
    }

    private void LoadGrandCompanyShops()
    {
        var categories = dataManager.GetExcelSheet<LuminaGCScripShopCategory>()
            .ToDictionary(category => category.RowId);
        foreach (var shopItem in dataManager.GetSubrowExcelSheet<LuminaGCScripShopItem>().Flatten())
        {
            if (!validItemIds.Contains(shopItem.Item.RowId))
            {
                continue;
            }

            var vendorName = "Grand Company Quartermaster";
            if (categories.TryGetValue(shopItem.RowId, out var category)
                && category.GrandCompany.IsValid)
            {
                var companyName = Clean(category.GrandCompany.Value.Name.ExtractText());
                if (companyName.Length > 0)
                {
                    vendorName = $"{companyName} Quartermaster";
                }
            }

            Add(shopItem.Item.RowId, new ItemAcquisitionSource(
                ItemAcquisitionKind.Vendor,
                Name: vendorName,
                Cost: shopItem.CostGCSeals > 0
                    ? $"{shopItem.CostGCSeals:N0} company seals"
                    : string.Empty,
                Metadata: new ItemAcquisitionMetadata(
                    shopItem.RowId,
                    "GCScripShopItem",
                    Provenance: "Installed local game data")));
        }
    }

    private Dictionary<ShopKey, ImmutableArray<VendorIdentity>> BuildVendorDirectory(
        IReadOnlySet<ShopKey> shopKeys)
    {
        var shopNpcs = new Dictionary<ShopKey, HashSet<uint>>();
        foreach (var npc in dataManager.GetExcelSheet<LuminaENpcBase>())
        {
            foreach (var data in npc.ENpcData)
            {
                ShopKey? key = data.Is<LuminaGilShop>()
                    ? new ShopKey(ShopKind.Gil, data.RowId)
                    : data.Is<LuminaSpecialShop>()
                        ? new ShopKey(ShopKind.Special, data.RowId)
                        : null;
                if (key is not { } shopKey || !shopKeys.Contains(shopKey))
                {
                    continue;
                }

                if (!shopNpcs.TryGetValue(shopKey, out var npcIds))
                {
                    npcIds = [];
                    shopNpcs.Add(shopKey, npcIds);
                }

                npcIds.Add(npc.RowId);
            }
        }

        var relevantNpcIds = shopNpcs.Values.SelectMany(ids => ids).ToHashSet();
        var locationsByNpc = new Dictionary<uint, HashSet<string>>();
        foreach (var level in dataManager.GetExcelSheet<LuminaLevel>())
        {
            var npcId = level.Object.RowId;
            if (!relevantNpcIds.Contains(npcId))
            {
                continue;
            }

            var location = ResolveLocation(level);
            if (location.Length == 0)
            {
                continue;
            }

            if (!locationsByNpc.TryGetValue(npcId, out var locations))
            {
                locations = [];
                locationsByNpc.Add(npcId, locations);
            }

            locations.Add(location);
        }

        var residentSheet = dataManager.GetExcelSheet<LuminaENpcResident>();
        return shopNpcs.ToDictionary(
            pair => pair.Key,
            pair => pair.Value
                .SelectMany(npcId =>
                {
                    var name = residentSheet.TryGetRow(npcId, out var resident)
                        ? Clean(resident.Singular.ExtractText())
                        : string.Empty;
                    IEnumerable<string> locations = locationsByNpc.TryGetValue(npcId, out var foundLocations)
                        ? foundLocations.OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                        : [string.Empty];
                    return locations.Select(location => new VendorIdentity(name, location));
                })
                .Where(vendor => vendor.Name.Length > 0 || vendor.Location.Length > 0)
                .Distinct()
                .ToImmutableArray());
    }

    private void AddVendorSources(
        uint itemId,
        IReadOnlyDictionary<ShopKey, ImmutableArray<VendorIdentity>> vendorDirectory,
        ShopKey shopKey,
        string fallbackName,
        string cost,
        string detail,
        string festivalName)
    {
        if (!validItemIds.Contains(itemId))
        {
            return;
        }

        var vendors = vendorDirectory.TryGetValue(shopKey, out var mappedVendors)
            && !mappedVendors.IsEmpty
                ? mappedVendors
                : [new VendorIdentity(fallbackName, string.Empty)];
        foreach (var vendor in vendors)
        {
            if (festivalName.Length > 0)
            {
                var seasonalDetail = string.Join(
                    " / ",
                    new[]
                    {
                        vendor.Name.Length > 0 ? $"Vendor: {vendor.Name}" : string.Empty,
                        detail,
                    }.Where(value => value.Length > 0));
                Add(itemId, new ItemAcquisitionSource(
                    ItemAcquisitionKind.SeasonalEvent,
                    Name: festivalName,
                    Location: vendor.Location,
                    Cost: cost,
                    Detail: seasonalDetail,
                    Metadata: new ItemAcquisitionMetadata(
                        shopKey.RowId,
                        shopKey.Kind == ShopKind.Gil ? "GilShop" : "SpecialShop",
                        Provenance: "Installed local game data")));
                continue;
            }

            Add(itemId, new ItemAcquisitionSource(
                ItemAcquisitionKind.Vendor,
                Name: vendor.Name.Length > 0 ? vendor.Name : fallbackName,
                Location: vendor.Location,
                Cost: cost,
                Detail: detail,
                Metadata: new ItemAcquisitionMetadata(
                    shopKey.RowId,
                    shopKey.Kind == ShopKind.Gil ? "GilShop" : "SpecialShop",
                    Provenance: "Installed local game data")));
        }
    }

    private static void AddReward(
        Dictionary<uint, uint> rewards,
        uint itemId,
        uint amount)
    {
        if (itemId == 0)
        {
            return;
        }

        if (!rewards.TryGetValue(itemId, out var existing) || amount > existing)
        {
            rewards[itemId] = amount;
        }
    }

    private static string FormatSpecialShopCost(LuminaSpecialShop.ItemStruct offer)
    {
        var costs = new List<string>(offer.ItemCosts.Count);
        foreach (var cost in offer.ItemCosts)
        {
            if (cost.CurrencyCost > 0)
            {
                var costName = cost.ItemCost.IsValid
                    ? Clean(cost.ItemCost.Value.Name.ExtractText())
                    : string.Empty;
                costs.Add(costName.Length > 0
                    ? $"{cost.CurrencyCost:N0} {costName}"
                    : $"{cost.CurrencyCost:N0} currency");
            }

            if (cost.CollectabilityCost > 0)
            {
                costs.Add($"collectability {cost.CollectabilityCost:N0}+");
            }
        }

        return string.Join(
            " + ",
            costs.Distinct(StringComparer.OrdinalIgnoreCase));
    }

    private string ResolveFestivalName(uint festivalId)
    {
        var sheet = dataManager.GetExcelSheet<LuminaFestival>();
        return sheet.TryGetRow(festivalId, out var festival)
            ? Clean(festival.Name.ExtractText())
            : string.Empty;
    }

    private static string ResolveLocation(LuminaLevel level)
    {
        if (level.Map.IsValid && level.Map.Value.PlaceName.IsValid)
        {
            var mapName = Clean(level.Map.Value.PlaceName.Value.Name.ExtractText());
            if (mapName.Length > 0)
            {
                return mapName;
            }
        }

        if (level.Territory.IsValid && level.Territory.Value.PlaceName.IsValid)
        {
            return Clean(level.Territory.Value.PlaceName.Value.Name.ExtractText());
        }

        return string.Empty;
    }

    private void Add(uint itemId, ItemAcquisitionSource source)
    {
        if (!validItemIds.Contains(itemId))
        {
            return;
        }

        if (!sourcesByItem.TryGetValue(itemId, out var sources))
        {
            sources = [];
            sourcesByItem.Add(itemId, sources);
        }

        sources.Add(source);
    }

    private void TryLoad(string sourceName, Action action)
    {
        try
        {
            action();
        }
        catch (Exception exception)
        {
            warnings.Add($"{sourceName}: {exception.GetType().Name}: {exception.Message}");
        }
    }

    private static string Clean(string value)
        => value.Trim();

    private enum ShopKind
    {
        Gil,
        Special,
    }

    private readonly record struct ShopKey(ShopKind Kind, uint RowId);

    private sealed record VendorIdentity(string Name, string Location);
}
