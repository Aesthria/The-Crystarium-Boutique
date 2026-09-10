using CrystariumBoutique.Core.Integrations;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CrystariumBoutique.Integrations.GlamourerIpc;

internal sealed class GlamourerIpcClient : IGlamourerIpcClient
{
    internal const int SupportedApiMajor = 1;
    internal const int MinimumApiMinor = 8;

    // Endpoint labels and subscriber signatures were verified against
    // https://github.com/Ottermandias/Glamourer.Api/tree/ea569211a7c3500f2ce0b1b9223df85d8cb85f1a
    private readonly IGlamourerIpcTransport transport;
    private bool disposed;

    public GlamourerIpcClient(IDalamudPluginInterface pluginInterface)
        : this(new DalamudTransport(pluginInterface))
    {
    }

    internal GlamourerIpcClient(IGlamourerIpcTransport transport)
    {
        this.transport = transport ?? throw new ArgumentNullException(nameof(transport));
        transport.Initialized += HandleInitialized;
        transport.Disposed += HandleDisposed;
        transport.StateFinalized += HandleStateFinalized;
    }

    public event Action? ProviderInitialized;

    public event Action? ProviderDisposed;

    public event Action<nint, GlamourerIpcStateFinalizationType>? StateFinalized;

    public bool SupportsStateFinalized => transport.HasStateFinalizedEvent;

    public GlamourerIpcAvailability DetectAvailability()
    {
        ThrowIfDisposed();
        if (!transport.HasVersionFunction)
        {
            return new GlamourerIpcAvailability(
                DependencyStatus.Unavailable,
                null,
                null,
                "Glamourer.ApiVersion.V2 is not currently provided.");
        }

        try
        {
            var (major, minor) = transport.InvokeVersion();
            if (major != SupportedApiMajor || minor < MinimumApiMinor)
            {
                return new GlamourerIpcAvailability(
                    DependencyStatus.Incompatible,
                    major,
                    minor,
                    $"Expected {SupportedApiMajor}.{MinimumApiMinor} or newer within major {SupportedApiMajor}.");
            }

            if (!transport.HasAllOperationalFunctions)
            {
                return new GlamourerIpcAvailability(
                    DependencyStatus.Incompatible,
                    major,
                    minor,
                    "One or more required Glamourer appearance endpoints are unavailable.");
            }

            return new GlamourerIpcAvailability(DependencyStatus.Available, major, minor);
        }
        catch (Exception exception)
        {
            return new GlamourerIpcAvailability(
                DependencyStatus.Unavailable,
                null,
                null,
                $"{exception.GetType().Name}: {exception.Message}");
        }
    }

    public GlamourerIpcStateResult GetState(int objectIndex, uint key)
    {
        var (errorCode, state) = transport.GetState(objectIndex, key);
        return new GlamourerIpcStateResult(ToErrorCode(errorCode), state);
    }

    public GlamourerIpcStateResult GetState(string playerName, uint key)
    {
        var (errorCode, state) = transport.GetState(playerName, key);
        return new GlamourerIpcStateResult(ToErrorCode(errorCode), state);
    }

    public GlamourerIpcStateResult GetStructuredState(int objectIndex, uint key)
    {
        var (errorCode, state) = transport.GetStructuredState(objectIndex, key);
        return new GlamourerIpcStateResult(ToErrorCode(errorCode), SerializeStructuredState(state));
    }

    public GlamourerIpcStateResult GetStructuredState(string playerName, uint key)
    {
        var (errorCode, state) = transport.GetStructuredState(playerName, key);
        return new GlamourerIpcStateResult(ToErrorCode(errorCode), SerializeStructuredState(state));
    }

    public GlamourerIpcErrorCode ApplyState(
        string serializedState,
        int objectIndex,
        uint key,
        GlamourerIpcApplyFlags flags)
        => ToErrorCode(transport.ApplyState((object)serializedState, objectIndex, key, (ulong)flags));

    public GlamourerIpcErrorCode ApplyState(
        string serializedState,
        string playerName,
        uint key,
        GlamourerIpcApplyFlags flags)
        => ToErrorCode(transport.ApplyState((object)serializedState, playerName, key, (ulong)flags));

    public GlamourerIpcErrorCode ReapplyState(int objectIndex, uint key, GlamourerIpcApplyFlags flags)
        => ToErrorCode(transport.ReapplyState(objectIndex, key, (ulong)flags));

    public GlamourerIpcErrorCode ReapplyState(string playerName, uint key, GlamourerIpcApplyFlags flags)
        => ToErrorCode(transport.ReapplyState(playerName, key, (ulong)flags));

    public GlamourerIpcErrorCode RevertState(int objectIndex, uint key, GlamourerIpcApplyFlags flags)
        => ToErrorCode(transport.RevertState(objectIndex, key, (ulong)flags));

