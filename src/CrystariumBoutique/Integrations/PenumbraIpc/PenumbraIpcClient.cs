using CrystariumBoutique.Core.Integrations;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;

namespace CrystariumBoutique.Integrations.PenumbraIpc;

internal sealed class PenumbraIpcClient : IPenumbraIpcClient
{
    internal const int SupportedBreakingVersion = 5;

    private readonly IPenumbraIpcTransport transport;
    private bool disposed;

    public PenumbraIpcClient(IDalamudPluginInterface pluginInterface)
        : this(new DalamudTransport(pluginInterface))
    {
    }

    internal PenumbraIpcClient(IPenumbraIpcTransport transport)
    {
        this.transport = transport ?? throw new ArgumentNullException(nameof(transport));
        transport.Initialized += HandleInitialized;
        transport.Disposed += HandleDisposed;
    }

    public event Action? ProviderInitialized;

    public event Action? ProviderDisposed;

    public PenumbraIpcAvailability DetectAvailability()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (!transport.HasVersionFunction)
        {
            return new PenumbraIpcAvailability(
                DependencyStatus.Unavailable,
                null,
                null,
                "Penumbra.ApiVersion.V5 is not currently provided.");
        }

        try
        {
            var (breakingVersion, featureVersion) = transport.InvokeVersion();
            if (breakingVersion != SupportedBreakingVersion)
            {
                return new PenumbraIpcAvailability(
                    DependencyStatus.Incompatible,
                    breakingVersion,
                    featureVersion,
                    $"Expected Penumbra breaking API {SupportedBreakingVersion}.");
            }

            if (!transport.HasRedrawFunction)
            {
                return new PenumbraIpcAvailability(
                    DependencyStatus.Incompatible,
                    breakingVersion,
                    featureVersion,
                    "Penumbra.RedrawObject.V5 is unavailable.");
            }

            return new PenumbraIpcAvailability(
                DependencyStatus.Available,
                breakingVersion,
                featureVersion);
        }
        catch (Exception exception)
        {
            return new PenumbraIpcAvailability(
                DependencyStatus.Unavailable,
                null,
                null,
                $"{exception.GetType().Name}: {exception.Message}");
        }
    }

    public void RedrawObject(int objectIndex, PenumbraIpcRedrawType redrawType)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ArgumentOutOfRangeException.ThrowIfNegative(objectIndex);
        transport.RedrawObject(objectIndex, (int)redrawType);
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        transport.Initialized -= HandleInitialized;
        transport.Disposed -= HandleDisposed;
        transport.Dispose();
    }

    private void HandleInitialized()
        => ProviderInitialized?.Invoke();

    private void HandleDisposed()
        => ProviderDisposed?.Invoke();

    internal interface IPenumbraIpcTransport : IDisposable
    {
        event Action? Initialized;

        event Action? Disposed;

        bool HasVersionFunction { get; }

        bool HasRedrawFunction { get; }

        (int BreakingVersion, int FeatureVersion) InvokeVersion();

        void RedrawObject(int objectIndex, int redrawType);
    }

    private sealed class DalamudTransport : IPenumbraIpcTransport
    {
        private const string ApiVersionLabel = "Penumbra.ApiVersion.V5";
        private const string RedrawObjectLabel = "Penumbra.RedrawObject.V5";
        private const string InitializedLabel = "Penumbra.Initialized";
        private const string DisposedLabel = "Penumbra.Disposed";

        private readonly ICallGateSubscriber<(int BreakingVersion, int FeatureVersion)> apiVersion;
        private readonly ICallGateSubscriber<int, int, object?> redrawObject;
        private readonly ICallGateSubscriber<object?> initialized;
        private readonly ICallGateSubscriber<object?> disposed;
        private bool isDisposed;

        public DalamudTransport(IDalamudPluginInterface pluginInterface)
        {
            ArgumentNullException.ThrowIfNull(pluginInterface);
            apiVersion = pluginInterface.GetIpcSubscriber<(int BreakingVersion, int FeatureVersion)>(ApiVersionLabel);
            redrawObject = pluginInterface.GetIpcSubscriber<int, int, object?>(RedrawObjectLabel);
            initialized = pluginInterface.GetIpcSubscriber<object?>(InitializedLabel);
            disposed = pluginInterface.GetIpcSubscriber<object?>(DisposedLabel);
            initialized.Subscribe(HandleInitialized);
            disposed.Subscribe(HandleDisposed);
        }

        public event Action? Initialized;

        public event Action? Disposed;

        public bool HasVersionFunction => apiVersion.HasFunction;

        public bool HasRedrawFunction => redrawObject.HasAction;

        public (int BreakingVersion, int FeatureVersion) InvokeVersion()
            => apiVersion.InvokeFunc();

        public void RedrawObject(int objectIndex, int redrawType)
            => redrawObject.InvokeAction(objectIndex, redrawType);

        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            isDisposed = true;
            disposed.Unsubscribe(HandleDisposed);
            initialized.Unsubscribe(HandleInitialized);
        }

        private void HandleInitialized()
            => Initialized?.Invoke();

        private void HandleDisposed()
            => Disposed?.Invoke();
    }
}
