using System.Numerics;
using CrystariumBoutique.Configuration;
using Dalamud.Bindings.ImGui;

namespace CrystariumBoutique.Ui;

internal static class BoutiqueTheme
{
    public const int StandardGridColumns = 6;
    public const int CompactGridRows = 3;
    public const int ExpandedGridRows = 4;
    public const float OutfitCardFontScale = 1f;
    public const float MinimumEquipmentCardHeight = 120f;
    public const float EquipmentCardInset = 5f;
    public const float MinimumGridHeight = 260f;
    public const float PreferredIconSize = 196f;
    public const float CrystariumPreferredIconSize = 245f;
    public const float CrystariumGridSpacingScale = 0.72f;
    public const float CrystariumTilePadding = 2f;
    public const float TileInset = 10f;
    public const float TooltipPadding = 7f;
    public const float ControlRounding = 8f;
    public const float RuleThickness = 3f;
    public const float ProminentRuleThickness = RuleThickness * 1.35f;
    public const float PreviousItemButtonSize = 66.3f;
    public const float PreviousItemButtonMargin = 12f;
    public const float MinimumAdaptiveFontScale = 0.35f;
    public const float PageControlScale = 1.05f;

    public static readonly Vector2 DefaultWindowSize = new(1500f, 980f);
    public static readonly Vector2 DefaultWardrobeSize = new(1500f, 590f);
    public static readonly Vector4 Accent = new(0.39f, 0.75f, 0.92f, 1f);
    public static readonly Vector4 MutedText = new(0.68f, 0.72f, 0.78f, 1f);
    public static readonly Vector4 Available = new(0.38f, 0.78f, 0.51f, 1f);
    public static readonly Vector4 Unavailable = new(0.93f, 0.64f, 0.30f, 1f);
    public static readonly Vector4 MarketUnavailable = new(0.92f, 0.25f, 0.25f, 1f);
    public static readonly Vector4 DustyRose = new(0.49f, 0.27f, 0.34f, 1f);
    public static readonly Vector4 DustyRoseHovered = new(0.62f, 0.34f, 0.43f, 1f);
    public static readonly Vector4 DustyRoseActive = new(0.41f, 0.21f, 0.28f, 1f);
    public static readonly Vector4 SteelBlue = new(87f / 255f, 136f / 255f, 153f / 255f, 1f);
    public static readonly Vector4 SteelBlueHovered = new(0.42f, 0.64f, 0.71f, 1f);
    public static readonly Vector4 SteelBlueActive = new(0.27f, 0.44f, 0.50f, 1f);
    public static readonly Vector4 WarmCharcoal = new(77f / 255f, 72f / 255f, 75f / 255f, 1f);
    public static readonly Vector4 WarmCharcoalHovered = new(0.37f, 0.35f, 0.36f, 1f);
    public static readonly Vector4 WarmCharcoalActive = new(0.25f, 0.23f, 0.24f, 1f);
    public static readonly Vector4 Eucalyptus = new(87f / 255f, 153f / 255f, 131f / 255f, 1f);
    public static readonly Vector4 EucalyptusHovered = new(0.42f, 0.70f, 0.61f, 1f);
    public static readonly Vector4 EucalyptusActive = new(0.27f, 0.50f, 0.43f, 1f);
    public static readonly Vector4 Terracotta = new(153f / 255f, 109f / 255f, 87f / 255f, 1f);
    public static readonly Vector4 TerracottaHovered = new(0.70f, 0.50f, 0.40f, 1f);
    public static readonly Vector4 TerracottaActive = new(0.48f, 0.31f, 0.25f, 1f);
    public static readonly Vector4 AntiqueGold = new(153f / 255f, 140f / 255f, 87f / 255f, 1f);
    public static readonly Vector4 AntiqueGoldShadow = new(0.18f, 0.15f, 0.10f, 1f);
    public static readonly Vector4 CrystariumGold = new(181f / 255f, 137f / 255f, 49f / 255f, 1f);
    public static readonly Vector4 ClearRed = new(0.52f, 0.10f, 0.10f, 1f);
    public static readonly Vector4 ClearRedHovered = new(0.72f, 0.16f, 0.16f, 1f);
    public static readonly Vector4 ClearRedActive = new(0.42f, 0.06f, 0.06f, 1f);
    public static readonly Vector4 NeonPink = new(0.92f, 0.20f, 0.60f, 1f);
    public static readonly Vector4 NeonPinkHovered = new(1.00f, 0.31f, 0.70f, 1f);
    public static readonly Vector4 NeonPinkActive = new(0.76f, 0.13f, 0.47f, 1f);
    public static readonly Vector4 SelectedTile = new(0.15f, 0.31f, 0.42f, 1f);
    public static readonly Vector4 SelectedBorder = new(0.48f, 0.84f, 1f, 1f);
    public static readonly Vector4 LightGreyBorder = new(0.66f, 0.66f, 0.68f, 1f);
    public static readonly Vector4 CrystariumGlass = new(38f / 255f, 44f / 255f, 50f / 255f, 1f);
    public static readonly Vector4 CrystariumGlassHovered = new(55f / 255f, 64f / 255f, 72f / 255f, 1f);
    public static readonly Vector4 CrystariumGlassActive = new(24f / 255f, 29f / 255f, 34f / 255f, 1f);
    public static readonly Vector4 CrystariumFrame = new(50f / 255f, 58f / 255f, 64f / 255f, 1f);
    public static readonly Vector4 CrystariumFrameHovered = new(78f / 255f, 91f / 255f, 101f / 255f, 1f);
    public static readonly Vector4 CrystariumFrameActive = new(17f / 255f, 22f / 255f, 26f / 255f, 1f);
    public static readonly Vector4 CrystariumGlow = new(110f / 255f, 210f / 255f, 240f / 255f, 1f);
    public static readonly Vector4 CrystariumText = new(229f / 255f, 240f / 255f, 246f / 255f, 1f);
    public static readonly Vector4 CrystariumMutedText = new(164f / 255f, 181f / 255f, 190f / 255f, 1f);
    public static readonly Vector4 CrystariumPanel = new(25f / 255f, 30f / 255f, 35f / 255f, 0.96f);
    public static readonly Vector4 CrystariumCard = new(29f / 255f, 34f / 255f, 39f / 255f, 0.98f);
    public static readonly Vector4 CrystariumSteelDark = new(20f / 255f, 25f / 255f, 29f / 255f, 1f);
    public static readonly Vector4 CrystariumWardrobeWorkspace = new(12f / 255f, 15f / 255f, 18f / 255f, 0.16f);
    public static readonly Vector4 CrystariumWardrobeCard = new(22f / 255f, 27f / 255f, 31f / 255f, 0.40f);
    public static readonly Vector4 SimpleCrystariumDeepCharcoal = new(24f / 255f, 27f / 255f, 32f / 255f, 1f);
    public static readonly Vector4 SimpleCrystariumDarkSteel = new(37f / 255f, 42f / 255f, 49f / 255f, 1f);
    public static readonly Vector4 SimpleCrystariumMediumSteel = new(58f / 255f, 65f / 255f, 75f / 255f, 1f);
    public static readonly Vector4 SimpleCrystariumSilverGrey = new(133f / 255f, 142f / 255f, 153f / 255f, 1f);
    public static readonly Vector4 SimpleCrystariumLightSilver = new(184f / 255f, 192f / 255f, 200f / 255f, 1f);
    public static readonly Vector4 SimpleCrystariumSoftWhite = new(226f / 255f, 229f / 255f, 232f / 255f, 1f);
    public static readonly Vector4 SimpleCrystariumMutedBlueGrey = new(102f / 255f, 116f / 255f, 131f / 255f, 1f);
    public static readonly Vector4 RestrictedItem = new(0.88f, 0.10f, 0.12f, 1f);
    public static readonly Vector4 CrystariumRestrictedItem = new(138f / 255f, 42f / 255f, 45f / 255f, 1f);
    public static readonly Vector3 TooltipBackground = new(0.08f, 0.08f, 0.08f);

