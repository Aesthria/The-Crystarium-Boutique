using System.Numerics;
using CrystariumBoutique.Configuration;
using CrystariumBoutique.Core.Appearance;
using CrystariumBoutique.Core.Catalog;
using CrystariumBoutique.Core.Dyes;
using CrystariumBoutique.Core.Sessions;
using CrystariumBoutique.Ui.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;

namespace CrystariumBoutique.Ui;

internal sealed class BoutiqueWardrobeWindow : Window
{
    private static readonly Vector2 MinimumWardrobeSize = new(420f, 360f);
    private static readonly Vector2 MaximumWardrobeSize = new(float.MaxValue);
    private readonly BoutiqueSessionController sessionController;
    private readonly EquipmentBrowserController browser;
    private readonly ConfigurationStore configuration;
    private readonly BoutiqueFontSet fonts;
    private readonly Action<EquipmentCardFeedback> reportFeedback;
    private readonly OutfitContextPanel outfitContext;
    private readonly ISharedImmediateTexture crystariumBackgroundTexture;
    private readonly WardrobeOrientationResize orientationResize = new();
    private bool customBackgroundPushed;
    private bool restoreDefaultSizeConditionAfterDraw;
    private CrystariumProfileStyleStack profileStyleStack;
    private IDisposable? titleFontScope;

