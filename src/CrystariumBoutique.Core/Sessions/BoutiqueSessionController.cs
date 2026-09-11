using System.Collections.Immutable;
using System.Globalization;
using CrystariumBoutique.Core.Appearance;
using CrystariumBoutique.Core.Catalog;
using CrystariumBoutique.Core.Errors;
using CrystariumBoutique.Core.Integrations;
using CrystariumBoutique.Core.Loadouts;

namespace CrystariumBoutique.Core.Sessions;

public sealed class BoutiqueSessionController
{
    private const int EquipmentPreviewHistoryLimit = 32;
    private readonly IAppearanceService appearanceService;
    private readonly IItemRepository? itemRepository;
    private readonly Dictionary<EquipmentSlot, PreviewEquipmentState> originalEquipment = [];
    private readonly Dictionary<EquipmentSlot, PreviewEquipmentState> previewEquipment = [];
    private readonly Dictionary<(EquipmentSlot Slot, byte Channel), StainId> dyeOverrides = [];
    private readonly List<EquipmentPreviewHistoryEntry> equipmentPreviewHistory = [];
    private bool? previewVisorState;

    public BoutiqueSessionController(
        IAppearanceService appearanceService,
        BoutiqueSession? session = null,
        IItemRepository? itemRepository = null)
    {
        this.appearanceService = appearanceService ?? throw new ArgumentNullException(nameof(appearanceService));
        this.itemRepository = itemRepository;
        Session = session ?? new BoutiqueSession();
    }

    public BoutiqueSession Session { get; }

    public IReadOnlyDictionary<EquipmentSlot, PreviewEquipmentState> PreviewEquipmentBySlot => previewEquipment;

    public int ChangedSlotCount
        => originalEquipment.Keys
            .Union(previewEquipment.Keys)
            .Count(slot => !originalEquipment.TryGetValue(slot, out var original)
                || !previewEquipment.TryGetValue(slot, out var current)
                || original.Appearance != current.Appearance);

    public bool CanUndoEquipmentPreview => equipmentPreviewHistory.Count > 0;

    public Result Open()
    {
        var beginResult = Session.Begin();
        if (!beginResult.IsSuccess)
        {
            return beginResult;
        }

        previewEquipment.Clear();
        originalEquipment.Clear();
        dyeOverrides.Clear();
        equipmentPreviewHistory.Clear();
        previewVisorState = null;
        if (!appearanceService.IsAvailable)
        {
            var message = appearanceService.Status == DependencyStatus.Incompatible
                ? $"The detected Glamourer API ({appearanceService.DetectedVersion ?? "unknown"}) is not compatible with this build."
                : "Glamourer is not available. Enable it before starting a preview session.";
            Session.MarkDependencyUnavailable(message);
            var code = appearanceService.Status == DependencyStatus.Incompatible
                ? BoutiqueErrorCode.UnsupportedApiVersion
                : BoutiqueErrorCode.DependencyUnavailable;
            return Result.Failure(new BoutiqueError(code, message));
        }

        var captureResult = appearanceService.CapturePlayerAppearance();
        if (!captureResult.IsSuccess || captureResult.Value is null)
        {
            var error = captureResult.Error ?? new BoutiqueError(
                BoutiqueErrorCode.AppearanceCaptureFailed,
                "The original appearance could not be captured.");

            Session.MarkDependencyUnavailable(error.Message);
            return Result.Failure(error);
        }

        var activateResult = Session.Activate(captureResult.Value);
        if (activateResult.IsSuccess)
        {
            LoadCapturedEquipment(captureResult.Value);
        }

        return activateResult;
    }

    public Result RetryOpenAfterDependencyAvailable()
    {
        if (Session.State != BoutiqueSessionState.DependencyUnavailable)
        {
            return Result.Failure(new BoutiqueError(
                BoutiqueErrorCode.InvalidGameState,
                "Only a browse-only Boutique session can be upgraded after Glamourer becomes available.",
                Session.State.ToString()));
        }

        if (!appearanceService.IsAvailable)
        {
            return Result.Failure(new BoutiqueError(
                appearanceService.Status == DependencyStatus.Incompatible
                    ? BoutiqueErrorCode.UnsupportedApiVersion
                    : BoutiqueErrorCode.DependencyUnavailable,
                "Glamourer is not currently available for Boutique previews."));
        }

        var closeResult = Close();
        return closeResult.IsSuccess
            ? Open()
            : closeResult;
    }

    public Result MarkDependencyUnavailable(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        if (Session.State == BoutiqueSessionState.DependencyUnavailable)
        {
            return Result.Ok;
        }

        if (Session.State != BoutiqueSessionState.Active)
        {
            return Result.Failure(new BoutiqueError(
                BoutiqueErrorCode.InvalidGameState,
                "Only an active Boutique session can be interrupted by a lost appearance dependency.",
                Session.State.ToString()));
        }

        return Session.MarkDependencyUnavailable(reason);
    }