    public static bool IsCrystariumProfile(PluginConfiguration configuration)
        => configuration.UiProfile == UiThemeProfile.Crystarium;

    public static bool IsSimpleCrystariumProfile(PluginConfiguration configuration)
        => configuration.UiProfile == UiThemeProfile.SimpleCrystarium;

    public static bool UsesCrystariumBackdrop(PluginConfiguration configuration)
        => IsCrystariumProfile(configuration) || IsSimpleCrystariumProfile(configuration);

    public static bool UsesTransparentParentContent(PluginConfiguration configuration)
        => IsSimpleCrystariumProfile(configuration);

    public static bool UsesTransparentItemTileContainer(PluginConfiguration configuration)
        => IsCrystariumProfile(configuration) || IsSimpleCrystariumProfile(configuration);

    public static Vector4 GetSimpleThemeGoldColor(PluginConfiguration configuration)
        => GetConfigurationColor(
            configuration.BoutiqueButtonTextColorRed,
            configuration.BoutiqueButtonTextColorGreen,
            configuration.BoutiqueButtonTextColorBlue);

    public static Vector4 GetItemBorderColor(PluginConfiguration configuration)
        => IsCrystariumProfile(configuration)
            ? CrystariumFrame
            : IsSimpleCrystariumProfile(configuration)
                ? GetSimpleThemeGoldColor(configuration)
            : new Vector4(
                Math.Clamp(configuration.ItemBorderColorRed, 0, 255) / 255f,
                Math.Clamp(configuration.ItemBorderColorGreen, 0, 255) / 255f,
                Math.Clamp(configuration.ItemBorderColorBlue, 0, 255) / 255f,
                1f);

