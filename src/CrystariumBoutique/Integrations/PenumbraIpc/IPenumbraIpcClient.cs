namespace CrystariumBoutique.Integrations.PenumbraIpc;

internal interface IPenumbraIpcClient : IDisposable
{
    event Action? ProviderInitialized;

    event Action? ProviderDisposed;

    PenumbraIpcAvailability DetectAvailability();

    void RedrawObject(int objectIndex, PenumbraIpcRedrawType redrawType);
}
