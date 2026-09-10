using CrystariumBoutique.Core.Appearance;
using CrystariumBoutique.Core.Integrations;

namespace CrystariumBoutique.Integrations.GlamourerIpc;

// Wire-contract facts verified against Glamourer.Api revision
// ea569211a7c3500f2ce0b1b9223df85d8cb85f1a. These are Boutique-owned
// interoperability types, not copies of Glamourer's subscriber implementations.
internal enum GlamourerIpcErrorCode
{
    Success = 0,
    NothingDone = 1,
    ActorNotFound = 2,
    ActorNotHuman = 3,
    DesignNotFound = 4,
    ItemInvalid = 5,
    InvalidKey = 6,
    InvalidState = 7,
    CouldNotParse = 8,
    UnknownError = int.MaxValue,
}

internal enum GlamourerIpcStateFinalizationType
{
    ModelChange = 0,
    DesignApplied = 1,
    Revert = 2,
    RevertCustomize = 3,
    RevertEquipment = 4,
    RevertAdvanced = 5,
    RevertAutomation = 6,
    Reapply = 7,
    ReapplyAutomation = 8,
    Gearset = 9,
}

internal enum GlamourerIpcEquipSlot : byte
{
    Unknown = 0,
    MainHand = 1,
    OffHand = 2,
    Head = 3,
    Body = 4,
    Hands = 5,
    Legs = 7,
    Feet = 8,
    Ears = 9,
    Neck = 10,
    Wrists = 11,
    RightFinger = 12,
    LeftFinger = 14,
}

[Flags]
internal enum GlamourerIpcApplyFlags : ulong
{
    Once = 1,
    Equipment = 2,
    Customization = 4,
    Lock = 8,
}

[Flags]
internal enum GlamourerIpcMetaFlags : ulong
{
    VisorState = 4,
}

internal readonly record struct GlamourerIpcAvailability(
    DependencyStatus Status,
    int? Major,
    int? Minor,
    string? Detail = null)
{
    public string? Version => Major.HasValue && Minor.HasValue
        ? $"{Major.Value}.{Minor.Value}"
        : null;
}

internal readonly record struct GlamourerIpcStateResult(
    GlamourerIpcErrorCode ErrorCode,
    string? SerializedState);

internal static class GlamourerIpcSlotMapper
{
    public static GlamourerIpcEquipSlot FromEquipmentSlot(EquipmentSlot slot)
        => slot switch
        {
            EquipmentSlot.MainHand => GlamourerIpcEquipSlot.MainHand,
            EquipmentSlot.OffHand => GlamourerIpcEquipSlot.OffHand,
            EquipmentSlot.Head => GlamourerIpcEquipSlot.Head,
            EquipmentSlot.Body => GlamourerIpcEquipSlot.Body,
            EquipmentSlot.Hands => GlamourerIpcEquipSlot.Hands,
            EquipmentSlot.Legs => GlamourerIpcEquipSlot.Legs,
            EquipmentSlot.Feet => GlamourerIpcEquipSlot.Feet,
            EquipmentSlot.Ears => GlamourerIpcEquipSlot.Ears,
            EquipmentSlot.Neck => GlamourerIpcEquipSlot.Neck,
            EquipmentSlot.Wrists => GlamourerIpcEquipSlot.Wrists,
            EquipmentSlot.RightRing => GlamourerIpcEquipSlot.RightFinger,
            EquipmentSlot.LeftRing => GlamourerIpcEquipSlot.LeftFinger,
            _ => GlamourerIpcEquipSlot.Unknown,
        };
}

internal static class GlamourerIpcCustomItemId
{
    // Verified against Penumbra.GameData 1.7.1.1's CustomItemId layout and
    // FullEquipType.Shield wire value. This reproduces only the value passed
    // across Glamourer's IPC boundary and does not require Penumbra CLR types.
    private const ulong CustomFlag = 1UL << 48;
    private const ulong ShieldEquipType = 37;

    public static ulong FromShieldModel(ulong model)
    {
        var primaryId = model & 0xFFFF;
        var secondaryId = (model >> 16) & 0xFFFF;
        var variant = (model >> 32) & 0xFF;
        return CustomFlag
            | (ShieldEquipType << 40)
            | (variant << 32)
            | (secondaryId << 16)
            | primaryId;
    }
}
