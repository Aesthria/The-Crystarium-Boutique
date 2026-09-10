using Dalamud.Plugin;

namespace CrystariumBoutique.Configuration;

public sealed class ConfigurationStore
{
    private readonly IDalamudPluginInterface pluginInterface;

    public ConfigurationStore(IDalamudPluginInterface pluginInterface)
    {
        this.pluginInterface = pluginInterface ?? throw new ArgumentNullException(nameof(pluginInterface));
        Current = pluginInterface.GetPluginConfig() as PluginConfiguration ?? new PluginConfiguration();
        if (Current.Version < 4)
        {
            Current.PluginTransparencyPercent = 0;
            Current.DisableInCombat = true;
        }

        if (Current.Version < 5)
        {
            Current.BackgroundTransparencyPercent = 0;
        }

        if (Current.Version < 6)
        {
            Current.AppearanceGridColumns = 6;
            if (Current.AppearanceGridRows == 2)
            {
                Current.AppearanceGridRows = 3;
            }
        }

        if (Current.Version < 7)
        {
            Current.AppearanceGridColumns = 6;
        }

        if (Current.Version < 8)
        {
            Current.WardrobeVerticalLayout = false;
        }

        if (Current.Version < 9)
        {
            Current.UseCustomBackgroundColor = false;
            Current.BackgroundColorRed = 31;
            Current.BackgroundColorGreen = 34;
            Current.BackgroundColorBlue = 45;
        }

        if (Current.Version < 10)
        {
            Current.ItemBorderColorRed = 153;
            Current.ItemBorderColorGreen = 140;
            Current.ItemBorderColorBlue = 87;
        }

        if (Current.Version < 11)
        {
            Current.TooltipsRequireShift = false;
            Current.HorizontalRuleColorRed = 87;
            Current.HorizontalRuleColorGreen = 136;
            Current.HorizontalRuleColorBlue = 153;
            Current.BoutiqueButtonColorRed = 125;
            Current.BoutiqueButtonColorGreen = 69;
            Current.BoutiqueButtonColorBlue = 87;
            Current.BoutiqueButtonTextColorRed = 255;
            Current.BoutiqueButtonTextColorGreen = 255;
            Current.BoutiqueButtonTextColorBlue = 255;
            Current.FilterBackgroundColorRed = 77;
            Current.FilterBackgroundColorGreen = 72;
            Current.FilterBackgroundColorBlue = 75;
            Current.FilterTextColorRed = 255;
            Current.FilterTextColorGreen = 255;
            Current.FilterTextColorBlue = 255;
        }

        if (Current.Version < 12)
        {
            Current.UiProfile = UiThemeProfile.Default;
        }

        if (Current.Version < 13)
        {
            Current.SelectedItemHighlightColorRed = 122;
            Current.SelectedItemHighlightColorGreen = 214;
            Current.SelectedItemHighlightColorBlue = 255;
        }

        if (Current.Version < 14)
        {
            Current.UseCustomWardrobeButtonColor = false;
            Current.WardrobeButtonColorRed = 87;
            Current.WardrobeButtonColorGreen = 136;
            Current.WardrobeButtonColorBlue = 153;
            Current.UseCustomWardrobeButtonTextColor = false;
            Current.WardrobeButtonTextColorRed = 255;
            Current.WardrobeButtonTextColorGreen = 255;
            Current.WardrobeButtonTextColorBlue = 255;
            Current.UseCustomSaveButtonColor = false;
            Current.SaveButtonColorRed = 87;
            Current.SaveButtonColorGreen = 153;
            Current.SaveButtonColorBlue = 131;
            Current.UseCustomSaveButtonTextColor = false;
            Current.SaveButtonTextColorRed = 255;
            Current.SaveButtonTextColorGreen = 255;
            Current.SaveButtonTextColorBlue = 255;
        }

        if (Current.Version < 15)
        {
            Current.SelectedItemHighlightTransparencyPercent = 0;
        }

        if (Current.Version < 16)
        {
            Current.TooltipFontScalePercent = Math.Max(Current.TooltipFontScalePercent, 100);
            Current.TooltipAcquisitionDetail = Core.Catalog.ItemAcquisitionDetailLevel.Standard;
            Current.TooltipShowVendors = true;
            Current.TooltipShowMarketBoard = true;
            Current.TooltipShowCrafting = true;
            Current.TooltipShowQuests = true;
            Current.TooltipShowAchievements = true;
            Current.TooltipShowSeasonalEvents = true;
            Current.TooltipShowOnlineStore = true;
            Current.TooltipShowDutyDrops = true;
            Current.TooltipShowSharedModels = true;
            Current.TooltipSharedModelLimit = 8;
        }

        if (Current.Version < 17)
        {
            Current.CrystariumBackgroundDarknessPercent = 0;
        }

        if (Current.Version < 18)
        {
            Current.InterfaceScalePercent = PluginConfiguration.DefaultInterfaceScalePercent;
        }

        if (Current.Version < 19)
        {
            // Preserve established custom values while replacing the former zero-value defaults.
            if (Current.BackgroundTransparencyPercent == 0)
            {
                Current.BackgroundTransparencyPercent
                    = PluginConfiguration.DefaultBackgroundTransparencyPercent;
            }

            if (Current.CrystariumBackgroundDarknessPercent == 0)
            {
                Current.CrystariumBackgroundDarknessPercent
                    = PluginConfiguration.DefaultCrystariumBackgroundDarknessPercent;
            }

            if (Current.SelectedItemHighlightColorRed == 122
                && Current.SelectedItemHighlightColorGreen == 214
                && Current.SelectedItemHighlightColorBlue == 255
                && Current.SelectedItemHighlightTransparencyPercent == 0)
            {
                Current.SelectedItemHighlightColorRed
                    = PluginConfiguration.DefaultHighlightColorRed;
                Current.SelectedItemHighlightColorGreen
                    = PluginConfiguration.DefaultHighlightColorGreen;
                Current.SelectedItemHighlightColorBlue
                    = PluginConfiguration.DefaultHighlightColorBlue;
                Current.SelectedItemHighlightTransparencyPercent
                    = PluginConfiguration.DefaultHighlightTransparencyPercent;
            }
        }

        if (Current.Version < 20)
        {
            if (Current.HorizontalRuleColorRed == 87
                && Current.HorizontalRuleColorGreen == 136
                && Current.HorizontalRuleColorBlue == 153)
            {
                Current.HorizontalRuleColorRed
                    = PluginConfiguration.DefaultHorizontalRuleColorRed;
                Current.HorizontalRuleColorGreen
                    = PluginConfiguration.DefaultHorizontalRuleColorGreen;
                Current.HorizontalRuleColorBlue
                    = PluginConfiguration.DefaultHorizontalRuleColorBlue;
            }

            if (Current.BoutiqueButtonTextColorRed == 255
                && Current.BoutiqueButtonTextColorGreen == 255
                && Current.BoutiqueButtonTextColorBlue == 255)
            {
                Current.BoutiqueButtonTextColorRed
                    = PluginConfiguration.DefaultBoutiqueTextColorRed;
                Current.BoutiqueButtonTextColorGreen
                    = PluginConfiguration.DefaultBoutiqueTextColorGreen;
                Current.BoutiqueButtonTextColorBlue
                    = PluginConfiguration.DefaultBoutiqueTextColorBlue;
            }
        }

        if (Current.Version < 21
            && Current.BoutiqueButtonTextColorRed == 153
            && Current.BoutiqueButtonTextColorGreen == 140
            && Current.BoutiqueButtonTextColorBlue == 87)
        {
            Current.BoutiqueButtonTextColorRed
                = PluginConfiguration.DefaultBoutiqueTextColorRed;
            Current.BoutiqueButtonTextColorGreen
                = PluginConfiguration.DefaultBoutiqueTextColorGreen;
            Current.BoutiqueButtonTextColorBlue
                = PluginConfiguration.DefaultBoutiqueTextColorBlue;
        }

        if (Current.Version < 22)
        {
            Current.ShowWelcomeGuideOnOpen = true;
        }

        Current.Favorites ??= new Core.Favorites.FavoriteCatalog();
        Current.Favorites.Normalize();

        if (Current.Version < 24)
        {
            Current.FavoriteFrameColorRed = PluginConfiguration.DefaultFavoriteFrameColorRed;
            Current.FavoriteFrameColorGreen = PluginConfiguration.DefaultFavoriteFrameColorGreen;
            Current.FavoriteFrameColorBlue = PluginConfiguration.DefaultFavoriteFrameColorBlue;
        }

        if (Current.Version < 25)
        {
            Current.HighlightFavoriteItemFrames = false;
        }

        Current.AutomaticallySyncWardrobe = NormalizeAutomaticWardrobeSync(
            Current.Version,
            Current.AutomaticallySyncWardrobe);

        Current.UiProfile = NormalizeUiProfile(Current.UiProfile);

        Current.TooltipTransparencyPercent = Math.Clamp(Current.TooltipTransparencyPercent, 0, 100);
        Current.TooltipFontScalePercent = Math.Clamp(Current.TooltipFontScalePercent, 75, 200);
        if (!Enum.IsDefined(Current.TooltipAcquisitionDetail))
        {
            Current.TooltipAcquisitionDetail = Core.Catalog.ItemAcquisitionDetailLevel.Standard;
        }

        Current.TooltipSharedModelLimit = Math.Clamp(Current.TooltipSharedModelLimit, 1, 24);
        Current.PluginTransparencyPercent = Math.Clamp(Current.PluginTransparencyPercent, 0, 90);
        Current.BackgroundTransparencyPercent = Math.Clamp(Current.BackgroundTransparencyPercent, 0, 100);
        Current.CrystariumBackgroundDarknessPercent = Math.Clamp(
            Current.CrystariumBackgroundDarknessPercent,
            0,
            100);
        Current.InterfaceScalePercent = Math.Clamp(Current.InterfaceScalePercent, 75, 200);
        Current.BackgroundColorRed = Math.Clamp(Current.BackgroundColorRed, 0, 255);
        Current.BackgroundColorGreen = Math.Clamp(Current.BackgroundColorGreen, 0, 255);
        Current.BackgroundColorBlue = Math.Clamp(Current.BackgroundColorBlue, 0, 255);
        Current.ItemBorderColorRed = Math.Clamp(Current.ItemBorderColorRed, 0, 255);
        Current.ItemBorderColorGreen = Math.Clamp(Current.ItemBorderColorGreen, 0, 255);
        Current.ItemBorderColorBlue = Math.Clamp(Current.ItemBorderColorBlue, 0, 255);
        Current.SelectedItemHighlightColorRed = Math.Clamp(Current.SelectedItemHighlightColorRed, 0, 255);
        Current.SelectedItemHighlightColorGreen = Math.Clamp(Current.SelectedItemHighlightColorGreen, 0, 255);
        Current.SelectedItemHighlightColorBlue = Math.Clamp(Current.SelectedItemHighlightColorBlue, 0, 255);
        Current.SelectedItemHighlightTransparencyPercent = Math.Clamp(
            Current.SelectedItemHighlightTransparencyPercent,
            0,
            100);
        Current.FavoriteFrameColorRed = Math.Clamp(Current.FavoriteFrameColorRed, 0, 255);
        Current.FavoriteFrameColorGreen = Math.Clamp(Current.FavoriteFrameColorGreen, 0, 255);
        Current.FavoriteFrameColorBlue = Math.Clamp(Current.FavoriteFrameColorBlue, 0, 255);
        Current.HorizontalRuleColorRed = Math.Clamp(Current.HorizontalRuleColorRed, 0, 255);
        Current.HorizontalRuleColorGreen = Math.Clamp(Current.HorizontalRuleColorGreen, 0, 255);
        Current.HorizontalRuleColorBlue = Math.Clamp(Current.HorizontalRuleColorBlue, 0, 255);
        Current.BoutiqueButtonColorRed = Math.Clamp(Current.BoutiqueButtonColorRed, 0, 255);
        Current.BoutiqueButtonColorGreen = Math.Clamp(Current.BoutiqueButtonColorGreen, 0, 255);
        Current.BoutiqueButtonColorBlue = Math.Clamp(Current.BoutiqueButtonColorBlue, 0, 255);
        Current.BoutiqueButtonTextColorRed = Math.Clamp(Current.BoutiqueButtonTextColorRed, 0, 255);
        Current.BoutiqueButtonTextColorGreen = Math.Clamp(Current.BoutiqueButtonTextColorGreen, 0, 255);
        Current.BoutiqueButtonTextColorBlue = Math.Clamp(Current.BoutiqueButtonTextColorBlue, 0, 255);
        Current.FilterBackgroundColorRed = Math.Clamp(Current.FilterBackgroundColorRed, 0, 255);
        Current.FilterBackgroundColorGreen = Math.Clamp(Current.FilterBackgroundColorGreen, 0, 255);
        Current.FilterBackgroundColorBlue = Math.Clamp(Current.FilterBackgroundColorBlue, 0, 255);
        Current.FilterTextColorRed = Math.Clamp(Current.FilterTextColorRed, 0, 255);
        Current.FilterTextColorGreen = Math.Clamp(Current.FilterTextColorGreen, 0, 255);
        Current.FilterTextColorBlue = Math.Clamp(Current.FilterTextColorBlue, 0, 255);
        Current.WardrobeButtonColorRed = Math.Clamp(Current.WardrobeButtonColorRed, 0, 255);
        Current.WardrobeButtonColorGreen = Math.Clamp(Current.WardrobeButtonColorGreen, 0, 255);
        Current.WardrobeButtonColorBlue = Math.Clamp(Current.WardrobeButtonColorBlue, 0, 255);
        Current.WardrobeButtonTextColorRed = Math.Clamp(Current.WardrobeButtonTextColorRed, 0, 255);
        Current.WardrobeButtonTextColorGreen = Math.Clamp(Current.WardrobeButtonTextColorGreen, 0, 255);
        Current.WardrobeButtonTextColorBlue = Math.Clamp(Current.WardrobeButtonTextColorBlue, 0, 255);
        Current.SaveButtonColorRed = Math.Clamp(Current.SaveButtonColorRed, 0, 255);
        Current.SaveButtonColorGreen = Math.Clamp(Current.SaveButtonColorGreen, 0, 255);
        Current.SaveButtonColorBlue = Math.Clamp(Current.SaveButtonColorBlue, 0, 255);
        Current.SaveButtonTextColorRed = Math.Clamp(Current.SaveButtonTextColorRed, 0, 255);
        Current.SaveButtonTextColorGreen = Math.Clamp(Current.SaveButtonTextColorGreen, 0, 255);
        Current.SaveButtonTextColorBlue = Math.Clamp(Current.SaveButtonTextColorBlue, 0, 255);
        Current.AppearanceGridRows = Current.AppearanceGridRows is 3 or 4
            ? Current.AppearanceGridRows
            : 4;
        Current.AppearanceGridColumns = 6;
        Current.Version = PluginConfiguration.CurrentSchemaVersion;
    }

    public PluginConfiguration Current { get; }

    internal static UiThemeProfile NormalizeUiProfile(UiThemeProfile profile)
        => Enum.IsDefined(profile)
            ? profile
            : UiThemeProfile.Crystarium;

    internal static bool NormalizeAutomaticWardrobeSync(int schemaVersion, bool enabled)
        => schemaVersion < 26 || enabled;

    public void Save()
        => pluginInterface.SavePluginConfig(Current);
}
