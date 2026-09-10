using System.Collections.Immutable;
using CrystariumBoutique.Core.Appearance;

namespace CrystariumBoutique.Core.Catalog;

public sealed record EquipmentSlotDefinition(
    EquipmentSlot Slot,
    string DisplayName,
    int SortOrder);

public static class EquipmentSlotDefinitions
{
    public static ImmutableArray<EquipmentSlotDefinition> All { get; } =
    [
        new(EquipmentSlot.MainHand, "Main Hand", 0),
        new(EquipmentSlot.OffHand, "Off Hand", 1),
        new(EquipmentSlot.Head, "Head", 2),
        new(EquipmentSlot.Body, "Body", 3),
        new(EquipmentSlot.Hands, "Hands", 4),
        new(EquipmentSlot.Legs, "Legs", 5),
        new(EquipmentSlot.Feet, "Feet", 6),
        new(EquipmentSlot.Ears, "Earrings", 7),
        new(EquipmentSlot.Neck, "Necklace", 8),
        new(EquipmentSlot.Wrists, "Bracelets", 9),
        new(EquipmentSlot.RightRing, "Right Ring", 10),
        new(EquipmentSlot.LeftRing, "Left Ring", 11),
    ];

    public static string GetDisplayName(EquipmentSlot slot)
        => All.FirstOrDefault(definition => definition.Slot == slot)?.DisplayName ?? slot.ToString();
}