    public Result PreviewEquipment(
        EquipmentSlot slot,
        AppearanceSelection appearance,
        byte dyeChannelCount = 0,
        string? displayName = null,
        uint iconId = 0,
        bool hasLinkedOffHandComponent = false,
        bool? visorState = null)
    {
        ArgumentNullException.ThrowIfNull(appearance);

        if (Session.State != BoutiqueSessionState.Active)
        {
            return Result.Failure(new BoutiqueError(
                BoutiqueErrorCode.InvalidGameState,
                "Equipment can only be previewed during an active Boutique session.",
                Session.State.ToString()));
        }

        if (hasLinkedOffHandComponent
            && slot is not (EquipmentSlot.MainHand or EquipmentSlot.OffHand))
        {
            return Result.Failure(new BoutiqueError(
                BoutiqueErrorCode.InvalidItem,
                "Only weapon items can include a linked off-hand component.",
                slot.ToString()));
        }

        var equipment = CreatePreviewEquipment(
            slot,
            ApplyDyeOverride(slot, appearance, dyeChannelCount),
            dyeChannelCount,
            displayName,
            iconId);
        var previousEquipment = CaptureEquipmentState();
        if (hasLinkedOffHandComponent)
        {
            var replaceResult = ReplaceEquipmentSlots(
                [
                    equipment with { Slot = EquipmentSlot.MainHand },
                    equipment with { Slot = EquipmentSlot.OffHand },
                ],
                [EquipmentSlot.MainHand, EquipmentSlot.OffHand]);
            if (replaceResult.IsSuccess)
            {
                PushEquipmentHistory(previousEquipment, previewVisorState);
            }

            return replaceResult;
        }

        if (IsWeaponSlot(slot)
            && previewEquipment.TryGetValue(slot, out var priorEquipment)
            && TryGetLinkedWeaponCounterpart(
                priorEquipment,
                out var linkedSlot,
                out _))
        {
            var replaceResult = ReplaceEquipmentSlots([equipment], [slot, linkedSlot]);
            if (replaceResult.IsSuccess)
            {
                PushEquipmentHistory(previousEquipment, previewVisorState);
            }

            return replaceResult;
        }

        var previousPreview = Session.PreviewAppearance;
        var applyResult = appearanceService.ApplyEquipment(slot, equipment.Appearance);
        if (!applyResult.IsSuccess)
        {
            return applyResult;
        }

        var previousVisorState = previewVisorState;
        if (slot == EquipmentSlot.Head && visorState.HasValue)
        {
            var visorResult = ApplyVisorState(visorState.Value);
            if (!visorResult.IsSuccess)
            {
                return RollBackMutation(previousPreview, visorResult.Error!);
            }
        }

        var captureResult = CaptureUpdatedPreview();
        if (!captureResult.IsSuccess)
        {
            return RollBackMutation(previousPreview, captureResult.Error!);
        }

        previewEquipment[slot] = equipment;
        if (slot == EquipmentSlot.Head && visorState.HasValue)
        {
            previewVisorState = visorState;
        }

        PushEquipmentHistory(previousEquipment, previousVisorState);
        return Result.Ok;
    }

    public Result UndoLastEquipmentPreview()
    {
        var validation = ValidateResetState();
        if (!validation.IsSuccess)
        {
            return validation;
        }

        if (equipmentPreviewHistory.Count == 0)
        {
            return Result.Failure(new BoutiqueError(
                BoutiqueErrorCode.InvalidGameState,
                "There is no earlier Boutique item selection to restore."));
        }

        var historyEntry = equipmentPreviewHistory[^1];
        var targetEquipment = historyEntry.Equipment;
        var previousPreview = Session.PreviewAppearance;
        var restoredEquipment = targetEquipment
            .Select(equipment => equipment with
            {
                Appearance = ApplyDyeOverride(
                    equipment.Slot,
                    equipment.Appearance,
                    equipment.DyeChannelCount),
            })
            .OrderBy(equipment => equipment.Slot)
            .ToArray();
        var restoredBySlot = restoredEquipment.ToDictionary(equipment => equipment.Slot);
        var changedSlots = previewEquipment.Keys
            .Union(restoredBySlot.Keys)
            .OrderBy(slot => slot)
            .Where(slot => !HasEquivalentAppearance(slot, restoredBySlot))
            .ToArray();

        foreach (var slot in changedSlots)
        {
            var appearance = restoredBySlot.TryGetValue(slot, out var equipment)
                ? equipment.Appearance
                : EmptyAppearance;
            var applyResult = appearanceService.ApplyEquipment(slot, appearance);
            if (!applyResult.IsSuccess)
            {
                return RollBackMutation(previousPreview, applyResult.Error!);
            }
        }

        var targetVisorState = historyEntry.VisorState;
        var visorChanged = targetVisorState.HasValue
            && targetVisorState != previewVisorState;
        if (visorChanged)
        {
            var visorResult = ApplyVisorState(targetVisorState.GetValueOrDefault());
            if (!visorResult.IsSuccess)
            {
                return RollBackMutation(previousPreview, visorResult.Error!);
            }
        }

        var updateResult = changedSlots.Length == 0 && !visorChanged
            ? Result.Ok
            : CaptureUpdatedPreview();
        if (!updateResult.IsSuccess)
        {
            return RollBackMutation(previousPreview, updateResult.Error!);
        }

        previewEquipment.Clear();
        foreach (var equipment in restoredEquipment)
        {
            previewEquipment.Add(equipment.Slot, equipment);
        }

        previewVisorState = historyEntry.VisorState;

        equipmentPreviewHistory.RemoveAt(equipmentPreviewHistory.Count - 1);
        return Result.Ok;
    }

