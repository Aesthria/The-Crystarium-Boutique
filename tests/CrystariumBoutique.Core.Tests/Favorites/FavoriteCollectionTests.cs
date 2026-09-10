using CrystariumBoutique.Core.Favorites;
using System.Collections.Immutable;
using CrystariumBoutique.Core.Appearance;
using CrystariumBoutique.Core.Catalog;

namespace CrystariumBoutique.Core.Tests.Favorites;

public sealed class FavoriteCatalogTests
{
    [Fact]
    public void SupportsMultipleMembershipsAndNewestFirstOrdering()
    {
        var favorites = new FavoriteCatalog();
        var armor = favorites.CreateList("Armor");
        var blue = favorites.CreateList("Blue");
        favorites.SetMembership(100, armor.Id, true, DateTimeOffset.UnixEpoch);
        favorites.SetMembership(100, blue.Id, true);
        favorites.Add(200, DateTimeOffset.UnixEpoch.AddDays(1));

        Assert.Equal([200u, 100u], favorites.GetItems().Select(item => item.ItemId));
        Assert.Equal([100u], favorites.GetItems(armor.Id).Select(item => item.ItemId));
        Assert.Equal(2, favorites.Items.Single(item => item.ItemId == 100).ListIds.Count);
    }

    [Fact]
    public void DeletingListPreservesFavoriteAndOtherMemberships()
    {
        var favorites = new FavoriteCatalog();
        var first = favorites.CreateList("First");
        var second = favorites.CreateList("Second");
        favorites.SetMembership(42, first.Id, true);
        favorites.SetMembership(42, second.Id, true);

        Assert.True(favorites.DeleteList(first.Id));

        var item = Assert.Single(favorites.Items);
        Assert.Equal(42u, item.ItemId);
        Assert.Equal([second.Id], item.ListIds);
    }

    [Fact]
    public void RemovingOneMembershipPreservesFavoriteAndOtherLists()
    {
        var favorites = new FavoriteCatalog();
        var first = favorites.CreateList("First");
        var second = favorites.CreateList("Second");
        favorites.SetMembership(42, first.Id, true);
        favorites.SetMembership(42, second.Id, true);

        Assert.True(favorites.SetMembership(42, first.Id, false));

        Assert.True(favorites.Contains(42));
        Assert.Equal([second.Id], Assert.Single(favorites.Items).ListIds);
    }

    [Fact]
    public void RenameAndRemoveAreIndependentOfSavedDesigns()
    {
        var favorites = new FavoriteCatalog();
        var list = favorites.CreateList("Old");
        favorites.SetMembership(7, list.Id, true);

        Assert.True(favorites.RenameList(list.Id, "New"));
        Assert.Equal("New", list.Name);
        Assert.True(favorites.Remove(7));
        Assert.False(favorites.Contains(7));
        Assert.Empty(favorites.Items);
    }

    [Fact]
    public void ViewFilterCombinesSlotAndSearchWithoutChangingMembership()
    {
        var body = CreateItem(1, "Weathered Formal Coat", EquipmentSlot.Body);
        var head = CreateItem(2, "Formal Hat", EquipmentSlot.Head);

        Assert.True(FavoriteViewFilter.Matches(body, null, string.Empty));
        Assert.True(FavoriteViewFilter.Matches(body, EquipmentSlot.Body, "formal coat"));
        Assert.False(FavoriteViewFilter.Matches(body, EquipmentSlot.Head, "formal"));
        Assert.False(FavoriteViewFilter.Matches(head, EquipmentSlot.Head, "coat"));
    }

    private static EquipmentItem CreateItem(uint itemId, string name, EquipmentSlot slot)
        => new(
            itemId,
            name,
            "Test Equipment",
            1,
            1,
            itemId,
            0,
            new AppearanceKey(itemId, 0),
            ContentGroupResolver.Resolve(1),
            ImmutableArray.Create(slot));
}
