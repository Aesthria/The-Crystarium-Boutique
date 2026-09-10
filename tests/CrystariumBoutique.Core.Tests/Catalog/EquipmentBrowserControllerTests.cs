using System.Collections.Immutable;
using CrystariumBoutique.Core.Appearance;
using CrystariumBoutique.Core.Catalog;

namespace CrystariumBoutique.Core.Tests.Catalog;

public sealed class EquipmentBrowserControllerTests
{
    [Fact]
    public void SelectSlotResetsGroupPageAndSelection()
    {
        var arr = ContentGroupResolver.Resolve(50);
        var catalog = new EquipmentCatalog(
        [
            CreateItem(1, EquipmentSlot.Head, arr),
            CreateItem(2, EquipmentSlot.Body, arr),
        ]);
        var browser = new EquipmentBrowserController(catalog);
        browser.SelectAppearance(new AppearanceKey(1, 0));

        var result = browser.SelectSlot(EquipmentSlot.Body);

        Assert.True(result.IsSuccess);
        Assert.Equal(EquipmentSlot.Body, browser.SelectedSlot);
        Assert.Equal(SpecialContentGroups.AllExpansions.Key, browser.SelectedContentGroupKey);
        Assert.Equal(0, browser.PageIndex);
        Assert.Null(browser.SelectedAppearanceKey);
    }

