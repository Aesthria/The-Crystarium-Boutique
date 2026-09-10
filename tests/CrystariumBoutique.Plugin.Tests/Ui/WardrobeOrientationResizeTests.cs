using System.Numerics;
using CrystariumBoutique.Ui;

namespace CrystariumBoutiquePlugin.Tests.Ui;

public sealed class WardrobeOrientationResizeTests
{
    private static readonly Vector2 UnrestrictedMinimum = Vector2.Zero;
    private static readonly Vector2 UnrestrictedMaximum = new(float.MaxValue);

    [Theory]
    [InlineData(420f, 760f, 760f, 420f)]
    [InlineData(900f, 500f, 500f, 900f)]
    public void SwapAndClampRotatesDimensions(
        float width,
        float height,
        float expectedWidth,
        float expectedHeight)
    {
        var result = WardrobeOrientationResize.SwapAndClamp(
            new Vector2(width, height),
            UnrestrictedMinimum,
            UnrestrictedMaximum);

        Assert.Equal(new Vector2(expectedWidth, expectedHeight), result);
    }

    [Fact]
    public void UnchangedOrientationDoesNotQueueResize()
    {
        var resize = new WardrobeOrientationResize();

        Assert.False(resize.Request(
            currentVertical: true,
            requestedVertical: true,
            new Vector2(420f, 760f),
            UnrestrictedMinimum,
            UnrestrictedMaximum));
        Assert.False(resize.TryConsume(out _));
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void OrientationTransitionQueuesExactlyOneResize(
        bool currentVertical,
        bool requestedVertical)
    {
        var resize = new WardrobeOrientationResize();

        Assert.True(resize.Request(
            currentVertical,
            requestedVertical,
            new Vector2(420f, 760f),
            UnrestrictedMinimum,
            UnrestrictedMaximum));
        Assert.True(resize.TryConsume(out var pending));
        Assert.Equal(new Vector2(760f, 420f), pending);
        Assert.False(resize.TryConsume(out _));
    }

    [Fact]
    public void LatestManualSizeIsUsedForNextTransition()
    {
        var resize = new WardrobeOrientationResize();
        resize.Request(
            currentVertical: true,
            requestedVertical: false,
            new Vector2(420f, 760f),
            UnrestrictedMinimum,
            UnrestrictedMaximum);
        Assert.True(resize.TryConsume(out _));

        resize.Request(
            currentVertical: false,
            requestedVertical: true,
            new Vector2(950f, 540f),
            UnrestrictedMinimum,
            UnrestrictedMaximum);

        Assert.True(resize.TryConsume(out var pending));
        Assert.Equal(new Vector2(540f, 950f), pending);
    }

    [Fact]
    public void SwappedSizeUsesExistingConstraints()
    {
        var result = WardrobeOrientationResize.SwapAndClamp(
            new Vector2(420f, 360f),
            new Vector2(420f, 360f),
            new Vector2(1000f, 1000f));

        Assert.Equal(new Vector2(420f, 420f), result);
    }

    [Fact]
    public void ActualWindowSizeIsConvertedToDalamudLogicalSizeBeforeSwap()
    {
        var logical = WardrobeOrientationResize.ToLogicalSize(
            new Vector2(840f, 1520f),
            globalScale: 2f);

        Assert.Equal(new Vector2(420f, 760f), logical);
        Assert.Equal(
            new Vector2(760f, 420f),
            WardrobeOrientationResize.SwapAndClamp(
                logical,
                UnrestrictedMinimum,
                UnrestrictedMaximum));
    }
}
