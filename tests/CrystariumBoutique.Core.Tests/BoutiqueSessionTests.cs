using CrystariumBoutique.Core.Appearance;
using CrystariumBoutique.Core.Errors;
using CrystariumBoutique.Core.Sessions;

namespace CrystariumBoutique.Core.Tests;

public sealed class BoutiqueSessionTests
{
    private static readonly AppearanceSnapshot Original = AppearanceSnapshot.Create("original", "test");
    private static readonly AppearanceSnapshot Preview = AppearanceSnapshot.Create("preview", "test");

    [Fact]
    public void BeginCreatesInitializingSession()
    {
        var session = new BoutiqueSession();

        var result = session.Begin();

        Assert.True(result.IsSuccess);
        Assert.Equal(BoutiqueSessionState.Initializing, session.State);
        Assert.NotNull(session.SessionId);
        Assert.Single(session.TransitionHistory);
    }

    [Fact]
    public void BeginRejectsOverlappingSession()
    {
        var session = new BoutiqueSession();
        session.Begin();

        var result = session.Begin();

        Assert.False(result.IsSuccess);
        Assert.Equal(BoutiqueErrorCode.InvalidGameState, result.Error?.Code);
        Assert.Equal(BoutiqueSessionState.Initializing, session.State);
    }

    [Fact]
    public void ActivateCapturesOriginalAndStartsClean()
    {
        var session = new BoutiqueSession();
        session.Begin();

        var result = session.Activate(Original);

        Assert.True(result.IsSuccess);
        Assert.Equal(BoutiqueSessionState.Active, session.State);
        Assert.Equal(Original, session.OriginalAppearance);
        Assert.Equal(Original, session.PreviewAppearance);
        Assert.False(session.IsDirty);
    }

    [Fact]
    public void UpdatePreviewMarksSessionDirty()
    {
        var session = ActiveSession();

        var result = session.UpdatePreview(Preview);

        Assert.True(result.IsSuccess);
        Assert.True(session.IsDirty);
        Assert.Equal(Preview, session.PreviewAppearance);
    }

    [Fact]
    public void RefreshBaselineReplacesOriginalAndPreviewWithCurrentAppearance()
    {
        var session = ActiveSession();
        session.UpdatePreview(Preview);
        var refreshed = AppearanceSnapshot.Create("refreshed", "test");

        var result = session.RefreshBaseline(refreshed);

        Assert.True(result.IsSuccess);
        Assert.Equal(refreshed, session.OriginalAppearance);
        Assert.Equal(refreshed, session.PreviewAppearance);
        Assert.False(session.IsDirty);
    }

    [Fact]
    public void DirtySessionCannotCloseWithoutRestoreOrCommit()
    {
        var session = ActiveSession();
        session.UpdatePreview(Preview);

        var result = session.CompleteClose("Window closed.");

        Assert.False(result.IsSuccess);
        Assert.Equal(BoutiqueSessionState.Active, session.State);
    }

    [Fact]
    public void TransitionHistoryIsBounded()
    {
        var session = new BoutiqueSession();

        for (var index = 0; index < 40; index++)
        {
            session.Begin();
            session.MarkDependencyUnavailable("Expected test dependency state.");
            session.CompleteClose("Test cycle complete.");
        }

        Assert.Equal(32, session.TransitionHistory.Count);
    }

    private static BoutiqueSession ActiveSession()
    {
        var session = new BoutiqueSession();
        session.Begin();
        session.Activate(Original);
        return session;
    }
}
