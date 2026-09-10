using System.Collections.Immutable;
using CrystariumBoutique.Core.Appearance;
using CrystariumBoutique.Core.Catalog;

namespace CrystariumBoutique.Core.Tests.Catalog;

public sealed class EquipmentCatalogTests
{
    [Fact]
    public void GetPageUsesTwentyFourItemDefault()
    {
        var catalog = new EquipmentCatalog(CreateItems(50, EquipmentSlot.Head, "arr"));

        var first = catalog.GetPage(EquipmentSlot.Head, "arr", 0);
        var second = catalog.GetPage(EquipmentSlot.Head, "arr", 1);
        var third = catalog.GetPage(EquipmentSlot.Head, "arr", 2);

        Assert.Equal(24, first.Items.Length);
        Assert.Equal(24, second.Items.Length);
        Assert.Equal(2, third.Items.Length);
        Assert.Equal(3, first.TotalPages);
        Assert.Equal(50, first.TotalResults);
    }

    [Fact]
    public void GetPageSupportsEighteenItemPages()
    {
        var catalog = new EquipmentCatalog(CreateItems(50, EquipmentSlot.Head, "arr"));

        var first = catalog.GetPage(EquipmentSlot.Head, "arr", 0, pageSize: 18);
        var second = catalog.GetPage(EquipmentSlot.Head, "arr", 1, pageSize: 18);
        var third = catalog.GetPage(EquipmentSlot.Head, "arr", 2, pageSize: 18);

        Assert.Equal(18, first.Items.Length);
        Assert.Equal(18, second.Items.Length);
        Assert.Equal(14, third.Items.Length);
        Assert.Equal(3, first.TotalPages);
        Assert.Equal(50, first.TotalResults);
    }

    [Fact]
    public void GetPageClampsOutOfRangePageIndex()
    {
        var catalog = new EquipmentCatalog(CreateItems(30, EquipmentSlot.Body, "arr"));

        var page = catalog.GetPage(EquipmentSlot.Body, "arr", 99);

        Assert.Equal(1, page.PageIndex);
        Assert.Equal(2, page.DisplayPage);
        Assert.Equal(6, page.Items.Length);
    }

    [Fact]
    public void CatalogDeduplicatesSharedAppearanceAndRetainsSources()
    {
        var group = ContentGroupResolver.Resolve(50);
        var appearance = new AppearanceKey(42, 7);
        var items = new[]
        {
            CreateItem(1, "First", EquipmentSlot.Head, group, appearance),
            CreateItem(2, "Second", EquipmentSlot.Head, group, appearance),
        };
        var catalog = new EquipmentCatalog(items);

        var page = catalog.GetPage(EquipmentSlot.Head, group.Key, 0);

        var entry = Assert.Single(page.Items);
        Assert.Equal(2, entry.SourceItems.Length);
        Assert.Equal("Second", entry.PrimaryItem.Name);
        Assert.Equal([2u, 1u], entry.SourceItems.Select(item => item.ItemId));
    }

    [Fact]
    public void CatalogPreservesSharedGeometryWithDifferentBakedVisualIcons()
    {
        var group = ContentGroupResolver.Resolve(50);
        var catalog = new EquipmentCatalog(
        [
            CreateItem(
                1,
                "Red and Black Coat",
                EquipmentSlot.Body,
                group,
                new AppearanceKey(42, 7, 1001, 0)),
            CreateItem(
                2,
                "White and Gold Coat",
                EquipmentSlot.Body,
                group,
                new AppearanceKey(42, 7, 1002, 0)),
        ]);

        var page = catalog.GetPage(EquipmentSlot.Body, group.Key, 0);

        Assert.Equal(2, page.TotalResults);
        Assert.Equal([2u, 1u], page.Items.Select(entry => entry.PrimaryItem.ItemId));
    }

    [Fact]
    public void CatalogPreservesSharedGeometryWithDifferentDyeCapabilities()
    {
        var group = ContentGroupResolver.Resolve(50);
        var catalog = new EquipmentCatalog(
        [
            CreateItem(
                1,
                "Fixed-color Gloves",
                EquipmentSlot.Hands,
                group,
                new AppearanceKey(42, 7, 1001, 0),
                dyeChannelCount: 0),
            CreateItem(
                2,
                "Single-dye Gloves",
                EquipmentSlot.Hands,
                group,
                new AppearanceKey(42, 7, 1001, 1),
                dyeChannelCount: 1),
            CreateItem(
                3,
                "Two-dye Gloves",
                EquipmentSlot.Hands,
                group,
                new AppearanceKey(42, 7, 1001, 2),
                dyeChannelCount: 2),
        ]);

        var page = catalog.GetPage(EquipmentSlot.Hands, group.Key, 0);

        Assert.Equal(3, page.TotalResults);
        Assert.Equal([3u, 2u, 1u], page.Items.Select(entry => entry.PrimaryItem.ItemId));
    }

