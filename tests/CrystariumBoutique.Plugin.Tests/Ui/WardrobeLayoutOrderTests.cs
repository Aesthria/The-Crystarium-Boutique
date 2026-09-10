using CrystariumBoutique.Core.Appearance;
using CrystariumBoutique.Ui.Components;

namespace CrystariumBoutiquePlugin.Tests.Ui;

public sealed class WardrobeLayoutOrderTests
{
    [Fact]
    public void VerticalLayoutUsesTheRequestedTwoColumnOrder()
        => Assert.Equal(
            [
                EquipmentSlot.MainHand,
                EquipmentSlot.OffHand,
                EquipmentSlot.Head,
                EquipmentSlot.Ears,
                EquipmentSlot.Body,
                EquipmentSlot.Neck,
                EquipmentSlot.Hands,
                EquipmentSlot.Wrists,
                EquipmentSlot.Legs,
                EquipmentSlot.RightRing,
                EquipmentSlot.Feet,
                EquipmentSlot.LeftRing,
            ],
            OutfitContextPanel.GetCardLayout(WardrobeLayout.Vertical)
                .Select(definition => definition.Slot));

    [Fact]
    public void HorizontalLayoutRemainsTheExistingSixColumnOrder()
        => Assert.Equal(
            [
                EquipmentSlot.MainHand,
                EquipmentSlot.Head,
                EquipmentSlot.Body,
                EquipmentSlot.Hands,
                EquipmentSlot.Legs,
                EquipmentSlot.Feet,
                EquipmentSlot.OffHand,
                EquipmentSlot.Ears,
                EquipmentSlot.Neck,
                EquipmentSlot.Wrists,
                EquipmentSlot.RightRing,
                EquipmentSlot.LeftRing,
            ],
            OutfitContextPanel.GetCardLayout(WardrobeLayout.Horizontal)
                .Select(definition => definition.Slot));
}