    public static Vector4 GetItemBorderShadowColor(PluginConfiguration configuration)
        => IsCrystariumProfile(configuration)
            ? new Vector4(0.015f, 0.04f, 0.06f, 1f)
            : IsSimpleCrystariumProfile(configuration)
                ? SimpleCrystariumDeepCharcoal
            : AntiqueGoldShadow;

    public static Vector4 GetSelectedItemHighlightColor(PluginConfiguration configuration)
    {
        var color = GetConfigurationColor(
            configuration.SelectedItemHighlightColorRed,
            configuration.SelectedItemHighlightColorGreen,
            configuration.SelectedItemHighlightColorBlue);
        color.W = 1f - (Math.Clamp(
            configuration.SelectedItemHighlightTransparencyPercent,
            0,
            100) / 100f);
        return color;
    }

    public static Vector4 GetFavoriteItemFrameColor(PluginConfiguration configuration)
        => new(
            configuration.FavoriteFrameColorRed / 255f,
            configuration.FavoriteFrameColorGreen / 255f,
            configuration.FavoriteFrameColorBlue / 255f,
            1f);

    public static bool ShouldDrawFavoriteBadge(bool isFavorite)
        => isFavorite;

    public static bool ShouldDrawFavoriteFrame(
        PluginConfiguration configuration,
        bool isFavorite)
        => isFavorite && configuration.HighlightFavoriteItemFrames;

    public static float GetCardBorderThickness(PluginConfiguration configuration)
        => IsCrystariumProfile(configuration) ? 3f : 2f;

    public static float GetBoutiquePanelBorderThickness(PluginConfiguration configuration)
        => UsesCrystariumBackdrop(configuration) ? 2f : 1f;

    public static float GetCardRounding(PluginConfiguration configuration)
        => IsCrystariumProfile(configuration) ? 4f : ControlRounding;

    public static Vector4 GetHorizontalRuleColor(PluginConfiguration configuration)
        => IsSimpleCrystariumProfile(configuration)
            ? GetSimpleThemeGoldColor(configuration)
            : GetConfigurationColor(
                configuration.HorizontalRuleColorRed,
                configuration.HorizontalRuleColorGreen,
                configuration.HorizontalRuleColorBlue);

    public static Vector4 GetBoutiqueButtonColor(PluginConfiguration configuration)
        => IsCrystariumProfile(configuration)
            ? CrystariumGlass
            : IsSimpleCrystariumProfile(configuration)
                ? SimpleCrystariumDarkSteel
            : GetConfigurationColor(
                configuration.BoutiqueButtonColorRed,
                configuration.BoutiqueButtonColorGreen,
                configuration.BoutiqueButtonColorBlue);

