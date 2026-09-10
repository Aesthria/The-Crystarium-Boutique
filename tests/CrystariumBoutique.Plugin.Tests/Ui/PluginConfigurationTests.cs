using System.Text.Json;
using CrystariumBoutique.Configuration;

namespace CrystariumBoutiquePlugin.Tests.Ui;

public sealed class PluginConfigurationTests
{
    [Fact]
    public void FreshConfigurationEnablesAutomaticWardrobeSync()
        => Assert.True(new PluginConfiguration().AutomaticallySyncWardrobe);

    [Fact]
    public void MissingSerializedSettingUsesEnabledInitializer()
    {
        var restored = JsonSerializer.Deserialize<PluginConfiguration>("{\"Version\":25}");

        Assert.NotNull(restored);
        Assert.True(restored.AutomaticallySyncWardrobe);
    }

    [Fact]
    public void ExistingConfigurationReceivesEnabledMigrationDefault()
        => Assert.True(ConfigurationStore.NormalizeAutomaticWardrobeSync(25, enabled: false));

    [Fact]
    public void CurrentConfigurationPreservesDisabledChoice()
        => Assert.False(ConfigurationStore.NormalizeAutomaticWardrobeSync(26, enabled: false));

    [Fact]
    public void MigrationEnablesAutomaticSyncWithoutChangingExistingPreferences()
    {
        const string json = """
            {
              "Version": 25,
              "DisableInCombat": false,
              "WardrobeVerticalLayout": true,
              "InterfaceScalePercent": 142,
              "ShowWelcomeGuideOnOpen": false
            }
            """;
        var restored = JsonSerializer.Deserialize<PluginConfiguration>(json)!;

        restored.AutomaticallySyncWardrobe = ConfigurationStore.NormalizeAutomaticWardrobeSync(
            restored.Version,
            restored.AutomaticallySyncWardrobe);

        Assert.True(restored.AutomaticallySyncWardrobe);
        Assert.False(restored.DisableInCombat);
        Assert.True(restored.WardrobeVerticalLayout);
        Assert.Equal(142, restored.InterfaceScalePercent);
        Assert.False(restored.ShowWelcomeGuideOnOpen);
    }
}