    [Fact]
    public void CatalogPreservesGlowingAndMatteRelicVariants()
    {
        var group = ContentGroupResolver.Resolve(90);
        var catalog = new EquipmentCatalog(
        [
            CreateItem(
                1,
                "Replica Relic",
                EquipmentSlot.MainHand,
                group,
                new AppearanceKey(500, 9, 2001, 1)),
            CreateItem(
                2,
                "Replica Relic (Matte)",
                EquipmentSlot.MainHand,
                group,
                new AppearanceKey(500, 9, 2002, 1)),
        ]);

        var page = catalog.GetPage(EquipmentSlot.MainHand, group.Key, 0);

        Assert.Equal(2, page.TotalResults);
        Assert.Contains(page.Items, entry => entry.PrimaryItem.Name == "Replica Relic");
        Assert.Contains(page.Items, entry => entry.PrimaryItem.Name == "Replica Relic (Matte)");
    }

    [Fact]
    public void CatalogPreservesActualRelicStageAndVisuallyDistinctReplica()
    {
        var group = ContentGroupResolver.Resolve(80);
        var catalog = new EquipmentCatalog(
        [
            CreateItem(
                33465,
                "Blade's Glory",
                EquipmentSlot.MainHand,
                group,
                new AppearanceKey(8596357621, 0, 31610, 2)),
            CreateItem(
                33737,
                "Matte Replica Blade's Glory",
                EquipmentSlot.MainHand,
                group,
                new AppearanceKey(4301390325, 0, 31610, 2)),
        ]);

        var page = catalog.GetPage(EquipmentSlot.MainHand, group.Key, 0);

        Assert.Equal(2, page.TotalResults);
        Assert.Contains(page.Items, entry => entry.PrimaryItem.ItemId == 33465);
        Assert.Contains(page.Items, entry => entry.PrimaryItem.ItemId == 33737);
    }

    [Fact]
    public void CatalogPrefersOriginalOverReplicaForTrueVisualDuplicate()
    {
        var group = ContentGroupResolver.Resolve(90);
        var appearance = new AppearanceKey(500, 9, 2001, 2);
        var catalog = new EquipmentCatalog(
        [
            CreateItem(39922, "Majestic Manderville Bardiche", EquipmentSlot.MainHand, group, appearance),
            CreateItem(40954, "Majestic Manderville Bardiche Replica", EquipmentSlot.MainHand, group, appearance),
        ]);

        var entry = Assert.Single(catalog.GetPage(EquipmentSlot.MainHand, group.Key, 0).Items);

        Assert.Equal(39922u, entry.PrimaryItem.ItemId);
        Assert.Equal([39922u, 40954u], entry.SourceItems.Select(item => item.ItemId));
    }

    [Fact]
    public void RetainedVisualVariantsKeepExactItemAcquisitionSources()
    {
        var group = ContentGroupResolver.Resolve(90);
        var dungeonSource = new ItemAcquisitionSource(
            ItemAcquisitionKind.DutyDrop,
            "The Dead Ends");
        var craftedSource = new ItemAcquisitionSource(
            ItemAcquisitionKind.Crafting,
            "Armorer");
        var catalog = new EquipmentCatalog(
        [
            CreateItem(1, "Dungeon Coat", EquipmentSlot.Body, group, new AppearanceKey(42, 7, 1001, 1)) with
            {
                AcquisitionSources = [dungeonSource],
            },
            CreateItem(2, "Crafted Coat", EquipmentSlot.Body, group, new AppearanceKey(42, 7, 1002, 1)) with
            {
                AcquisitionSources = [craftedSource],
            },
        ]);

        var entries = catalog.GetPage(EquipmentSlot.Body, group.Key, 0).Items;

        Assert.Equal(dungeonSource, entries.Single(entry => entry.PrimaryItem.ItemId == 1).PrimaryItem.AcquisitionSources.Single());
        Assert.Equal(craftedSource, entries.Single(entry => entry.PrimaryItem.ItemId == 2).PrimaryItem.AcquisitionSources.Single());
    }