    public GlamourerIpcErrorCode RevertState(string playerName, uint key, GlamourerIpcApplyFlags flags)
        => ToErrorCode(transport.RevertState(playerName, key, (ulong)flags));

    public GlamourerIpcErrorCode SetItem(
        int objectIndex,
        GlamourerIpcEquipSlot slot,
        ulong itemId,
        IReadOnlyList<byte> stains,
        uint key,
        GlamourerIpcApplyFlags flags)
        => ToErrorCode(transport.SetItem(objectIndex, (byte)slot, itemId, CopyStains(stains), key, (ulong)flags));

    public GlamourerIpcErrorCode SetItem(
        string playerName,
        GlamourerIpcEquipSlot slot,
        ulong itemId,
        IReadOnlyList<byte> stains,
        uint key,
        GlamourerIpcApplyFlags flags)
        => ToErrorCode(transport.SetItem(playerName, (byte)slot, itemId, CopyStains(stains), key, (ulong)flags));

    public GlamourerIpcErrorCode SetMetaState(
        int objectIndex,
        GlamourerIpcMetaFlags metaFlags,
        bool enabled,
        uint key,
        GlamourerIpcApplyFlags flags)
        => ToErrorCode(transport.SetMetaState(objectIndex, (ulong)metaFlags, enabled, key, (ulong)flags));