    private bool HasEquivalentAppearance(
        EquipmentSlot slot,
        Dictionary<EquipmentSlot, PreviewEquipmentState> targetEquipment)
    {
        var hasCurrent = previewEquipment.TryGetValue(slot, out var current);
        var hasTarget = targetEquipment.TryGetValue(slot, out var target);
        if (!hasCurrent || !hasTarget)
        {
            return hasCurrent == hasTarget;
        }

        return AreEquivalentAppearances(current!.Appearance, target!.Appearance);
    }

    private static bool AreEquivalentAppearances(
        AppearanceSelection current,
        AppearanceSelection target)
        => current.AppearanceId == target.AppearanceId
            && current.SourceItemId == target.SourceItemId
            && GetStain(current, 0) == GetStain(target, 0)
            && GetStain(current, 1) == GetStain(target, 1);

    private static StainId GetStain(AppearanceSelection appearance, int channel)
        => channel < appearance.Stains.Length
            ? appearance.Stains[channel]
            : StainId.None;

    public Result PreviewDye(EquipmentSlot slot, byte dyeChannel, StainId stain)
    {
        if (Session.State != BoutiqueSessionState.Active)
        {
            return Result.Failure(new BoutiqueError(
                BoutiqueErrorCode.InvalidGameState,
                "Dyes can only be previewed during an active Boutique session.",
                Session.State.ToString()));
        }

        if (!previewEquipment.TryGetValue(slot, out var equipment))
        {
            return Result.Failure(new BoutiqueError(
                BoutiqueErrorCode.InvalidGameState,
                "Select an item in this equipment slot before opening its dye channels.",
                slot.ToString()));
        }

        if (dyeChannel >= equipment.DyeChannelCount)
        {
            return Result.Failure(new BoutiqueError(
                BoutiqueErrorCode.InvalidDye,
                "The selected item does not support that dye channel.",
                $"Slot={slot}; Channel={dyeChannel + 1}; Available={equipment.DyeChannelCount}"));
        }

        var previousPreview = Session.PreviewAppearance;
        var updatedAppearance = equipment.Appearance.WithStain(dyeChannel, stain);
        var applyResult = appearanceService.ApplyDye(slot, updatedAppearance);
        if (!applyResult.IsSuccess)
        {
            return applyResult;
        }

        PreviewEquipmentState? linkedEquipment = null;
        if (TryGetLinkedWeaponCounterpart(equipment, out var linkedSlot, out var linkedState))
        {
            var linkedApplyResult = appearanceService.ApplyDye(linkedSlot, updatedAppearance);
            if (!linkedApplyResult.IsSuccess)
            {
                return RollBackMutation(previousPreview, linkedApplyResult.Error!);
            }

            linkedEquipment = linkedState with { Appearance = updatedAppearance };
        }

        var captureResult = CaptureUpdatedPreview();
        if (!captureResult.IsSuccess)
        {
            return RollBackMutation(previousPreview, captureResult.Error!);
        }

        previewEquipment[slot] = equipment with { Appearance = updatedAppearance };
        UpdateDyeOverride(slot, dyeChannel, stain);
        if (linkedEquipment is not null)
        {
            previewEquipment[linkedEquipment.Slot] = linkedEquipment;
            UpdateDyeOverride(linkedEquipment.Slot, dyeChannel, stain);
        }

        return Result.Ok;
    }

    public bool TryGetPreviewEquipment(EquipmentSlot slot, out PreviewEquipmentState equipment)
        => previewEquipment.TryGetValue(slot, out equipment!);

