using CrystariumBoutique.Core.Appearance;
using CrystariumBoutique.Core.Errors;

namespace CrystariumBoutique.Core.Integrations;

public interface IAppearanceService
{
    bool IsAvailable { get; }

    DependencyStatus Status { get; }

    string? DetectedVersion { get; }

    Result<AppearanceSnapshot> CapturePlayerAppearance();

    Result ApplyEquipment(EquipmentSlot slot, AppearanceSelection appearance);

    Result ApplyDye(EquipmentSlot slot, AppearanceSelection appearance);

    Result ApplyAppearance(AppearanceSnapshot appearance);

    Result RevertToGame();

    Result RestoreAppearance(AppearanceSnapshot snapshot);
}

public interface IVisorAppearanceService
{
    Result SetVisorState(bool enabled);
}