    [Fact]
    public void SelectingCurrentSlotPreservesPageGroupFilterAndSelection()
    {
        var arr = ContentGroupResolver.Resolve(50);
        var items = Enumerable.Range(1, 30)
            .Select(index => CreateItem((uint)index, EquipmentSlot.Head, arr));
        var browser = new EquipmentBrowserController(new EquipmentCatalog(items));
        var selection = new AppearanceKey(20, 0);
        browser.ApplyFilter(CatalogFilter.Default with { MinimumItemLevel = 1 });
        browser.MovePage(1);
        browser.SelectAppearance(selection);

        var result = browser.SelectSlot(EquipmentSlot.Head);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, browser.PageIndex);
        Assert.Equal(SpecialContentGroups.AllExpansions.Key, browser.SelectedContentGroupKey);
        Assert.Equal(1u, browser.Filter.MinimumItemLevel);
        Assert.Equal(selection, browser.SelectedAppearanceKey);
    }

    [Fact]
    public void MovePageNeverLeavesAvailableRange()
    {
        var arr = ContentGroupResolver.Resolve(50);
        var items = Enumerable.Range(1, 30)
            .Select(index => CreateItem((uint)index, EquipmentSlot.Head, arr));
        var browser = new EquipmentBrowserController(new EquipmentCatalog(items));

        browser.MovePage(99);
        Assert.Equal(1, browser.PageIndex);

        browser.MovePage(-99);
        Assert.Equal(0, browser.PageIndex);
    }

    [Theory]
    [InlineData(15, 4)]
    [InlineData(20, 3)]
    [InlineData(18, 3)]
    [InlineData(24, 3)]
    public void SetPageSizeRefreshesPaginationAndResetsPage(int pageSize, int expectedPages)
    {
        var arr = ContentGroupResolver.Resolve(50);
        var items = Enumerable.Range(1, 50)
            .Select(index => CreateItem((uint)index, EquipmentSlot.Head, arr));
        var browser = new EquipmentBrowserController(new EquipmentCatalog(items), pageSize: 12);
        browser.MovePage(2);
        var selection = new AppearanceKey(40, 0);
        browser.SelectAppearance(selection);

        browser.SetPageSize(pageSize);

        Assert.Equal(pageSize, browser.PageSize);
        Assert.Equal(0, browser.PageIndex);
        Assert.Equal(pageSize, browser.CurrentPage!.Items.Length);
        Assert.Equal(expectedPages, browser.CurrentPage.TotalPages);
        Assert.Equal(selection, browser.SelectedAppearanceKey);
    }

    [Fact]
    public void SelectContentGroupRejectsUnavailableGroup()
    {
        var arr = ContentGroupResolver.Resolve(50);
        var browser = new EquipmentBrowserController(new EquipmentCatalog([CreateItem(1, EquipmentSlot.Head, arr)]));

        var result = browser.SelectContentGroup("missing");

        Assert.False(result.IsSuccess);
        Assert.Equal(SpecialContentGroups.AllExpansions.Key, browser.SelectedContentGroupKey);
    }

    [Fact]
    public void MoveContentGroupUsesChronologicalOrderAndClampsAtTheEnds()
    {
        var arr = ContentGroupResolver.Resolve(50);
        var hw = ContentGroupResolver.Resolve(60);
        var browser = new EquipmentBrowserController(new EquipmentCatalog(
        [
            CreateItem(1, EquipmentSlot.Head, arr),
            CreateItem(2, EquipmentSlot.Head, hw),
        ]));

        browser.MoveContentGroup(1);
        Assert.Equal("arr", browser.SelectedContentGroupKey);

        browser.MoveContentGroup(10);
        Assert.Equal("hw", browser.SelectedContentGroupKey);

        browser.MoveContentGroup(-10);
        Assert.Equal(SpecialContentGroups.AllExpansions.Key, browser.SelectedContentGroupKey);
    }

    [Fact]
    public void ApplyingFilterResetsPageButKeepsThePreviewSelection()
    {
        var arr = ContentGroupResolver.Resolve(50);
        var items = Enumerable.Range(1, 30)
            .Select(index => CreateItem((uint)index, EquipmentSlot.Head, arr));
        var browser = new EquipmentBrowserController(new EquipmentCatalog(items));
        var selection = new AppearanceKey(20, 0);
        browser.MovePage(1);
        browser.SelectAppearance(selection);

        browser.ApplyFilter(new CatalogFilter(
            "Item 20",
            null,
            null,
            null,
            null,
            DyeSupportFilter.Any));

        Assert.Equal(0, browser.PageIndex);
        Assert.Equal(selection, browser.SelectedAppearanceKey);
        Assert.Equal((uint)20, Assert.Single(browser.CurrentPage!.Items).PrimaryItem.ItemId);
    }

    [Fact]
    public void ClearAppearanceSelectionRemovesOnlyTheTileHighlight()
    {
        var arr = ContentGroupResolver.Resolve(50);
        var browser = new EquipmentBrowserController(
            new EquipmentCatalog([CreateItem(1, EquipmentSlot.Head, arr)]));
        browser.SelectAppearance(new AppearanceKey(1, 0));

        browser.ClearAppearanceSelection();

        Assert.Null(browser.SelectedAppearanceKey);
        Assert.Equal(EquipmentSlot.Head, browser.SelectedSlot);
        Assert.NotNull(browser.CurrentPage);
    }

    [Fact]
    public void ReadingCurrentPageDoesNotRepeatAnUnchangedRepositoryQuery()
    {
        var arr = ContentGroupResolver.Resolve(50);
        var repository = new CountingRepository(
            new EquipmentCatalog([CreateItem(1, EquipmentSlot.Head, arr)]));
        var browser = new EquipmentBrowserController(repository);

        for (var index = 0; index < 20; index++)
        {
            _ = browser.CurrentPage;
        }

        Assert.Equal(1, repository.PageRequestCount);
    }

    [Fact]
    public void ActiveClassJobRefreshesAndFiltersWeaponResults()
    {
        var arr = ContentGroupResolver.Resolve(50);
        var paladin = default(ClassJobMask).Add(19);
        var warrior = default(ClassJobMask).Add(21);
        var catalog = new EquipmentCatalog(
        [
            CreateItem(1, EquipmentSlot.MainHand, arr) with { EquippableClassJobs = paladin },
            CreateItem(2, EquipmentSlot.MainHand, arr) with { EquippableClassJobs = warrior },
        ]);
        var browser = new EquipmentBrowserController(catalog);
        browser.SelectSlot(EquipmentSlot.MainHand);
        browser.SelectAppearance(new AppearanceKey(2, 0));

        browser.SetActiveClassJob(19);

        Assert.Equal((uint)19, browser.ActiveClassJobId);
        Assert.Null(browser.SelectedAppearanceKey);
        Assert.Equal((uint)1, Assert.Single(browser.CurrentPage!.Items).PrimaryItem.ItemId);
    }

    private static EquipmentItem CreateItem(uint itemId, EquipmentSlot slot, ContentGroup group)
        => new(
            itemId,
            $"Item {itemId}",
            "Test Equipment",
            group.MaximumEquipLevel,
            itemId,
            itemId,
            1,
            new AppearanceKey(itemId, 0),
            group,
            ImmutableArray.Create(slot));

    private sealed class CountingRepository(IItemRepository inner) : IItemRepository
    {
        public int PageRequestCount { get; private set; }

        public int SourceItemCount => inner.SourceItemCount;

        public int AppearanceCount => inner.AppearanceCount;

        public int MogStationSourceItemCount => inner.MogStationSourceItemCount;

        public bool TryGetItem(uint itemId, EquipmentSlot slot, out EquipmentItem item)
            => inner.TryGetItem(itemId, slot, out item!);

        public bool TryFindItem(string itemName, EquipmentSlot slot, out EquipmentItem item)
            => inner.TryFindItem(itemName, slot, out item!);

        public bool TryGetAppearance(
            AppearanceKey appearanceKey,
            EquipmentSlot slot,
            out AppearanceEntry entry)
            => inner.TryGetAppearance(appearanceKey, slot, out entry!);

        public ImmutableArray<EquipmentItem> GetItemsSharingModel(
            ModelFamilyKey modelFamilyKey,
            EquipmentSlot slot)
            => inner.GetItemsSharingModel(modelFamilyKey, slot);

        public ImmutableArray<ContentGroup> GetContentGroups(EquipmentSlot slot)
            => inner.GetContentGroups(slot);

        public CatalogPage GetPage(
            EquipmentSlot slot,
            string contentGroupKey,
            int pageIndex,
            CatalogFilter? filter = null,
            int pageSize = EquipmentCatalog.DefaultPageSize)
        {
            PageRequestCount++;
            return inner.GetPage(slot, contentGroupKey, pageIndex, filter, pageSize);
        }
    }
}