    [Fact]
    public void ModelFamilyLookupRelatesDrachenVariantsKeptAsSeparateAppearances()
    {
        var group = ContentGroupResolver.Resolve(50);
        var catalog = new EquipmentCatalog(
        [
            CreateItem(
                3223,
                "Drachen Mail",
                EquipmentSlot.Body,
                group,
                new AppearanceKey(65572, 0, 43088, 0),
                dyeChannelCount: 0),
            CreateItem(
                8407,
                "Augmented Drachen Mail",
                EquipmentSlot.Body,
                group,
                new AppearanceKey(65572, 0, 43088, 2),
                dyeChannelCount: 2),
        ]);

        var appearances = catalog.GetPage(EquipmentSlot.Body, group.Key, 0);
        var family = catalog.GetItemsSharingModel(new ModelFamilyKey(65572, 0), EquipmentSlot.Body);

        Assert.Equal(2, appearances.TotalResults);
        Assert.Equal([8407u, 3223u], family.Select(item => item.ItemId));
    }

    [Fact]
    public void ModelFamilyLookupUsesBothWeaponMainAndSubModels()
    {
        var group = ContentGroupResolver.Resolve(50);
        var catalog = new EquipmentCatalog(
        [
            CreateItem(
                8639,
                "Ironworks Magitek Sword",
                EquipmentSlot.MainHand,
                group,
                new AppearanceKey(4295033040, 0, 30513, 0),
                dyeChannelCount: 0),
            CreateItem(
                8933,
                "Augmented Ironworks Magitek Sword",
                EquipmentSlot.MainHand,
                group,
                new AppearanceKey(4295033040, 0, 30513, 1),
                dyeChannelCount: 1),
            CreateItem(
                9000,
                "Different Linked Component",
                EquipmentSlot.MainHand,
                group,
                new AppearanceKey(4295033040, 18, 30513, 1),
                dyeChannelCount: 1),
        ]);

        var family = catalog.GetItemsSharingModel(
            new ModelFamilyKey(4295033040, 0),
            EquipmentSlot.MainHand);

        Assert.Equal([8933u, 8639u], family.Select(item => item.ItemId));
        Assert.DoesNotContain(family, item => item.ItemId == 9000);
    }

    [Fact]
    public void ModelFamilyLookupExcludesLegacyItemsAndDifferentSlots()
    {
        var group = ContentGroupResolver.Resolve(50);
        var visible = CreateItem(10, "Current Gloves", EquipmentSlot.Hands, group, new AppearanceKey(42, 0));
        var excluded = CreateItem(11, "Dated Gloves", EquipmentSlot.Hands, group, new AppearanceKey(42, 0)) with
        {
            IsExcludedFromBrowsing = true,
        };
        var body = CreateItem(12, "Current Body", EquipmentSlot.Body, group, new AppearanceKey(42, 0));
        var catalog = new EquipmentCatalog([visible, excluded, body]);

        var family = catalog.GetItemsSharingModel(new ModelFamilyKey(42, 0), EquipmentSlot.Hands);

        Assert.Equal(10u, Assert.Single(family).ItemId);
        Assert.DoesNotContain(family, item => item.ItemId == 11);
        Assert.DoesNotContain(family, item => item.ItemId == 12);
        Assert.True(catalog.TryGetItem(11, EquipmentSlot.Hands, out _));
    }

    [Fact]
    public void AppearanceLookupCombinesSharedItemsAcrossContentGroups()
    {
        var appearance = new AppearanceKey(42, 7);
        var catalog = new EquipmentCatalog(
        [
            CreateItem(1, "Original", EquipmentSlot.Head, ContentGroupResolver.Resolve(50), appearance),
            CreateItem(2, "Later Alias", EquipmentSlot.Head, ContentGroupResolver.Resolve(100), appearance),
        ]);

        var found = catalog.TryGetAppearance(appearance, EquipmentSlot.Head, out var entry);

        Assert.True(found);
        Assert.Equal([2u, 1u], entry.SourceItems.Select(item => item.ItemId));
        Assert.False(catalog.TryGetAppearance(appearance, EquipmentSlot.Body, out _));
    }

