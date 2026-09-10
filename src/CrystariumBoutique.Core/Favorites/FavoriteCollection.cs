namespace CrystariumBoutique.Core.Favorites;

[Serializable]
public sealed class FavoriteCatalog
{
    public List<FavoriteList> Lists { get; set; } = [];

    public List<FavoriteItem> Items { get; set; } = [];

    public FavoriteList CreateList(string name)
    {
        var normalized = NormalizeName(name);
        if (Lists.Any(list => string.Equals(list.Name, normalized, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ArgumentException("A Favorites list with that name already exists.", nameof(name));
        }

        var list = new FavoriteList(Guid.NewGuid(), normalized);
        Lists.Add(list);
        return list;
    }

    public bool RenameList(Guid listId, string name)
    {
        var list = Lists.FirstOrDefault(candidate => candidate.Id == listId);
        if (list is null)
        {
            return false;
        }

        var normalized = NormalizeName(name);
        if (Lists.Any(candidate => candidate.Id != listId
                && string.Equals(candidate.Name, normalized, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ArgumentException("A Favorites list with that name already exists.", nameof(name));
        }

        list.Name = normalized;
        return true;
    }

    public bool DeleteList(Guid listId)
    {
        var removed = Lists.RemoveAll(list => list.Id == listId) > 0;
        if (!removed)
        {
            return false;
        }

        foreach (var item in Items)
        {
            item.ListIds.RemoveAll(id => id == listId);
        }

        return true;
    }

    public FavoriteItem Add(uint itemId, DateTimeOffset? savedAt = null)
    {
        ArgumentOutOfRangeException.ThrowIfZero(itemId);
        var existing = Items.FirstOrDefault(item => item.ItemId == itemId);
        if (existing is not null)
        {
            return existing;
        }

        var favorite = new FavoriteItem(itemId, savedAt ?? DateTimeOffset.UtcNow);
        Items.Add(favorite);
        return favorite;
    }

    public bool Remove(uint itemId)
        => Items.RemoveAll(item => item.ItemId == itemId) > 0;

    public bool Contains(uint itemId)
        => Items.Any(item => item.ItemId == itemId);

    public bool SetMembership(uint itemId, Guid listId, bool member, DateTimeOffset? savedAt = null)
    {
        if (!Lists.Any(list => list.Id == listId))
        {
            return false;
        }

        var favorite = Add(itemId, savedAt);
        var contains = favorite.ListIds.Contains(listId);
        if (member == contains)
        {
            return false;
        }

        if (member)
        {
            favorite.ListIds.Add(listId);
        }
        else
        {
            favorite.ListIds.Remove(listId);
        }

        return true;
    }

    public IReadOnlyList<FavoriteItem> GetItems(Guid? listId = null)
        => Items
            .Where(item => listId is null || item.ListIds.Contains(listId.Value))
            .OrderByDescending(item => item.SavedAtUtc)
            .ThenByDescending(item => item.ItemId)
            .ToArray();

    public void Normalize()
    {
        Lists ??= [];
        Items ??= [];
        Lists = Lists
            .Where(list => list.Id != Guid.Empty && !string.IsNullOrWhiteSpace(list.Name))
            .GroupBy(list => list.Id)
            .Select(group => group.First())
            .ToList();
        var validListIds = Lists.Select(list => list.Id).ToHashSet();
        Items = Items
            .Where(item => item.ItemId > 0)
            .GroupBy(item => item.ItemId)
            .Select(group => group.OrderByDescending(item => item.SavedAtUtc).First())
            .ToList();
        foreach (var item in Items)
        {
            item.ListIds ??= [];
            item.ListIds = item.ListIds.Where(validListIds.Contains).Distinct().ToList();
        }
    }

    private static string NormalizeName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var normalized = name.Trim();
        if (normalized.Length > 64)
        {
            throw new ArgumentException("Favorites list names may contain at most 64 characters.", nameof(name));
        }

        return normalized;
    }
}

[Serializable]
public sealed class FavoriteList(Guid id, string name)
{
    public Guid Id { get; set; } = id;

    public string Name { get; set; } = name;
}

[Serializable]
public sealed class FavoriteItem(uint itemId, DateTimeOffset savedAtUtc)
{
    public uint ItemId { get; set; } = itemId;

    public DateTimeOffset SavedAtUtc { get; set; } = savedAtUtc;

    public List<Guid> ListIds { get; set; } = [];
}
