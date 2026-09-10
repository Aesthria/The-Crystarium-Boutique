using CrystariumBoutique.Core.Appearance;
using CrystariumBoutique.Core.Errors;
using CrystariumBoutique.Core.Integrations;

namespace CrystariumBoutique.Integrations;

internal sealed class UnavailableAppearanceService : IContextAwareAppearanceService
{
    private const string Message = "Glamourer IPC is not currently available; Boutique remains browse-only.";

    public bool IsAvailable => false;

    public DependencyStatus Status => DependencyStatus.Unavailable;

    public string? DetectedVersion => null;

    public Result<AppearanceSnapshot> CapturePlayerAppearance()
        => Result.Failure<AppearanceSnapshot>(Unavailable());

    public Result ApplyEquipment(EquipmentSlot slot, AppearanceSelection appearance)
        => Result.Failure(Unavailable());

    public Result ApplyDye(EquipmentSlot slot, AppearanceSelection appearance)
        => Result.Failure(Unavailable());

    public Result ApplyAppearance(AppearanceSnapshot appearance)
        => Result.Failure(Unavailable());

    public Result RevertToGame()
        => Result.Failure(Unavailable());

    public Result RestoreAppearance(AppearanceSnapshot snapshot)
        => Result.Failure(Unavailable());

    public void PrepareForContextTransition()
    {
    }

    public Result RebindToCurrentContext()
        => Result.Failure(Unavailable());

    public void Dispose()
    {
    }

    private static BoutiqueError Unavailable()
        => new(BoutiqueErrorCode.DependencyUnavailable, Message);
}