    public Result ClearEquipment(EquipmentSlot slot)
    {
        var validation = ValidateResetState();
        if (!validation.IsSuccess)
        {
            return validation;
        }

        var previousEquipment = CaptureEquipmentState();
        var previousPreview = Session.PreviewAppearance;
        var clearResult = appearanceService.ApplyEquipment(
            slot,
            AppearanceSelection.WithoutStains(AppearanceId.None, 0));
        if (!clearResult.IsSuccess)
        {
            return clearResult;
        }

        var captureResult = CaptureUpdatedPreview();
        if (!captureResult.IsSuccess)
        {
            return RollBackMutation(previousPreview, captureResult.Error!);
        }

        previewEquipment.Remove(slot);
        PushEquipmentHistory(previousEquipment, previewVisorState);
        return Result.Ok;
    }

    public Result ResetSlot(EquipmentSlot slot)
    {
        var validation = ValidateResetState();
        if (!validation.IsSuccess)
        {
            return validation;
        }

        var resetSlots = new HashSet<EquipmentSlot> { slot };
        if (previewEquipment.TryGetValue(slot, out var resetEquipment)
            && TryGetLinkedWeaponCounterpart(resetEquipment, out var linkedSlot, out _))
        {
            resetSlots.Add(linkedSlot);
        }

        var original = Session.OriginalAppearance!;
        var previousPreview = Session.PreviewAppearance;
        var remainingEquipment = previewEquipment.Values
            .Where(equipment => !resetSlots.Contains(equipment.Slot))
            .OrderBy(equipment => equipment.Slot)
            .ToList();
        foreach (var resetSlot in resetSlots)
        {
            if (originalEquipment.TryGetValue(resetSlot, out var originalEquipmentState))
            {
                remainingEquipment.Add(originalEquipmentState with
                {
                    Appearance = ApplyDyeOverride(
                        resetSlot,
                        originalEquipmentState.Appearance,
                        originalEquipmentState.DyeChannelCount),
                });
            }
        }

        var resetResult = appearanceService.RevertToGame();
        if (!resetResult.IsSuccess)
        {
            return resetResult;
        }

        foreach (var equipment in remainingEquipment.OrderBy(equipment => equipment.Slot))
        {
            var replayResult = appearanceService.ApplyEquipment(equipment.Slot, equipment.Appearance);
            if (!replayResult.IsSuccess)
            {
                return RollBackMutation(previousPreview, replayResult.Error!);
            }
        }

        foreach (var clearedSlot in GetClearedOriginalSlots(remainingEquipment))
        {
            var clearResult = appearanceService.ApplyEquipment(clearedSlot, EmptyAppearance);
            if (!clearResult.IsSuccess)
            {
                return RollBackMutation(previousPreview, clearResult.Error!);
            }
        }

        var restoredVisorState = resetSlots.Contains(EquipmentSlot.Head)
            ? null
            : previewVisorState;
        if (restoredVisorState.HasValue)
        {
            var visorResult = ApplyVisorState(restoredVisorState.Value);
            if (!visorResult.IsSuccess)
            {
                return RollBackMutation(previousPreview, visorResult.Error!);
            }
        }

        Result updateResult;
        if (remainingEquipment.Count == 0)
        {
            updateResult = Session.UpdatePreview(original);
        }
        else
        {
            updateResult = CaptureUpdatedPreview();
            if (!updateResult.IsSuccess)
            {
                return RollBackMutation(previousPreview, updateResult.Error!);
            }
        }

        if (updateResult.IsSuccess)
        {
            foreach (var resetSlot in resetSlots)
            {
                previewEquipment.Remove(resetSlot);
                var replacement = remainingEquipment.FirstOrDefault(item => item.Slot == resetSlot);
                if (replacement is not null)
                {
                    previewEquipment[resetSlot] = replacement;
                }
            }

            equipmentPreviewHistory.Clear();
            previewVisorState = restoredVisorState;
        }

        return updateResult;
    }

    public Result ResetAll()
    {
        var validation = ValidateResetState();
        if (!validation.IsSuccess)
        {
            return validation;
        }

        var original = Session.OriginalAppearance!;
        var resetResult = appearanceService.RevertToGame();
        if (!resetResult.IsSuccess)
        {
            return resetResult;
        }

        var restoredEquipment = originalEquipment.Values
            .OrderBy(equipment => equipment.Slot)
            .ToArray();
        var updateResult = Session.UpdatePreview(original);
        if (updateResult.IsSuccess)
        {
            previewEquipment.Clear();
            foreach (var equipment in restoredEquipment)
            {
                previewEquipment[equipment.Slot] = equipment;
            }

            equipmentPreviewHistory.Clear();
            previewVisorState = null;
        }

        return updateResult;
    }

    public Result RefreshSlots()
    {
        var relinquishResult = RelinquishPreviewForSynchronization();
        if (!relinquishResult.IsSuccess)
        {
            return relinquishResult;
        }

        return CaptureSynchronizedGameState();
    }

    public Result RelinquishPreviewForSynchronization()
    {
        var validation = ValidateResetState();
        return validation.IsSuccess
            ? appearanceService.RevertToGame()
            : validation;
    }

