using CrystariumBoutique.Core.Catalog;
using CrystariumBoutique.Core.Favorites;
using Dalamud.Configuration;

namespace CrystariumBoutique.Configuration;

[Serializable]
public sealed class PluginConfiguration : IPluginConfiguration
{
    public const int CurrentSchemaVersion = 26;
    public const int DefaultTooltipFontScalePercent = 100;
    public const int DefaultInterfaceScalePercent = 125;
    public const int DefaultBackgroundTransparencyPercent = 20;
    public const int DefaultCrystariumBackgroundDarknessPercent = 30;
    public const int DefaultHighlightColorRed = 15;
    public const int DefaultHighlightColorGreen = 252;
    public const int DefaultHighlightColorBlue = 205;
    public const int DefaultHighlightTransparencyPercent = 15;
    public const int DefaultHorizontalRuleColorRed = 63;
    public const int DefaultHorizontalRuleColorGreen = 47;
    public const int DefaultHorizontalRuleColorBlue = 15;
    public const int DefaultBoutiqueTextColorRed = 255;
    public const int DefaultBoutiqueTextColorGreen = 212;
    public const int DefaultBoutiqueTextColorBlue = 141;
    public const int DefaultFavoriteFrameColorRed = 231;
    public const int DefaultFavoriteFrameColorGreen = 84;
    public const int DefaultFavoriteFrameColorBlue = 166;

    public int Version { get; set; } = CurrentSchemaVersion;

    public UiThemeProfile UiProfile { get; set; } = UiThemeProfile.Crystarium;

    public bool ShowTechnicalStatus { get; set; } = true;

    public bool ShowWelcomeGuideOnOpen { get; set; } = true;

    public FavoriteCatalog Favorites { get; set; } = new();

    public int TooltipTransparencyPercent { get; set; } = 50;

    public int TooltipFontScalePercent { get; set; } = DefaultTooltipFontScalePercent;

    public ItemAcquisitionDetailLevel TooltipAcquisitionDetail { get; set; }
        = ItemAcquisitionDetailLevel.Standard;

    public bool TooltipShowVendors { get; set; } = true;

    public bool TooltipShowMarketBoard { get; set; } = true;

    public bool TooltipShowCrafting { get; set; } = true;

    public bool TooltipShowQuests { get; set; } = true;

    public bool TooltipShowAchievements { get; set; } = true;

    public bool TooltipShowSeasonalEvents { get; set; } = true;

    public bool TooltipShowOnlineStore { get; set; } = true;

    public bool TooltipShowDutyDrops { get; set; } = true;

    public bool TooltipShowSharedModels { get; set; } = true;

    public int TooltipSharedModelLimit { get; set; } = 8;

    public int AppearanceGridRows { get; set; } = 4;

    public int AppearanceGridColumns { get; set; } = 6;

    public int InterfaceScalePercent { get; set; } = DefaultInterfaceScalePercent;

    public bool ShowOutfitContext { get; set; } = true;

    public int PluginTransparencyPercent { get; set; }

    public int BackgroundTransparencyPercent { get; set; } = DefaultBackgroundTransparencyPercent;

    public int CrystariumBackgroundDarknessPercent { get; set; }
        = DefaultCrystariumBackgroundDarknessPercent;

    public bool DisableInCombat { get; set; } = true;

    public bool AutomaticallySyncWardrobe { get; set; } = true;

    public bool WardrobeVerticalLayout { get; set; }

    public bool UseCustomBackgroundColor { get; set; }

    public int BackgroundColorRed { get; set; } = 31;

    public int BackgroundColorGreen { get; set; } = 34;

    public int BackgroundColorBlue { get; set; } = 45;

    public int ItemBorderColorRed { get; set; } = 153;

    public int ItemBorderColorGreen { get; set; } = 140;

    public int ItemBorderColorBlue { get; set; } = 87;

    public int SelectedItemHighlightColorRed { get; set; } = DefaultHighlightColorRed;

    public int SelectedItemHighlightColorGreen { get; set; } = DefaultHighlightColorGreen;

    public int SelectedItemHighlightColorBlue { get; set; } = DefaultHighlightColorBlue;

    public int SelectedItemHighlightTransparencyPercent { get; set; }
        = DefaultHighlightTransparencyPercent;

    public int FavoriteFrameColorRed { get; set; } = DefaultFavoriteFrameColorRed;

    public int FavoriteFrameColorGreen { get; set; } = DefaultFavoriteFrameColorGreen;

    public int FavoriteFrameColorBlue { get; set; } = DefaultFavoriteFrameColorBlue;

    public bool HighlightFavoriteItemFrames { get; set; }

    public bool TooltipsRequireShift { get; set; }

    public int HorizontalRuleColorRed { get; set; } = DefaultHorizontalRuleColorRed;

    public int HorizontalRuleColorGreen { get; set; } = DefaultHorizontalRuleColorGreen;

    public int HorizontalRuleColorBlue { get; set; } = DefaultHorizontalRuleColorBlue;

    public int BoutiqueButtonColorRed { get; set; } = 125;

    public int BoutiqueButtonColorGreen { get; set; } = 69;

    public int BoutiqueButtonColorBlue { get; set; } = 87;

    public int BoutiqueButtonTextColorRed { get; set; } = DefaultBoutiqueTextColorRed;

    public int BoutiqueButtonTextColorGreen { get; set; } = DefaultBoutiqueTextColorGreen;

    public int BoutiqueButtonTextColorBlue { get; set; } = DefaultBoutiqueTextColorBlue;

    public int FilterBackgroundColorRed { get; set; } = 77;

    public int FilterBackgroundColorGreen { get; set; } = 72;

    public int FilterBackgroundColorBlue { get; set; } = 75;

    public int FilterTextColorRed { get; set; } = 255;

    public int FilterTextColorGreen { get; set; } = 255;

    public int FilterTextColorBlue { get; set; } = 255;

    public bool UseCustomWardrobeButtonColor { get; set; }

    public int WardrobeButtonColorRed { get; set; } = 87;

    public int WardrobeButtonColorGreen { get; set; } = 136;

    public int WardrobeButtonColorBlue { get; set; } = 153;

    public bool UseCustomWardrobeButtonTextColor { get; set; }

    public int WardrobeButtonTextColorRed { get; set; } = 255;

    public int WardrobeButtonTextColorGreen { get; set; } = 255;

    public int WardrobeButtonTextColorBlue { get; set; } = 255;

    public bool UseCustomSaveButtonColor { get; set; }

    public int SaveButtonColorRed { get; set; } = 87;

    public int SaveButtonColorGreen { get; set; } = 153;

    public int SaveButtonColorBlue { get; set; } = 131;

    public bool UseCustomSaveButtonTextColor { get; set; }

    public int SaveButtonTextColorRed { get; set; } = 255;

    public int SaveButtonTextColorGreen { get; set; } = 255;

    public int SaveButtonTextColorBlue { get; set; } = 255;
}
