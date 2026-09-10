using CrystariumBoutique.Core.Errors;

namespace CrystariumBoutique.Core.Integrations;

public interface IPenumbraService
{
    bool IsAvailable { get; }

    DependencyStatus Status { get; }

    string? DetectedVersion { get; }

    Result RedrawObject(int objectIndex, bool afterGpose = false);
}
