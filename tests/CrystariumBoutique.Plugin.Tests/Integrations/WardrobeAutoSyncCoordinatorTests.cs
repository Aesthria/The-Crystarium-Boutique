using CrystariumBoutique.Core.Errors;
using CrystariumBoutique.Integrations;

namespace CrystariumBoutiquePlugin.Tests.Integrations;

public sealed class WardrobeAutoSyncCoordinatorTests
{
    [Fact]
    public void FinalizedGearsetWithoutPreviewSynchronizesOnceOnSafeUpdate()
    {
        var source = new FakeSource();
        var synchronizations = 0;
        using var coordinator = CreateCoordinator(
            source,
            hasActivePreview: () => false,
            synchronizeImmediately: () =>
            {
                synchronizations++;
                return Result.Ok;
            });

        source.RaiseGearsetFinalized();
        source.RaiseGearsetFinalized();

        Assert.True(coordinator.HasPendingRequest);
        Assert.True(coordinator.ConsumePending()?.IsSuccess);
        Assert.Equal(1, synchronizations);
        Assert.False(coordinator.HasPendingRequest);
    }

    [Fact]
    public void ActivePreviewWaitsForExactRevertFinalizationBeforeCapture()
    {
        var source = new FakeSource();
        var relinquishments = 0;
        var captures = 0;
        using var coordinator = CreateCoordinator(
            source,
            hasActivePreview: () => true,
            relinquishPreview: () =>
            {
                relinquishments++;
                source.RaiseGameStateReverted();
                return Result.Ok;
            },
            captureSynchronizedState: () =>
            {
                captures++;
                return Result.Ok;
            });

        source.RaiseGearsetFinalized();

        Assert.Null(coordinator.ConsumePending());
        Assert.Equal(1, relinquishments);
        Assert.Equal(0, captures);
        Assert.True(coordinator.IsAwaitingPreviewRelinquish);
        Assert.True(coordinator.HasPendingRequest);

        Assert.True(coordinator.ConsumePending()?.IsSuccess);
        Assert.Equal(1, captures);
        Assert.False(coordinator.IsAwaitingPreviewRelinquish);
    }

    [Fact]
    public void ActivePreviewIsNeverCapturedWithoutRevertFinalization()
    {
        var source = new FakeSource();
        var captures = 0;
        using var coordinator = CreateCoordinator(
            source,
            hasActivePreview: () => true,
            captureSynchronizedState: () =>
            {
                captures++;
                return Result.Ok;
            });

        source.RaiseGearsetFinalized();
        coordinator.ConsumePending();
        coordinator.ConsumePending();

        Assert.True(coordinator.IsAwaitingPreviewRelinquish);
        Assert.Equal(0, captures);
    }

    [Fact]
    public void RevertFinalizationWithoutPendingGearsetDoesNotCapture()
    {
        var source = new FakeSource();
        var captures = 0;
        using var coordinator = CreateCoordinator(
            source,
            hasActivePreview: () => true,
            captureSynchronizedState: () =>
            {
                captures++;
                return Result.Ok;
            });

        source.RaiseGameStateReverted();

        Assert.Null(coordinator.ConsumePending());
        Assert.Equal(0, captures);
    }

    [Fact]
    public void DisabledOrInactiveSynchronizationDoesNotRelinquishPreview()
    {
        var source = new FakeSource();
        var enabled = false;
        var relinquishments = 0;
        using var coordinator = CreateCoordinator(
            source,
            canSynchronize: () => enabled,
            hasActivePreview: () => true,
            relinquishPreview: () =>
            {
                relinquishments++;
                return Result.Ok;
            });

        source.RaiseGearsetFinalized();
        enabled = true;
        coordinator.ConsumePending();

        Assert.Equal(0, relinquishments);
        Assert.False(coordinator.HasPendingRequest);
    }

    [Fact]
    public void RapidGearsetChangesWhilePreviewActiveCaptureLatestStateOnly()
    {
        var source = new FakeSource();
        var latestGearset = 1;
        var capturedGearsets = new List<int>();
        using var coordinator = CreateCoordinator(
            source,
            hasActivePreview: () => true,
            relinquishPreview: () =>
            {
                source.RaiseGameStateReverted();
                return Result.Ok;
            },
            captureSynchronizedState: () =>
            {
                capturedGearsets.Add(latestGearset);
                return Result.Ok;
            });

        source.RaiseGearsetFinalized();
        coordinator.ConsumePending();
        latestGearset = 2;
        source.RaiseGearsetFinalized();
        latestGearset = 3;
        source.RaiseGearsetFinalized();
        coordinator.ConsumePending();
        coordinator.ConsumePending();

        Assert.Equal([3], capturedGearsets);
    }

    [Fact]
    public void NotificationRaisedDuringImmediateSynchronizationIsDeferredWithoutRecursion()
    {
        var source = new FakeSource();
        var synchronizations = 0;
        using var coordinator = CreateCoordinator(
            source,
            hasActivePreview: () => false,
            synchronizeImmediately: () =>
            {
                synchronizations++;
                if (synchronizations == 1)
                {
                    source.RaiseGearsetFinalized();
                }

                return Result.Ok;
            });

        source.RaiseGearsetFinalized();
        coordinator.ConsumePending();

        Assert.Equal(1, synchronizations);
        Assert.True(coordinator.HasPendingRequest);
        coordinator.ConsumePending();
        Assert.Equal(2, synchronizations);
    }

    [Fact]
    public void DisposalUnsubscribesBothEventsAndDropsPendingWork()
    {
        var source = new FakeSource();
        var coordinator = CreateCoordinator(source, hasActivePreview: () => true);
        source.RaiseGearsetFinalized();

        coordinator.Dispose();
        source.RaiseGearsetFinalized();
        source.RaiseGameStateReverted();

        Assert.Equal(1, source.GearsetRemoveCount);
        Assert.Equal(1, source.RevertRemoveCount);
        Assert.False(coordinator.HasPendingRequest);
        Assert.Null(coordinator.ConsumePending());
    }

    private static WardrobeAutoSyncCoordinator CreateCoordinator(
        FakeSource source,
        Func<bool>? canSynchronize = null,
        Func<bool>? hasActivePreview = null,
        Func<Result>? relinquishPreview = null,
        Func<Result>? captureSynchronizedState = null,
        Func<Result>? synchronizeImmediately = null)
        => new(
            source,
            canSynchronize ?? (() => true),
            hasActivePreview ?? (() => false),
            relinquishPreview ?? (() => Result.Ok),
            captureSynchronizedState ?? (() => Result.Ok),
            synchronizeImmediately ?? (() => Result.Ok));

    private sealed class FakeSource : ILocalPlayerGearsetFinalizationSource
    {
        private Action? gearsetFinalized;
        private Action? gameStateReverted;

        public int GearsetRemoveCount { get; private set; }

        public int RevertRemoveCount { get; private set; }

        public event Action? LocalPlayerGearsetFinalized
        {
            add => gearsetFinalized += value;
            remove
            {
                gearsetFinalized -= value;
                GearsetRemoveCount++;
            }
        }

        public event Action? LocalPlayerGameStateReverted
        {
            add => gameStateReverted += value;
            remove
            {
                gameStateReverted -= value;
                RevertRemoveCount++;
            }
        }

        public void RaiseGearsetFinalized()
            => gearsetFinalized?.Invoke();

        public void RaiseGameStateReverted()
            => gameStateReverted?.Invoke();
    }
}
