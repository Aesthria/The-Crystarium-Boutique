using CrystariumBoutique.Core.Appearance;

namespace CrystariumBoutique.Core.Sessions;

public sealed record PreviewEquipmentState(
    EquipmentSlot Slot,
    AppearanceSelection Appearance,
    byte DyeChannelCount,
    string DisplayName,
    uint IconId)
{
    public StainId GetStain(byte dyeChannel)
        => dyeChannel < Appearance.Stains.Length
            ? Appearance.Stains[dyeChannel]
            : StainId.None;
}
