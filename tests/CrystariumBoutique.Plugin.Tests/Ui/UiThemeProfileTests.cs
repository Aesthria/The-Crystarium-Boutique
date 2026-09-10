using System.Text.Json;
using CrystariumBoutique.Configuration;
using CrystariumBoutique.Ui;

namespace CrystariumBoutiquePlugin.Tests.Ui;

public sealed class UiThemeProfileTests
{
    [Fact]
    public void FreshConfigurationDefaultsToCrystarium()
    {
        Assert.Equal(0, (int)UiThemeProfile.Default);
        Assert.Equal(1, (int)UiThemeProfile.Crystarium);
        Assert.Equal(UiThemeProfile.Crystarium, new PluginConfiguration().UiProfile);
    }

    [Fact]
    public void MissingSerializedProfileUsesCrystariumInitializer()
    {
        var restored = JsonSerializer.Deserialize<PluginConfiguration>("{}");

        Assert.NotNull(restored);
        Assert.Equal(UiThemeProfile.Crystarium, restored.UiProfile);
    }

    [Fact]
    public void InvalidSerializedProfileFallsBackToCrystarium()
        => Assert.Equal(
            UiThemeProfile.Crystarium,
            ConfigurationStore.NormalizeUiProfile((UiThemeProfile)999));

    [Theory]
    [InlineData(UiThemeProfile.Default)]
    [InlineData(UiThemeProfile.Crystarium)]
    [InlineData(UiThemeProfile.SimpleCrystarium)]
    public void EveryExistingValidProfileIsPreserved(UiThemeProfile profile)
    {
        var configuration = new PluginConfiguration { UiProfile = profile };
        var json = JsonSerializer.Serialize(configuration);
        var restored = JsonSerializer.Deserialize<PluginConfiguration>(json);

        Assert.NotNull(restored);
        Assert.Equal(profile, ConfigurationStore.NormalizeUiProfile(restored.UiProfile));
    }

    [Fact]
    public void SimpleCrystariumProfilePersistsThroughConfigurationRoundTrip()
    {
        var configuration = new PluginConfiguration
        {
            UiProfile = UiThemeProfile.SimpleCrystarium,
        };

        var json = JsonSerializer.Serialize(configuration);
        var restored = JsonSerializer.Deserialize<PluginConfiguration>(json);

        Assert.NotNull(restored);
        Assert.Equal(UiThemeProfile.SimpleCrystarium, restored.UiProfile);
    }

    [Fact]
    public void SimpleCrystariumUsesBackdropButNotOrnateLayoutBranch()
    {
        var configuration = new PluginConfiguration
        {
            UiProfile = UiThemeProfile.SimpleCrystarium,
        };

        Assert.True(BoutiqueTheme.UsesCrystariumBackdrop(configuration));
        Assert.True(BoutiqueTheme.IsSimpleCrystariumProfile(configuration));
        Assert.False(BoutiqueTheme.IsCrystariumProfile(configuration));
        Assert.Equal(2f, BoutiqueTheme.GetCardBorderThickness(configuration));
        Assert.Equal(BoutiqueTheme.ControlRounding, BoutiqueTheme.GetCardRounding(configuration));
    }

    [Fact]
    public void SimpleCrystariumDoesNotRecolorWardrobeOrSaveButtons()
    {
        var simple = new PluginConfiguration { UiProfile = UiThemeProfile.Default };
        var simpleCrystarium = new PluginConfiguration { UiProfile = UiThemeProfile.SimpleCrystarium };

        Assert.Equal(
            BoutiqueTheme.GetWardrobeButtonColor(simple),
            BoutiqueTheme.GetWardrobeButtonColor(simpleCrystarium));
        Assert.Equal(
            BoutiqueTheme.GetWardrobeButtonTextColor(simple),
            BoutiqueTheme.GetWardrobeButtonTextColor(simpleCrystarium));
        Assert.Equal(
            BoutiqueTheme.GetSaveButtonColor(simple),
            BoutiqueTheme.GetSaveButtonColor(simpleCrystarium));
        Assert.Equal(
            BoutiqueTheme.GetSaveButtonTextColor(simple),
            BoutiqueTheme.GetSaveButtonTextColor(simpleCrystarium));
    }

    [Fact]
    public void SimpleCrystariumUsesTransparentParentAndTileContainersOnlyForItsNewPath()
    {
        var simple = new PluginConfiguration { UiProfile = UiThemeProfile.Default };
        var crystarium = new PluginConfiguration { UiProfile = UiThemeProfile.Crystarium };
        var simpleCrystarium = new PluginConfiguration { UiProfile = UiThemeProfile.SimpleCrystarium };

        Assert.False(BoutiqueTheme.UsesTransparentParentContent(simple));
        Assert.False(BoutiqueTheme.UsesTransparentParentContent(crystarium));
        Assert.True(BoutiqueTheme.UsesTransparentParentContent(simpleCrystarium));
        Assert.False(BoutiqueTheme.UsesTransparentItemTileContainer(simple));
        Assert.True(BoutiqueTheme.UsesTransparentItemTileContainer(crystarium));
        Assert.True(BoutiqueTheme.UsesTransparentItemTileContainer(simpleCrystarium));
    }