    public static Vector4 GetBoutiqueButtonTextColor(PluginConfiguration configuration)
        => GetBoutiqueTabTextColor(configuration);

    public static Vector4 GetBoutiqueTabTextColor(PluginConfiguration configuration)
        => IsSimpleCrystariumProfile(configuration)
            ? GetSimpleThemeGoldColor(configuration)
            : GetConfigurationColor(
                configuration.BoutiqueButtonTextColorRed,
                configuration.BoutiqueButtonTextColorGreen,
                configuration.BoutiqueButtonTextColorBlue);

    public static Vector4 GetFilterBackgroundColor(PluginConfiguration configuration)
        => IsCrystariumProfile(configuration)
            ? CrystariumPanel
            : IsSimpleCrystariumProfile(configuration)
                ? SimpleCrystariumDarkSteel
            : GetConfigurationColor(
                configuration.FilterBackgroundColorRed,
                configuration.FilterBackgroundColorGreen,
                configuration.FilterBackgroundColorBlue);

    public static Vector4 GetFilterTextColor(PluginConfiguration configuration)
        => IsCrystariumProfile(configuration)
            ? CrystariumText
            : IsSimpleCrystariumProfile(configuration)
                ? GetSimpleThemeGoldColor(configuration)
            : GetConfigurationColor(
                configuration.FilterTextColorRed,
                configuration.FilterTextColorGreen,
                configuration.FilterTextColorBlue);

    public static Vector4 GetTabActiveColor(PluginConfiguration configuration)
        => IsCrystariumProfile(configuration)
            ? CrystariumGlassHovered
            : IsSimpleCrystariumProfile(configuration) ? SimpleCrystariumMutedBlueGrey : SteelBlue;

    public static Vector4 GetTabUnfocusedActiveColor(PluginConfiguration configuration)
        => IsCrystariumProfile(configuration)
            ? CrystariumGlassActive
            : IsSimpleCrystariumProfile(configuration) ? SimpleCrystariumDarkSteel : SteelBlueActive;

    public static Vector4 GetTabHoveredColor(PluginConfiguration configuration)
        => IsCrystariumProfile(configuration)
            ? CrystariumFrameHovered
            : IsSimpleCrystariumProfile(configuration) ? SimpleCrystariumMediumSteel : SteelBlueHovered;

    public static Vector4 GetDecorativeRuleColor(PluginConfiguration configuration)
        => IsCrystariumProfile(configuration)
            ? CrystariumFrameActive
            : IsSimpleCrystariumProfile(configuration) ? GetSimpleThemeGoldColor(configuration) : DustyRose;

    public static Vector4 GetDesignCardBackground(PluginConfiguration configuration)
        => IsCrystariumProfile(configuration)
            ? CrystariumCard
            : IsSimpleCrystariumProfile(configuration)
                ? SimpleCrystariumDeepCharcoal with { W = 0.82f }
                : WarmCharcoal;

    public static Vector4 GetDesignCardBorder(PluginConfiguration configuration)
        => IsCrystariumProfile(configuration)
            ? CrystariumFrame
            : IsSimpleCrystariumProfile(configuration) ? GetSimpleThemeGoldColor(configuration) : AntiqueGold;

    public static Vector4 GetPanelBorderColor(PluginConfiguration configuration)
        => IsCrystariumProfile(configuration)
            ? CrystariumGold
            : IsSimpleCrystariumProfile(configuration) ? GetSimpleThemeGoldColor(configuration) : AntiqueGold;

    public static Vector4 GetSelectedSlotButtonColor(PluginConfiguration configuration)
        => IsSimpleCrystariumProfile(configuration) ? SimpleCrystariumMutedBlueGrey : SelectedTile;

    public static Vector4 GetSelectedSlotBorderColor(PluginConfiguration configuration)
        => IsSimpleCrystariumProfile(configuration) ? GetSimpleThemeGoldColor(configuration) : SelectedBorder;

    public static Vector4 GetHeadingColor(PluginConfiguration configuration)
        => IsSimpleCrystariumProfile(configuration) ? GetSimpleThemeGoldColor(configuration) : Accent;

