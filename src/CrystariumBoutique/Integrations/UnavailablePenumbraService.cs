using CrystariumBoutique.Core.Integrations;
using CrystariumBoutique.Core.Errors;

namespace CrystariumBoutique.Integrations;

public sealed class UnavailablePenumbraService : IPenumbraService
{
    public bool IsAvailable => false;

    public DependencyStatus Status => DependencyStatus.Unavailable;

    public string? DetectedVersion => null;

    public Result RedrawObject(int objectIndex, bool afterGpose = false)
        => Result.Failure(new BoutiqueError(
            BoutiqueErrorCode.DependencyUnavailable,
            "Penumbra is not currently available to redraw the Boutique actor."));
}
