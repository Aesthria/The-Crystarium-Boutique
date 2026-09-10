using System.Text.Json;
using CrystariumBoutique.Configuration;
using CrystariumBoutique.Ui;

namespace CrystariumBoutiquePlugin.Tests.Ui;

public sealed class FavoriteIndicatorTests
{
    [Fact]
    public void BadgeFollowsFavoriteState()
    {
        Assert.True(BoutiqueTheme.ShouldDrawFavoriteBadge(true));
        Assert.False(BoutiqueTheme.ShouldDrawFavoriteBadge(false));
    }

    [Fact]
    public void FavoriteFrameIsDisabledByDefaultAndRequiresFavoriteState()
    {
        var configuration = new PluginConfiguration();

        Assert.False(configuration.HighlightFavoriteItemFrames);
        Assert.False(BoutiqueTheme.ShouldDrawFavoriteFrame(configuration, true));

        configuration.HighlightFavoriteItemFrames = true;

        Assert.True(BoutiqueTheme.ShouldDrawFavoriteFrame(configuration, true));
        Assert.False(BoutiqueTheme.ShouldDrawFavoriteFrame(configuration, false));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FavoriteFrameToggleAndColorSurviveConfigurationRoundTrip(bool enabled)
    {
        var configuration = new PluginConfiguration
        {
            HighlightFavoriteItemFrames = enabled,
            FavoriteFrameColorRed = 17,
            FavoriteFrameColorGreen = 34,
            FavoriteFrameColorBlue = 51,
        };

        var json = JsonSerializer.Serialize(configuration);
        var restored = JsonSerializer.Deserialize<PluginConfiguration>(json);

        Assert.NotNull(restored);
        Assert.Equal(enabled, restored.HighlightFavoriteItemFrames);
        Assert.Equal(17, restored.FavoriteFrameColorRed);
        Assert.Equal(34, restored.FavoriteFrameColorGreen);
        Assert.Equal(51, restored.FavoriteFrameColorBlue);
    }

    [Fact]
    public void MissingFavoriteFrameToggleDefaultsOff()
    {
        var restored = JsonSerializer.Deserialize<PluginConfiguration>("{}");

        Assert.NotNull(restored);
        Assert.False(restored.HighlightFavoriteItemFrames);
    }
}