    public static Vector4 GetSecondaryTextColor(PluginConfiguration configuration)
        => IsSimpleCrystariumProfile(configuration) ? GetSimpleThemeGoldColor(configuration) : MutedText;

    public static Vector4 GetWardrobeCardBackground(
        PluginConfiguration configuration,
        bool selected,
        Vector4 inheritedBackground)
    {
        if (IsCrystariumProfile(configuration))
        {
            return selected
                ? GetSelectedSlotButtonColor(configuration) with { W = 0.58f }
                : CrystariumWardrobeCard;
        }

        if (IsSimpleCrystariumProfile(configuration))
        {
            return selected
                ? GetSelectedSlotButtonColor(configuration) with { W = 0.96f }
                : SimpleCrystariumDeepCharcoal with { W = 0.94f };
        }

        return selected
            ? GetSelectedSlotButtonColor(configuration)
            : inheritedBackground;
    }

    public static Vector4 GetWardrobeCardBorderColor(PluginConfiguration configuration)
        => IsCrystariumProfile(configuration)
            ? GetBoutiqueTabTextColor(configuration)
            : GetItemBorderColor(configuration);

    public static Vector4 GetWardrobeCardDividerColor(PluginConfiguration configuration)
        => IsCrystariumProfile(configuration)
            ? GetBoutiqueTabTextColor(configuration)
            : GetDecorativeRuleColor(configuration);

    public static Vector4 GetWardrobeButtonColor(PluginConfiguration configuration)
        => configuration.UseCustomWardrobeButtonColor
            ? GetConfigurationColor(
                configuration.WardrobeButtonColorRed,
                configuration.WardrobeButtonColorGreen,
                configuration.WardrobeButtonColorBlue)
            : SteelBlue;

    public static Vector4 GetWardrobeButtonTextColor(PluginConfiguration configuration)
        => configuration.UseCustomWardrobeButtonTextColor
            ? GetConfigurationColor(
                configuration.WardrobeButtonTextColorRed,
                configuration.WardrobeButtonTextColorGreen,
                configuration.WardrobeButtonTextColorBlue)
            : Vector4.One;

    public static Vector4 GetWardrobeToolbarButtonColor(PluginConfiguration configuration)
        => IsCrystariumProfile(configuration)
            ? CrystariumGlass
            : IsSimpleCrystariumProfile(configuration) ? SimpleCrystariumDarkSteel : DustyRose;

    public static Vector4 GetNeutralActionButtonColor(PluginConfiguration configuration)
        => IsSimpleCrystariumProfile(configuration) ? SimpleCrystariumDarkSteel : DustyRose;

    public static Vector4 GetSaveButtonColor(PluginConfiguration configuration)
        => configuration.UseCustomSaveButtonColor
            ? GetConfigurationColor(
                configuration.SaveButtonColorRed,
                configuration.SaveButtonColorGreen,
                configuration.SaveButtonColorBlue)
            : Eucalyptus;

    public static Vector4 GetSaveButtonTextColor(PluginConfiguration configuration)
        => configuration.UseCustomSaveButtonTextColor
            ? GetConfigurationColor(
                configuration.SaveButtonTextColorRed,
                configuration.SaveButtonTextColorGreen,
                configuration.SaveButtonTextColorBlue)
            : Vector4.One;

    public static Vector4 GetHoveredControlColor(Vector4 color)
        => new(
            color.X + ((1f - color.X) * 0.18f),
            color.Y + ((1f - color.Y) * 0.18f),
            color.Z + ((1f - color.Z) * 0.18f),
            color.W);

    public static Vector4 GetActiveControlColor(Vector4 color)
        => new(color.X * 0.78f, color.Y * 0.78f, color.Z * 0.78f, color.W);

    public static bool ShouldShowItemTooltip(PluginConfiguration configuration)
        => !configuration.TooltipsRequireShift || ImGui.GetIO().KeyShift;

