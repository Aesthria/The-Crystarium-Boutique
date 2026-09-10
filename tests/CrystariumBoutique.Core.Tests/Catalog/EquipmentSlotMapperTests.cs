using CrystariumBoutique.Core.Appearance;
using CrystariumBoutique.Core.Catalog;

namespace CrystariumBoutique.Core.Tests.Catalog;

public sealed class EquipmentSlotMapperTests
{
    [Fact]
    public void MapReturnsEveryEnabledVisibleSlotInDisplayOrder()
    {
        var flags = new EquipSlotFlags(
            MainHand: true,
            OffHand: false,
            Head: true,
            Body: false,
            Hands: false,
            Legs: false,
            Feet: false,
            Ears: false,
            Neck: false,
            Wrists: false,
            RightRing: true,
            LeftRing: true);

        var slots = EquipmentSlotMapper.Map(flags);

        Assert.Equal(
            [EquipmentSlot.MainHand, EquipmentSlot.Head, EquipmentSlot.RightRing, EquipmentSlot.LeftRing],
            slots);
    }

    [Fact]
    public void MapReturnsEmptyForNonVisibleSlotFlags()
    {
        var slots = EquipmentSlotMapper.Map(default);

        Assert.Empty(slots);
    }

    [Fact]
    public void EquipmentItemIdentifiesOnlyDualSlotRowsWithSubModelAsLinked()
    {
        var linked = CreateItem([EquipmentSlot.MainHand, EquipmentSlot.OffHand], modelSub: 2);
        var standalone = CreateItem([EquipmentSlot.MainHand], modelSub: 2);
        var dualSlotWithoutSubModel = CreateItem(
            [EquipmentSlot.MainHand, EquipmentSlot.OffHand],
            modelSub: 0);

        Assert.True(linked.HasLinkedOffHandComponent);
        Assert.False(standalone.HasLinkedOffHandComponent);
        Assert.False(dualSlotWithoutSubModel.HasLinkedOffHandComponent);
    }

    private static EquipmentItem CreateItem(EquipmentSlot[] slots, ulong modelSub)
        => new(
            1,
            "Test Weapon",
            "Weapon",
            1,
            1,
            1,
            0,
            new AppearanceKey(1, modelSub),
            ContentGroupResolver.Resolve(1),
            [.. slots]);
}