    public GlamourerIpcErrorCode SetMetaState(
        string playerName,
        GlamourerIpcMetaFlags metaFlags,
        bool enabled,
        uint key,
        GlamourerIpcApplyFlags flags)
        => ToErrorCode(transport.SetMetaState(playerName, (ulong)metaFlags, enabled, key, (ulong)flags));

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        transport.StateFinalized -= HandleStateFinalized;
        transport.Initialized -= HandleInitialized;
        transport.Disposed -= HandleDisposed;
        transport.Dispose();
    }

    internal static GlamourerIpcErrorCode ToErrorCode(int errorCode)
        => (GlamourerIpcErrorCode)errorCode;

    private static List<byte> CopyStains(IReadOnlyList<byte> stains)
    {
        ArgumentNullException.ThrowIfNull(stains);
        if (stains.Count != 2)
        {
            throw new ArgumentException("Glamourer SetItem requires exactly two stain bytes.", nameof(stains));
        }

        return [stains[0], stains[1]];
    }

    private static string? SerializeStructuredState(object? state)
        => state switch
        {
            null => null,
            JToken token => token.ToString(Formatting.None),
            _ => state.ToString(),
        };

    private void HandleInitialized()
        => ProviderInitialized?.Invoke();

    private void HandleDisposed()
        => ProviderDisposed?.Invoke();

    private void HandleStateFinalized(nint actor, int finalizationType)
        => StateFinalized?.Invoke(actor, (GlamourerIpcStateFinalizationType)finalizationType);

    private void ThrowIfDisposed()
        => ObjectDisposedException.ThrowIf(disposed, this);

    internal interface IGlamourerIpcTransport : IDisposable
    {
        event Action? Initialized;

        event Action? Disposed;

        event Action<nint, int>? StateFinalized;

        bool HasVersionFunction { get; }

        bool HasAllOperationalFunctions { get; }

        bool HasStateFinalizedEvent { get; }

        (int Major, int Minor) InvokeVersion();

        (int ErrorCode, string State) GetState(int objectIndex, uint key);

        (int ErrorCode, string State) GetState(string playerName, uint key);

        (int ErrorCode, object? State) GetStructuredState(int objectIndex, uint key);

        (int ErrorCode, object? State) GetStructuredState(string playerName, uint key);

        int ApplyState(object state, int objectIndex, uint key, ulong flags);

        int ApplyState(object state, string playerName, uint key, ulong flags);

        int ReapplyState(int objectIndex, uint key, ulong flags);

        int ReapplyState(string playerName, uint key, ulong flags);

        int RevertState(int objectIndex, uint key, ulong flags);

        int RevertState(string playerName, uint key, ulong flags);

        int SetItem(int objectIndex, byte slot, ulong itemId, IReadOnlyList<byte> stains, uint key, ulong flags);

        int SetItem(string playerName, byte slot, ulong itemId, IReadOnlyList<byte> stains, uint key, ulong flags);

        int SetMetaState(int objectIndex, ulong metaFlags, bool enabled, uint key, ulong flags);

        int SetMetaState(string playerName, ulong metaFlags, bool enabled, uint key, ulong flags);
    }

    private sealed class DalamudTransport : IGlamourerIpcTransport
    {
        private const string ApiVersionLabel = "Glamourer.ApiVersion.V2";
        private const string GetStateBase64Label = "Glamourer.GetStateBase64";
        private const string GetStateBase64NameLabel = "Glamourer.GetStateBase64Name";
        private const string GetStateLabel = "Glamourer.GetState";
        private const string GetStateNameLabel = "Glamourer.GetStateName";
        private const string ApplyStateLabel = "Glamourer.ApplyState";
        private const string ApplyStateNameLabel = "Glamourer.ApplyStateName";
        private const string ReapplyStateLabel = "Glamourer.ReapplyState";
        private const string ReapplyStateNameLabel = "Glamourer.ReapplyStateName";
        private const string RevertStateLabel = "Glamourer.RevertState";
        private const string RevertStateNameLabel = "Glamourer.RevertStateName";
        private const string SetItemLabel = "Glamourer.SetItem.V3";
        private const string SetItemNameLabel = "Glamourer.SetItemName.V2";
        private const string SetMetaStateLabel = "Glamourer.SetMetaState";
        private const string SetMetaStateNameLabel = "Glamourer.SetMetaStateName";
        private const string InitializedLabel = "Glamourer.Initialized";
        private const string DisposedLabel = "Glamourer.Disposed";
        // Glamourer 1.7.1.0 / Glamourer.Api ea569211 retains this historical IPC label.
        private const string StateFinalizedLabel = "Penumbra.StateFinalized";

        private readonly ICallGateSubscriber<(int Major, int Minor)> apiVersion;
        private readonly ICallGateSubscriber<int, uint, (int ErrorCode, string State)> getStateBase64;
        private readonly ICallGateSubscriber<string, uint, (int ErrorCode, string State)> getStateBase64Name;
        private readonly ICallGateSubscriber<int, uint, (int ErrorCode, JObject State)> getState;
        private readonly ICallGateSubscriber<string, uint, (int ErrorCode, JObject State)> getStateName;
        private readonly ICallGateSubscriber<object, int, uint, ulong, int> applyState;
        private readonly ICallGateSubscriber<object, string, uint, ulong, int> applyStateName;
        private readonly ICallGateSubscriber<int, uint, ulong, int> reapplyState;
        private readonly ICallGateSubscriber<string, uint, ulong, int> reapplyStateName;
        private readonly ICallGateSubscriber<int, uint, ulong, int> revertState;
        private readonly ICallGateSubscriber<string, uint, ulong, int> revertStateName;
        private readonly ICallGateSubscriber<int, byte, ulong, IReadOnlyList<byte>, uint, ulong, int> setItem;
        private readonly ICallGateSubscriber<string, byte, ulong, IReadOnlyList<byte>, uint, ulong, int> setItemName;
        private readonly ICallGateSubscriber<int, ulong, bool, uint, ulong, int> setMetaState;
        private readonly ICallGateSubscriber<string, ulong, bool, uint, ulong, int> setMetaStateName;
        private readonly ICallGateSubscriber<object?> initialized;
        private readonly ICallGateSubscriber<object?> disposed;
        private readonly ICallGateSubscriber<nint, int, object?> stateFinalized;
        private bool isDisposed;

        public DalamudTransport(IDalamudPluginInterface pluginInterface)
        {
            ArgumentNullException.ThrowIfNull(pluginInterface);
            apiVersion = pluginInterface.GetIpcSubscriber<(int Major, int Minor)>(ApiVersionLabel);
            getStateBase64 = pluginInterface.GetIpcSubscriber<int, uint, (int ErrorCode, string State)>(GetStateBase64Label);
            getStateBase64Name = pluginInterface.GetIpcSubscriber<string, uint, (int ErrorCode, string State)>(GetStateBase64NameLabel);
            getState = pluginInterface.GetIpcSubscriber<int, uint, (int ErrorCode, JObject State)>(GetStateLabel);
            getStateName = pluginInterface.GetIpcSubscriber<string, uint, (int ErrorCode, JObject State)>(GetStateNameLabel);
            applyState = pluginInterface.GetIpcSubscriber<object, int, uint, ulong, int>(ApplyStateLabel);
            applyStateName = pluginInterface.GetIpcSubscriber<object, string, uint, ulong, int>(ApplyStateNameLabel);
            reapplyState = pluginInterface.GetIpcSubscriber<int, uint, ulong, int>(ReapplyStateLabel);
            reapplyStateName = pluginInterface.GetIpcSubscriber<string, uint, ulong, int>(ReapplyStateNameLabel);
            revertState = pluginInterface.GetIpcSubscriber<int, uint, ulong, int>(RevertStateLabel);
            revertStateName = pluginInterface.GetIpcSubscriber<string, uint, ulong, int>(RevertStateNameLabel);
            setItem = pluginInterface.GetIpcSubscriber<int, byte, ulong, IReadOnlyList<byte>, uint, ulong, int>(SetItemLabel);
            setItemName = pluginInterface.GetIpcSubscriber<string, byte, ulong, IReadOnlyList<byte>, uint, ulong, int>(SetItemNameLabel);
            setMetaState = pluginInterface.GetIpcSubscriber<int, ulong, bool, uint, ulong, int>(SetMetaStateLabel);
            setMetaStateName = pluginInterface.GetIpcSubscriber<string, ulong, bool, uint, ulong, int>(SetMetaStateNameLabel);
            initialized = pluginInterface.GetIpcSubscriber<object?>(InitializedLabel);
            disposed = pluginInterface.GetIpcSubscriber<object?>(DisposedLabel);
            stateFinalized = pluginInterface.GetIpcSubscriber<nint, int, object?>(StateFinalizedLabel);
            initialized.Subscribe(HandleInitialized);
            disposed.Subscribe(HandleDisposed);
            stateFinalized.Subscribe(HandleStateFinalized);
        }

        public event Action? Initialized;

        public event Action? Disposed;

        public event Action<nint, int>? StateFinalized;

        public bool HasVersionFunction => apiVersion.HasFunction;

        public bool HasAllOperationalFunctions
            => getStateBase64.HasFunction
                && getStateBase64Name.HasFunction
                && getState.HasFunction
                && getStateName.HasFunction
                && applyState.HasFunction
                && applyStateName.HasFunction
                && reapplyState.HasFunction
                && reapplyStateName.HasFunction
                && revertState.HasFunction
                && revertStateName.HasFunction
                && setItem.HasFunction
                && setItemName.HasFunction
                && setMetaState.HasFunction
                && setMetaStateName.HasFunction;

        public bool HasStateFinalizedEvent => stateFinalized.HasFunction;

        public (int Major, int Minor) InvokeVersion()
            => apiVersion.InvokeFunc();

        public (int ErrorCode, string State) GetState(int objectIndex, uint key)
            => getStateBase64.InvokeFunc(objectIndex, key);

        public (int ErrorCode, string State) GetState(string playerName, uint key)
            => getStateBase64Name.InvokeFunc(playerName, key);

        public (int ErrorCode, object? State) GetStructuredState(int objectIndex, uint key)
        {
            var result = getState.InvokeFunc(objectIndex, key);
            return (result.ErrorCode, result.State);
        }

        public (int ErrorCode, object? State) GetStructuredState(string playerName, uint key)
        {
            var result = getStateName.InvokeFunc(playerName, key);
            return (result.ErrorCode, result.State);
        }

        public int ApplyState(object state, int objectIndex, uint key, ulong flags)
            => applyState.InvokeFunc(state, objectIndex, key, flags);

        public int ApplyState(object state, string playerName, uint key, ulong flags)
            => applyStateName.InvokeFunc(state, playerName, key, flags);

        public int ReapplyState(int objectIndex, uint key, ulong flags)
            => reapplyState.InvokeFunc(objectIndex, key, flags);

        public int ReapplyState(string playerName, uint key, ulong flags)
            => reapplyStateName.InvokeFunc(playerName, key, flags);

        public int RevertState(int objectIndex, uint key, ulong flags)
            => revertState.InvokeFunc(objectIndex, key, flags);

        public int RevertState(string playerName, uint key, ulong flags)
            => revertStateName.InvokeFunc(playerName, key, flags);

        public int SetItem(int objectIndex, byte slot, ulong itemId, IReadOnlyList<byte> stains, uint key, ulong flags)
            => setItem.InvokeFunc(objectIndex, slot, itemId, stains, key, flags);

        public int SetItem(string playerName, byte slot, ulong itemId, IReadOnlyList<byte> stains, uint key, ulong flags)
            => setItemName.InvokeFunc(playerName, slot, itemId, stains, key, flags);

        public int SetMetaState(int objectIndex, ulong metaFlags, bool enabled, uint key, ulong flags)
            => setMetaState.InvokeFunc(objectIndex, metaFlags, enabled, key, flags);

        public int SetMetaState(string playerName, ulong metaFlags, bool enabled, uint key, ulong flags)
            => setMetaStateName.InvokeFunc(playerName, metaFlags, enabled, key, flags);

        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            isDisposed = true;
            stateFinalized.Unsubscribe(HandleStateFinalized);
            disposed.Unsubscribe(HandleDisposed);
            initialized.Unsubscribe(HandleInitialized);
        }

        private void HandleInitialized()
            => Initialized?.Invoke();

        private void HandleDisposed()
            => Disposed?.Invoke();

        private void HandleStateFinalized(nint actor, int finalizationType)
            => StateFinalized?.Invoke(actor, finalizationType);
    }
}