    public static void DrawIconInteractionGlow(
        ImDrawListPtr drawList,
        Vector2 iconMinimum,
        Vector2 iconMaximum,
        Vector4 color,
        float outerExtent = 0f,
        bool outerOnly = false)
    {
        var outerMinimum = iconMinimum - new Vector2(outerExtent);
        var outerMaximum = iconMaximum + new Vector2(outerExtent);
        var rounding = ControlRounding + Math.Max(outerExtent * 0.12f, 0f);
        if (outerOnly)
        {
            drawList.AddRect(
                outerMinimum - new Vector2(1f),
                outerMaximum + new Vector2(1f),
                ImGui.ColorConvertFloat4ToU32(color),
                rounding + Math.Max(outerExtent * 0.55f, 0f),
                ImDrawFlags.None,
                4f);
            return;
        }

        var baseAlpha = color.W;
        var halo = color;
        halo.W = baseAlpha * 0.38f;
        var inner = color;
        inner.W = baseAlpha * 0.58f;
        var wash = color;
        wash.W = baseAlpha * 0.07f;

        drawList.AddRectFilled(
            iconMinimum + new Vector2(1f),
            iconMaximum - new Vector2(1f),
            ImGui.ColorConvertFloat4ToU32(wash),
            Math.Max(1f, ControlRounding - 1f));
        drawList.AddRect(
            iconMinimum + new Vector2(2f),
            iconMaximum - new Vector2(2f),
            ImGui.ColorConvertFloat4ToU32(inner),
            Math.Max(1f, ControlRounding - 2f),
            ImDrawFlags.None,
            5f);
        drawList.AddRect(
            outerMinimum - new Vector2(2f),
            outerMaximum + new Vector2(2f),
            ImGui.ColorConvertFloat4ToU32(color),
            rounding + 3f,
            ImDrawFlags.None,
            3f);
        drawList.AddRect(
            outerMinimum - new Vector2(5f),
            outerMaximum + new Vector2(5f),
            ImGui.ColorConvertFloat4ToU32(halo),
            rounding + 6f,
            ImDrawFlags.None,
            2f);
    }

    public static void DrawHorizontalRule(Vector4 color, float thickness = RuleThickness)
    {
        var position = ImGui.GetCursorScreenPos();
        var width = ImGui.GetContentRegionAvail().X;
        var y = position.Y + (thickness / 2f);
        ImGui.GetWindowDrawList().AddLine(
            new Vector2(position.X, y),
            new Vector2(position.X + width, y),
            ImGui.ColorConvertFloat4ToU32(color),
            thickness);
        ImGui.Dummy(new Vector2(0f, thickness));
    }

    public static void DrawCrystariumTitleBarRule(PluginConfiguration configuration)
    {
        if (!IsCrystariumProfile(configuration))
        {
            return;
        }

        var windowPosition = ImGui.GetWindowPos();
        var windowSize = ImGui.GetWindowSize();
        var titleBarBottom = windowPosition.Y
            + ImGui.GetFontSize()
            + (ImGui.GetStyle().FramePadding.Y * 2f);
        var minimumX = windowPosition.X + 1f;
        var maximumX = windowPosition.X + windowSize.X - 1f;
        var drawList = ImGui.GetWindowDrawList();
        var gold = GetBoutiqueTabTextColor(configuration);
        var shadow = new Vector4(gold.X * 0.22f, gold.Y * 0.22f, gold.Z * 0.22f, gold.W);
        var body = new Vector4(gold.X * 0.68f, gold.Y * 0.62f, gold.Z * 0.50f, gold.W);
        drawList.PushClipRect(windowPosition, windowPosition + windowSize, false);
        drawList.AddLine(
            new Vector2(minimumX, titleBarBottom),
            new Vector2(maximumX, titleBarBottom),
            ImGui.ColorConvertFloat4ToU32(shadow),
            6f);
        drawList.AddLine(
            new Vector2(minimumX, titleBarBottom - 0.5f),
            new Vector2(maximumX, titleBarBottom - 0.5f),
            ImGui.ColorConvertFloat4ToU32(body),
            3.5f);
        drawList.AddLine(
            new Vector2(minimumX, titleBarBottom - 1.25f),
            new Vector2(maximumX, titleBarBottom - 1.25f),
            ImGui.ColorConvertFloat4ToU32(gold),
            1f);
        drawList.PopClipRect();
    }

