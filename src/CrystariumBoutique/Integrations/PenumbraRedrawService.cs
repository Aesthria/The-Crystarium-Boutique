using CrystariumBoutique.Core.Errors;
using CrystariumBoutique.Core.Integrations;
using CrystariumBoutique.Integrations.PenumbraIpc;
using Dalamud.Plugin.Services;

namespace CrystariumBoutique.Integrations;

internal sealed class PenumbraRedrawService :
    IPenumbraService,
    IDependencyAvailabilityRefresher,
    IDisposable
{
    private readonly IPenumbraIpcClient ipcClient;
    private readonly IPluginLog pluginLog;
    private bool disposed;

    public PenumbraRedrawService(IPenumbraIpcClient ipcClient, IPluginLog pluginLog)
    {
        this.ipcClient = ipcClient ?? throw new ArgumentNullException(nameof(ipcClient));
        this.pluginLog = pluginLog ?? throw new ArgumentNullException(nameof(pluginLog));
        ipcClient.ProviderInitialized += HandleDependencyInitialized;
        ipcClient.ProviderDisposed += HandleDependencyDisposed;
        RefreshAvailability();
    }

    public bool IsAvailable => Status == DependencyStatus.Available;

    public DependencyStatus Status { get; private set; } = DependencyStatus.Unknown;

    public string? DetectedVersion { get; private set; }

    public bool RefreshAvailability()
    {
        if (disposed)
        {
            return false;
        }

        var previousStatus = Status;
        var previousVersion = DetectedVersion;
        var availability = ipcClient.DetectAvailability();
        Status = availability.Status;
        DetectedVersion = availability.Version;

        var changed = previousStatus != Status
            || !string.Equals(previousVersion, DetectedVersion, StringComparison.Ordinal);
        if (!changed)
        {
            return false;
        }

        if (Status == DependencyStatus.Available)
        {
            pluginLog.Information(
                previousStatus == DependencyStatus.Unknown
                    ? "Penumbra API {DetectedVersion} detected; dependency-loss redraw recovery is available."
                    : "Penumbra API {DetectedVersion} detected after startup; dependency-loss redraw recovery is now available.",
                DetectedVersion ?? "unknown");
        }
        else
        {
            pluginLog.Warning(
                previousStatus == DependencyStatus.Unknown
                    ? "Penumbra was not available at Boutique startup; waiting for its IPC provider. {Detail}"
                    : "Penumbra redraw recovery became unavailable; waiting for its IPC provider. {Detail}",
                availability.Detail ?? Status.ToString());
        }

        return true;
    }

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
        ipcClient.ProviderInitialized -= HandleDependencyInitialized;
        ipcClient.Dispose();
    }

    private void HandleDependencyInitialized()
        => RefreshAvailability();

    private void HandleDependencyDisposed()
    {
        if (disposed)
        {
            return;
        }

        if (Status == DependencyStatus.Unavailable && DetectedVersion is null)
        {
            return;
        }

        Status = DependencyStatus.Unavailable;
        DetectedVersion = null;
        pluginLog.Warning("Penumbra became unavailable; dependency-loss redraw recovery is waiting for its IPC provider.");
    }
}
