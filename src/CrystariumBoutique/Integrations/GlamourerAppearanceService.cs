using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json;
using CrystariumBoutique.Core.Appearance;
using CrystariumBoutique.Core.Catalog;
using CrystariumBoutique.Core.Errors;
using CrystariumBoutique.Core.Integrations;
using CrystariumBoutique.Integrations.GlamourerIpc;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;

namespace CrystariumBoutique.Integrations;

internal sealed class GlamourerAppearanceService :
    IContextAwareAppearanceService,
    IVisorAppearanceService,
    ILocalPlayerGearsetFinalizationSource,
    IDependencyAvailabilityRefresher
{
    private const uint NoLockKey = 0;
    private const string SnapshotFormat = "Glamourer.Base64";

    private readonly IGlamourerIpcClient ipcClient;
    private readonly IItemRepository itemRepository;
    private readonly IPenumbraService penumbraService;
    private readonly IPluginLog pluginLog;
    private readonly IClientState clientState;
    private readonly ITargetManager targetManager;
    private readonly IPlayerState playerState;
    private readonly IObjectTable objectTable;
    private readonly Dictionary<EquipmentSlot, uint> customShieldSources = [];
    private int? boundObjectIndex;
    private string? boundPlayerName;
    private bool boundObjectIsGpose;
    private string? localPlayerName;
    private uint? localPlayerHomeWorldId;
    private bool hasAppliedMutation;
    private bool disposed;

    public GlamourerAppearanceService(
        IGlamourerIpcClient ipcClient,
        IPenumbraService penumbraService,
        IPluginLog pluginLog,
        IClientState clientState,
        ITargetManager targetManager,
        IPlayerState playerState,
        IObjectTable objectTable,
        IItemRepository itemRepository)
    {
        this.ipcClient = ipcClient ?? throw new ArgumentNullException(nameof(ipcClient));
        this.penumbraService = penumbraService ?? throw new ArgumentNullException(nameof(penumbraService));
        this.pluginLog = pluginLog ?? throw new ArgumentNullException(nameof(pluginLog));
        this.clientState = clientState ?? throw new ArgumentNullException(nameof(clientState));
        this.targetManager = targetManager ?? throw new ArgumentNullException(nameof(targetManager));
        this.playerState = playerState ?? throw new ArgumentNullException(nameof(playerState));
        this.objectTable = objectTable ?? throw new ArgumentNullException(nameof(objectTable));
        this.itemRepository = itemRepository ?? throw new ArgumentNullException(nameof(itemRepository));

        ipcClient.ProviderInitialized += HandleDependencyInitialized;
        ipcClient.ProviderDisposed += HandleDependencyDisposed;
        ipcClient.StateFinalized += HandleStateFinalized;

        RefreshAvailability();
    }

    public bool IsAvailable => Status == DependencyStatus.Available;

    public DependencyStatus Status { get; private set; } = DependencyStatus.Unknown;

    public string? DetectedVersion { get; private set; }

    public event Action? LocalPlayerGearsetFinalized;

    public event Action? LocalPlayerGameStateReverted;

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

        switch (availability.Status)
        {
            case DependencyStatus.Available:
                pluginLog.Information(
                    previousStatus == DependencyStatus.Unknown
                        ? "Glamourer API {DetectedVersion} detected; capture, equipment/dye preview, and restoration are available."
                        : "Glamourer API {DetectedVersion} detected after startup; Boutique appearance integration is now available.",
                    DetectedVersion ?? "unknown");
                break;
            case DependencyStatus.Incompatible:
                pluginLog.Warning(
                    "Glamourer API {DetectedVersion} is incompatible with Boutique's verified IPC contract: {Detail}",
                    DetectedVersion ?? "unknown",
                    availability.Detail ?? "One or more required endpoints are unavailable.");
                break;
            default:
                pluginLog.Warning(
                    previousStatus == DependencyStatus.Unknown
                        ? "Glamourer was not available at Boutique startup; waiting for its IPC provider. {Detail}"
                        : "Glamourer became unavailable; Boutique appearance integration is waiting for its IPC provider. {Detail}",
                    availability.Detail ?? string.Empty);
                break;
        }

        return true;
    }

    public void PrepareForContextTransition()
        => ReleaseAppearanceTarget();

    public Result RebindToCurrentContext()
    {
        ReleaseAppearanceTarget();
        RefreshLocalPlayerIdentity();
        var targetResult = ResolveCurrentAppearanceObjectIndex();
        if (targetResult.IsSuccess)
        {
            boundObjectIndex = targetResult.Value;
            boundObjectIsGpose = clientState.IsGPosing;
            pluginLog.Information(
                "Boutique appearance session handed off to {Context} object index {ObjectIndex}.",
                boundObjectIsGpose ? "the local GPose actor" : "the local player",
                boundObjectIndex.Value);
            return Result.Ok;
        }

        if (!clientState.IsGPosing || localPlayerName is null)
        {
            return Result.Failure(targetResult.Error!);
        }

        boundPlayerName = localPlayerName;
        boundObjectIsGpose = true;
        pluginLog.Information(
            "Boutique appearance session handed off through Glamourer's persisted local-player state because no mutable GPose object index was exposed.");
        return Result.Ok;
    }

    public Result<AppearanceSnapshot> CapturePlayerAppearance()
    {
        var availabilityError = GetAvailabilityError();
        if (availabilityError is not null)
        {
            return Result.Failure<AppearanceSnapshot>(availabilityError);
        }

        try
        {
            var targetResult = EnsureAppearanceTarget();
            if (!targetResult.IsSuccess)
            {
                return Result.Failure<AppearanceSnapshot>(targetResult.Error!);
            }

            var stateResult = boundPlayerName is { } playerName
                ? ipcClient.GetState(playerName, NoLockKey)
                : ipcClient.GetState(boundObjectIndex!.Value, NoLockKey);
            if (stateResult.ErrorCode != GlamourerIpcErrorCode.Success)
            {
                return Result.Failure<AppearanceSnapshot>(MapError(
                    stateResult.ErrorCode,
                    BoutiqueErrorCode.AppearanceCaptureFailed,
                    "capture the local player's appearance"));
            }

            var state = stateResult.SerializedState;
            if (string.IsNullOrWhiteSpace(state))
            {
                return Result.Failure<AppearanceSnapshot>(new BoutiqueError(
                    BoutiqueErrorCode.AppearanceCaptureFailed,
                    "Glamourer returned an empty appearance snapshot."));
            }

            var structuredResult = boundPlayerName is { } structuredPlayerName
                ? ipcClient.GetStructuredState(structuredPlayerName, NoLockKey)
                : ipcClient.GetStructuredState(boundObjectIndex!.Value, NoLockKey);
            if (structuredResult.ErrorCode != GlamourerIpcErrorCode.Success)
            {
                return Result.Failure<AppearanceSnapshot>(MapError(
                    structuredResult.ErrorCode,
                    BoutiqueErrorCode.AppearanceCaptureFailed,
                    "read the local player's equipped items and dyes"));
            }

            var equipment = ParseCapturedEquipment(
                structuredResult.SerializedState,
                customShieldSources);
            return Result.Success(AppearanceSnapshot.Create(
                state,
                SnapshotFormat,
                DetectedVersion,
                equipment));
        }
        catch (Exception exception)
        {
            pluginLog.Error(exception, "Glamourer GetStateBase64 IPC threw while capturing the Boutique actor.");
            return Result.Failure<AppearanceSnapshot>(InvocationFailure(
                "capture the local player's appearance",
                exception));
        }
    }

    public Result ApplyEquipment(EquipmentSlot slot, AppearanceSelection appearance)
        => ApplyItem(
            slot,
            appearance,
            BoutiqueErrorCode.AppearanceApplyFailed,
            "apply the selected item");

    public Result ApplyDye(EquipmentSlot slot, AppearanceSelection appearance)
        => ApplyItem(
            slot,
            appearance,
            BoutiqueErrorCode.InvalidDye,
            "apply the selected dyes");

    public Result SetVisorState(bool enabled)
    {
        var availabilityError = GetAvailabilityError();
        if (availabilityError is not null)
        {
            return Result.Failure(availabilityError);
        }

        try
        {
            var targetResult = EnsureAppearanceTarget();
            if (!targetResult.IsSuccess)
            {
                return Result.Failure(targetResult.Error!);
            }

            var errorCode = boundPlayerName is { } playerName
                ? ipcClient.SetMetaState(
                    playerName,
                    GlamourerIpcMetaFlags.VisorState,
                    enabled,
                    NoLockKey,
                    GlamourerIpcApplyFlags.Once)
                : ipcClient.SetMetaState(
                    boundObjectIndex!.Value,
                    GlamourerIpcMetaFlags.VisorState,
                    enabled,
                    NoLockKey,
                    GlamourerIpcApplyFlags.Once);
            var result = MapMutationResult(
                errorCode,
                BoutiqueErrorCode.AppearanceApplyFailed,
                $"set Visor State {(enabled ? "on" : "off")}");
            if (result.IsSuccess)
            {
                hasAppliedMutation = true;
            }

            return result;
        }
        catch (Exception exception)
        {
            pluginLog.Error(
                exception,
                "Glamourer SetMetaState IPC threw while setting Visor State to {VisorState}.",
                enabled);
            return Result.Failure(InvocationFailure(
                $"set Visor State {(enabled ? "on" : "off")}",
                exception));
        }
    }

    private Result ApplyItem(
        EquipmentSlot slot,
        AppearanceSelection appearance,
        BoutiqueErrorCode fallbackCode,
        string operation)
    {
        ArgumentNullException.ThrowIfNull(appearance);

        var availabilityError = GetAvailabilityError();
        if (availabilityError is not null)
        {
            return Result.Failure(availabilityError);
        }

        var itemId = ResolveApplicationItemId(
            slot,
            appearance,
            itemRepository,
            playerState.ClassJob.RowId);
        var clearingSlot = itemId == 0
            && appearance.AppearanceId == AppearanceId.None
            && appearance.SourceItemId is null or 0;
        if (itemId == 0 && !clearingSlot)
        {
            return Result.Failure(new BoutiqueError(
                BoutiqueErrorCode.InvalidItem,
                "The selected appearance does not have an item ID that Glamourer can apply."));
        }

        var stains = NormalizeStains(appearance);
        try
        {
            var targetResult = EnsureAppearanceTarget();
            if (!targetResult.IsSuccess)
            {
                return Result.Failure(targetResult.Error!);
            }

            var apiSlot = GlamourerIpcSlotMapper.FromEquipmentSlot(slot);
            var errorCode = boundPlayerName is { } playerName
                ? ipcClient.SetItem(
                    playerName,
                    apiSlot,
                    itemId,
                    stains,
                    NoLockKey,
                    GlamourerIpcApplyFlags.Once)
                : ipcClient.SetItem(
                    boundObjectIndex!.Value,
                    apiSlot,
                    itemId,
                    stains,
                    NoLockKey,
                    GlamourerIpcApplyFlags.Once);
            var result = MapMutationResult(
                errorCode,
                fallbackCode,
                operation);
            if (result.IsSuccess)
            {
                hasAppliedMutation = true;
                if (slot == EquipmentSlot.OffHand
                    && appearance.SourceItemId is > 0 and var sourceItemId
                    && itemId != sourceItemId)
                {
                    customShieldSources[slot] = sourceItemId;
                }
                else
                {
                    customShieldSources.Remove(slot);
                }

                pluginLog.Debug(
                    "Glamourer SetItem accepted item {ItemId} for {Slot} with result {ErrorCode}.",
                    itemId,
                    slot,
                    errorCode);
            }
            else
            {
                pluginLog.Warning(
                    "Glamourer SetItem rejected item {ItemId} for {Slot}: {ErrorCode}.",
                    itemId,
                    slot,
                    errorCode);
            }

            if (result.IsSuccess && slot == EquipmentSlot.Head)
            {
                try
                {
                    var reapplyErrorCode = boundPlayerName is { } reapplyPlayerName
                        ? ipcClient.ReapplyState(
                            reapplyPlayerName,
                            NoLockKey,
                            GlamourerIpcApplyFlags.Once)
                        : ipcClient.ReapplyState(
                            boundObjectIndex!.Value,
                            NoLockKey,
                            GlamourerIpcApplyFlags.Once);
                    result = MapMutationResult(
                        reapplyErrorCode,
                        fallbackCode,
                        "refresh the selected head item");
                    if (!result.IsSuccess)
                    {
                        pluginLog.Warning(
                            "Glamourer ReapplyState rejected the post-head-preview refresh: {ErrorCode}.",
                            reapplyErrorCode);
                    }
                }
                catch (Exception exception)
                {
                    pluginLog.Error(
                        exception,
                        "Glamourer ReapplyState IPC threw during the post-head-preview refresh.");
                    return Result.Failure(InvocationFailure(
                        "refresh the selected head item",
                        exception,
                        fallbackCode));
                }
            }

            return result;
        }
        catch (Exception exception)
        {
            pluginLog.Error(
                exception,
                "Glamourer SetItem IPC threw for item {ItemId}, slot {Slot}, stains [{Stain0}, {Stain1}], flags {Flags}.",
                itemId,
                slot,
                stains[0],
                stains[1],
                GlamourerIpcApplyFlags.Once);
            return Result.Failure(InvocationFailure(
                operation,
                exception,
                fallbackCode));
        }
    }

    internal static ulong ResolveApplicationItemId(
        EquipmentSlot slot,
        AppearanceSelection appearance,
        IItemRepository itemRepository,
        uint activeClassJobId)
    {
        ArgumentNullException.ThrowIfNull(appearance);
        ArgumentNullException.ThrowIfNull(itemRepository);

        if (slot == EquipmentSlot.OffHand
            && appearance.SourceItemId is > 0 and var sourceItemId
            && itemRepository.TryGetItem(sourceItemId, slot, out var item)
            && item.IsShield
            && item.EquippableClassJobs.Contains(activeClassJobId)
            && item.AppearanceKey.ModelMain != 0)
        {
            return GlamourerIpcCustomItemId.FromShieldModel(item.AppearanceKey.ModelMain);
        }

        return appearance.SourceItemId is > 0
            ? appearance.SourceItemId.Value
            : appearance.AppearanceId.Value;
    }

    internal static ImmutableArray<CapturedEquipmentState> ParseCapturedEquipment(
        string? serializedState,
        IReadOnlyDictionary<EquipmentSlot, uint>? customItemSources = null)
    {
        if (string.IsNullOrWhiteSpace(serializedState))
        {
            throw new InvalidDataException("Glamourer returned an empty structured appearance state.");
        }

        using var document = JsonDocument.Parse(serializedState);
        if (!document.RootElement.TryGetProperty("Equipment", out var equipment)
            || equipment.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidDataException("Glamourer's structured appearance state did not contain equipment data.");
        }

        var captured = ImmutableArray.CreateBuilder<CapturedEquipmentState>();
        foreach (var (propertyName, slot) in StructuredEquipmentSlots)
        {
            if (!equipment.TryGetProperty(propertyName, out var slotState)
                || slotState.ValueKind != JsonValueKind.Object
                || !TryReadUInt64(slotState, "ItemId", out var rawItemId)
                || rawItemId == 0)
            {
                continue;
            }

            var itemId = customItemSources is not null
                && customItemSources.TryGetValue(slot, out var customSourceItemId)
                    ? customSourceItemId
                    : rawItemId <= uint.MaxValue
                        ? (uint)rawItemId
                        : 0;
            if (itemId == 0)
            {
                continue;
            }

            var stain1 = TryReadByte(slotState, "Stain", out var first) ? first : (byte)0;
            var stain2 = TryReadByte(slotState, "Stain2", out var second) ? second : (byte)0;
            captured.Add(new CapturedEquipmentState(
                slot,
                itemId,
                [new StainId(stain1), new StainId(stain2)]));
        }

        return captured.ToImmutable();
    }

    private static readonly (string PropertyName, EquipmentSlot Slot)[] StructuredEquipmentSlots =
    [
        ("MainHand", EquipmentSlot.MainHand),
        ("OffHand", EquipmentSlot.OffHand),
        ("Head", EquipmentSlot.Head),
        ("Body", EquipmentSlot.Body),
        ("Hands", EquipmentSlot.Hands),
        ("Legs", EquipmentSlot.Legs),
        ("Feet", EquipmentSlot.Feet),
        ("Ears", EquipmentSlot.Ears),
        ("Neck", EquipmentSlot.Neck),
        ("Wrists", EquipmentSlot.Wrists),
        ("RFinger", EquipmentSlot.RightRing),
        ("LFinger", EquipmentSlot.LeftRing),
    ];

    private static bool TryReadUInt64(JsonElement parent, string propertyName, out ulong value)
    {
        value = 0;
        if (!parent.TryGetProperty(propertyName, out var property))
        {
            return false;
        }

        return property.ValueKind switch
        {
            JsonValueKind.Number => property.TryGetUInt64(out value),
            JsonValueKind.String => ulong.TryParse(
                property.GetString(),
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out value),
            _ => false,
        };
    }

    private static bool TryReadByte(JsonElement parent, string propertyName, out byte value)
    {
        value = 0;
        if (!parent.TryGetProperty(propertyName, out var property))
        {
            return false;
        }

        return property.ValueKind switch
        {
            JsonValueKind.Number => property.TryGetByte(out value),
            JsonValueKind.String => byte.TryParse(
                property.GetString(),
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out value),
            _ => false,
        };
    }

    public Result ApplyAppearance(AppearanceSnapshot appearance)
    {
        ArgumentNullException.ThrowIfNull(appearance);
        var result = ApplySnapshot(
            appearance,
            "apply an appearance snapshot",
            BoutiqueErrorCode.AppearanceApplyFailed);
        if (result.IsSuccess)
        {
            hasAppliedMutation = true;
        }

        return result;
    }

    public Result RevertToGame()
    {
        var targetResult = EnsureAppearanceTarget();
        if (!targetResult.IsSuccess)
        {
            return targetResult;
        }

        var result = boundPlayerName is { } playerName
            ? RevertPersistedPlayerToGame(
                playerName,
                "return the Boutique player to game state",
                BoutiqueErrorCode.AppearanceApplyFailed)
            : RevertToGameAt(
                boundObjectIndex,
                "return the Boutique actor to game state",
                BoutiqueErrorCode.AppearanceApplyFailed);
        if (result.IsSuccess)
        {
            hasAppliedMutation = false;
            customShieldSources.Clear();
        }

        return result;
    }

    public Result RestoreAppearance(AppearanceSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        var restoreNormalPlayerAfterGpose = boundObjectIsGpose && !clientState.IsGPosing;
        Result result;
        if (boundPlayerName is { } boundName)
        {
            result = RevertPersistedPlayerToGame(
                boundName,
                "restore the Boutique player to game state",
                BoutiqueErrorCode.RestoreFailed);
        }
        else
        {
            var restoreObjectIndex = boundObjectIndex.HasValue
                ? AppearanceTargetResolver.ResolveRestoreTarget(
                    boundObjectIsGpose,
                    clientState.IsGPosing,
                    boundObjectIndex.Value)
                : (int?)null;
            result = RevertToGameAt(
                restoreObjectIndex,
                "restore the Boutique actor to game state",
                BoutiqueErrorCode.RestoreFailed);
            if (result.IsSuccess
                && boundObjectIsGpose
                && clientState.IsGPosing
                && localPlayerName is not null)
            {
                result = RevertPersistedPlayerToGame(
                    localPlayerName,
                    "restore the persisted normal player to game state while closing in GPose",
                    BoutiqueErrorCode.RestoreFailed);
            }
        }

        if (result.IsSuccess)
        {
            hasAppliedMutation = false;
            customShieldSources.Clear();
            if (restoreNormalPlayerAfterGpose)
            {
                pluginLog.Information(
                    "Returned the normal local-player actor to game state after the Boutique GPose session ended.");
            }

            ReleaseAppearanceTarget();
        }

        return result;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        ReleaseAppearanceTarget();
        ipcClient.StateFinalized -= HandleStateFinalized;
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

        if (hasAppliedMutation)
        {
            QueueDependencyLossRedraw();
        }

        Status = DependencyStatus.Unavailable;
        DetectedVersion = null;
        customShieldSources.Clear();
        ReleaseAppearanceTarget();
        pluginLog.Warning("Glamourer was unloaded; new Boutique previews are disabled until it is available again.");
    }

    private void HandleStateFinalized(nint actor, GlamourerIpcStateFinalizationType finalizationType)
    {
        var localPlayerAddress = objectTable.LocalPlayer?.Address ?? nint.Zero;
        if (!IsValidLocalPlayerFinalization(actor, localPlayerAddress, clientState.IsGPosing))
        {
            return;
        }

        if (finalizationType == GlamourerIpcStateFinalizationType.Gearset)
        {
            customShieldSources.Clear();
            LocalPlayerGearsetFinalized?.Invoke();
        }
        else if (finalizationType is GlamourerIpcStateFinalizationType.Revert
                 or GlamourerIpcStateFinalizationType.RevertEquipment)
        {
            customShieldSources.Clear();
            LocalPlayerGameStateReverted?.Invoke();
        }
    }

    internal static bool IsLocalPlayerGearsetFinalization(
        nint actor,
        GlamourerIpcStateFinalizationType finalizationType,
        nint localPlayerAddress,
        bool isGPosing)
        => finalizationType == GlamourerIpcStateFinalizationType.Gearset
            && IsValidLocalPlayerFinalization(actor, localPlayerAddress, isGPosing);

    internal static bool IsLocalPlayerGameStateRevertFinalization(
        nint actor,
        GlamourerIpcStateFinalizationType finalizationType,
        nint localPlayerAddress,
        bool isGPosing)
        => finalizationType is GlamourerIpcStateFinalizationType.Revert
                or GlamourerIpcStateFinalizationType.RevertEquipment
            && IsValidLocalPlayerFinalization(actor, localPlayerAddress, isGPosing);

    private static bool IsValidLocalPlayerFinalization(
        nint actor,
        nint localPlayerAddress,
        bool isGPosing)
        => !isGPosing
            && actor != nint.Zero
            && localPlayerAddress != nint.Zero
            && actor == localPlayerAddress;

    private void QueueDependencyLossRedraw()
    {
        var objectIndex = boundObjectIndex;
        var redrawAfterGpose = false;
        if (!objectIndex.HasValue)
        {
            var resolvedTarget = ResolveCurrentAppearanceObjectIndex();
            if (resolvedTarget.IsSuccess)
            {
                objectIndex = resolvedTarget.Value;
            }
            else if (objectTable.LocalPlayer is { } localPlayer)
            {
                objectIndex = (int)localPlayer.ObjectIndex;
                redrawAfterGpose = clientState.IsGPosing && objectIndex.Value == 0;
            }
        }

        if (!objectIndex.HasValue)
        {
            pluginLog.Warning(
                "Glamourer was unloaded during a Boutique preview, but no local actor index was available for a Penumbra game-state redraw.");
            return;
        }

        var redrawResult = penumbraService.RedrawObject(objectIndex.Value, redrawAfterGpose);
        if (redrawResult.IsSuccess)
        {
            hasAppliedMutation = false;
            pluginLog.Information(
                "Queued a Penumbra game-state redraw for Boutique actor index {ObjectIndex} after Glamourer was unloaded.",
                objectIndex.Value);
        }
        else
        {
            pluginLog.Warning(
                "Boutique could not queue its Glamourer-unload redraw fallback: {Error}",
                redrawResult.Error?.ToString() ?? "Unknown Penumbra redraw error.");
        }
    }

    private Result ApplySnapshot(
        AppearanceSnapshot snapshot,
        string operation,
        BoutiqueErrorCode fallbackCode,
        int? objectIndexOverride = null)
    {
        var availabilityError = GetAvailabilityError();
        if (availabilityError is not null)
        {
            return Result.Failure(availabilityError);
        }

        if (!string.Equals(snapshot.Format, SnapshotFormat, StringComparison.Ordinal))
        {
            return Result.Failure(new BoutiqueError(
                fallbackCode,
                "The appearance snapshot format is not supported by the Glamourer adapter.",
                snapshot.Format));
        }

        try
        {
            var targetResult = objectIndexOverride.HasValue
                ? Result.Ok
                : EnsureAppearanceTarget();
            if (!targetResult.IsSuccess)
            {
                return Result.Failure(targetResult.Error!);
            }

            var flags = GlamourerIpcApplyFlags.Once
                | GlamourerIpcApplyFlags.Equipment
                | GlamourerIpcApplyFlags.Customization;
            var useNameTarget = !objectIndexOverride.HasValue && boundPlayerName is not null;
            var errorCode = useNameTarget
                ? ipcClient.ApplyState(
                    snapshot.SerializedState,
                    boundPlayerName!,
                    NoLockKey,
                    flags)
                : ipcClient.ApplyState(
                    snapshot.SerializedState,
                    objectIndexOverride ?? boundObjectIndex!.Value,
                    NoLockKey,
                    flags);
            var result = MapMutationResult(errorCode, fallbackCode, operation);
            if (!result.IsSuccess)
            {
                return result;
            }

            var reapplyErrorCode = useNameTarget
                ? ipcClient.ReapplyState(
                    boundPlayerName!,
                    NoLockKey,
                    GlamourerIpcApplyFlags.Once)
                : ipcClient.ReapplyState(
                    objectIndexOverride ?? boundObjectIndex!.Value,
                    NoLockKey,
                    GlamourerIpcApplyFlags.Once);
            result = MapMutationResult(
                reapplyErrorCode,
                fallbackCode,
                $"refresh the character after attempting to {operation}");
            if (!result.IsSuccess)
            {
                pluginLog.Warning(
                    "Glamourer ReapplyState rejected the post-snapshot weapon/equipment refresh: {ErrorCode}.",
                    reapplyErrorCode);
            }

            return result;
        }
        catch (Exception exception)
        {
            pluginLog.Error(
                exception,
                "Glamourer appearance-state IPC threw while attempting to {Operation} and refresh it.",
                operation);
            return Result.Failure(InvocationFailure(operation, exception, fallbackCode));
        }
    }

    private Result RevertToGameAt(
        int? objectIndexOverride,
        string operation,
        BoutiqueErrorCode fallbackCode)
    {
        var availabilityError = GetAvailabilityError();
        if (availabilityError is not null)
        {
            return Result.Failure(availabilityError);
        }

        try
        {
            var targetResult = objectIndexOverride.HasValue
                ? Result.Success(objectIndexOverride.Value)
                : GetAppearanceObjectIndex();
            if (!targetResult.IsSuccess)
            {
                return Result.Failure(targetResult.Error!);
            }

            var objectIndex = targetResult.Value;
            var flags = GlamourerIpcApplyFlags.Equipment | GlamourerIpcApplyFlags.Customization;
            var errorCode = ipcClient.RevertState(objectIndex, NoLockKey, flags);
            var result = MapMutationResult(errorCode, fallbackCode, operation);
            if (result.IsSuccess)
            {
                pluginLog.Debug(
                    "Glamourer RevertState returned Boutique actor {ObjectIndex} to game state with result {ErrorCode}.",
                    objectIndex,
                    errorCode);
            }
            else
            {
                pluginLog.Warning(
                    "Glamourer RevertState rejected the Boutique game-state restoration for actor {ObjectIndex}: {ErrorCode}.",
                    objectIndex,
                    errorCode);
            }

            return result;
        }
        catch (Exception exception)
        {
            pluginLog.Error(
                exception,
                "Glamourer RevertState IPC threw while attempting to {Operation}.",
                operation);
            return Result.Failure(InvocationFailure(operation, exception, fallbackCode));
        }
    }

    private Result RevertPersistedPlayerToGame(
        string playerName,
        string operation,
        BoutiqueErrorCode fallbackCode)
    {
        var availabilityError = GetAvailabilityError();
        if (availabilityError is not null)
        {
            return Result.Failure(availabilityError);
        }

        try
        {
            var flags = GlamourerIpcApplyFlags.Equipment | GlamourerIpcApplyFlags.Customization;
            var errorCode = ipcClient.RevertState(playerName, NoLockKey, flags);
            var result = MapMutationResult(errorCode, fallbackCode, operation);
            if (result.IsSuccess)
            {
                pluginLog.Debug(
                    "Glamourer RevertStateName returned the persisted Boutique player to game state with result {ErrorCode}.",
                    errorCode);
            }
            else
            {
                pluginLog.Warning(
                    "Glamourer RevertStateName rejected persisted normal-player cleanup: {ErrorCode}.",
                    errorCode);
            }

            return result;
        }
        catch (Exception exception)
        {
            pluginLog.Error(
                exception,
                "Glamourer RevertStateName IPC threw while attempting to {Operation}.",
                operation);
            return Result.Failure(InvocationFailure(operation, exception, fallbackCode));
        }
    }

    private BoutiqueError? GetAvailabilityError()
        => Status switch
        {
            DependencyStatus.Available => null,
            DependencyStatus.Incompatible => new BoutiqueError(
                BoutiqueErrorCode.UnsupportedApiVersion,
                $"Glamourer API {DetectedVersion ?? "unknown"} is not compatible with this build."),
            _ => new BoutiqueError(
                BoutiqueErrorCode.DependencyUnavailable,
                "Glamourer is not currently available."),
        };

    private Result EnsureAppearanceTarget()
    {
        if (boundObjectIndex.HasValue || boundPlayerName is not null)
        {
            return Result.Ok;
        }

        return RebindToCurrentContext();
    }

    private Result<int> GetAppearanceObjectIndex()
    {
        if (boundObjectIndex is int objectIndex)
        {
            return Result.Success(objectIndex);
        }

        return ResolveCurrentAppearanceObjectIndex();
    }

    private Result<int> ResolveCurrentAppearanceObjectIndex()
    {
        var gposeTarget = clientState.IsGPosing ? targetManager.GPoseTarget : null;
        var gposeTargetIsLocalPlayer = gposeTarget is null || IsLocalPlayer(gposeTarget, allowIdentityAdoption: true);
        var discoveredLocalPlayerObjectIndex = clientState.IsGPosing
            ? DiscoverLocalGposePlayerObjectIndex()
            : null;
        return AppearanceTargetResolver.Resolve(
            clientState.IsGPosing,
            gposeTarget is null ? null : (int)gposeTarget.ObjectIndex,
            gposeTargetIsLocalPlayer,
            discoveredLocalPlayerObjectIndex);
    }

    private int? DiscoverLocalGposePlayerObjectIndex()
    {
        RefreshLocalPlayerIdentity();
        if (localPlayerName is null)
        {
            return null;
        }

        var candidates = objectTable.PlayerObjects
            .Where(player => player.ObjectIndex != 0 && IsLocalPlayer(player, allowIdentityAdoption: false))
            .Select(player => (int)player.ObjectIndex)
            .Distinct()
            .ToArray();
        if (candidates.Length == 1)
        {
            return candidates[0];
        }

        if (candidates.Length > 1)
        {
            pluginLog.Debug(
                "Boutique found {CandidateCount} matching non-normal GPose player objects and will wait for an unambiguous actor or local GPose target.",
                candidates.Length);
        }

        return null;
    }

    private bool IsLocalPlayer(IGameObject target, bool allowIdentityAdoption)
    {
        RefreshLocalPlayerIdentity();
        if (localPlayerName is null)
        {
            if (!allowIdentityAdoption
                || target.ObjectKind != ObjectKind.Pc
                || string.IsNullOrWhiteSpace(target.Name.TextValue))
            {
                return false;
            }

            localPlayerName = target.Name.TextValue;
            var selectedHomeWorldId = (target as IPlayerCharacter)?.HomeWorld.RowId;
            localPlayerHomeWorldId = selectedHomeWorldId is > 0 ? selectedHomeWorldId : null;
            pluginLog.Information(
                "Boutique adopted the explicitly selected GPose player identity because normal-world player state was unavailable at plugin initialization.");
        }

        var targetHomeWorld = (target as IPlayerCharacter)?.HomeWorld.RowId;
        return AppearanceTargetResolver.MatchesLocalPlayer(
            target.Name.TextValue,
            localPlayerName,
            targetHomeWorld,
            localPlayerHomeWorldId);
    }

    private void RefreshLocalPlayerIdentity()
    {
        if (playerState.IsLoaded && !string.IsNullOrWhiteSpace(playerState.CharacterName))
        {
            localPlayerName = playerState.CharacterName;
            var homeWorldId = playerState.HomeWorld.RowId;
            localPlayerHomeWorldId = homeWorldId == 0 ? null : homeWorldId;
            return;
        }

        var localPlayer = objectTable.LocalPlayer;
        if (localPlayer is null || string.IsNullOrWhiteSpace(localPlayer.Name.TextValue))
        {
            return;
        }

        localPlayerName = localPlayer.Name.TextValue;
        var objectTableHomeWorldId = localPlayer.HomeWorld.RowId;
        localPlayerHomeWorldId = objectTableHomeWorldId == 0 ? null : objectTableHomeWorldId;
    }

    private void ReleaseAppearanceTarget()
    {
        boundObjectIndex = null;
        boundPlayerName = null;
        boundObjectIsGpose = false;
    }

    internal static Result MapMutationResult(
        GlamourerIpcErrorCode errorCode,
        BoutiqueErrorCode fallbackCode,
        string operation)
        => errorCode is GlamourerIpcErrorCode.Success or GlamourerIpcErrorCode.NothingDone
            ? Result.Ok
            : Result.Failure(MapError(errorCode, fallbackCode, operation));

    private static BoutiqueError MapError(
        GlamourerIpcErrorCode errorCode,
        BoutiqueErrorCode fallbackCode,
        string operation)
    {
        var (code, message) = errorCode switch
        {
            GlamourerIpcErrorCode.ActorNotFound => (
                BoutiqueErrorCode.PlayerUnavailable,
                "The Boutique appearance actor is not available for preview."),
            GlamourerIpcErrorCode.ActorNotHuman => (
                BoutiqueErrorCode.InvalidGameState,
                "The Boutique appearance actor is not in a human character state that Glamourer can modify."),
            GlamourerIpcErrorCode.ItemInvalid => (
                BoutiqueErrorCode.InvalidItem,
                "Glamourer rejected the selected item for this equipment slot."),
            GlamourerIpcErrorCode.InvalidKey => (
                fallbackCode,
                "Glamourer rejected the operation because another state lock owns the character."),
            GlamourerIpcErrorCode.InvalidState or GlamourerIpcErrorCode.CouldNotParse => (
                fallbackCode,
                "Glamourer could not read the captured appearance state."),
            _ => (fallbackCode, $"Glamourer could not {operation}."),
        };

        return new BoutiqueError(code, message, $"GlamourerIpcErrorCode.{errorCode} ({(int)errorCode})");
    }

    private static BoutiqueError InvocationFailure(
        string operation,
        Exception exception,
        BoutiqueErrorCode code = BoutiqueErrorCode.UnknownIntegrationFailure)
        => new(
            code,
            $"Glamourer failed to {operation}.",
            DescribeException(exception));

    private static string DescribeException(Exception exception)
    {
        var descriptions = new List<string>();
        for (var current = exception; current is not null; current = current.InnerException)
        {
            descriptions.Add($"{current.GetType().Name}: {current.Message}");
        }

        return string.Join(" -> ", descriptions);
    }

    private static List<byte> NormalizeStains(AppearanceSelection appearance)
    {
        var stains = new List<byte>(2) { 0, 0 };
        for (var index = 0; index < Math.Min(stains.Count, appearance.Stains.Length); index++)
        {
            stains[index] = appearance.Stains[index].Value;
        }

        return stains;
    }
}