    public Result CaptureSynchronizedGameState()
    {
        var validation = ValidateResetState();
        if (!validation.IsSuccess)
        {
            return validation;
        }

        var captureResult = appearanceService.CapturePlayerAppearance();
        if (!captureResult.IsSuccess || captureResult.Value is null)
        {
            return Result.Failure(captureResult.Error ?? new BoutiqueError(
                BoutiqueErrorCode.AppearanceCaptureFailed,
                "The current game-state equipment could not be captured after releasing the Boutique preview."));
        }

        var refreshResult = Session.RefreshBaseline(captureResult.Value);
        if (refreshResult.IsSuccess)
        {
            LoadCapturedEquipment(captureResult.Value);
            equipmentPreviewHistory.Clear();
            previewVisorState = null;
        }

        return refreshResult;
    }

    public Result ClearAllDyeSlots()
    {
        var validation = ValidateResetState();
        if (!validation.IsSuccess)
        {
            return validation;
        }

        var previousPreview = Session.PreviewAppearance;
        var previousEquipment = CaptureEquipmentState();
        var previousDyeOverrides = dyeOverrides.ToArray();
        var channels = dyeOverrides.Keys
            .OrderBy(key => key.Slot)
            .ThenBy(key => key.Channel)
            .ToArray();
        foreach (var (slot, channel) in channels)
        {
            // Clearing one half of a linked weapon pair clears the counterpart too.
            // Skip that snapshot entry once the shared individual path removes it.
            if (!dyeOverrides.ContainsKey((slot, channel)))
            {
                continue;
            }

            // Overrides survive an equipment-card clear so the next compatible item
            // can inherit them. There is no live item to mutate in that case; the
            // successful transaction-wide dictionary clear below removes them.
            if (!previewEquipment.TryGetValue(slot, out var activeEquipment)
                || channel >= activeEquipment.DyeChannelCount)
            {
                continue;
            }

            var clearResult = PreviewDye(slot, channel, StainId.None);
            if (!clearResult.IsSuccess)
            {
                previewEquipment.Clear();
                foreach (var equipment in previousEquipment)
                {
                    previewEquipment[equipment.Slot] = equipment;
                }

                RestoreDyeOverrides(previousDyeOverrides);
                if (previousPreview is not null)
                {
                    Session.UpdatePreview(previousPreview);
                    return RollBackMutation(previousPreview, clearResult.Error!);
                }

                return clearResult;
            }
        }

        // PreviewDye clears every channel that is currently visible in the Wardrobe.
        // A user override can also belong to a slot whose equipment card was cleared,
        // so remove those dormant overrides only after every live channel succeeded.
        dyeOverrides.Clear();

        return Result.Ok;
    }

    public Result TransferPreviewToCurrentActor()
    {
        var validation = ValidateResetState();
        if (!validation.IsSuccess)
        {
            return validation;
        }

        var original = Session.OriginalAppearance!;
        var targetEquipment = previewEquipment.Values
            .OrderBy(equipment => equipment.Slot)
            .ToArray();
        var resetResult = appearanceService.RevertToGame();
        if (!resetResult.IsSuccess)
        {
            return resetResult;
        }

        foreach (var equipment in targetEquipment)
        {
            var applyResult = appearanceService.ApplyEquipment(equipment.Slot, equipment.Appearance);
            if (!applyResult.IsSuccess)
            {
                return CleanTransferFailure(applyResult.Error!);
            }
        }

        foreach (var clearedSlot in GetClearedOriginalSlots(targetEquipment))
        {
            var clearResult = appearanceService.ApplyEquipment(clearedSlot, EmptyAppearance);
            if (!clearResult.IsSuccess)
            {
                return CleanTransferFailure(clearResult.Error!);
            }
        }

        if (targetEquipment.Length == 0)
        {
            return Session.UpdatePreview(original);
        }

        var captureResult = CaptureUpdatedPreview();
        return captureResult.IsSuccess
            ? captureResult
            : CleanTransferFailure(captureResult.Error!);
    }

    public Result ApplyLoadout(BoutiqueLoadout loadout)
    {
        ArgumentNullException.ThrowIfNull(loadout);
        var validation = ValidateResetState();
        if (!validation.IsSuccess)
        {
            return validation;
        }

        var loadoutValidation = LoadoutValidation.Validate(loadout);
        if (!loadoutValidation.IsSuccess)
        {
            return loadoutValidation;
        }

        var previousPreview = Session.PreviewAppearance;
        var resetResult = appearanceService.RevertToGame();
        if (!resetResult.IsSuccess)
        {
            return resetResult;
        }

        var loadoutEquipment = loadout.Equipment
            .Select(equipment => equipment.ToPreview())
            .OrderBy(equipment => equipment.Slot)
            .ToArray();
        foreach (var equipment in loadoutEquipment)
        {
            var applyResult = appearanceService.ApplyEquipment(equipment.Slot, equipment.Appearance);
            if (!applyResult.IsSuccess)
            {
                return RollBackMutation(previousPreview, applyResult.Error!);
            }
        }

        var captureResult = CaptureUpdatedPreviewSnapshot();
        if (!captureResult.IsSuccess || captureResult.Value is null)
        {
            return RollBackMutation(previousPreview, captureResult.Error!);
        }

        AdoptActiveEquipment(captureResult.Value, loadoutEquipment);

        equipmentPreviewHistory.Clear();

        return Result.Ok;
    }