    public static void DrawProfileHorizontalRule(
        PluginConfiguration configuration,
        Vector4 defaultColor,
        float defaultThickness = RuleThickness)
    {
        if (!IsCrystariumProfile(configuration))
        {
            DrawHorizontalRule(defaultColor, defaultThickness);
            return;
        }

        var thickness = defaultThickness * 3f;
        var position = ImGui.GetCursorScreenPos();
        var width = ImGui.GetContentRegionAvail().X;
        var maximum = position + new Vector2(width, thickness);
        var drawList = ImGui.GetWindowDrawList();
        var darkColor = new Vector4(
            defaultColor.X * 0.28f,
            defaultColor.Y * 0.28f,
            defaultColor.Z * 0.28f,
            defaultColor.W);
        var highlightColor = GetHoveredControlColor(defaultColor);
        drawList.AddRectFilled(
            position,
            maximum,
            ImGui.ColorConvertFloat4ToU32(darkColor),
            1.5f);
        drawList.AddLine(
            position + new Vector2(1f, 1f),
            new Vector2(maximum.X - 1f, position.Y + 1f),
            ImGui.ColorConvertFloat4ToU32(highlightColor),
            Math.Max(1f, defaultThickness * 0.70f));
        drawList.AddLine(
            new Vector2(position.X + 1f, position.Y + (thickness / 2f)),
            new Vector2(maximum.X - 1f, position.Y + (thickness / 2f)),
            ImGui.ColorConvertFloat4ToU32(defaultColor),
            Math.Max(1f, defaultThickness));
        drawList.AddLine(
            new Vector2(position.X + 1f, maximum.Y - 1f),
            maximum - Vector2.One,
            ImGui.ColorConvertFloat4ToU32(new Vector4(0.01f, 0.012f, 0.014f, 1f)),
            Math.Max(1f, defaultThickness * 0.85f));
        ImGui.Dummy(new Vector2(0f, thickness));
    }

    public static float GetProfileHorizontalRuleThickness(
        PluginConfiguration configuration,
        float defaultThickness = RuleThickness)
        => IsCrystariumProfile(configuration) ? defaultThickness * 3f : defaultThickness;

    public static bool BeginResponsiveCombo(
        PluginConfiguration configuration,
        string id,
        string preview,
        Vector4? previewTextColor = null)
    {
        // Render only the preview at a fitted size. Changing the parent window font scale here
        // also changes popup entries once BeginCombo opens, which makes every option inherit the
        // selected long value's tiny font size.
        var parentDrawList = ImGui.GetWindowDrawList();
        var opened = ImGui.BeginCombo(id, string.Empty);
        var minimum = ImGui.GetItemRectMin();
        var maximum = ImGui.GetItemRectMax();
        DrawGoldControlFrame(configuration, parentDrawList, minimum, maximum);
        var textMinimum = new Vector2(
            minimum.X + ImGui.GetStyle().FramePadding.X,
            minimum.Y + 1f);
        var textMaximum = new Vector2(
            maximum.X - ImGui.GetFrameHeight(),
            maximum.Y - 1f);
        DrawFittedText(
            parentDrawList,
            preview,
            textMinimum,
            textMaximum,
            previewTextColor ?? Vector4.One,
            1f,
            MinimumAdaptiveFontScale,
            horizontalAlignment: 0f);
        return opened;
    }

    public static void EndResponsiveCombo()
    {
        ImGui.EndCombo();
    }

    public static bool FittedTextButton(
        PluginConfiguration configuration,
        string id,
        string text,
        Vector2 size,
        float desiredFontScale = 1f,
        float minimumFontScale = MinimumAdaptiveFontScale,
        Vector4? textColor = null)
    {
        var clicked = FramedButton(configuration, id, size);
        var contentInset = IsCrystariumProfile(configuration) ? 5f : 2f;
        DrawFittedText(
            ImGui.GetWindowDrawList(),
            text,
            ImGui.GetItemRectMin() + new Vector2(contentInset, 1f),
            ImGui.GetItemRectMax() - new Vector2(contentInset, 1f),
            textColor ?? Vector4.One,
            desiredFontScale,
            minimumFontScale);
        return clicked;
    }

