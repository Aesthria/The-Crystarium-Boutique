namespace CrystariumBoutique.Core.Catalog;

public static class ItemMarketabilityResolver
{
    public static bool IsMarketable(uint itemSearchCategoryId, bool isUntradable)
        => itemSearchCategoryId > 0 && !isUntradable;
}
