using System.Collections.Immutable;
using System.Diagnostics;
using CrystariumBoutique.Core.Appearance;

namespace CrystariumBoutique.Core.Catalog;

public sealed class EquipmentCatalog : IItemRepository
{
    public const int DefaultPageSize = 24;
    private const int FilterCacheCapacity = 64;

    private readonly ImmutableDictionary<CatalogIndexKey, ImmutableArray<AppearanceEntry>> index;
    private readonly ImmutableDictionary<EquipmentSlot, ImmutableArray<ContentGroup>> groupsBySlot;
    private readonly ImmutableDictionary<uint, EquipmentItem> itemsById;
    private readonly ImmutableDictionary<ItemNameIndexKey, EquipmentItem> itemsByNameAndSlot;
    private readonly ImmutableDictionary<AppearanceIndexKey, AppearanceEntry> appearancesBySlotAndKey;
    private readonly ImmutableDictionary<ModelFamilyIndexKey, ImmutableArray<EquipmentItem>> itemsBySlotAndModelFamily;
    private readonly ImmutableDictionary<uint, string> searchKeysByItemId;
    private readonly Dictionary<CatalogQuery, ImmutableArray<AppearanceEntry>> filterCache = [];
    private readonly Queue<CatalogQuery> filterCacheOrder = [];
    private readonly object filterCacheLock = new();
    private readonly bool hasBrowseExclusions;

    public EquipmentCatalog(IEnumerable<EquipmentItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        var compatibleItems = items
            .Where(item => item.CompatibleSlots.Length > 0)
            .ToImmutableArray();
        var sourceItems = compatibleItems
            .Where(item => !item.AppearanceKey.IsEmpty)
            .ToImmutableArray();
        hasBrowseExclusions = sourceItems.Any(item => item.IsExcludedFromBrowsing);

        SourceItemCount = sourceItems.Length;
        MogStationSourceItemCount = sourceItems.Count(item => item.IsMogStationExclusive);
        itemsById = compatibleItems
            .DistinctBy(item => item.ItemId)
            .ToImmutableDictionary(item => item.ItemId);
        itemsByNameAndSlot = compatibleItems
            .SelectMany(item => item.CompatibleSlots.Select(slot => new { Item = item, Slot = slot }))
            .GroupBy(entry => new ItemNameIndexKey(entry.Slot, CatalogSearch.Normalize(entry.Item.Name)))
            .ToImmutableDictionary(
                group => group.Key,
                group => group
                    .Select(entry => entry.Item)
                    .OrderByDescending(item => item.ItemId)
                    .First());
        searchKeysByItemId = sourceItems
            .DistinctBy(item => item.ItemId)
            .ToImmutableDictionary(
                item => item.ItemId,
                item => CatalogSearch.Normalize($"{item.Name} {item.CategoryName}"));
        appearancesBySlotAndKey = sourceItems
            .SelectMany(item => item.CompatibleSlots.Select(slot => new { Item = item, Slot = slot }))
            .GroupBy(entry => new AppearanceIndexKey(entry.Slot, entry.Item.AppearanceKey))
            .ToImmutableDictionary(
                group => group.Key,
                group => CreateEntry(group.Select(entry => entry.Item)));
        itemsBySlotAndModelFamily = sourceItems
            .Where(item => !item.IsExcludedFromBrowsing)
            .SelectMany(item => item.CompatibleSlots.Select(slot => new { Item = item, Slot = slot }))
            .GroupBy(entry => new ModelFamilyIndexKey(entry.Slot, entry.Item.ModelFamilyKey))
            .ToImmutableDictionary(
                group => group.Key,
                group => group
                    .Select(entry => entry.Item)
                    .DistinctBy(item => item.ItemId)
                    .OrderBy(GetRepresentativePriority)
                    .ThenByDescending(item => item.ItemId)
                    .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
                    .ToImmutableArray());

        var mutableIndex = new Dictionary<CatalogIndexKey, List<EquipmentItem>>();
        foreach (var item in sourceItems)
        {
            foreach (var slot in item.CompatibleSlots)
            {
                AddToIndex(mutableIndex, new CatalogIndexKey(slot, item.ContentGroup.Key), item);
                AddToIndex(
                    mutableIndex,
                    new CatalogIndexKey(slot, SpecialContentGroups.AllExpansions.Key),
                    item);
            }
        }

        index = mutableIndex.ToImmutableDictionary(
            pair => pair.Key,
            pair => CreateEntries(pair.Value));

        var contentGroupsByKey = sourceItems
            .Select(item => item.ContentGroup)
            .DistinctBy(contentGroup => contentGroup.Key)
            .ToDictionary(contentGroup => contentGroup.Key, StringComparer.Ordinal);
        contentGroupsByKey.TryAdd(
            SpecialContentGroups.AllExpansions.Key,
            SpecialContentGroups.AllExpansions);

        groupsBySlot = index.Keys
            .GroupBy(key => key.Slot)
            .ToImmutableDictionary(
                group => group.Key,
                group => group
                    .Select(key => contentGroupsByKey[key.ContentGroupKey])
                    .DistinctBy(contentGroup => contentGroup.Key)
                    .OrderBy(contentGroup => contentGroup.SortOrder)
                    .ThenBy(contentGroup => contentGroup.DisplayName, StringComparer.Ordinal)
                    .ToImmutableArray());

        AppearanceCount = index
            .Where(pair => pair.Key.ContentGroupKey != SpecialContentGroups.AllExpansions.Key)
            .Sum(pair => pair.Value.Length);
    }

