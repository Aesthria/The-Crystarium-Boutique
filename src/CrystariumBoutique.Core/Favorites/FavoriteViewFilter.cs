using CrystariumBoutique.Core.Appearance;
using CrystariumBoutique.Core.Catalog;

namespace CrystariumBoutique.Core.Favorites;

public static class FavoriteViewFilter
{
    public static bool Matches(
        EquipmentItem item,
        EquipmentSlot? slot,
        string? search)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (slot is { } requiredSlot && !item.CompatibleSlots.Contains(requiredSlot))
        {
            return false;
        }

        var normalizedSearch = CatalogSearch.Normalize(search ?? string.Empty);
        return normalizedSearch.Length == 0
            || CatalogSearch.MatchesAllTerms(
                CatalogSearch.Normalize($"{item.Name} {item.CategoryName}"),
                normalizedSearch);
    }
}
