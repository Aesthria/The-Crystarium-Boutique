using System.Collections.Immutable;
using CrystariumBoutique.Core.Appearance;

namespace CrystariumBoutique.Core.Catalog;

public readonly record struct EquipSlotFlags(
    bool MainHand,
    bool OffHand,
    bool Head,
    bool Body,
    bool Hands,
    bool Legs,
    bool Feet,
    bool Ears,
    bool Neck,
    bool Wrists,
    bool RightRing,
    bool LeftRing);

public static class EquipmentSlotMapper
{
    public static ImmutableArray<EquipmentSlot> Map(EquipSlotFlags flags)
    {
        var slots = ImmutableArray.CreateBuilder<EquipmentSlot>();
        AddIf(flags.MainHand, EquipmentSlot.MainHand, slots);
        AddIf(flags.OffHand, EquipmentSlot.OffHand, slots);
        AddIf(flags.Head, EquipmentSlot.Head, slots);
        AddIf(flags.Body, EquipmentSlot.Body, slots);
        AddIf(flags.Hands, EquipmentSlot.Hands, slots);
        AddIf(flags.Legs, EquipmentSlot.Legs, slots);
        AddIf(flags.Feet, EquipmentSlot.Feet, slots);
        AddIf(flags.Ears, EquipmentSlot.Ears, slots);
        AddIf(flags.Neck, EquipmentSlot.Neck, slots);
        AddIf(flags.Wrists, EquipmentSlot.Wrists, slots);
        AddIf(flags.RightRing, EquipmentSlot.RightRing, slots);
        AddIf(flags.LeftRing, EquipmentSlot.LeftRing, slots);
        return slots.ToImmutable();
    }

    private static void AddIf(
        bool condition,
        EquipmentSlot slot,
        ImmutableArray<EquipmentSlot>.Builder slots)
    {
        if (condition)
        {
            slots.Add(slot);
        }
    }
}
