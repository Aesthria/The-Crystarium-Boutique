namespace CrystariumBoutique.Integrations.GlamourerIpc;

internal interface IGlamourerIpcClient : IDisposable
{
    event Action? ProviderInitialized;

    event Action? ProviderDisposed;

    event Action<nint, GlamourerIpcStateFinalizationType>? StateFinalized;

    bool SupportsStateFinalized { get; }

    GlamourerIpcAvailability DetectAvailability();

    GlamourerIpcStateResult GetState(int objectIndex, uint key);

    GlamourerIpcStateResult GetState(string playerName, uint key);

    GlamourerIpcStateResult GetStructuredState(int objectIndex, uint key);

    GlamourerIpcStateResult GetStructuredState(string playerName, uint key);

    GlamourerIpcErrorCode ApplyState(
        string serializedState,
        int objectIndex,
        uint key,
        GlamourerIpcApplyFlags flags);

    GlamourerIpcErrorCode ApplyState(
        string serializedState,
        string playerName,
        uint key,
        GlamourerIpcApplyFlags flags);

    GlamourerIpcErrorCode ReapplyState(int objectIndex, uint key, GlamourerIpcApplyFlags flags);

    GlamourerIpcErrorCode ReapplyState(string playerName, uint key, GlamourerIpcApplyFlags flags);

    GlamourerIpcErrorCode RevertState(int objectIndex, uint key, GlamourerIpcApplyFlags flags);

    GlamourerIpcErrorCode RevertState(string playerName, uint key, GlamourerIpcApplyFlags flags);

    GlamourerIpcErrorCode SetItem(
        int objectIndex,
        GlamourerIpcEquipSlot slot,
        ulong itemId,
        IReadOnlyList<byte> stains,
        uint key,
        GlamourerIpcApplyFlags flags);

    GlamourerIpcErrorCode SetItem(
        string playerName,
        GlamourerIpcEquipSlot slot,
        ulong itemId,
        IReadOnlyList<byte> stains,
        uint key,
        GlamourerIpcApplyFlags flags);

    GlamourerIpcErrorCode SetMetaState(
        int objectIndex,
        GlamourerIpcMetaFlags metaFlags,
        bool enabled,
        uint key,
        GlamourerIpcApplyFlags flags);

    GlamourerIpcErrorCode SetMetaState(
        string playerName,
        GlamourerIpcMetaFlags metaFlags,
        bool enabled,
        uint key,
        GlamourerIpcApplyFlags flags);
}
