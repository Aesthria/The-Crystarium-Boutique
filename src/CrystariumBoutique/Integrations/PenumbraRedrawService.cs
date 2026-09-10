using CrystariumBoutique.Core.Errors;
using CrystariumBoutique.Core.Integrations;
using CrystariumBoutique.Integrations.PenumbraIpc;
using Dalamud.Plugin.Services;

namespace CrystariumBoutique.Integrations;

internal sealed class PenumbraRedrawService : IPenumbraService, IDisposable
{
    private readonly IPenumbraIpcClient ipcClient;
    private readonly IPluginLog pluginLog;
    private bool disposed;

    public PenumbraRedrawService(IPenumbraIpcClient ipcClient, IPluginLog pluginLog)
    {
        this.ipcClient = ipcClient ?? throw new ArgumentNullException(nameof(ipcClient));
        this.pluginLog = pluginLog ?? throw new ArgumentNullException(nameof(pluginLog));
        ipcClient.ProviderInitialized += DetectDependency;
        ipcClient.ProviderDisposed += HandleDependencyDisposed;
        DetectDependency();
    }

    public bool IsAvailable => Status == DependencyStatus.Available;

    public DependencyStatus Status { get; private set; } = DependencyStatus.Unknown;

    public string? DetectedVersion { get; private set; }

    public Result RedrawObject(int objectIndex, bool afterGpose = false)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(objectIndex);
        if (!IsAvailable)
        {
            return Result.Failure(new BoutiqueError(
                Status == DependencyStatus.Incompatible
                    ? BoutiqueErrorCode.UnsupportedApiVersion
                    : BoutiqueErrorCode.DependencyUnavailable,
                "Penumbra is not currently available to redraw the Boutique actor."));
        }

        try
        {
            ipcClient.RedrawObject(
                objectIndex,
                afterGpose ? PenumbraIpcRedrawType.AfterGpose : PenumbraIpcRedrawType.Redraw);
            return Result.Ok;
        }
        catch (Exception exception)
        {
            pluginLog.Error(
                exception,
                "Penumbra redraw IPC failed for Boutique actor index {ObjectIndex}.",
                objectIndex);
            return Result.Failure(new BoutiqueError(
                BoutiqueErrorCode.RestoreFailed,
                "Penumbra could not redraw the Boutique actor after Glamourer was unloaded.",
                $"{exception.GetType().FullName}: {exception.Message}",
                IsRecoverable: true));
        }
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        ipcClient.ProviderDisposed -= HandleDependencyDisposed;
        ipcClient.ProviderInitialized -= DetectDependency;
        ipcClient.Dispose();
    }

    private void DetectDependency()
    {
        if (disposed)
        {
            return;
        }

        var availability = ipcClient.DetectAvailability();
        Status = availability.Status;
        DetectedVersion = availability.Version;
        if (Status == DependencyStatus.Available)
        {
            pluginLog.Information(
                "Penumbra API {DetectedVersion} detected; dependency-loss redraw recovery is available.",
                DetectedVersion ?? "unknown");
        }
        else
        {
            pluginLog.Warning(
                "Penumbra redraw recovery is unavailable: {Detail}",
                availability.Detail ?? Status.ToString());
        }
    }

    private void HandleDependencyDisposed()
    {
        if (disposed)
        {
            return;
        }

        Status = DependencyStatus.Unavailable;
        DetectedVersion = null;
    }
}