    public BoutiqueWardrobeWindow(
        IStainRepository stains,
        IItemRepository itemRepository,
        BoutiqueSessionController sessionController,
        EquipmentBrowserController browser,
        ITextureProvider textureProvider,
        ConfigurationStore configuration,
        ItemRarityColorResolver itemRarityColors,
        BoutiqueFontSet fonts,
        string assetDirectory,
        Action<EquipmentCardFeedback> reportFeedback)
        : base("Crystal Wardrobe###CrystariumBoutiqueWardrobe")
    {
        this.sessionController = sessionController ?? throw new ArgumentNullException(nameof(sessionController));
        this.browser = browser ?? throw new ArgumentNullException(nameof(browser));
        this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        this.fonts = fonts ?? throw new ArgumentNullException(nameof(fonts));
        this.reportFeedback = reportFeedback ?? throw new ArgumentNullException(nameof(reportFeedback));
        crystariumBackgroundTexture = textureProvider.GetFromFile(
            Path.Combine(assetDirectory, "images", "crystarium-stained-glass.png"));
        outfitContext = new OutfitContextPanel(
            stains,
            itemRepository,
            sessionController,
            browser,
            textureProvider,
            itemRarityColors,
            configuration,
            fonts,
            assetDirectory,
            reportFeedback);

        Size = BoutiqueTheme.DefaultWardrobeSize;
        SizeCondition = ImGuiCond.FirstUseEver;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = MinimumWardrobeSize,
            MaximumSize = MaximumWardrobeSize,
        };
        IsOpen = false;
    }

    public override void Draw()
    {
        BoutiqueScaleStyle.ApplyResponsiveWindowFontScale(
            configuration.Current.InterfaceScalePercent,
            BoutiqueTheme.DefaultWardrobeSize);
        using var bodyFont = fonts.Body.Push();
        PluginBackgroundStyle.DrawCrystariumBackdrop(
            configuration.Current,
            crystariumBackgroundTexture);
        DrawToolbar();
        BoutiqueTheme.DrawProfileHorizontalRule(
            configuration.Current,
            BoutiqueTheme.GetHorizontalRuleColor(configuration.Current));
        ImGui.Spacing();
        var crystariumWorkspace = BoutiqueTheme.IsCrystariumProfile(configuration.Current);
        var transparentSimpleCrystariumWorkspace
            = BoutiqueTheme.UsesTransparentParentContent(configuration.Current);
        if (crystariumWorkspace)
        {
            ImGui.PushStyleColor(ImGuiCol.ChildBg, BoutiqueTheme.CrystariumWardrobeWorkspace);
        }
        else if (transparentSimpleCrystariumWorkspace)
        {
            ImGui.PushStyleColor(ImGuiCol.ChildBg, Vector4.Zero);
        }

        if (ImGui.BeginChild("##BoutiqueWardrobeCards", Vector2.Zero, false))
        {
            var layout = configuration.Current.WardrobeVerticalLayout
                ? WardrobeLayout.Vertical
                : WardrobeLayout.Horizontal;
            var rowCount = layout == WardrobeLayout.Vertical ? 6 : 2;
            var availableHeight = ImGui.GetContentRegionAvail().Y;
            var tablePadding = ImGui.GetStyle().CellPadding.Y * rowCount * 2f;
            var cardHeight = Math.Max(
                BoutiqueTheme.MinimumEquipmentCardHeight,
                (availableHeight - tablePadding) / rowCount);
            outfitContext.DrawEquipmentCards(
                cardHeight,
                layout);
        }

        ImGui.EndChild();
        if (crystariumWorkspace || transparentSimpleCrystariumWorkspace)
        {
            ImGui.PopStyleColor();
        }
    }

    public override void PreDraw()
    {
        if (orientationResize.TryConsume(out var pendingSize))
        {
            Size = pendingSize;
            SizeCondition = ImGuiCond.Always;
            restoreDefaultSizeConditionAfterDraw = true;
        }

        titleFontScope = fonts.MainTitle.Push();
        var backgroundTransparency = Math.Clamp(
            configuration.Current.BackgroundTransparencyPercent,
            0,
            100);
        BgAlpha = 1f - (backgroundTransparency / 100f);
        profileStyleStack = CrystariumProfileStyle.Push(configuration.Current);
        customBackgroundPushed = PluginBackgroundStyle.Push(configuration.Current);
        var transparency = Math.Clamp(configuration.Current.PluginTransparencyPercent, 0, 90);
        ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 1f - (transparency / 100f));
        ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, BoutiqueTheme.ControlRounding);
    }

    public override void PostDraw()
    {
        ImGui.PopStyleVar(2);
        if (customBackgroundPushed)
        {
            ImGui.PopStyleColor();
        }

        profileStyleStack.Pop();
        profileStyleStack = default;
        titleFontScope?.Dispose();
        titleFontScope = null;
        if (restoreDefaultSizeConditionAfterDraw)
        {
            Size = BoutiqueTheme.DefaultWardrobeSize;
            SizeCondition = ImGuiCond.FirstUseEver;
            restoreDefaultSizeConditionAfterDraw = false;
        }
    }

    private void DrawToolbar()
    {
        using var navigationFont = fonts.Navigation.Push();
        if (!ImGui.BeginTable(
                "##BoutiqueWardrobeToolbar",
                2,
                ImGuiTableFlags.SizingStretchSame | ImGuiTableFlags.NoSavedSettings))
        {
            return;
        }

        var buttonColor = BoutiqueTheme.GetWardrobeToolbarButtonColor(configuration.Current);
        ImGui.PushStyleColor(ImGuiCol.Button, buttonColor);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, BoutiqueTheme.GetHoveredControlColor(buttonColor));
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, BoutiqueTheme.GetActiveControlColor(buttonColor));
        ImGui.PushStyleColor(ImGuiCol.Text, BoutiqueTheme.GetBoutiqueButtonTextColor(configuration.Current));
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        if (BoutiqueTheme.FramedButton(
                configuration.Current,
                "Horizontal Layout",
                new Vector2(-1f, 0f)))
        {
            SetLayout(vertical: false);
        }

        ImGui.TableNextColumn();
        if (BoutiqueTheme.FramedButton(
                configuration.Current,
                "Vertical Layout",
                new Vector2(-1f, 0f)))
        {
            SetLayout(vertical: true);
        }

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        if (BoutiqueTheme.FramedButton(
                configuration.Current,
                "Reset Character",
                new Vector2(-1f, 0f)))
        {
            RefreshSlots();
        }

        ImGui.TableNextColumn();
        if (BoutiqueTheme.FramedButton(
                configuration.Current,
                "Clear All Dye Slots",
                new Vector2(-1f, 0f)))
        {
            ClearAllDyeSlots();
        }

        ImGui.PopStyleColor(4);
        ImGui.EndTable();
    }

    private void SetLayout(bool vertical)
    {
        if (configuration.Current.WardrobeVerticalLayout == vertical)
        {
            return;
        }

        orientationResize.Request(
            configuration.Current.WardrobeVerticalLayout,
            vertical,
            WardrobeOrientationResize.ToLogicalSize(
                ImGui.GetWindowSize(),
                ImGuiHelpers.GlobalScale),
            MinimumWardrobeSize,
            MaximumWardrobeSize);
        configuration.Current.WardrobeVerticalLayout = vertical;
        configuration.Save();
    }

    private void RefreshSlots()
    {
        var result = sessionController.RefreshSlots();
        if (result.IsSuccess)
        {
            browser.ClearAppearanceSelection();
        }

        reportFeedback(new EquipmentCardFeedback(
            result.IsSuccess,
            result.IsSuccess
                ? "Refreshed the Wardrobe from the character's current game-state equipment and dyes."
                : result.Error?.Message ?? "The Wardrobe could not be refreshed from game state.",
            result.Error));
    }

    private void ClearAllDyeSlots()
    {
        var result = sessionController.ClearAllDyeSlots();
        reportFeedback(new EquipmentCardFeedback(
            result.IsSuccess,
            result.IsSuccess
                ? "Cleared all Boutique dye slots without changing equipment."
                : result.Error?.Message ?? "The Boutique dye slots could not be cleared.",
            result.Error));
    }
}

internal sealed class WardrobeOrientationResize
{
    private Vector2? pendingSize;

    public bool Request(
        bool currentVertical,
        bool requestedVertical,
        Vector2 currentSize,
        Vector2 minimumSize,
        Vector2 maximumSize)
    {
        if (currentVertical == requestedVertical)
        {
            return false;
        }

        pendingSize = SwapAndClamp(currentSize, minimumSize, maximumSize);
        return true;
    }

    public bool TryConsume(out Vector2 size)
    {
        if (pendingSize is not { } value)
        {
            size = default;
            return false;
        }

        size = value;
        pendingSize = null;
        return true;
    }

    public static Vector2 SwapAndClamp(
        Vector2 currentSize,
        Vector2 minimumSize,
        Vector2 maximumSize)
        => new(
            Math.Clamp(currentSize.Y, minimumSize.X, maximumSize.X),
            Math.Clamp(currentSize.X, minimumSize.Y, maximumSize.Y));

    public static Vector2 ToLogicalSize(Vector2 actualSize, float globalScale)
    {
        var safeScale = globalScale > 0f && float.IsFinite(globalScale)
            ? globalScale
            : 1f;
        return actualSize / safeScale;
    }
}