    public static bool FramedButton(
        PluginConfiguration configuration,
        string label,
        Vector2 size = default)
    {
        var clicked = ImGui.Button(label, size);
        DrawLastGoldControlFrame(configuration);
        return clicked;
    }

    public static bool BeginFramedCombo(
        PluginConfiguration configuration,
        string id,
        string preview)
    {
        var parentDrawList = ImGui.GetWindowDrawList();
        var opened = ImGui.BeginCombo(id, preview);
        DrawGoldControlFrame(
            configuration,
            parentDrawList,
            ImGui.GetItemRectMin(),
            ImGui.GetItemRectMax());
        return opened;
    }

    public static bool FramedCheckbox(
        PluginConfiguration configuration,
        string label,
        ref bool value)
    {
        var changed = ImGui.Checkbox(label, ref value);
        if (IsCrystariumProfile(configuration))
        {
            var minimum = ImGui.GetItemRectMin();
            var side = ImGui.GetFrameHeight();
            DrawGoldControlFrame(
                configuration,
                ImGui.GetWindowDrawList(),
                minimum,
                minimum + new Vector2(side));
        }

        return changed;
    }

    public static void DrawLastGoldControlFrame(PluginConfiguration configuration)
    {
        if (!IsCrystariumProfile(configuration))
        {
            return;
        }

        DrawGoldControlFrame(
            configuration,
            ImGui.GetWindowDrawList(),
            ImGui.GetItemRectMin(),
            ImGui.GetItemRectMax());
    }

    private static void DrawGoldControlFrame(
        PluginConfiguration configuration,
        ImDrawListPtr drawList,
        Vector2 minimum,
        Vector2 maximum)
    {
        if (!IsCrystariumProfile(configuration))
        {
            return;
        }

        CrystariumGoldFrameRenderer.DrawControl(drawList, minimum, maximum);
    }

    public static void DrawTooltipGoldFrame(PluginConfiguration configuration)
    {
        if (!IsCrystariumProfile(configuration))
        {
            return;
        }

        var minimum = ImGui.GetWindowPos();
        CrystariumGoldFrameRenderer.DrawTooltip(
            ImGui.GetWindowDrawList(),
            minimum,
            minimum + ImGui.GetWindowSize());
    }

    public static float DrawFittedText(
        ImDrawListPtr drawList,
        string text,
        Vector2 minimum,
        Vector2 maximum,
        Vector4 color,
        float desiredScale = 1f,
        float minimumScale = MinimumAdaptiveFontScale,
        float horizontalAlignment = 0.5f)
    {
        if (string.IsNullOrEmpty(text))
        {
            return desiredScale;
        }

        var available = Vector2.Max(maximum - minimum, Vector2.One);
        var unscaledSize = ImGui.CalcTextSize(text);
        var widthScale = unscaledSize.X > 0f ? available.X / unscaledSize.X : desiredScale;
        var heightScale = unscaledSize.Y > 0f ? available.Y / unscaledSize.Y : desiredScale;
        var fittedScale = Math.Clamp(
            Math.Min(desiredScale, Math.Min(widthScale, heightScale)),
            Math.Min(minimumScale, desiredScale),
            desiredScale);
        var scaledSize = unscaledSize * fittedScale;
        var position = new Vector2(
            minimum.X + ((available.X - scaledSize.X) * Math.Clamp(horizontalAlignment, 0f, 1f)),
            minimum.Y + ((available.Y - scaledSize.Y) / 2f));
        var clipRectangle = new Vector4(minimum.X, minimum.Y, maximum.X, maximum.Y);
        color.W *= ImGui.GetStyle().Alpha;
        drawList.AddText(
            ImGui.GetFont(),
            ImGui.GetFontSize() * fittedScale,
            position,
            ImGui.ColorConvertFloat4ToU32(color),
            text,
            0f,
            in clipRectangle);
        return fittedScale;
    }

    private static Vector4 GetConfigurationColor(int red, int green, int blue)
        => new(
            Math.Clamp(red, 0, 255) / 255f,
            Math.Clamp(green, 0, 255) / 255f,
            Math.Clamp(blue, 0, 255) / 255f,
            1f);
}
