using CrystariumBoutique.Core.Appearance;
using CrystariumBoutique.Core.Errors;

namespace CrystariumBoutique.Core.Sessions;

public sealed class BoutiqueSession
{
    private const int MaximumTransitionHistory = 32;
    private readonly Queue<SessionTransition> transitionHistory = new(MaximumTransitionHistory);

    public BoutiqueSessionState State { get; private set; } = BoutiqueSessionState.Closed;

    public Guid? SessionId { get; private set; }

    public AppearanceSnapshot? OriginalAppearance { get; private set; }

    public AppearanceSnapshot? PreviewAppearance { get; private set; }

    public bool IsDirty { get; private set; }

    public IReadOnlyCollection<SessionTransition> TransitionHistory => transitionHistory;

    public Result Begin()
    {
        if (State != BoutiqueSessionState.Closed)
        {
            return InvalidTransition(BoutiqueSessionState.Initializing, "A Boutique session is already open.");
        }

        SessionId = Guid.NewGuid();
        OriginalAppearance = null;
        PreviewAppearance = null;
        IsDirty = false;
        return TransitionTo(BoutiqueSessionState.Initializing, "Session requested.");
    }

    public Result WaitForCharacter()
        => TransitionFrom(
            BoutiqueSessionState.Initializing,
            BoutiqueSessionState.WaitingForCharacter,
            "Waiting for a usable local player character.");

    public Result Activate(AppearanceSnapshot originalAppearance)
    {
        ArgumentNullException.ThrowIfNull(originalAppearance);

        if (State is not (BoutiqueSessionState.Initializing or BoutiqueSessionState.WaitingForCharacter))
        {
            return InvalidTransition(BoutiqueSessionState.Active, "The session is not ready to become active.");
        }

        OriginalAppearance = originalAppearance;
        PreviewAppearance = originalAppearance;
        IsDirty = false;
        return TransitionTo(BoutiqueSessionState.Active, "Original appearance captured.");
    }

    public Result MarkDependencyUnavailable(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        if (State is not (BoutiqueSessionState.Initializing or BoutiqueSessionState.WaitingForCharacter or BoutiqueSessionState.Active))
        {
            return InvalidTransition(BoutiqueSessionState.DependencyUnavailable, reason);
        }

        return TransitionTo(BoutiqueSessionState.DependencyUnavailable, reason);
    }

    public Result UpdatePreview(AppearanceSnapshot previewAppearance)
    {
        ArgumentNullException.ThrowIfNull(previewAppearance);

        if (State != BoutiqueSessionState.Active)
        {
            return InvalidTransition(BoutiqueSessionState.Active, "Preview state can only change during an active session.");
        }

        PreviewAppearance = previewAppearance;
        IsDirty = !Equals(OriginalAppearance, PreviewAppearance);
        return Result.Ok;
    }

    public Result RefreshBaseline(AppearanceSnapshot currentAppearance)
    {
        ArgumentNullException.ThrowIfNull(currentAppearance);

        if (State != BoutiqueSessionState.Active)
        {
            return InvalidTransition(
                BoutiqueSessionState.Active,
                "The Boutique baseline can only be refreshed during an active session.");
        }

        OriginalAppearance = currentAppearance;
        PreviewAppearance = currentAppearance;
        IsDirty = false;
        return Result.Ok;
    }

    public Result BeginRestore()
    {
        if (State is not (BoutiqueSessionState.Active
                or BoutiqueSessionState.Suspended
                or BoutiqueSessionState.DependencyUnavailable
                or BoutiqueSessionState.RestoreFailed))
        {
            return InvalidTransition(BoutiqueSessionState.Restoring, "The current session state cannot be restored.");
        }

        return TransitionTo(BoutiqueSessionState.Restoring, "Restoration requested.");
    }

    public Result MarkRestoreFailed(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        return TransitionFrom(BoutiqueSessionState.Restoring, BoutiqueSessionState.RestoreFailed, reason);
    }

    public Result CompleteClose(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        if (State is BoutiqueSessionState.Active && IsDirty)
        {
            return Result.Failure(new BoutiqueError(
                BoutiqueErrorCode.InvalidGameState,
                "A dirty active session must restore or commit before closing.",
                State.ToString()));
        }

        if (State == BoutiqueSessionState.Closed)
        {
            return Result.Ok;
        }

        var result = TransitionTo(BoutiqueSessionState.Closed, reason);
        if (result.IsSuccess)
        {
            SessionId = null;
            OriginalAppearance = null;
            PreviewAppearance = null;
            IsDirty = false;
        }

        return result;
    }

    private Result TransitionFrom(BoutiqueSessionState expected, BoutiqueSessionState destination, string reason)
        => State == expected
            ? TransitionTo(destination, reason)
            : InvalidTransition(destination, reason);

    private Result TransitionTo(BoutiqueSessionState destination, string reason)
    {
        var transition = new SessionTransition(State, destination, DateTimeOffset.UtcNow, reason);
        State = destination;

        if (transitionHistory.Count == MaximumTransitionHistory)
        {
            transitionHistory.Dequeue();
        }

        transitionHistory.Enqueue(transition);
        return Result.Ok;
    }

    private Result InvalidTransition(BoutiqueSessionState destination, string reason)
        => Result.Failure(new BoutiqueError(
            BoutiqueErrorCode.InvalidGameState,
            $"Cannot transition a Boutique session from {State} to {destination}.",
            reason));
}
