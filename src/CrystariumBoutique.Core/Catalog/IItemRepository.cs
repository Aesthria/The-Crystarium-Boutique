using System.Collections.Immutable;
using CrystariumBoutique.Core.Appearance;

namespace CrystariumBoutique.Core.Catalog;

public interface IItemRepository
{
    int SourceItemCount { get; }

    int AppearanceCount { get; }

    int MogStationSourceItemCount { get; }

    bool TryGetItem(uint itemId, EquipmentSlot slot, out EquipmentItem item);

    bool TryFindItem(string itemName, EquipmentSlot slot, out EquipmentItem item);

    bool TryGetAppearance(
        AppearanceKey appearanceKey,
        EquipmentSlot slot,
        out AppearanceEntry entry);

    ImmutableArray<EquipmentItem> GetItemsSharingModel(
        ModelFamilyKey modelFamilyKey,
        EquipmentSlot slot);

    ImmutableArray<ContentGroup> GetContentGroups(EquipmentSlot slot);

    CatalogPage GetPage(
        EquipmentSlot slot,
        string contentGroupKey,
        int pageIndex,
        CatalogFilter? filter = null,
        int pageSize = EquipmentCatalog.DefaultPageSize);
}
