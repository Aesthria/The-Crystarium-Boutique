using CrystariumBoutique.Core.Errors;

namespace CrystariumBoutique.Integrations;

internal sealed class WardrobeAutoSyncCoordinator : IDisposable
{
    private readonly ILocalPlayerGearsetFinalizationSource source;
    private readonly Func<bool> canSynchronize;
    private readonly Func<bool> hasActivePreview;
    private readonly Func<Result> relinquishPreview;
    private readonly Func<Result> captureSynchronizedState;
    private readonly Func<Result> synchronizeImmediately;
    private long requestedGeneration;
    private long handledGeneration;
    private long awaitingRevertGeneration;
    private long captureReadyGeneration;
    private int synchronizing;
    private bool disposed;

    public WardrobeAutoSyncCoordinator(
        ILocalPlayerGearsetFinalizationSource source,
        Func<bool> canSynchronize,
        Func<bool> hasActivePreview,
        Func<Result> relinquishPreview,
        Func<Result> captureSynchronizedState,
        Func<Result> synchronizeImmediately)
    {
        this.source = source ?? throw new ArgumentNullException(nameof(source));
        this.canSynchronize = canSynchronize ?? throw new ArgumentNullException(nameof(canSynchronize));
        this.hasActivePreview = hasActivePreview ?? throw new ArgumentNullException(nameof(hasActivePreview));
        this.relinquishPreview = relinquishPreview ?? throw new ArgumentNullException(nameof(relinquishPreview));
        this.captureSynchronizedState = captureSynchronizedState
            ?? throw new ArgumentNullException(nameof(captureSynchronizedState));
        this.synchronizeImmediately = synchronizeImmediately
            ?? throw new ArgumentNullException(nameof(synchronizeImmediately));
        source.LocalPlayerGearsetFinalized += RequestSynchronization;
        source.LocalPlayerGameStateReverted += ConfirmPreviewRelinquished;
    }

    internal bool HasPendingRequest
        => Volatile.Read(ref requestedGeneration) > Volatile.Read(ref handledGeneration)
            || Volatile.Read(ref captureReadyGeneration) > 0;

    internal bool IsAwaitingPreviewRelinquish
        => Volatile.Read(ref awaitingRevertGeneration) > 0;

    public Result? ConsumePending()
    {
        if (disposed)
        {
            return null;
        }

        if (!canSynchronize())
        {
            CancelPending();
            return null;
        }

        if (Interlocked.CompareExchange(ref synchronizing, 1, 0) != 0)
        {
            return null;
        }

        try
        {
            var requested = Volatile.Read(ref requestedGeneration);
            var handled = Volatile.Read(ref handledGeneration);
            if (requested > handled)
            {
                // A newer finalized gearset always supersedes a capture made ready by
                // an older preview-relinquish cycle.
                Interlocked.Exchange(ref captureReadyGeneration, 0);
                if (!hasActivePreview())
                {
                    Interlocked.Exchange(ref awaitingRevertGeneration, 0);
                    Volatile.Write(ref handledGeneration, requested);
                    return synchronizeImmediately();
                }

                Volatile.Write(ref awaitingRevertGeneration, requested);
                Volatile.Write(ref handledGeneration, requested);
                var result = relinquishPreview();
                if (!result.IsSuccess)
                {
                    Interlocked.Exchange(ref awaitingRevertGeneration, 0);
                    Interlocked.Exchange(ref captureReadyGeneration, 0);
                    return result;
                }

                // Capture is deliberately deferred until a later safe UI pass, even
                // when Glamourer publishes its Revert finalization synchronously.
                return null;
            }

            var captureGeneration = Volatile.Read(ref captureReadyGeneration);
            if (captureGeneration == 0)
            {
                return null;
            }

            // A newer gearset request will be handled on the next pass rather than
            // committing a stale capture from the superseded generation.
            if (Volatile.Read(ref requestedGeneration) > captureGeneration)
            {
                Interlocked.Exchange(ref captureReadyGeneration, 0);
                return null;
            }

            Interlocked.Exchange(ref captureReadyGeneration, 0);
            Interlocked.Exchange(ref awaitingRevertGeneration, 0);
            return captureSynchronizedState();
        }
        finally
        {
            Volatile.Write(ref synchronizing, 0);
        }
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        CancelPending();
        source.LocalPlayerGameStateReverted -= ConfirmPreviewRelinquished;
        source.LocalPlayerGearsetFinalized -= RequestSynchronization;
    }

    private void RequestSynchronization()
    {
        if (!disposed && canSynchronize())
        {
            Interlocked.Increment(ref requestedGeneration);
        }
    }

    private void ConfirmPreviewRelinquished()
    {
        var awaiting = Volatile.Read(ref awaitingRevertGeneration);
        if (!disposed && awaiting > 0)
        {
            Volatile.Write(ref captureReadyGeneration, awaiting);
        }
    }

    private void CancelPending()
    {
        Volatile.Write(ref handledGeneration, Volatile.Read(ref requestedGeneration));
        Interlocked.Exchange(ref awaitingRevertGeneration, 0);
        Interlocked.Exchange(ref captureReadyGeneration, 0);
    }
}