    public static EquipmentCatalog Empty { get; } = new(Array.Empty<EquipmentItem>());

    public int SourceItemCount { get; }

    public int AppearanceCount { get; }

    public int MogStationSourceItemCount { get; }

    public bool TryGetItem(uint itemId, EquipmentSlot slot, out EquipmentItem item)
    {
        if (itemsById.TryGetValue(itemId, out item!)
            && item.CompatibleSlots.Contains(slot))
        {
            return true;
        }

        item = null!;
        return false;
    }

    public bool TryFindItem(string itemName, EquipmentSlot slot, out EquipmentItem item)
    {
        ArgumentNullException.ThrowIfNull(itemName);
        return itemsByNameAndSlot.TryGetValue(
            new ItemNameIndexKey(slot, CatalogSearch.Normalize(itemName)),
            out item!);
    }

    public bool TryGetAppearance(
        AppearanceKey appearanceKey,
        EquipmentSlot slot,
        out AppearanceEntry entry)
        => appearancesBySlotAndKey.TryGetValue(
            new AppearanceIndexKey(slot, appearanceKey),
            out entry!);

    public ImmutableArray<EquipmentItem> GetItemsSharingModel(
        ModelFamilyKey modelFamilyKey,
        EquipmentSlot slot)
        => !modelFamilyKey.IsEmpty
            && itemsBySlotAndModelFamily.TryGetValue(
                new ModelFamilyIndexKey(slot, modelFamilyKey),
                out var items)
                ? items
                : ImmutableArray<EquipmentItem>.Empty;

    public ImmutableArray<ContentGroup> GetContentGroups(EquipmentSlot slot)
        => groupsBySlot.TryGetValue(slot, out var groups)
            ? groups
            : ImmutableArray<ContentGroup>.Empty;

