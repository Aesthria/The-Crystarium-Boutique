using System.Numerics;
using CrystariumBoutique.Ui;

namespace CrystariumBoutiquePlugin.Tests.Ui;

public sealed class WelcomeGuideLayoutTests
{
    [Fact]
    public void NormalWorkAreaUsesFullPreferredHeight()
    {
        var bounds = WelcomeGuideLayout.CalculateBounds(new Vector2(1920f, 1080f), 1f);

        Assert.Equal(WelcomeGuideLayout.PreferredWindowSize, bounds.OpeningSize);
        Assert.True(bounds.OpeningSize.Y > 620f);
        Assert.False(bounds.UsesConstrainedHeightFallback);
    }

    [Fact]
    public void ConstrainedWorkAreaClampsHeightAndLeavesScrollingFallbackAvailable()
    {
        var bounds = WelcomeGuideLayout.CalculateBounds(new Vector2(800f, 600f), 1f);

        Assert.Equal(552f, bounds.OpeningSize.Y);
        Assert.Equal(bounds.OpeningSize.Y, bounds.MaximumSize.Y);
        Assert.True(bounds.UsesConstrainedHeightFallback);
    }

    [Fact]
    public void WorkAreaIsConvertedToLogicalSizeBeforeClamping()
    {
        var bounds = WelcomeGuideLayout.CalculateBounds(new Vector2(1280f, 720f), 2f);

        Assert.Equal(new Vector2(592f, 312f), bounds.OpeningSize);
        Assert.Equal(bounds.MaximumSize, bounds.OpeningSize);
        Assert.True(bounds.MinimumSize.X <= bounds.MaximumSize.X);
        Assert.True(bounds.MinimumSize.Y <= bounds.MaximumSize.Y);
    }
}