    [Fact]
    public void CatalogOrdersNewestItemRowsFirstWithinAContentGroup()
    {
        var group = ContentGroupResolver.Resolve(100);
        var catalog = new EquipmentCatalog(
        [
            CreateItem(100, "Oldest", EquipmentSlot.Head, group, new AppearanceKey(100, 0)),
            CreateItem(350, "Newest", EquipmentSlot.Head, group, new AppearanceKey(350, 0)),
            CreateItem(220, "Middle", EquipmentSlot.Head, group, new AppearanceKey(220, 0)),
        ]);

        var page = catalog.GetPage(EquipmentSlot.Head, group.Key, 0);

        Assert.Equal([350u, 220u, 100u], page.Items.Select(entry => entry.PrimaryItem.ItemId));
    }

    [Fact]
    public void CatalogSeparatesSlotAndContentGroupIndexes()
    {
        var arr = ContentGroupResolver.Resolve(50);
        var hw = ContentGroupResolver.Resolve(60);
        var items = new[]
        {
            CreateItem(1, "ARR Head", EquipmentSlot.Head, arr, new AppearanceKey(1, 0)),
            CreateItem(2, "HW Head", EquipmentSlot.Head, hw, new AppearanceKey(2, 0)),
            CreateItem(3, "ARR Body", EquipmentSlot.Body, arr, new AppearanceKey(3, 0)),
        };
        var catalog = new EquipmentCatalog(items);

        Assert.Single(catalog.GetPage(EquipmentSlot.Head, arr.Key, 0).Items);
        Assert.Single(catalog.GetPage(EquipmentSlot.Head, hw.Key, 0).Items);
        Assert.Single(catalog.GetPage(EquipmentSlot.Body, arr.Key, 0).Items);
    }

    [Fact]
    public void SpecialGroupsSeparateMogStationAndCombineExpansionItems()
    {
        var arr = ContentGroupResolver.Resolve(50);
        var hw = ContentGroupResolver.Resolve(60);
        var mogStationItem = CreateItem(
            3,
            "Store Hat",
            EquipmentSlot.Head,
            SpecialContentGroups.MogStation,
            new AppearanceKey(3, 0)) with
        {
            IsMogStationExclusive = true,
        };
        var catalog = new EquipmentCatalog(
        [
            CreateItem(1, "ARR Hat", EquipmentSlot.Head, arr, new AppearanceKey(1, 0)),
            CreateItem(2, "HW Hat", EquipmentSlot.Head, hw, new AppearanceKey(2, 0)),
            mogStationItem,
        ]);

        var groups = catalog.GetContentGroups(EquipmentSlot.Head);
        var all = catalog.GetPage(EquipmentSlot.Head, SpecialContentGroups.AllExpansions.Key, 0);
        var store = catalog.GetPage(EquipmentSlot.Head, SpecialContentGroups.MogStation.Key, 0);

        Assert.Equal(["all-expansions", "arr", "hw", "mogstation"], groups.Select(group => group.Key));
        Assert.Equal("All", groups[0].DisplayName);
        Assert.Equal(3, all.TotalResults);
        Assert.Contains(all.Items, entry => entry.PrimaryItem.Name == "Store Hat");
        Assert.Equal("Store Hat", Assert.Single(store.Items).PrimaryItem.Name);
        Assert.Equal(1, catalog.MogStationSourceItemCount);
        Assert.Equal(3, catalog.AppearanceCount);
    }

    private static IEnumerable<EquipmentItem> CreateItems(int count, EquipmentSlot slot, string groupKey)
    {
        var group = groupKey == "arr" ? ContentGroupResolver.Resolve(50) : ContentGroupResolver.Resolve(60);
        for (var index = 0; index < count; index++)
        {
            yield return CreateItem(
                (uint)(index + 1),
                $"Item {index + 1:D2}",
                slot,
                group,
                new AppearanceKey((ulong)(index + 1), 0));
        }
    }

    private static EquipmentItem CreateItem(
        uint itemId,
        string name,
        EquipmentSlot slot,
        ContentGroup group,
        AppearanceKey appearanceKey,
        byte dyeChannelCount = 1)
        => new(
            itemId,
            name,
            "Test Equipment",
            group.MaximumEquipLevel,
            itemId,
            itemId,
            dyeChannelCount,
            appearanceKey,
            group,
            ImmutableArray.Create(slot));
}