    public CatalogPage GetPage(
        EquipmentSlot slot,
        string contentGroupKey,
        int pageIndex,
        CatalogFilter? filter = null,
        int pageSize = DefaultPageSize)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contentGroupKey);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageSize);
        var stopwatch = Stopwatch.StartNew();

        var groups = GetContentGroups(slot);
        var contentGroup = groups.FirstOrDefault(group => group.Key == contentGroupKey)
            ?? new ContentGroup(contentGroupKey, contentGroupKey, int.MaxValue, 0, 0);

        if (!index.TryGetValue(new CatalogIndexKey(slot, contentGroupKey), out var entries))
        {
            stopwatch.Stop();
            return new CatalogPage(slot, contentGroup, 0, 0, 0, ImmutableArray<AppearanceEntry>.Empty)
            {
                QueryDuration = stopwatch.Elapsed,
            };
        }

        var query = CatalogQuery.Create(slot, contentGroupKey, filter ?? CatalogFilter.Default);
        ImmutableArray<AppearanceEntry> filteredEntries;
        bool usedCachedFilter;
        if (query.IsDefault && !hasBrowseExclusions)
        {
            filteredEntries = entries;
            usedCachedFilter = false;
        }
        else if (TryGetCachedFilter(query, out filteredEntries))
        {
            usedCachedFilter = true;
        }
        else
        {
            filteredEntries = FilterEntries(entries, query);
            CacheFilter(query, filteredEntries);
            usedCachedFilter = false;
        }

        var totalPages = (filteredEntries.Length + pageSize - 1) / pageSize;
        var clampedPage = Math.Clamp(pageIndex, 0, Math.Max(totalPages - 1, 0));
        var pageItems = filteredEntries
            .Skip(clampedPage * pageSize)
            .Take(pageSize)
            .ToImmutableArray();

        stopwatch.Stop();
        return new CatalogPage(slot, contentGroup, clampedPage, totalPages, filteredEntries.Length, pageItems)
        {
            QueryDuration = stopwatch.Elapsed,
            UsedCachedFilter = usedCachedFilter,
        };
    }

    private ImmutableArray<AppearanceEntry> FilterEntries(
        ImmutableArray<AppearanceEntry> entries,
        CatalogQuery query)
    {
        var results = ImmutableArray.CreateBuilder<AppearanceEntry>();
        foreach (var entry in entries)
        {
            EquipmentItem? firstMatchingSource = null;
            foreach (var source in entry.SourceItems)
            {
                if (Matches(source, query))
                {
                    firstMatchingSource = source;
                    break;
                }
            }

            if (firstMatchingSource is not null)
            {
                results.Add(ReferenceEquals(firstMatchingSource, entry.PrimaryItem)
                    ? entry
                    : entry with { PrimaryItem = firstMatchingSource });
            }
        }

        return results.ToImmutable();
    }

    private bool Matches(EquipmentItem item, CatalogQuery query)
    {
        if (item.IsExcludedFromBrowsing)
        {
            return false;
        }

        if (query.NormalizedSearch.Length > 0
            && !CatalogSearch.MatchesAllTerms(searchKeysByItemId[item.ItemId], query.NormalizedSearch))
        {
            return false;
        }

        if ((query.MinimumEquipLevel.HasValue && item.EquipLevel < query.MinimumEquipLevel.Value)
            || (query.MaximumEquipLevel.HasValue && item.EquipLevel > query.MaximumEquipLevel.Value)
            || (query.MinimumItemLevel.HasValue && item.ItemLevel < query.MinimumItemLevel.Value)
            || (query.MaximumItemLevel.HasValue && item.ItemLevel > query.MaximumItemLevel.Value))
        {
            return false;
        }

        var dyeMatches = query.DyeSupport switch
        {
            DyeSupportFilter.Any => true,
            DyeSupportFilter.None => item.DyeChannelCount == 0,
            DyeSupportFilter.SingleChannel => item.DyeChannelCount == 1,
            DyeSupportFilter.TwoChannels => item.DyeChannelCount > 1,
            _ => false,
        };
        var activeClassJobMatches = query.ActiveClassJobId is null
            || (query.Slot is not (EquipmentSlot.MainHand or EquipmentSlot.OffHand))
            || item.EquippableClassJobs.Contains(query.ActiveClassJobId.Value);
        return dyeMatches
            && activeClassJobMatches
            && MatchesJobRole(item.JobRoles, query.JobRole);
    }

    private static bool MatchesJobRole(EquipmentRoles roles, JobRoleFilter filter)
        => filter switch
        {
            JobRoleFilter.Any => true,
            JobRoleFilter.Tank => (roles & EquipmentRoles.Tank) != 0,
            JobRoleFilter.Healer => (roles & EquipmentRoles.Healer) != 0,
            JobRoleFilter.MeleeDps => (roles & EquipmentRoles.MeleeDps) != 0,
            JobRoleFilter.PhysicalRangedDps => (roles & EquipmentRoles.PhysicalRangedDps) != 0,
            JobRoleFilter.MagicalRangedDps => (roles & EquipmentRoles.MagicalRangedDps) != 0,
            JobRoleFilter.Limited => (roles & EquipmentRoles.Limited) != 0,
            JobRoleFilter.Crafter => (roles & EquipmentRoles.Crafter) != 0,
            JobRoleFilter.Gatherer => (roles & EquipmentRoles.Gatherer) != 0,
            _ => false,
        };

    private bool TryGetCachedFilter(
        CatalogQuery query,
        out ImmutableArray<AppearanceEntry> entries)
    {
        lock (filterCacheLock)
        {
            return filterCache.TryGetValue(query, out entries);
        }
    }

    private void CacheFilter(CatalogQuery query, ImmutableArray<AppearanceEntry> entries)
    {
        lock (filterCacheLock)
        {
            if (filterCache.ContainsKey(query))
            {
                return;
            }

            while (filterCache.Count >= FilterCacheCapacity)
            {
                filterCache.Remove(filterCacheOrder.Dequeue());
            }

            filterCache.Add(query, entries);
            filterCacheOrder.Enqueue(query);
        }
    }

    private static ImmutableArray<AppearanceEntry> CreateEntries(IEnumerable<EquipmentItem> items)
        => items
            .GroupBy(item => item.AppearanceKey)
            .Select(CreateEntry)
            .OrderByDescending(entry => entry.PrimaryItem.ItemId)
            .ThenByDescending(entry => entry.PrimaryItem.ItemLevel)
            .ThenByDescending(entry => entry.PrimaryItem.EquipLevel)
            .ThenBy(entry => entry.PrimaryItem.Name, StringComparer.OrdinalIgnoreCase)
            .ToImmutableArray();

    private static AppearanceEntry CreateEntry(IEnumerable<EquipmentItem> items)
    {
        var sources = items
            .OrderBy(GetRepresentativePriority)
            .ThenByDescending(item => item.ItemId)
            .ThenByDescending(item => item.ItemLevel)
            .ThenByDescending(item => item.EquipLevel)
            .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToImmutableArray();
        return new AppearanceEntry(sources[0].AppearanceKey, sources[0], sources);
    }

    private static int GetRepresentativePriority(EquipmentItem item)
    {
        if (item.IsExcludedFromBrowsing)
        {
            return 2;
        }

        return item.Name.Contains("Replica", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
    }

    private static void AddToIndex(
        Dictionary<CatalogIndexKey, List<EquipmentItem>> mutableIndex,
        CatalogIndexKey key,
        EquipmentItem item)
    {
        if (!mutableIndex.TryGetValue(key, out var indexedItems))
        {
            indexedItems = [];
            mutableIndex.Add(key, indexedItems);
        }

        indexedItems.Add(item);
    }

    private readonly record struct CatalogIndexKey(EquipmentSlot Slot, string ContentGroupKey);

    private readonly record struct ItemNameIndexKey(EquipmentSlot Slot, string NormalizedName);

    private readonly record struct AppearanceIndexKey(EquipmentSlot Slot, AppearanceKey AppearanceKey);

    private readonly record struct ModelFamilyIndexKey(EquipmentSlot Slot, ModelFamilyKey ModelFamilyKey);

    private readonly record struct CatalogQuery(
        EquipmentSlot Slot,
        string ContentGroupKey,
        string NormalizedSearch,
        byte? MinimumEquipLevel,
        byte? MaximumEquipLevel,
        uint? MinimumItemLevel,
        uint? MaximumItemLevel,
        DyeSupportFilter DyeSupport,
        JobRoleFilter JobRole,
        uint? ActiveClassJobId)
    {
        public bool IsDefault
            => NormalizedSearch.Length == 0
                && MinimumEquipLevel is null
                && MaximumEquipLevel is null
                && MinimumItemLevel is null
                && MaximumItemLevel is null
                && DyeSupport == DyeSupportFilter.Any
                && JobRole == JobRoleFilter.Any
                && ActiveClassJobId is null;

        public static CatalogQuery Create(
            EquipmentSlot slot,
            string contentGroupKey,
            CatalogFilter filter)
            => new(
                slot,
                contentGroupKey,
                CatalogSearch.Normalize(filter.SearchText),
                filter.MinimumEquipLevel,
                filter.MaximumEquipLevel,
                filter.MinimumItemLevel,
                filter.MaximumItemLevel,
                filter.DyeSupport,
                filter.JobRole,
                filter.ActiveClassJobId);
    }
}
