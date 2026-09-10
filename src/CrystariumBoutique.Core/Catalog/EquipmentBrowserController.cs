using System.Collections.Immutable;
using CrystariumBoutique.Core.Appearance;
using CrystariumBoutique.Core.Errors;

namespace CrystariumBoutique.Core.Catalog;

public sealed class EquipmentBrowserController
{
    private readonly IItemRepository repository;
    private CatalogPage? currentPage;

    public EquipmentBrowserController(
        IItemRepository repository,
        int pageSize = EquipmentCatalog.DefaultPageSize)
    {
        this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageSize);
        PageSize = pageSize;
        SelectedSlot = EquipmentSlot.Head;
        SelectFirstAvailableGroup();
        RefreshPage();
    }

    public EquipmentSlot SelectedSlot { get; private set; }

    public string? SelectedContentGroupKey { get; private set; }

    public int PageIndex { get; private set; }

    public int PageSize { get; private set; }

    public AppearanceKey? SelectedAppearanceKey { get; private set; }

    public CatalogFilter Filter { get; private set; } = CatalogFilter.Default;

    public uint? ActiveClassJobId { get; private set; }

    public ImmutableArray<ContentGroup> AvailableContentGroups
        => repository.GetContentGroups(SelectedSlot);

    public CatalogPage? CurrentPage => currentPage;

    public Result SelectSlot(EquipmentSlot slot)
    {
        if (SelectedSlot == slot)
        {
            return Result.Ok;
        }

        SelectedSlot = slot;
        PageIndex = 0;
        SelectedAppearanceKey = null;
        SelectFirstAvailableGroup();
        RefreshPage();
        return Result.Ok;
    }

    public Result SelectContentGroup(string contentGroupKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contentGroupKey);
        if (!AvailableContentGroups.Any(group => group.Key == contentGroupKey))
        {
            return Result.Failure(new BoutiqueError(
                BoutiqueErrorCode.InvalidGameState,
                "The requested content group is not available for the selected equipment slot.",
                contentGroupKey));
        }

        SelectedContentGroupKey = contentGroupKey;
        PageIndex = 0;
        SelectedAppearanceKey = null;
        RefreshPage();
        return Result.Ok;
    }

    public void MoveContentGroup(int offset)
    {
        var groups = AvailableContentGroups;
        if (groups.IsEmpty || SelectedContentGroupKey is null)
        {
            return;
        }

        var currentIndex = 0;
        while (currentIndex < groups.Length && groups[currentIndex].Key != SelectedContentGroupKey)
        {
            currentIndex++;
        }

        if (currentIndex == groups.Length)
        {
            return;
        }

        var targetIndex = Math.Clamp(currentIndex + offset, 0, groups.Length - 1);
        if (targetIndex != currentIndex)
        {
            SelectContentGroup(groups[targetIndex].Key);
        }
    }

    public void ApplyFilter(CatalogFilter filter)
    {
        ArgumentNullException.ThrowIfNull(filter);
        Filter = filter;
        PageIndex = 0;
        RefreshPage();
    }

    public void SetPageSize(int pageSize)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageSize);
        if (PageSize == pageSize)
        {
            return;
        }

        PageSize = pageSize;
        PageIndex = 0;
        RefreshPage();
    }

    public void SetActiveClassJob(uint? classJobId)
    {
        classJobId = classJobId is > 0 and < 128 ? classJobId : null;
        if (ActiveClassJobId == classJobId)
        {
            return;
        }

        ActiveClassJobId = classJobId;
        PageIndex = 0;
        SelectedAppearanceKey = null;
        RefreshPage();
    }

    public void MovePage(int offset)
    {
        var page = CurrentPage;
        if (page is null || page.TotalPages == 0)
        {
            PageIndex = 0;
            return;
        }

        PageIndex = Math.Clamp(PageIndex + offset, 0, page.TotalPages - 1);
        RefreshPage();
    }

    public void SelectAppearance(AppearanceKey appearanceKey)
        => SelectedAppearanceKey = appearanceKey;

    public void ClearAppearanceSelection()
        => SelectedAppearanceKey = null;

    private void SelectFirstAvailableGroup()
    {
        var groups = AvailableContentGroups;
        SelectedContentGroupKey = groups
            .FirstOrDefault(group => group.Key == SpecialContentGroups.AllExpansions.Key)?.Key
            ?? (groups.IsEmpty ? null : groups[0].Key);
    }

    private void RefreshPage()
    {
        currentPage = SelectedContentGroupKey is null
            ? null
            : repository.GetPage(
                SelectedSlot,
                SelectedContentGroupKey,
                PageIndex,
                Filter with { ActiveClassJobId = ActiveClassJobId },
                PageSize);
        PageIndex = currentPage?.PageIndex ?? 0;
    }
}