    private static PreviewEquipmentState CreatePreviewEquipment(
        EquipmentSlot slot,
        AppearanceSelection appearance,
        byte dyeChannelCount,
        string? displayName,
        uint iconId)
        => new(
            slot,
            appearance,
            Math.Min(dyeChannelCount, (byte)2),
            string.IsNullOrWhiteSpace(displayName)
                ? $"Item {appearance.SourceItemId?.ToString(CultureInfo.InvariantCulture) ?? appearance.AppearanceId.Value.ToString(CultureInfo.InvariantCulture)}"
                : displayName,
            iconId);

    private void LoadCapturedEquipment(AppearanceSnapshot snapshot)
    {
        originalEquipment.Clear();
        previewEquipment.Clear();
        dyeOverrides.Clear();
        if (snapshot.Equipment.IsDefaultOrEmpty)
        {
            return;
        }

        foreach (var captured in snapshot.Equipment.Where(item => item.ItemId > 0))
        {
            var appearance = new AppearanceSelection(
                new AppearanceId(captured.ItemId),
                captured.ItemId,
                NormalizeStains(captured.Stains));
            var equipment = itemRepository is not null
                && itemRepository.TryGetItem(captured.ItemId, captured.Slot, out var item)
                    ? CreatePreviewEquipment(
                        captured.Slot,
                        appearance,
                        item.DyeChannelCount,
                        item.Name,
                        item.IconId)
                    : CreatePreviewEquipment(
                        captured.Slot,
                        appearance,
                        2,
                        $"Item {captured.ItemId.ToString(CultureInfo.InvariantCulture)}",
                        0);
            originalEquipment[captured.Slot] = equipment;
            previewEquipment[captured.Slot] = equipment;
            SeedActiveDyeState(equipment);
        }
    }

    private void SeedActiveDyeState(PreviewEquipmentState equipment)
    {
        for (byte channel = 0; channel < equipment.DyeChannelCount; channel++)
        {
            var stain = equipment.GetStain(channel);
            if (stain != StainId.None)
            {
                dyeOverrides[(equipment.Slot, channel)] = stain;
            }
        }
    }

    private void AdoptActiveEquipment(
        AppearanceSnapshot activeAppearance,
        IReadOnlyCollection<PreviewEquipmentState> requestedEquipment)
    {
        var priorEquipment = previewEquipment.Values.ToArray();
        previewEquipment.Clear();
        dyeOverrides.Clear();

        foreach (var captured in activeAppearance.Equipment
                     .Where(item => item.ItemId > 0)
                     .OrderBy(item => item.Slot))
        {
            var metadata = FindMatchingMetadata(captured, requestedEquipment)
                ?? FindMatchingMetadata(captured, priorEquipment)
                ?? FindMatchingMetadata(captured, originalEquipment.Values);
            var appearance = new AppearanceSelection(
                new AppearanceId(captured.ItemId),
                captured.ItemId,
                NormalizeStains(captured.Stains));
            PreviewEquipmentState activeEquipment;
            if (itemRepository is not null
                && itemRepository.TryGetItem(captured.ItemId, captured.Slot, out var item))
            {
                activeEquipment = CreatePreviewEquipment(
                    captured.Slot,
                    appearance,
                    item.DyeChannelCount,
                    item.Name,
                    item.IconId);
            }
            else if (metadata is not null)
            {
                activeEquipment = CreatePreviewEquipment(
                    captured.Slot,
                    appearance,
                    metadata.DyeChannelCount,
                    metadata.DisplayName,
                    metadata.IconId);
            }
            else
            {
                activeEquipment = CreatePreviewEquipment(
                    captured.Slot,
                    appearance,
                    2,
                    $"Item {captured.ItemId.ToString(CultureInfo.InvariantCulture)}",
                    0);
            }

            previewEquipment[captured.Slot] = activeEquipment;
            SeedActiveDyeState(activeEquipment);
        }
    }

    private static PreviewEquipmentState? FindMatchingMetadata(
        CapturedEquipmentState captured,
        IEnumerable<PreviewEquipmentState> candidates)
        => candidates.FirstOrDefault(candidate =>
            candidate.Slot == captured.Slot
            && candidate.Appearance.SourceItemId == captured.ItemId);