    [Fact]
    public void SimpleCrystariumReusesConfiguredSimpleGoldForTextBordersAndRules()
    {
        var configuration = new PluginConfiguration
        {
            UiProfile = UiThemeProfile.SimpleCrystarium,
            BoutiqueButtonTextColorRed = 255,
            BoutiqueButtonTextColorGreen = 212,
            BoutiqueButtonTextColorBlue = 141,
        };
        var gold = new System.Numerics.Vector4(1f, 212f / 255f, 141f / 255f, 1f);

        Assert.Equal(gold, BoutiqueTheme.GetSimpleThemeGoldColor(configuration));
        Assert.Equal(gold, BoutiqueTheme.GetBoutiqueTabTextColor(configuration));
        Assert.Equal(gold, BoutiqueTheme.GetFilterTextColor(configuration));
        Assert.Equal(gold, BoutiqueTheme.GetItemBorderColor(configuration));
        Assert.Equal(gold, BoutiqueTheme.GetPanelBorderColor(configuration));
        Assert.Equal(gold, BoutiqueTheme.GetHorizontalRuleColor(configuration));
    }

    [Fact]
    public void SimpleCrystariumWardrobeParentCanBeTransparentWhileCardsRemainSolid()
    {
        var configuration = new PluginConfiguration
        {
            UiProfile = UiThemeProfile.SimpleCrystarium,
        };

        var card = BoutiqueTheme.GetWardrobeCardBackground(
            configuration,
            selected: false,
            System.Numerics.Vector4.Zero);

        Assert.True(BoutiqueTheme.UsesTransparentParentContent(configuration));
        Assert.True(card.W >= 0.9f);
    }

    [Fact]
    public void CrystariumThemesUseDoubleBoutiquePanelBorderThickness()
    {
        var simple = new PluginConfiguration { UiProfile = UiThemeProfile.Default };
        var crystarium = new PluginConfiguration { UiProfile = UiThemeProfile.Crystarium };
        var simpleCrystarium = new PluginConfiguration { UiProfile = UiThemeProfile.SimpleCrystarium };

        Assert.Equal(1f, BoutiqueTheme.GetBoutiquePanelBorderThickness(simple));
        Assert.Equal(2f, BoutiqueTheme.GetBoutiquePanelBorderThickness(crystarium));
        Assert.Equal(2f, BoutiqueTheme.GetBoutiquePanelBorderThickness(simpleCrystarium));
    }

    [Fact]
    public void OriginalCrystariumWardrobeLineworkUsesItsConfiguredFontGold()
    {
        var configuration = new PluginConfiguration
        {
            UiProfile = UiThemeProfile.Crystarium,
            BoutiqueButtonTextColorRed = 255,
            BoutiqueButtonTextColorGreen = 212,
            BoutiqueButtonTextColorBlue = 141,
        };
        var fontGold = BoutiqueTheme.GetBoutiqueTabTextColor(configuration);

        Assert.Equal(fontGold, BoutiqueTheme.GetWardrobeCardBorderColor(configuration));
        Assert.Equal(fontGold, BoutiqueTheme.GetWardrobeCardDividerColor(configuration));
    }

    [Fact]
    public void WardrobeGoldCorrectionDoesNotChangeOtherThemeColorPaths()
    {
        var simple = new PluginConfiguration { UiProfile = UiThemeProfile.Default };
        var simpleCrystarium = new PluginConfiguration { UiProfile = UiThemeProfile.SimpleCrystarium };

        Assert.Equal(
            BoutiqueTheme.GetItemBorderColor(simple),
            BoutiqueTheme.GetWardrobeCardBorderColor(simple));
        Assert.Equal(
            BoutiqueTheme.GetDecorativeRuleColor(simple),
            BoutiqueTheme.GetWardrobeCardDividerColor(simple));
        Assert.Equal(
            BoutiqueTheme.GetItemBorderColor(simpleCrystarium),
            BoutiqueTheme.GetWardrobeCardBorderColor(simpleCrystarium));
        Assert.Equal(
            BoutiqueTheme.GetDecorativeRuleColor(simpleCrystarium),
            BoutiqueTheme.GetWardrobeCardDividerColor(simpleCrystarium));
    }

    [Fact]
    public void ExistingSimpleConfiguredColorsRemainUnchanged()
    {
        var configuration = new PluginConfiguration
        {
            UiProfile = UiThemeProfile.Default,
            BoutiqueButtonColorRed = 17,
            BoutiqueButtonColorGreen = 34,
            BoutiqueButtonColorBlue = 51,
            FilterTextColorRed = 68,
            FilterTextColorGreen = 85,
            FilterTextColorBlue = 102,
        };

        Assert.Equal(new System.Numerics.Vector4(17f / 255f, 34f / 255f, 51f / 255f, 1f),
            BoutiqueTheme.GetBoutiqueButtonColor(configuration));
        Assert.Equal(new System.Numerics.Vector4(68f / 255f, 85f / 255f, 102f / 255f, 1f),
            BoutiqueTheme.GetFilterTextColor(configuration));
    }
}
