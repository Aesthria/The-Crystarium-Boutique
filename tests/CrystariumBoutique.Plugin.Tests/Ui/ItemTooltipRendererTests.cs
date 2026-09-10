using System.Collections.Immutable;
using CrystariumBoutique.Core.Appearance;
using CrystariumBoutique.Core.Catalog;
using CrystariumBoutique.Ui;

namespace CrystariumBoutiquePlugin.Tests.Ui;

public sealed class ItemTooltipRendererTests
{
    [Fact]
    public void SharedModelSelectionExcludesHoveredItemDuplicatesAndHonorsLimit()
    {
        var hovered = CreateItem(1, "Hovered");
        var second = CreateItem(2, "Second");
        var third = CreateItem(3, "Third");

        var related = ItemTooltipRenderer.GetSharedItems(
            hovered,
            [hovered, second, second, third],
            limit: 1);

        Assert.Equal(2u, Assert.Single(related).ItemId);
    }

    private static EquipmentItem CreateItem(uint itemId, string name)
        => new(
            itemId,
            name,
            "Body",
            50,
            itemId,
            itemId,
            1,
            new AppearanceKey(42, 0, itemId, 1),
            ContentGroupResolver.Resolve(50),
            ImmutableArray.Create(EquipmentSlot.Body));
}