    private AppearanceSelection ApplyDyeOverride(
        EquipmentSlot slot,
        AppearanceSelection appearance,
        byte dyeChannelCount)
    {
        var firstOverride = StainId.None;
        var secondOverride = StainId.None;
        var hasFirstOverride = dyeChannelCount > 0
            && dyeOverrides.TryGetValue((slot, 0), out firstOverride);
        var hasSecondOverride = dyeChannelCount > 1
            && dyeOverrides.TryGetValue((slot, 1), out secondOverride);
        if (!hasFirstOverride && !hasSecondOverride)
        {
            return appearance;
        }

        var stains = NormalizeStains(appearance.Stains);
        var first = hasFirstOverride ? firstOverride : stains[0];
        var second = hasSecondOverride ? secondOverride : stains[1];
        return appearance with { Stains = [first, second] };
    }

    private void RestoreDyeOverrides(
        IEnumerable<KeyValuePair<(EquipmentSlot Slot, byte Channel), StainId>> overrides)
    {
        dyeOverrides.Clear();
        foreach (var entry in overrides)
        {
            dyeOverrides[entry.Key] = entry.Value;
        }
    }

    private void UpdateDyeOverride(EquipmentSlot slot, byte channel, StainId stain)
    {
        if (stain == StainId.None)
        {
            dyeOverrides.Remove((slot, channel));
            return;
        }

        dyeOverrides[(slot, channel)] = stain;
    }

    private static ImmutableArray<StainId> NormalizeStains(ImmutableArray<StainId> stains)
        =>
        [
            stains.Length > 0 ? stains[0] : StainId.None,
            stains.Length > 1 ? stains[1] : StainId.None,
        ];

    private Result ReplaceEquipmentSlots(
        IReadOnlyCollection<PreviewEquipmentState> replacements,
        IReadOnlyCollection<EquipmentSlot> replacedSlots)
    {
        var previousPreview = Session.PreviewAppearance;
        var targetEquipment = previewEquipment.Values
            .Where(equipment => !replacedSlots.Contains(equipment.Slot))
            .Concat(replacements)
            .OrderBy(equipment => equipment.Slot)
            .ToArray();

        var resetResult = appearanceService.RevertToGame();
        if (!resetResult.IsSuccess)
        {
            return resetResult;
        }

        foreach (var equipment in targetEquipment)
        {
            var applyResult = appearanceService.ApplyEquipment(equipment.Slot, equipment.Appearance);
            if (!applyResult.IsSuccess)
            {
                return RollBackMutation(previousPreview, applyResult.Error!);
            }
        }

        foreach (var clearedSlot in GetClearedOriginalSlots(targetEquipment))
        {
            var clearResult = appearanceService.ApplyEquipment(clearedSlot, EmptyAppearance);
            if (!clearResult.IsSuccess)
            {
                return RollBackMutation(previousPreview, clearResult.Error!);
            }
        }

        var captureResult = CaptureUpdatedPreview();
        if (!captureResult.IsSuccess)
        {
            return RollBackMutation(previousPreview, captureResult.Error!);
        }

        foreach (var replacedSlot in replacedSlots)
        {
            previewEquipment.Remove(replacedSlot);
        }

        foreach (var equipment in replacements)
        {
            previewEquipment[equipment.Slot] = equipment;
        }

        return Result.Ok;
    }

    private bool TryGetLinkedWeaponCounterpart(
        PreviewEquipmentState equipment,
        out EquipmentSlot linkedSlot,
        out PreviewEquipmentState linkedEquipment)
    {
        linkedSlot = equipment.Slot switch
        {
            EquipmentSlot.MainHand => EquipmentSlot.OffHand,
            EquipmentSlot.OffHand => EquipmentSlot.MainHand,
            _ => default,
        };
        if (!IsWeaponSlot(equipment.Slot)
            || !previewEquipment.TryGetValue(linkedSlot, out linkedEquipment!))
        {
            linkedEquipment = null!;
            return false;
        }

        return equipment.Appearance.AppearanceId == linkedEquipment.Appearance.AppearanceId
            && equipment.Appearance.SourceItemId == linkedEquipment.Appearance.SourceItemId;
    }

    private static bool IsWeaponSlot(EquipmentSlot slot)
        => slot is EquipmentSlot.MainHand or EquipmentSlot.OffHand;

    private static AppearanceSelection EmptyAppearance
        => AppearanceSelection.WithoutStains(AppearanceId.None, 0);

    private EquipmentSlot[] GetClearedOriginalSlots(
        IEnumerable<PreviewEquipmentState> targetEquipment)
    {
        var populatedSlots = targetEquipment
            .Select(equipment => equipment.Slot)
            .ToHashSet();
        return originalEquipment.Keys
            .Where(slot => !populatedSlots.Contains(slot))
            .OrderBy(slot => slot)
            .ToArray();
    }

