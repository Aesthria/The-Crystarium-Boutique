using CrystariumBoutique.Core.Errors;
using CrystariumBoutique.Core.Integrations;

namespace CrystariumBoutique.Core.Tests.Integrations;

public sealed class AppearanceTargetResolverTests
{
    [Fact]
    public void NormalWorldUsesLocalPlayerObjectIndex()
    {
        var result = AppearanceTargetResolver.Resolve(false, 214);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value);
    }

    [Fact]
    public void GposeUsesSupportedTargetObjectIndex()
    {
        var result = AppearanceTargetResolver.Resolve(true, 214);

        Assert.True(result.IsSuccess);
        Assert.Equal(214, result.Value);
    }

    [Fact]
    public void GposeUsesDiscoveredLocalCloneWithoutManualTarget()
    {
        var result = AppearanceTargetResolver.Resolve(
            isGposing: true,
            gposeTargetObjectIndex: null,
            discoveredLocalPlayerObjectIndex: 214);

        Assert.True(result.IsSuccess);
        Assert.Equal(214, result.Value);
    }

    [Fact]
    public void GposePrefersValidSelectedLocalTargetOverDiscoveredClone()
    {
        var result = AppearanceTargetResolver.Resolve(
            isGposing: true,
            gposeTargetObjectIndex: 220,
            isLocalPlayerTarget: true,
            discoveredLocalPlayerObjectIndex: 214);

        Assert.True(result.IsSuccess);
        Assert.Equal(220, result.Value);
    }

    [Fact]
    public void GposeUsesDiscoveredLocalCloneWhenSelectedTargetIsSomeoneElse()
    {
        var result = AppearanceTargetResolver.Resolve(
            isGposing: true,
            gposeTargetObjectIndex: 220,
            isLocalPlayerTarget: false,
            discoveredLocalPlayerObjectIndex: 214);

        Assert.True(result.IsSuccess);
        Assert.Equal(214, result.Value);
    }

    [Fact]
    public void GposeWithoutATargetFailsSafely()
    {
        var result = AppearanceTargetResolver.Resolve(true, null);

        Assert.False(result.IsSuccess);
        Assert.Equal(BoutiqueErrorCode.PlayerUnavailable, result.Error?.Code);
    }

    [Fact]
    public void GposeDoesNotMistakeNormalIndexZeroForTheCloneDuringTransition()
    {
        var result = AppearanceTargetResolver.Resolve(true, 0);

        Assert.False(result.IsSuccess);
        Assert.Equal(BoutiqueErrorCode.PlayerUnavailable, result.Error?.Code);
    }

    [Fact]
    public void GposeRejectsATargetThatIsNotTheLocalPlayer()
    {
        var result = AppearanceTargetResolver.Resolve(true, 214, false);

        Assert.False(result.IsSuccess);
        Assert.Equal(BoutiqueErrorCode.InvalidGameState, result.Error?.Code);
    }

    [Theory]
    [InlineData(true, false, 214, 0)]
    [InlineData(true, true, 214, 214)]
    [InlineData(false, false, 0, 0)]
    public void RestoreTargetMovesAnEndedGposeSessionBackToTheLocalPlayer(
        bool boundInGpose,
        bool isCurrentlyGposing,
        int boundObjectIndex,
        int expectedObjectIndex)
        => Assert.Equal(
            expectedObjectIndex,
            AppearanceTargetResolver.ResolveRestoreTarget(
                boundInGpose,
                isCurrentlyGposing,
                boundObjectIndex));

    [Fact]
    public void GposeCloneWithoutPlayerSubtypeMatchesTheLocalPlayerByName()
        => Assert.True(AppearanceTargetResolver.MatchesLocalPlayer(
            "Fixture Character",
            "Fixture Character",
            targetHomeWorldId: null,
            localPlayerHomeWorldId: 42));

    [Fact]
    public void PlayerTargetWithDifferentHomeWorldIsRejected()
        => Assert.False(AppearanceTargetResolver.MatchesLocalPlayer(
            "Fixture Character",
            "Fixture Character",
            targetHomeWorldId: 41,
            localPlayerHomeWorldId: 42));

    [Fact]
    public void DifferentlyNamedGposeTargetIsRejectedWithoutWorldMetadata()
        => Assert.False(AppearanceTargetResolver.MatchesLocalPlayer(
            "Another Character",
            "Fixture Character",
            targetHomeWorldId: null,
            localPlayerHomeWorldId: 42));
}