    private ImmutableArray<PreviewEquipmentState> CaptureEquipmentState()
        => [.. previewEquipment.Values.OrderBy(equipment => equipment.Slot)];

    private void PushEquipmentHistory(
        ImmutableArray<PreviewEquipmentState> equipment,
        bool? visorState)
    {
        if (equipmentPreviewHistory.Count == EquipmentPreviewHistoryLimit)
        {
            equipmentPreviewHistory.RemoveAt(0);
        }

        equipmentPreviewHistory.Add(new EquipmentPreviewHistoryEntry(equipment, visorState));
    }

    private Result ApplyVisorState(bool enabled)
        => appearanceService is IVisorAppearanceService visorService
            ? visorService.SetVisorState(enabled)
            : Result.Failure(new BoutiqueError(
                BoutiqueErrorCode.AppearanceApplyFailed,
                "The active appearance service cannot set Visor State."));

    private Result CaptureUpdatedPreview()
    {
        var captureResult = CaptureUpdatedPreviewSnapshot();
        return captureResult.IsSuccess
            ? Result.Ok
            : Result.Failure(captureResult.Error!);
    }

    private Result<AppearanceSnapshot> CaptureUpdatedPreviewSnapshot()
    {
        var captureResult = appearanceService.CapturePlayerAppearance();
        if (!captureResult.IsSuccess || captureResult.Value is null)
        {
            return Result.Failure<AppearanceSnapshot>(captureResult.Error ?? new BoutiqueError(
                BoutiqueErrorCode.AppearanceCaptureFailed,
                "The preview was applied, but its resulting appearance could not be captured."));
        }

        var updateResult = Session.UpdatePreview(captureResult.Value);
        return updateResult.IsSuccess
            ? Result.Success(captureResult.Value)
            : Result.Failure<AppearanceSnapshot>(updateResult.Error!);
    }

    private Result ValidateResetState()
    {
        if (Session.State != BoutiqueSessionState.Active || Session.OriginalAppearance is null)
        {
            return Result.Failure(new BoutiqueError(
                BoutiqueErrorCode.InvalidGameState,
                "Outfit reset is only available during an active Boutique session.",
                Session.State.ToString()));
        }

        return Result.Ok;
    }

    private Result RollBackMutation(AppearanceSnapshot? previousPreview, BoutiqueError mutationError)
    {
        if (previousPreview is null)
        {
            return Result.Failure(mutationError);
        }

        var rollbackResult = appearanceService.ApplyAppearance(previousPreview);
        if (rollbackResult.IsSuccess)
        {
            return Result.Failure(mutationError);
        }

        return Result.Failure(new BoutiqueError(
            mutationError.Code,
            mutationError.Message,
            $"Mutation failed: {mutationError.TechnicalContext ?? mutationError.Code.ToString()}; preview rollback also failed: {rollbackResult.Error}",
            IsRecoverable: false));
    }

    private Result CleanTransferFailure(BoutiqueError transferError)
    {
        var cleanupResult = appearanceService.RevertToGame();
        if (cleanupResult.IsSuccess)
        {
            return Result.Failure(transferError);
        }

        return Result.Failure(new BoutiqueError(
            transferError.Code,
            transferError.Message,
            $"GPose actor handoff failed: {transferError.TechnicalContext ?? transferError.Code.ToString()}; game-state cleanup also failed: {cleanupResult.Error}",
            IsRecoverable: false));
    }

    public Result Close()
    {
        if (Session.State == BoutiqueSessionState.Closed)
        {
            return Result.Ok;
        }

        if ((Session.State is BoutiqueSessionState.Active
                or BoutiqueSessionState.Suspended
                or BoutiqueSessionState.DependencyUnavailable
                or BoutiqueSessionState.RestoreFailed)
            && Session.OriginalAppearance is not null)
        {
            var beginRestore = Session.BeginRestore();
            if (!beginRestore.IsSuccess)
            {
                return beginRestore;
            }

            var restoreResult = appearanceService.RestoreAppearance(Session.OriginalAppearance);
            if (!restoreResult.IsSuccess)
            {
                var error = restoreResult.Error ?? new BoutiqueError(
                    BoutiqueErrorCode.RestoreFailed,
                    "The original appearance could not be restored.",
                    IsRecoverable: false);
                Session.MarkRestoreFailed(error.Message);
                return Result.Failure(error);
            }
        }

        var closeResult = Session.CompleteClose("Boutique closed.");
        if (closeResult.IsSuccess)
        {
            originalEquipment.Clear();
            previewEquipment.Clear();
            dyeOverrides.Clear();
            equipmentPreviewHistory.Clear();
            previewVisorState = null;
        }

        return closeResult;
    }

    private sealed record EquipmentPreviewHistoryEntry(
        ImmutableArray<PreviewEquipmentState> Equipment,
        bool? VisorState);
}
