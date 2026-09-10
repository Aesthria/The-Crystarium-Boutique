using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Collections.Immutable;
using CrystariumBoutique.Configuration;
using CrystariumBoutique.Core.Appearance;
using CrystariumBoutique.Core.Catalog;
using CrystariumBoutique.Core.Dyes;
using CrystariumBoutique.Core.Errors;
using CrystariumBoutique.Core.Favorites;
using CrystariumBoutique.Core.Integrations;
using CrystariumBoutique.Core.Loadouts;
using CrystariumBoutique.Core.Sessions;
using CrystariumBoutique.Integrations;
using CrystariumBoutique.Ui.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;

namespace CrystariumBoutique.Ui;

public sealed class BoutiqueWindow : Window
{
    private static string VersionLabel => BoutiqueVersion.DisplayLabel;
    private static readonly string[] DyeFilterLabels =
    [
        "None <-> All",
        "No dye slots",
        "Single dye slot",
        "Two dye slots",
    ];
    private static readonly string[] JobRoleFilterLabels =
    [
        "All",
        "Tank",
        "Healer",
        "Melee DPS",
        "Physical Ranged DPS",
        "Caster / Magical Ranged DPS",
        "Limited Classes",
        "Crafter",
        "Gatherer",
    ];
    private static readonly (int Rows, int Columns)[] GridLayoutOptions =
    [
        (BoutiqueTheme.CompactGridRows, BoutiqueTheme.StandardGridColumns),
        (BoutiqueTheme.ExpandedGridRows, BoutiqueTheme.StandardGridColumns),
    ];
    private static readonly EquipmentSlotDefinition[] BoutiqueSlotButtons =
    [
        FindSlotDefinition(EquipmentSlot.MainHand),
        FindSlotDefinition(EquipmentSlot.Head),
        FindSlotDefinition(EquipmentSlot.Body),
        FindSlotDefinition(EquipmentSlot.Hands),
        FindSlotDefinition(EquipmentSlot.Legs),
        FindSlotDefinition(EquipmentSlot.Feet),
        FindSlotDefinition(EquipmentSlot.OffHand),
        FindSlotDefinition(EquipmentSlot.Ears),
        FindSlotDefinition(EquipmentSlot.Neck),
        FindSlotDefinition(EquipmentSlot.Wrists),
        FindSlotDefinition(EquipmentSlot.RightRing),
        FindSlotDefinition(EquipmentSlot.LeftRing),
    ];
    private readonly BoutiqueSessionController sessionController;
    private readonly BoutiqueSession session;
    private readonly IAppearanceService appearanceService;
    private readonly IPenumbraService penumbraService;
    private readonly ConfigurationStore configuration;
    private readonly IItemRepository itemRepository;
    private readonly EquipmentBrowserController browser;
    private readonly ITextureProvider textureProvider;
    private readonly ISharedImmediateTexture equipmentSlotTexture;
    private readonly ISharedImmediateTexture previousItemTexture;
    private readonly ISharedImmediateTexture crystalWardrobeTexture;
    private readonly ISharedImmediateTexture crystariumBackgroundTexture;
    private readonly ISharedImmediateTexture crystariumItemFrameTexture;
    private readonly ISharedImmediateTexture favoriteBadgeTexture;
    private readonly ItemRarityColorResolver itemRarityColors;
    private readonly IPlayerState playerState;
    private readonly BoutiqueFontSet fonts;
    private readonly string? catalogError;
    private readonly Action closeSession;
    private readonly Action toggleWardrobe;
    private readonly Action showWelcomeGuide;
    private readonly int stainCount;
    private readonly LoadoutPanel loadoutPanel;
    private string? previewStatus;
    private bool previewStatusIsError;
    private BoutiqueError? previewError;
    private string searchText = string.Empty;
    private string maximumEquipLevelText = string.Empty;
    private byte? maximumEquipLevel;
    private DyeSupportFilter dyeSupportFilter;
    private JobRoleFilter jobRoleFilter;
    private double averageDrawMilliseconds;
    private double peakDrawMilliseconds;
    private bool hasDrawTiming;
    private BoutiqueTab? pendingTab = BoutiqueTab.Boutique;
    private BoutiqueTab activeTab = BoutiqueTab.Boutique;
    private bool settingsLayoutExpanded;
    private bool settingsAppearanceExpanded;
    private bool settingsTooltipExpanded;
    private bool settingsWardrobeSyncExpanded;
    private bool settingsCombatExpanded;
    private bool settingsUiProjectExpanded;
    private bool helpBoutiqueExpanded;
    private bool helpWardrobeExpanded;
    private bool helpFavoritesExpanded;
    private bool helpDesignsExpanded;
    private bool helpEorzeaExpanded;
    private bool helpThemesExpanded;
    private Guid? selectedFavoriteListId;
    private EquipmentSlot? favoriteSlotFilter;
    private string favoriteSearchText = string.Empty;
    private string favoriteListName = string.Empty;
    private string favoriteRenameName = string.Empty;
    private string? favoriteStatus;
    private bool customBackgroundPushed;
    private CrystariumProfileStyleStack profileStyleStack;
    private IDisposable? titleFontScope;

    public BoutiqueWindow(
        BoutiqueSessionController sessionController,
        IAppearanceService appearanceService,
        IPenumbraService penumbraService,
        ConfigurationStore configuration,
        IItemRepository itemRepository,
        EquipmentBrowserController browser,
        ITextureProvider textureProvider,
        ItemRarityColorResolver itemRarityColors,
        IPlayerState playerState,
        string? catalogError,
        IStainRepository stainRepository,
        LoadoutLibrary loadoutLibrary,
        string? loadoutError,
        EorzeaCollectionImportService eorzeaCollection,
        BoutiqueFontSet fonts,
        string assetDirectory,
        Action<BoutiqueLoadout> showDesignReference,
        Action toggleWardrobe,
        Action showWelcomeGuide,
        Action closeSession)
        : base("The Crystarium Boutique###CrystariumBoutiqueMain")
    {
        this.sessionController = sessionController ?? throw new ArgumentNullException(nameof(sessionController));
        session = sessionController.Session;
        this.appearanceService = appearanceService ?? throw new ArgumentNullException(nameof(appearanceService));
        this.penumbraService = penumbraService ?? throw new ArgumentNullException(nameof(penumbraService));
        this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        this.itemRepository = itemRepository ?? throw new ArgumentNullException(nameof(itemRepository));
        this.browser = browser ?? throw new ArgumentNullException(nameof(browser));
        this.textureProvider = textureProvider ?? throw new ArgumentNullException(nameof(textureProvider));
        this.itemRarityColors = itemRarityColors ?? throw new ArgumentNullException(nameof(itemRarityColors));
        this.playerState = playerState ?? throw new ArgumentNullException(nameof(playerState));
        this.fonts = fonts ?? throw new ArgumentNullException(nameof(fonts));
        ArgumentException.ThrowIfNullOrWhiteSpace(assetDirectory);
        equipmentSlotTexture = textureProvider.GetFromFile(
            Path.Combine(assetDirectory, "images", "equipment-slots.png"));
        previousItemTexture = textureProvider.GetFromFile(
            Path.Combine(assetDirectory, "images", "previous-item.png"));
        crystalWardrobeTexture = textureProvider.GetFromFile(
            Path.Combine(assetDirectory, "images", "crystal-wardrobe.png"));
        crystariumBackgroundTexture = textureProvider.GetFromFile(
            Path.Combine(assetDirectory, "images", "crystarium-stained-glass.png"));
        crystariumItemFrameTexture = textureProvider.GetFromFile(
            Path.Combine(assetDirectory, "images", "crystarium-item-frame-cornered.png"));
        favoriteBadgeTexture = textureProvider.GetFromFile(
            Path.Combine(assetDirectory, "images", "favorite-badge.png"));
        this.catalogError = catalogError;
        this.toggleWardrobe = toggleWardrobe ?? throw new ArgumentNullException(nameof(toggleWardrobe));
        this.showWelcomeGuide = showWelcomeGuide ?? throw new ArgumentNullException(nameof(showWelcomeGuide));
        this.closeSession = closeSession ?? throw new ArgumentNullException(nameof(closeSession));
        stainCount = stainRepository.Count;
        loadoutPanel = new LoadoutPanel(
            loadoutLibrary,
            sessionController,
            browser,
            eorzeaCollection,
            stainRepository,
            itemRepository,
            textureProvider,
            itemRarityColors,
            configuration,
            fonts,
            HandleLoadoutFeedback,
            showDesignReference,
            loadoutError);

        Size = BoutiqueTheme.DefaultWindowSize;
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public override void Draw()
    {
        BoutiqueScaleStyle.ApplyResponsiveWindowFontScale(
            configuration.Current.InterfaceScalePercent,
            BoutiqueTheme.DefaultWindowSize);
        var startedAt = Stopwatch.GetTimestamp();
        try
        {
            PluginBackgroundStyle.DrawCrystariumBackdrop(
                configuration.Current,
                crystariumBackgroundTexture);
            BoutiqueTheme.DrawCrystariumTitleBarRule(configuration.Current);
            using var bodyFont = fonts.Body.Push();
            var restoreCrystariumContentText = BoutiqueTheme.IsCrystariumProfile(configuration.Current);
            if (restoreCrystariumContentText)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, BoutiqueTheme.CrystariumText);
            }

            try
            {
                var tabRequest = pendingTab;
                pendingTab = null;
                ImGui.PushStyleColor(
                    ImGuiCol.TabActive,
                    BoutiqueTheme.GetTabActiveColor(configuration.Current));
                ImGui.PushStyleColor(
                    ImGuiCol.TabUnfocusedActive,
                    BoutiqueTheme.GetTabUnfocusedActiveColor(configuration.Current));
                ImGui.PushStyleColor(
                    ImGuiCol.TabHovered,
                    BoutiqueTheme.GetTabHoveredColor(configuration.Current));
                using (fonts.Navigation.Push())
                {
                    if (ImGui.BeginTabBar("##BoutiquePrimaryTabs"))
                    {
                        DrawPrimaryTab(BoutiqueTab.Boutique, "Crystarium Boutique", tabRequest);
                        DrawPrimaryTab(BoutiqueTab.Favorites, "Favorites", tabRequest);
                        DrawPrimaryTab(BoutiqueTab.Designs, "Designs", tabRequest);
                        DrawPrimaryTab(BoutiqueTab.Settings, "Settings", tabRequest);
                        DrawPrimaryTab(BoutiqueTab.Help, "Help", tabRequest);
                        DrawPrimaryTab(BoutiqueTab.Info, "Info", tabRequest);
                        ImGui.EndTabBar();
                    }
                }

                ImGui.PopStyleColor(3);
            }
            finally
            {
                if (restoreCrystariumContentText)
                {
                    ImGui.PopStyleColor();
                }
            }

        }
        finally
        {
            RecordDrawDuration(Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds);
        }
    }

    public override void PreDraw()
    {
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
    }

    public override void OnClose()
    {
        ResetAfterClose();
        closeSession();
    }

    public override void OnOpen()
    {
        if (configuration.Current.ShowWelcomeGuideOnOpen)
        {
            showWelcomeGuide();
        }
    }

    public void RequestSettingsTab()
        => pendingTab = BoutiqueTab.Settings;

    public void ResetAfterClose()
    {
        previewStatus = null;
        previewStatusIsError = false;
        previewError = null;
        loadoutPanel.ResetActiveDesign();
        pendingTab = BoutiqueTab.Boutique;
        activeTab = BoutiqueTab.Boutique;
    }

    private void DrawPrimaryTab(BoutiqueTab tab, string label, BoutiqueTab? tabRequest)
    {
        var flags = tabRequest == tab
            ? ImGuiTabItemFlags.SetSelected
            : ImGuiTabItemFlags.None;
        var crystariumProfile = BoutiqueTheme.IsCrystariumProfile(configuration.Current);
        var expectedActive = tabRequest == tab || (tabRequest is null && activeTab == tab);
        if (crystariumProfile)
        {
            var tabTextColor = BoutiqueTheme.GetBoutiqueTabTextColor(configuration.Current);
            if (!expectedActive)
            {
                tabTextColor.W *= 0.50f;
            }

            ImGui.PushStyleColor(ImGuiCol.Text, tabTextColor);
        }

        var tabOpen = ImGui.BeginTabItem(label, flags);
        if (crystariumProfile)
        {
            ImGui.PopStyleColor();
        }

        if (tabOpen)
        {
            activeTab = tab;
        }

        if (crystariumProfile && !tabOpen)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.Alpha, ImGui.GetStyle().Alpha * 0.50f);
            BoutiqueTheme.DrawLastGoldControlFrame(configuration.Current);
            ImGui.PopStyleVar();
        }
        else
        {
            BoutiqueTheme.DrawLastGoldControlFrame(configuration.Current);
        }

        if (!tabOpen)
        {
            return;
        }

        using (fonts.Body.Push())
        {
            if (BoutiqueTheme.IsCrystariumProfile(configuration.Current))
            {
                ImGui.SetCursorPosY(Math.Max(
                    0f,
                    ImGui.GetCursorPosY() - ImGui.GetStyle().ItemSpacing.Y));
            }

            BoutiqueTheme.DrawProfileHorizontalRule(
                configuration.Current,
                BoutiqueTheme.GetHorizontalRuleColor(configuration.Current),
                BoutiqueTheme.IsCrystariumProfile(configuration.Current)
                    ? BoutiqueTheme.ProminentRuleThickness
                    : BoutiqueTheme.RuleThickness);
            ImGui.Spacing();

            switch (tab)
            {
                case BoutiqueTab.Boutique:
                    DrawBoutiqueTab();
                    break;
                case BoutiqueTab.Designs:
                    loadoutPanel.DrawDesignsTab();
                    break;
                case BoutiqueTab.Favorites:
                    DrawFavoritesTab();
                    break;
                case BoutiqueTab.Settings:
                    DrawSettingsTab();
                    break;
                case BoutiqueTab.Help:
                    DrawHelpTab();
                    break;
                case BoutiqueTab.Info:
                    DrawInfoTab();
                    break;
            }
        }

        ImGui.EndTabItem();
    }

    private void DrawBoutiqueTab()
    {
        var transparentWorkspace = BoutiqueTheme.UsesCrystariumBackdrop(configuration.Current);
        ImGui.PushStyleColor(
            ImGuiCol.Text,
            BoutiqueTheme.GetBoutiqueTabTextColor(configuration.Current));
        if (transparentWorkspace)
        {
            ImGui.PushStyleColor(ImGuiCol.ChildBg, Vector4.Zero);
        }

        if (ImGui.BeginChild(
                "##BoutiqueMainArea",
                Vector2.Zero,
                false,
                ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
        {
            BoutiqueScaleStyle.ApplyResponsiveWindowFontScale(
                configuration.Current.InterfaceScalePercent,
                BoutiqueTheme.DefaultWindowSize);
            DrawWorkspace();
        }

        ImGui.EndChild();
        if (transparentWorkspace)
        {
            ImGui.PopStyleColor();
        }

        ImGui.PopStyleColor();
    }

    private void DrawInfoTab()
    {
        var workspaceStyleColors = PushSimpleCrystariumWorkspaceStyle();
        if (!ImGui.BeginChild(
                "##InfoWorkspace",
                Vector2.Zero,
                !BoutiqueTheme.UsesTransparentParentContent(configuration.Current)))
        {
            ImGui.EndChild();
            PopSimpleCrystariumWorkspaceStyle(workspaceStyleColors);
            return;
        }

        using (fonts.Navigation.Push())
        {
            ImGui.TextColored(BoutiqueTheme.GetHeadingColor(configuration.Current), "PLUGIN INFORMATION");
        }
        ImGui.TextUnformatted("The Crystarium Boutique");
        ImGui.TextColored(BoutiqueTheme.MutedText, VersionLabel);
        ImGui.TextColored(BoutiqueTheme.MutedText, "Local-only Dalamud development build");
        ImGui.TextColored(BoutiqueTheme.MutedText, "Created and maintained by Aesthria");
        ImGui.TextColored(BoutiqueTheme.MutedText, "License: GNU AGPL v3 only (AGPL-3.0-only)");
        ImGui.TextWrapped("Independent, unofficial fan project. Not affiliated with or endorsed by Square Enix, XIVLauncher/Dalamud, Glamourer, or Penumbra.");
        ImGui.Separator();

        if (ImGui.BeginTable(
                "##InfoStatusColumns",
                2,
                ImGuiTableFlags.SizingStretchSame
                | ImGuiTableFlags.NoSavedSettings
                | ImGuiTableFlags.BordersInnerV))
        {
            ImGui.TableSetupColumn("Runtime", ImGuiTableColumnFlags.WidthStretch, 1f);
            ImGui.TableSetupColumn("Technical", ImGuiTableColumnFlags.WidthStretch, 1f);
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            DrawCompactStatus();
            ImGui.TableNextColumn();
            DrawTechnicalStatusSettings();
            ImGui.EndTable();
        }

        ImGui.Spacing();
        BoutiqueTheme.DrawProfileHorizontalRule(
            configuration.Current,
            BoutiqueTheme.GetHorizontalRuleColor(configuration.Current));
        ImGui.Spacing();
        DrawDebugSection();

        ImGui.EndChild();
        PopSimpleCrystariumWorkspaceStyle(workspaceStyleColors);
    }

    private void DrawFavoritesTab()
    {
        var favorites = configuration.Current.Favorites;
        var workspaceStyleColors = PushSimpleCrystariumWorkspaceStyle();
        if (!ImGui.BeginChild(
                "##FavoritesWorkspace",
                Vector2.Zero,
                !BoutiqueTheme.UsesTransparentParentContent(configuration.Current)))
        {
            ImGui.EndChild();
            PopSimpleCrystariumWorkspaceStyle(workspaceStyleColors);
            return;
        }

        using (fonts.Navigation.Push())
        {
            ImGui.TextColored(BoutiqueTheme.GetHeadingColor(configuration.Current), "FAVORITES");
        }

        ImGui.TextWrapped("Right click any Boutique or Favorites item to add it to Favorites and one or more named lists.");
        if (ImGui.BeginTable(
                "##FavoriteFilters",
                3,
                ImGuiTableFlags.SizingStretchSame | ImGuiTableFlags.NoSavedSettings))
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.TextUnformatted("LIST");
            ImGui.SetNextItemWidth(-1f);
            var selectedLabel = selectedFavoriteListId is { } selectedId
                ? favorites.Lists.FirstOrDefault(list => list.Id == selectedId)?.Name ?? "All Favorites"
                : "All Favorites";
            if (BoutiqueTheme.BeginResponsiveCombo(
                    configuration.Current,
                    "##FavoriteListFilter",
                    selectedLabel,
                    BoutiqueTheme.GetBoutiqueTabTextColor(configuration.Current)))
            {
                if (ImGui.Selectable("All Favorites", selectedFavoriteListId is null))
                {
                    selectedFavoriteListId = null;
                    favoriteRenameName = string.Empty;
                }

                foreach (var list in favorites.Lists.OrderBy(list => list.Name, StringComparer.OrdinalIgnoreCase))
                {
                    if (ImGui.Selectable(list.Name, selectedFavoriteListId == list.Id))
                    {
                        selectedFavoriteListId = list.Id;
                        favoriteRenameName = list.Name;
                    }
                }

                BoutiqueTheme.EndResponsiveCombo();
            }

            ImGui.TableNextColumn();
            ImGui.TextUnformatted("ITEM TYPE");
            ImGui.SetNextItemWidth(-1f);
            var slotLabel = favoriteSlotFilter is { } selectedSlot
                ? EquipmentSlotDefinitions.GetDisplayName(selectedSlot)
                : "Show All Item Types";
            if (BoutiqueTheme.BeginResponsiveCombo(
                    configuration.Current,
                    "##FavoriteSlotFilter",
                    slotLabel,
                    BoutiqueTheme.GetBoutiqueTabTextColor(configuration.Current)))
            {
                if (ImGui.Selectable("Show All Item Types", favoriteSlotFilter is null))
                {
                    favoriteSlotFilter = null;
                }

                foreach (var definition in BoutiqueSlotButtons)
                {
                    if (ImGui.Selectable(definition.DisplayName, favoriteSlotFilter == definition.Slot))
                    {
                        favoriteSlotFilter = definition.Slot;
                    }
                }

                BoutiqueTheme.EndResponsiveCombo();
            }

            ImGui.TableNextColumn();
            ImGui.TextUnformatted("SEARCH");
            ImGui.SetNextItemWidth(-1f);
            ImGui.InputTextWithHint(
                "##FavoriteSearch",
                "Item or category",
                ref favoriteSearchText,
                128);
            ImGui.EndTable();
        }

        ImGui.SetNextItemWidth(Math.Max(140f, Math.Min(280f, ImGui.GetContentRegionAvail().X * 0.45f)));
        ImGui.InputTextWithHint("##NewFavoriteList", "New list name", ref favoriteListName, 64);
        ImGui.SameLine();
        if (BoutiqueTheme.FramedButton(configuration.Current, "Create List"))
        {
            try
            {
                var list = favorites.CreateList(favoriteListName);
                selectedFavoriteListId = list.Id;
                favoriteRenameName = list.Name;
                favoriteListName = string.Empty;
                favoriteStatus = $"Created {list.Name}.";
                configuration.Save();
            }
            catch (ArgumentException exception)
            {
                favoriteStatus = exception.Message;
            }
        }

        if (selectedFavoriteListId is { } activeListId
            && favorites.Lists.FirstOrDefault(list => list.Id == activeListId) is { } activeList)
        {
            ImGui.SetNextItemWidth(Math.Max(140f, Math.Min(280f, ImGui.GetContentRegionAvail().X * 0.35f)));
            ImGui.InputText("##RenameFavoriteList", ref favoriteRenameName, 64);
            ImGui.SameLine();
            if (BoutiqueTheme.FramedButton(configuration.Current, "Rename"))
            {
                try
                {
                    favorites.RenameList(activeListId, favoriteRenameName);
                    favoriteStatus = $"Renamed list to {activeList.Name}.";
                    configuration.Save();
                }
                catch (ArgumentException exception)
                {
                    favoriteStatus = exception.Message;
                }
            }

            ImGui.SameLine();
            if (BoutiqueTheme.FramedButton(configuration.Current, "Delete List"))
            {
                favorites.DeleteList(activeListId);
                selectedFavoriteListId = null;
                favoriteRenameName = string.Empty;
                favoriteStatus = $"Deleted {activeList.Name}; its items remain in All Favorites and any other lists.";
                configuration.Save();
            }
        }

        if (!string.IsNullOrWhiteSpace(favoriteStatus))
        {
            ImGui.TextWrapped(favoriteStatus);
        }

        BoutiqueTheme.DrawProfileHorizontalRule(
            configuration.Current,
            BoutiqueTheme.GetHorizontalRuleColor(configuration.Current),
            BoutiqueTheme.RuleThickness);

        var favoriteItems = favorites.GetItems(selectedFavoriteListId)
            .Select(favorite => TryResolveFavorite(favorite, favoriteSlotFilter))
            .Where(result => result is not null)
            .Select(result => result!.Value)
            .Where(result => FavoriteMatchesSearch(result.Entry.PrimaryItem, favoriteSearchText))
            .ToArray();
        var favoritesFooterLayout = CalculatePreviousItemOnlyFooterLayout(
            ImGui.GetContentRegionAvail().X);
        var favoritesGridHeight = Math.Max(
            ImGui.GetTextLineHeightWithSpacing(),
            ImGui.GetContentRegionAvail().Y
                - favoritesFooterLayout.Height
                - BoutiqueTheme.PreviousItemButtonMargin);
        if (ImGui.BeginChild(
                "##FavoritesGridRegion",
                new Vector2(0f, favoritesGridHeight),
                false))
        {
            if (favoriteItems.Length == 0)
            {
                ImGui.TextColored(BoutiqueTheme.MutedText, "No items are saved in this view.");
            }
            else
            {
                var columns = Math.Clamp((int)(ImGui.GetContentRegionAvail().X / 150f), 3, 8);
                const float tileHeight = 150f;
                if (ImGui.BeginTable(
                        "##FavoritesGrid",
                        columns,
                        ImGuiTableFlags.SizingStretchSame | ImGuiTableFlags.NoPadInnerX))
                {
                    for (var index = 0; index < favoriteItems.Length; index++)
                    {
                        if (index % columns == 0)
                        {
                            ImGui.TableNextRow(ImGuiTableRowFlags.None, tileHeight);
                        }

                        ImGui.TableSetColumnIndex(index % columns);
                        var favorite = favoriteItems[index];
                        DrawItemTile(
                            favorite.Entry,
                            index,
                            tileHeight,
                            favorite.Slot,
                            "Favorite",
                            selectedFavoriteListId);
                    }

                    ImGui.EndTable();
                }
            }
        }

        ImGui.EndChild();
        var favoritesFooterStart = ImGui.GetCursorPos();
        var favoritesFooterWidth = ImGui.GetContentRegionAvail().X;
        DrawPreviousItemButton(
            favoritesFooterStart,
            favoritesFooterWidth,
            CalculatePreviousItemOnlyFooterLayout(favoritesFooterWidth));

        ImGui.EndChild();
        PopSimpleCrystariumWorkspaceStyle(workspaceStyleColors);
    }

    private (AppearanceEntry Entry, EquipmentSlot Slot)? TryResolveFavorite(
        Core.Favorites.FavoriteItem favorite,
        EquipmentSlot? requiredSlot)
    {
        if (requiredSlot is { } filteredSlot)
        {
            return TryResolveFavoriteForSlot(favorite.ItemId, filteredSlot);
        }

        foreach (var slot in EquipmentSlotDefinitions.All.Select(definition => definition.Slot).Distinct())
        {
            var resolved = TryResolveFavoriteForSlot(favorite.ItemId, slot);
            if (resolved is not null)
            {
                return resolved;
            }
        }

        return null;
    }

    private (AppearanceEntry Entry, EquipmentSlot Slot)? TryResolveFavoriteForSlot(
        uint itemId,
        EquipmentSlot slot)
    {
        if (!itemRepository.TryGetItem(itemId, slot, out var item))
        {
            return null;
        }

        var entry = itemRepository.TryGetAppearance(item.AppearanceKey, slot, out var appearance)
            ? appearance with { PrimaryItem = item }
            : new AppearanceEntry(item.AppearanceKey, item, ImmutableArray.Create(item));
        return (entry, slot);
    }

    private static bool FavoriteMatchesSearch(EquipmentItem item, string search)
        => FavoriteViewFilter.Matches(item, null, search);

    private void DrawFavoriteManagementPopup(
        EquipmentItem item,
        bool favoriteView = false,
        Guid? listContext = null)
    {
        if (!ImGui.BeginPopup($"##FavoriteItem{item.ItemId}"))
        {
            return;
        }

        var favorites = configuration.Current.Favorites;
        ImGui.TextWrapped(item.Name);
        ImGui.Separator();
        var favorite = favorites.Items.FirstOrDefault(entry => entry.ItemId == item.ItemId);
        if (favorite is null)
        {
            if (ImGui.MenuItem("Add to All Favorites"))
            {
                favorites.Add(item.ItemId);
                configuration.Save();
            }
        }
        else
        {
            if (favoriteView
                && listContext is { } listId
                && favorite.ListIds.Contains(listId)
                && ImGui.MenuItem("Remove from this list"))
            {
                favorites.SetMembership(item.ItemId, listId, false);
                configuration.Save();
                ImGui.EndPopup();
                return;
            }

            if (ImGui.MenuItem("Remove from Favorites"))
            {
                favorites.Remove(item.ItemId);
                configuration.Save();
                ImGui.EndPopup();
                return;
            }
        }

        if (favorites.Lists.Count == 0)
        {
            ImGui.TextColored(BoutiqueTheme.MutedText, "Create named lists on the Favorites tab.");
        }
        else
        {
            ImGui.Separator();
            ImGui.TextUnformatted("Named lists");
            foreach (var list in favorites.Lists.OrderBy(list => list.Name, StringComparer.OrdinalIgnoreCase))
            {
                var member = favorite?.ListIds.Contains(list.Id) == true;
                if (ImGui.Checkbox($"{list.Name}##FavoriteMembership{item.ItemId}{list.Id:N}", ref member))
                {
                    favorites.SetMembership(item.ItemId, list.Id, member);
                    configuration.Save();
                    favorite = favorites.Items.FirstOrDefault(entry => entry.ItemId == item.ItemId);
                }
            }
        }

        ImGui.EndPopup();
    }

    private void DrawSettingsTab()
    {
        var workspaceStyleColors = PushSimpleCrystariumWorkspaceStyle();
        if (!ImGui.BeginChild(
                "##SettingsWorkspace",
                Vector2.Zero,
                !BoutiqueTheme.UsesTransparentParentContent(configuration.Current)))
        {
            ImGui.EndChild();
            PopSimpleCrystariumWorkspaceStyle(workspaceStyleColors);
            return;
        }

        using (fonts.Navigation.Push())
        {
            ImGui.TextColored(BoutiqueTheme.GetHeadingColor(configuration.Current), "SETTINGS");
        }
        ImGui.TextWrapped("Presentation and safety preferences are saved locally on this PC.");
        ImGui.Separator();

        if (DrawSettingsSectionHeader(
                "THEMES",
                "UiProject",
                ref settingsUiProjectExpanded))
        {
            DrawUiProfileSettings();
        }

        if (DrawSettingsSectionHeader(
                "BOUTIQUE ITEM GRID SIZE",
                "Layout",
                ref settingsLayoutExpanded))
        {
            DrawGridLayoutSettings();
        }

        if (DrawSettingsSectionHeader(
                "UI OPTIONS",
                "Appearance",
                ref settingsAppearanceExpanded))
        {
            DrawPluginAppearanceSettings();
        }

        if (DrawSettingsSectionHeader(
                "ITEM TOOLTIP",
                "Tooltip",
                ref settingsTooltipExpanded))
        {
            DrawTooltipSettings();
        }

        if (DrawSettingsSectionHeader(
                "WARDROBE SYNCHRONIZATION",
                "WardrobeSync",
                ref settingsWardrobeSyncExpanded))
        {
            DrawWardrobeSynchronizationSettings();
        }

        if (DrawSettingsSectionHeader(
                "COMBAT SAFETY",
                "Combat",
                ref settingsCombatExpanded))
        {
            DrawCombatSafetySettings();
        }

        ImGui.EndChild();
        PopSimpleCrystariumWorkspaceStyle(workspaceStyleColors);
    }

    private bool DrawSettingsSectionHeader(string title, string id, ref bool expanded)
    {
        var indicator = expanded ? "-  " : string.Empty;
        bool clicked;
        using (fonts.SectionHeading.Push())
        {
            clicked = BoutiqueTheme.FramedButton(
                configuration.Current,
                $"{indicator}{title}##SettingsSection{id}",
                new Vector2(-1f, 0f));
        }

        if (clicked)
        {
            expanded = !expanded;
        }

        return expanded;
    }

    private void DrawGridLayoutSettings()
    {
        using (fonts.Helper.Push())
        {
            ImGui.TextWrapped("Options for the number of Rows & Columns displayed in the Crystarium Boutique");
        }

        if (!ImGui.BeginTable(
                "##GridLayoutSettings",
                GridLayoutOptions.Length,
                ImGuiTableFlags.SizingStretchSame | ImGuiTableFlags.NoSavedSettings))
        {
            return;
        }

        ImGui.TableNextRow();
        foreach (var (rows, columns) in GridLayoutOptions)
        {
            ImGui.TableNextColumn();
            var selected = configuration.Current.AppearanceGridRows == rows
                && configuration.Current.AppearanceGridColumns == columns;
            ImGui.BeginDisabled();
            BoutiqueTheme.FramedCheckbox(
                configuration.Current,
                $"##GridLayoutActive{rows}x{columns}",
                ref selected);
            ImGui.EndDisabled();
            ImGui.SameLine();
            if (BoutiqueTheme.FramedButton(
                    configuration.Current,
                    $"{rows} x {columns}##GridLayout{rows}x{columns}",
                    new Vector2(-1f, 0f)))
            {
                SetGridLayout(rows, columns);
            }
        }

        ImGui.EndTable();
    }

    private void DrawPluginAppearanceSettings()
    {
        var interfaceScale = configuration.Current.InterfaceScalePercent;
        ImGui.TextUnformatted("Boutique interface scale");
        ImGui.SetNextItemWidth(GetSettingsControlWidth());
        if (DrawNumericSliderInt("##BoutiqueInterfaceScale", ref interfaceScale, 75, 200))
        {
            configuration.Current.InterfaceScalePercent = interfaceScale;
        }

        SaveConfigurationAfterSliderEdit();
        if (BoutiqueTheme.FramedButton(configuration.Current, "Reset interface scale"))
        {
            configuration.Current.InterfaceScalePercent
                = PluginConfiguration.DefaultInterfaceScalePercent;
            configuration.Save();
        }

        ImGui.TextColored(
            BoutiqueTheme.MutedText,
            "Sets the Boutique's own UI size. Text and controls also grow automatically as a Boutique window is enlarged. Default: 125%. This does not rebuild fonts or change other plugins.");
        DrawAppearanceOptionRule();

        if (BoutiqueTheme.UsesCrystariumBackdrop(configuration.Current))
        {
            ImGui.TextColored(
                BoutiqueTheme.GetSecondaryTextColor(configuration.Current),
                "SIMPLE color controls remain saved while a stained-glass theme is active. Hover, Crystal Wardrobe, and Save button options are intentionally shared by every theme.");
            DrawAppearanceOptionRule();

            var backgroundDarkness = configuration.Current.CrystariumBackgroundDarknessPercent;
            ImGui.TextUnformatted("Stained-glass background darkness");
            ImGui.SetNextItemWidth(GetSettingsControlWidth());
            if (DrawNumericSliderInt(
                    "##CrystariumBackgroundDarkness",
                    ref backgroundDarkness,
                    0,
                    100))
            {
                configuration.Current.CrystariumBackgroundDarknessPercent = backgroundDarkness;
            }

            SaveConfigurationAfterSliderEdit();
            if (BoutiqueTheme.FramedButton(configuration.Current, "Reset stained-glass darkness"))
            {
                configuration.Current.CrystariumBackgroundDarknessPercent
                    = PluginConfiguration.DefaultCrystariumBackgroundDarknessPercent;
                configuration.Save();
            }

            ImGui.TextColored(
                BoutiqueTheme.CrystariumMutedText,
                "Darkens only the stained-glass sheet across Boutique, Designs, Settings, Help, Info, Crystal Wardrobe, and generated design-list windows. Controls, icons, frames, and text retain their existing brightness. Default: 30%.");
            DrawAppearanceOptionRule();
        }

        var pluginTransparency = configuration.Current.PluginTransparencyPercent;
        ImGui.TextUnformatted("Whole-window transparency");
        ImGui.SetNextItemWidth(GetSettingsControlWidth());
        if (DrawNumericSliderInt("##PluginTransparency", ref pluginTransparency, 0, 90))
        {
            configuration.Current.PluginTransparencyPercent = pluginTransparency;
        }

        SaveConfigurationAfterSliderEdit();
        ImGui.TextColored(BoutiqueTheme.MutedText, "Limited to 90% so the Settings tab remains recoverable.");
        DrawAppearanceOptionRule();

        var backgroundTransparency = configuration.Current.BackgroundTransparencyPercent;
        ImGui.TextUnformatted("Background-only transparency");
        ImGui.SetNextItemWidth(GetSettingsControlWidth());
        if (DrawNumericSliderInt("##BackgroundTransparency", ref backgroundTransparency, 0, 100))
        {
            configuration.Current.BackgroundTransparencyPercent = backgroundTransparency;
        }

        SaveConfigurationAfterSliderEdit();
        if (BoutiqueTheme.FramedButton(configuration.Current, "Reset background transparency"))
        {
            configuration.Current.BackgroundTransparencyPercent
                = PluginConfiguration.DefaultBackgroundTransparencyPercent;
            configuration.Save();
        }

        ImGui.TextColored(
            BoutiqueTheme.MutedText,
            "Changes the window background while keeping controls, text, icons, and overlays fully opaque. Default: 20%.");
        DrawAppearanceOptionRule();
        var useCustomBackground = configuration.Current.UseCustomBackgroundColor;
        if (BoutiqueTheme.FramedCheckbox(
                configuration.Current,
                "Use a custom plugin background color",
                ref useCustomBackground))
        {
            configuration.Current.UseCustomBackgroundColor = useCustomBackground;
            configuration.Save();
        }

        var backgroundColor = new Vector3(
            configuration.Current.BackgroundColorRed / 255f,
            configuration.Current.BackgroundColorGreen / 255f,
            configuration.Current.BackgroundColorBlue / 255f);
        ImGui.BeginDisabled(!useCustomBackground);
        ImGui.SetNextItemWidth(GetSettingsControlWidth());
        var backgroundColorChanged = ImGui.ColorEdit3("##PluginBackgroundColor", ref backgroundColor);
        BoutiqueTheme.DrawLastGoldControlFrame(configuration.Current);
        if (backgroundColorChanged)
        {
            configuration.Current.BackgroundColorRed = (int)Math.Round(backgroundColor.X * 255f);
            configuration.Current.BackgroundColorGreen = (int)Math.Round(backgroundColor.Y * 255f);
            configuration.Current.BackgroundColorBlue = (int)Math.Round(backgroundColor.Z * 255f);
        }

        SaveConfigurationAfterSliderEdit();
        if (BoutiqueTheme.FramedButton(configuration.Current, "Reset background color"))
        {
            configuration.Current.BackgroundColorRed = 31;
            configuration.Current.BackgroundColorGreen = 34;
            configuration.Current.BackgroundColorBlue = 45;
            configuration.Save();
        }

        ImGui.EndDisabled();
        ImGui.TextColored(
            BoutiqueTheme.MutedText,
            "Applies the selected RGB background to the Boutique, Wardrobe, and generated design-list windows.");
        DrawAppearanceOptionRule();
        ImGui.TextUnformatted("Item icon and Wardrobe card border color");
        var itemBorderColor = new Vector3(
            configuration.Current.ItemBorderColorRed / 255f,
            configuration.Current.ItemBorderColorGreen / 255f,
            configuration.Current.ItemBorderColorBlue / 255f);
        ImGui.SetNextItemWidth(GetSettingsControlWidth());
        var itemBorderColorChanged = ImGui.ColorEdit3("##ItemBorderColor", ref itemBorderColor);
        BoutiqueTheme.DrawLastGoldControlFrame(configuration.Current);
        if (itemBorderColorChanged)
        {
            configuration.Current.ItemBorderColorRed = (int)Math.Round(itemBorderColor.X * 255f);
            configuration.Current.ItemBorderColorGreen = (int)Math.Round(itemBorderColor.Y * 255f);
            configuration.Current.ItemBorderColorBlue = (int)Math.Round(itemBorderColor.Z * 255f);
        }

        SaveConfigurationAfterSliderEdit();
        if (BoutiqueTheme.FramedButton(configuration.Current, "Reset borders to Antique Gold"))
        {
            configuration.Current.ItemBorderColorRed = 153;
            configuration.Current.ItemBorderColorGreen = 140;
            configuration.Current.ItemBorderColorBlue = 87;
            configuration.Save();
        }

        ImGui.TextColored(
            BoutiqueTheme.MutedText,
            "Defaults to Antique Gold (#998C57) and applies to browser item icons and Crystal Wardrobe cards.");
        DrawAppearanceOptionRule();

        var highlightFavoriteItemFrames = configuration.Current.HighlightFavoriteItemFrames;
        if (ImGui.Checkbox("Highlight Favorite Item Frames", ref highlightFavoriteItemFrames))
        {
            configuration.Current.HighlightFavoriteItemFrames = highlightFavoriteItemFrames;
            configuration.Save();
        }

        ImGui.TextColored(
            BoutiqueTheme.MutedText,
            "Adds the configured colored frame as an optional secondary Favorite indicator. The Favorite badge remains visible when this is off.");
        ImGui.BeginDisabled(!highlightFavoriteItemFrames);
        ImGui.TextUnformatted("Favorite item frame color");
        var favoriteFrameColor = new Vector3(
            configuration.Current.FavoriteFrameColorRed / 255f,
            configuration.Current.FavoriteFrameColorGreen / 255f,
            configuration.Current.FavoriteFrameColorBlue / 255f);
        ImGui.SetNextItemWidth(GetSettingsControlWidth());
        var favoriteFrameColorChanged = ImGui.ColorEdit3(
            "##FavoriteItemFrameColor",
            ref favoriteFrameColor);
        BoutiqueTheme.DrawLastGoldControlFrame(configuration.Current);
        if (favoriteFrameColorChanged)
        {
            configuration.Current.FavoriteFrameColorRed = ToColorByte(favoriteFrameColor.X);
            configuration.Current.FavoriteFrameColorGreen = ToColorByte(favoriteFrameColor.Y);
            configuration.Current.FavoriteFrameColorBlue = ToColorByte(favoriteFrameColor.Z);
        }

        SaveConfigurationAfterSliderEdit();
        if (BoutiqueTheme.FramedButton(configuration.Current, "Reset Favorite frame color"))
        {
            configuration.Current.FavoriteFrameColorRed
                = PluginConfiguration.DefaultFavoriteFrameColorRed;
            configuration.Current.FavoriteFrameColorGreen
                = PluginConfiguration.DefaultFavoriteFrameColorGreen;
            configuration.Current.FavoriteFrameColorBlue
                = PluginConfiguration.DefaultFavoriteFrameColorBlue;
            configuration.Save();
        }

        ImGui.TextColored(
            BoutiqueTheme.MutedText,
            "Default: medium-bright pink (#E754A6).");
        ImGui.EndDisabled();
        DrawAppearanceOptionRule();

        ImGui.TextUnformatted("Hovered item highlight color");
        var selectedItemHighlightColor = new Vector3(
            configuration.Current.SelectedItemHighlightColorRed / 255f,
            configuration.Current.SelectedItemHighlightColorGreen / 255f,
            configuration.Current.SelectedItemHighlightColorBlue / 255f);
        ImGui.SetNextItemWidth(GetSettingsControlWidth());
        var selectedHighlightColorChanged = ImGui.ColorEdit3(
            "##SelectedItemHighlightColor",
            ref selectedItemHighlightColor);
        BoutiqueTheme.DrawLastGoldControlFrame(configuration.Current);
        if (selectedHighlightColorChanged)
        {
            configuration.Current.SelectedItemHighlightColorRed = ToColorByte(selectedItemHighlightColor.X);
            configuration.Current.SelectedItemHighlightColorGreen = ToColorByte(selectedItemHighlightColor.Y);
            configuration.Current.SelectedItemHighlightColorBlue = ToColorByte(selectedItemHighlightColor.Z);
        }

        SaveConfigurationAfterSliderEdit();
        ImGui.TextUnformatted("Hovered item highlight transparency");
        var hoverTransparency = configuration.Current.SelectedItemHighlightTransparencyPercent;
        ImGui.SetNextItemWidth(GetSettingsControlWidth());
        if (DrawNumericSliderInt(
                "##SelectedItemHighlightTransparency",
                ref hoverTransparency,
                0,
                100))
        {
            configuration.Current.SelectedItemHighlightTransparencyPercent = hoverTransparency;
        }

        SaveConfigurationAfterSliderEdit();
        if (BoutiqueTheme.FramedButton(configuration.Current, "Reset item hover highlight to #0FFCCD"))
        {
            configuration.Current.SelectedItemHighlightColorRed
                = PluginConfiguration.DefaultHighlightColorRed;
            configuration.Current.SelectedItemHighlightColorGreen
                = PluginConfiguration.DefaultHighlightColorGreen;
            configuration.Current.SelectedItemHighlightColorBlue
                = PluginConfiguration.DefaultHighlightColorBlue;
            configuration.Current.SelectedItemHighlightTransparencyPercent
                = PluginConfiguration.DefaultHighlightTransparencyPercent;
            configuration.Save();
        }

        ImGui.TextColored(
            BoutiqueTheme.MutedText,
            "Changes the color and transparency of the temporary outline while the pointer is over an item. In the Crystarium theme this is one outer contour around the metal frame, with no inner border. Clicking leaves no persistent colored frame. Defaults to #0FFCCD at 15% transparency.");
        DrawAppearanceOptionRule();

        ImGui.TextUnformatted("Plugin horizontal bar color");
        var horizontalRuleColor = new Vector3(
            configuration.Current.HorizontalRuleColorRed / 255f,
            configuration.Current.HorizontalRuleColorGreen / 255f,
            configuration.Current.HorizontalRuleColorBlue / 255f);
        ImGui.SetNextItemWidth(GetSettingsControlWidth());
        var horizontalRuleColorChanged = ImGui.ColorEdit3(
            "##HorizontalRuleColor",
            ref horizontalRuleColor);
        BoutiqueTheme.DrawLastGoldControlFrame(configuration.Current);
        if (horizontalRuleColorChanged)
        {
            configuration.Current.HorizontalRuleColorRed = ToColorByte(horizontalRuleColor.X);
            configuration.Current.HorizontalRuleColorGreen = ToColorByte(horizontalRuleColor.Y);
            configuration.Current.HorizontalRuleColorBlue = ToColorByte(horizontalRuleColor.Z);
        }

        SaveConfigurationAfterSliderEdit();
        if (BoutiqueTheme.FramedButton(configuration.Current, "Reset bars to Dark Bronze"))
        {
            configuration.Current.HorizontalRuleColorRed
                = PluginConfiguration.DefaultHorizontalRuleColorRed;
            configuration.Current.HorizontalRuleColorGreen
                = PluginConfiguration.DefaultHorizontalRuleColorGreen;
            configuration.Current.HorizontalRuleColorBlue
                = PluginConfiguration.DefaultHorizontalRuleColorBlue;
            configuration.Save();
        }

        ImGui.TextColored(
            BoutiqueTheme.MutedText,
            "Changes the structural horizontal bars across all Boutique windows in both SIMPLE and Crystarium themes. Default: dark bronze (#3F2F0F).");
        DrawAppearanceOptionRule();

        ImGui.TextUnformatted("Boutique slot and page button color");
        var boutiqueButtonColor = new Vector3(
            configuration.Current.BoutiqueButtonColorRed / 255f,
            configuration.Current.BoutiqueButtonColorGreen / 255f,
            configuration.Current.BoutiqueButtonColorBlue / 255f);
        ImGui.SetNextItemWidth(GetSettingsControlWidth());
        var boutiqueButtonColorChanged = ImGui.ColorEdit3(
            "##BoutiqueButtonColor",
            ref boutiqueButtonColor);
        BoutiqueTheme.DrawLastGoldControlFrame(configuration.Current);
        if (boutiqueButtonColorChanged)
        {
            configuration.Current.BoutiqueButtonColorRed = ToColorByte(boutiqueButtonColor.X);
            configuration.Current.BoutiqueButtonColorGreen = ToColorByte(boutiqueButtonColor.Y);
            configuration.Current.BoutiqueButtonColorBlue = ToColorByte(boutiqueButtonColor.Z);
        }

        SaveConfigurationAfterSliderEdit();
        if (BoutiqueTheme.FramedButton(configuration.Current, "Reset Boutique buttons to Dusty Rose"))
        {
            configuration.Current.BoutiqueButtonColorRed = 125;
            configuration.Current.BoutiqueButtonColorGreen = 69;
            configuration.Current.BoutiqueButtonColorBlue = 87;
            configuration.Save();
        }

        ImGui.TextColored(
            BoutiqueTheme.MutedText,
            "Changes equipment-slot filters, page navigation, and the previous-item button. Crystal Wardrobe, Save, Search Clear, and dialog buttons keep their assigned colors.");
        DrawAppearanceOptionRule();

        ImGui.TextUnformatted("Boutique tab font color");
        var boutiqueButtonTextColor = new Vector3(
            configuration.Current.BoutiqueButtonTextColorRed / 255f,
            configuration.Current.BoutiqueButtonTextColorGreen / 255f,
            configuration.Current.BoutiqueButtonTextColorBlue / 255f);
        ImGui.SetNextItemWidth(GetSettingsControlWidth());
        var boutiqueButtonTextColorChanged = ImGui.ColorEdit3(
            "##BoutiqueButtonTextColor",
            ref boutiqueButtonTextColor);
        BoutiqueTheme.DrawLastGoldControlFrame(configuration.Current);
        if (boutiqueButtonTextColorChanged)
        {
            configuration.Current.BoutiqueButtonTextColorRed = ToColorByte(boutiqueButtonTextColor.X);
            configuration.Current.BoutiqueButtonTextColorGreen = ToColorByte(boutiqueButtonTextColor.Y);
            configuration.Current.BoutiqueButtonTextColorBlue = ToColorByte(boutiqueButtonTextColor.Z);
        }

        SaveConfigurationAfterSliderEdit();
        if (BoutiqueTheme.FramedButton(configuration.Current, "Reset Boutique tab text to Soft Gold"))
        {
            configuration.Current.BoutiqueButtonTextColorRed
                = PluginConfiguration.DefaultBoutiqueTextColorRed;
            configuration.Current.BoutiqueButtonTextColorGreen
                = PluginConfiguration.DefaultBoutiqueTextColorGreen;
            configuration.Current.BoutiqueButtonTextColorBlue
                = PluginConfiguration.DefaultBoutiqueTextColorBlue;
            configuration.Save();
        }

        ImGui.TextColored(
            BoutiqueTheme.MutedText,
            "Changes the Boutique tab, primary menu-tab, window-title, and title-bar control text. Crystal Wardrobe and Save retain their independent font colors. Default: Soft Gold (#FFD48D); inactive menu tabs use 50% opacity.");
        DrawAppearanceOptionRule();

        DrawConfigurableButtonColor(
            "Crystal Wardrobe button color",
            "WardrobeButtonColor",
            new Vector3(
                configuration.Current.WardrobeButtonColorRed / 255f,
                configuration.Current.WardrobeButtonColorGreen / 255f,
                configuration.Current.WardrobeButtonColorBlue / 255f),
            color =>
            {
                configuration.Current.UseCustomWardrobeButtonColor = true;
                configuration.Current.WardrobeButtonColorRed = ToColorByte(color.X);
                configuration.Current.WardrobeButtonColorGreen = ToColorByte(color.Y);
                configuration.Current.WardrobeButtonColorBlue = ToColorByte(color.Z);
            },
            () => configuration.Current.UseCustomWardrobeButtonColor = false,
            "Revert Crystal Wardrobe button to theme default",
            "Changes the Crystal Wardrobe action on the Boutique tab. Revert follows the active SIMPLE or Crystarium theme default.");

        DrawConfigurableButtonColor(
            "Crystal Wardrobe button font color",
            "WardrobeButtonTextColor",
            new Vector3(
                configuration.Current.WardrobeButtonTextColorRed / 255f,
                configuration.Current.WardrobeButtonTextColorGreen / 255f,
                configuration.Current.WardrobeButtonTextColorBlue / 255f),
            color =>
            {
                configuration.Current.UseCustomWardrobeButtonTextColor = true;
                configuration.Current.WardrobeButtonTextColorRed = ToColorByte(color.X);
                configuration.Current.WardrobeButtonTextColorGreen = ToColorByte(color.Y);
                configuration.Current.WardrobeButtonTextColorBlue = ToColorByte(color.Z);
            },
            () => configuration.Current.UseCustomWardrobeButtonTextColor = false,
            "Revert Crystal Wardrobe font to theme default",
            "Changes only the Crystal Wardrobe action's label and local dress-form glyph tint.");

        DrawConfigurableButtonColor(
            "Save button color",
            "SaveButtonColor",
            new Vector3(
                configuration.Current.SaveButtonColorRed / 255f,
                configuration.Current.SaveButtonColorGreen / 255f,
                configuration.Current.SaveButtonColorBlue / 255f),
            color =>
            {
                configuration.Current.UseCustomSaveButtonColor = true;
                configuration.Current.SaveButtonColorRed = ToColorByte(color.X);
                configuration.Current.SaveButtonColorGreen = ToColorByte(color.Y);
                configuration.Current.SaveButtonColorBlue = ToColorByte(color.Z);
            },
            () => configuration.Current.UseCustomSaveButtonColor = false,
            "Revert Save button to theme default",
            "Changes the Save action on the Boutique tab. Revert follows the active SIMPLE or Crystarium theme default.");

        DrawConfigurableButtonColor(
            "Save button font color",
            "SaveButtonTextColor",
            new Vector3(
                configuration.Current.SaveButtonTextColorRed / 255f,
                configuration.Current.SaveButtonTextColorGreen / 255f,
                configuration.Current.SaveButtonTextColorBlue / 255f),
            color =>
            {
                configuration.Current.UseCustomSaveButtonTextColor = true;
                configuration.Current.SaveButtonTextColorRed = ToColorByte(color.X);
                configuration.Current.SaveButtonTextColorGreen = ToColorByte(color.Y);
                configuration.Current.SaveButtonTextColorBlue = ToColorByte(color.Z);
            },
            () => configuration.Current.UseCustomSaveButtonTextColor = false,
            "Revert Save font to theme default",
            "Changes only the Save action's label color.");

        ImGui.TextUnformatted("Filter option background color");
        var filterBackgroundColor = new Vector3(
            configuration.Current.FilterBackgroundColorRed / 255f,
            configuration.Current.FilterBackgroundColorGreen / 255f,
            configuration.Current.FilterBackgroundColorBlue / 255f);
        ImGui.SetNextItemWidth(GetSettingsControlWidth());
        var filterBackgroundColorChanged = ImGui.ColorEdit3(
            "##FilterBackgroundColor",
            ref filterBackgroundColor);
        BoutiqueTheme.DrawLastGoldControlFrame(configuration.Current);
        if (filterBackgroundColorChanged)
        {
            configuration.Current.FilterBackgroundColorRed = ToColorByte(filterBackgroundColor.X);
            configuration.Current.FilterBackgroundColorGreen = ToColorByte(filterBackgroundColor.Y);
            configuration.Current.FilterBackgroundColorBlue = ToColorByte(filterBackgroundColor.Z);
        }

        SaveConfigurationAfterSliderEdit();
        if (BoutiqueTheme.FramedButton(configuration.Current, "Reset filter backgrounds to Warm Charcoal"))
        {
            configuration.Current.FilterBackgroundColorRed = 77;
            configuration.Current.FilterBackgroundColorGreen = 72;
            configuration.Current.FilterBackgroundColorBlue = 75;
            configuration.Save();
        }

        ImGui.TextColored(
            BoutiqueTheme.MutedText,
            "Changes the Expansion, Role, Dye Slots, Search, and Designs field backgrounds. Defaults to Warm Charcoal (#4D484B).");
        DrawAppearanceOptionRule();

        ImGui.TextUnformatted("Filter option font color");
        var filterTextColor = new Vector3(
            configuration.Current.FilterTextColorRed / 255f,
            configuration.Current.FilterTextColorGreen / 255f,
            configuration.Current.FilterTextColorBlue / 255f);
        ImGui.SetNextItemWidth(GetSettingsControlWidth());
        var filterTextColorChanged = ImGui.ColorEdit3(
            "##FilterTextColor",
            ref filterTextColor);
        BoutiqueTheme.DrawLastGoldControlFrame(configuration.Current);
        if (filterTextColorChanged)
        {
            configuration.Current.FilterTextColorRed = ToColorByte(filterTextColor.X);
            configuration.Current.FilterTextColorGreen = ToColorByte(filterTextColor.Y);
            configuration.Current.FilterTextColorBlue = ToColorByte(filterTextColor.Z);
        }

        SaveConfigurationAfterSliderEdit();
        if (BoutiqueTheme.FramedButton(configuration.Current, "Reset filter text to White"))
        {
            configuration.Current.FilterTextColorRed = 255;
            configuration.Current.FilterTextColorGreen = 255;
            configuration.Current.FilterTextColorBlue = 255;
            configuration.Save();
        }

        ImGui.TextColored(
            BoutiqueTheme.MutedText,
            "Changes selected filter text and the choices shown in each filter menu.");
        DrawAppearanceOptionRule();

    }

    private void DrawAppearanceOptionRule()
    {
        ImGui.Spacing();
        BoutiqueTheme.DrawProfileHorizontalRule(
            configuration.Current,
            BoutiqueTheme.GetHorizontalRuleColor(configuration.Current));
        ImGui.Spacing();
    }

    private void DrawConfigurableButtonColor(
        string heading,
        string id,
        Vector3 color,
        Action<Vector3> apply,
        Action reset,
        string resetLabel,
        string helpText)
    {
        ImGui.TextUnformatted(heading);
        ImGui.SetNextItemWidth(GetSettingsControlWidth());
        var colorChanged = ImGui.ColorEdit3($"##{id}", ref color);
        BoutiqueTheme.DrawLastGoldControlFrame(configuration.Current);
        if (colorChanged)
        {
            apply(color);
        }

        SaveConfigurationAfterSliderEdit();
        if (BoutiqueTheme.FramedButton(configuration.Current, $"{resetLabel}##Reset{id}"))
        {
            reset();
            configuration.Save();
        }

        using (fonts.Helper.Push())
        {
            ImGui.TextColored(BoutiqueTheme.MutedText, helpText);
        }
        DrawAppearanceOptionRule();
    }

    private static int ToColorByte(float value)
        => (int)Math.Round(Math.Clamp(value, 0f, 1f) * 255f);

    private void DrawTooltipSettings()
    {
        ImGui.TextWrapped("These controls adjust the anchored acquisition card shown beside a hovered item icon. Tooltips use the plugin's Jupiter game font and only read locally installed game data.");
        if (ImGui.BeginTable(
                "##TooltipVisibilityMode",
                2,
                ImGuiTableFlags.SizingStretchSame | ImGuiTableFlags.NoSavedSettings))
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            DrawTooltipModeButton("Default tooltips", requiresShift: false);
            ImGui.TableNextColumn();
            DrawTooltipModeButton("Hidden tooltips", requiresShift: true);
            ImGui.EndTable();
        }

        ImGui.TextColored(
            BoutiqueTheme.MutedText,
            configuration.Current.TooltipsRequireShift
                ? "Hidden tooltips: hold Shift while hovering an item to show its tooltip."
                : "Default tooltips: item tooltips appear whenever an item is hovered.");
        ImGui.Spacing();
        var tooltipTransparency = configuration.Current.TooltipTransparencyPercent;
        ImGui.TextUnformatted("Overlay transparency");
        ImGui.SetNextItemWidth(GetSettingsControlWidth());
        if (DrawNumericSliderInt("##TooltipTransparency", ref tooltipTransparency, 0, 100))
        {
            configuration.Current.TooltipTransparencyPercent = tooltipTransparency;
        }

        SaveConfigurationAfterSliderEdit();

        var tooltipFontScale = configuration.Current.TooltipFontScalePercent;
        ImGui.TextUnformatted("Jupiter font size");
        ImGui.SetNextItemWidth(GetSettingsControlWidth());
        if (DrawNumericSliderInt("##TooltipFontScale", ref tooltipFontScale, 75, 200))
        {
            configuration.Current.TooltipFontScalePercent = tooltipFontScale;
        }

        SaveConfigurationAfterSliderEdit();
        if (BoutiqueTheme.FramedButton(configuration.Current, "Reset Jupiter font size"))
        {
            configuration.Current.TooltipFontScalePercent
                = PluginConfiguration.DefaultTooltipFontScalePercent;
            configuration.Save();
        }

        ImGui.TextColored(
            BoutiqueTheme.MutedText,
            "The expanded 75-200% range provides four larger steps beyond the original size while retaining a compact option. Default: 100% at the 125% Boutique interface scale.");
        DrawAppearanceOptionRule();

        ImGui.TextUnformatted("Acquisition detail");
        if (ImGui.BeginTable(
                "##TooltipAcquisitionDetail",
                3,
                ImGuiTableFlags.SizingStretchSame | ImGuiTableFlags.NoSavedSettings))
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            DrawTooltipDetailButton("Source only", ItemAcquisitionDetailLevel.SourceOnly);
            ImGui.TableNextColumn();
            DrawTooltipDetailButton("Standard", ItemAcquisitionDetailLevel.Standard);
            ImGui.TableNextColumn();
            DrawTooltipDetailButton("Detailed", ItemAcquisitionDetailLevel.Detailed);
            ImGui.EndTable();
        }

        ImGui.TextColored(
            BoutiqueTheme.MutedText,
            "Source only shows compact badges. Standard adds the source name. Detailed adds locally available NPC, area, cost, recipe, and requirement details.");
        DrawAppearanceOptionRule();

        ImGui.TextUnformatted("Acquisition sources");
        if (ImGui.BeginTable(
                "##TooltipAcquisitionSources",
                2,
                ImGuiTableFlags.SizingStretchSame | ImGuiTableFlags.NoSavedSettings))
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            DrawTooltipSourceToggle(
                "Vendors / exchanges",
                configuration.Current.TooltipShowVendors,
                value => configuration.Current.TooltipShowVendors = value);
            ImGui.TableNextColumn();
            DrawTooltipSourceToggle(
                "Marketboard availability",
                configuration.Current.TooltipShowMarketBoard,
                value => configuration.Current.TooltipShowMarketBoard = value);
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            DrawTooltipSourceToggle(
                "Crafting",
                configuration.Current.TooltipShowCrafting,
                value => configuration.Current.TooltipShowCrafting = value);
            ImGui.TableNextColumn();
            DrawTooltipSourceToggle(
                "Quests",
                configuration.Current.TooltipShowQuests,
                value => configuration.Current.TooltipShowQuests = value);
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            DrawTooltipSourceToggle(
                "Achievements",
                configuration.Current.TooltipShowAchievements,
                value => configuration.Current.TooltipShowAchievements = value);
            ImGui.TableNextColumn();
            DrawTooltipSourceToggle(
                "Seasonal events",
                configuration.Current.TooltipShowSeasonalEvents,
                value => configuration.Current.TooltipShowSeasonalEvents = value);
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            DrawTooltipSourceToggle(
                "Mogstation / Online Store",
                configuration.Current.TooltipShowOnlineStore,
                value => configuration.Current.TooltipShowOnlineStore = value);
            ImGui.TableNextColumn();
            DrawTooltipSourceToggle(
                "Duty drops",
                configuration.Current.TooltipShowDutyDrops,
                value => configuration.Current.TooltipShowDutyDrops = value);
            ImGui.EndTable();
        }

        ImGui.TextColored(
            BoutiqueTheme.MutedText,
            "Marketboard means the item is locally marked marketable; no live price service is contacted. Exact boss/chest drop provenance appears only when installed game data or the versioned local acquisition supplement verifies that exact item relationship.");
        DrawAppearanceOptionRule();

        var showSharedModels = configuration.Current.TooltipShowSharedModels;
        if (BoutiqueTheme.FramedCheckbox(
                configuration.Current,
                "Show other items using the same appearance",
                ref showSharedModels))
        {
            configuration.Current.TooltipShowSharedModels = showSharedModels;
            configuration.Save();
        }

        var sharedModelLimit = configuration.Current.TooltipSharedModelLimit;
        ImGui.BeginDisabled(!showSharedModels);
        ImGui.TextUnformatted("Maximum shared items shown");
        ImGui.SetNextItemWidth(GetSettingsControlWidth());
        using (fonts.Numeric.Push())
        {
            var sharedLimitChanged = ImGui.SliderInt(
                "##TooltipSharedModelLimit",
                ref sharedModelLimit,
                1,
                24,
                "%d");
            BoutiqueTheme.DrawLastGoldControlFrame(configuration.Current);
            if (sharedLimitChanged)
            {
                configuration.Current.TooltipSharedModelLimit = sharedModelLimit;
            }
        }

        SaveConfigurationAfterSliderEdit();
        ImGui.EndDisabled();
    }

    private void DrawTooltipDetailButton(string label, ItemAcquisitionDetailLevel detailLevel)
    {
        var selected = configuration.Current.TooltipAcquisitionDetail == detailLevel;
        if (selected)
        {
            ImGui.PushStyleColor(
                ImGuiCol.Button,
                BoutiqueTheme.GetSelectedSlotButtonColor(configuration.Current));
            ImGui.PushStyleColor(
                ImGuiCol.Border,
                BoutiqueTheme.GetSelectedSlotBorderColor(configuration.Current));
            ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 2f);
        }

        if (BoutiqueTheme.FramedButton(
                configuration.Current,
                $"{label}##TooltipDetail{detailLevel}",
                new Vector2(-1f, 0f)))
        {
            configuration.Current.TooltipAcquisitionDetail = detailLevel;
            configuration.Save();
        }

        if (selected)
        {
            ImGui.PopStyleVar();
            ImGui.PopStyleColor(2);
        }
    }

    private void DrawTooltipSourceToggle(string label, bool value, Action<bool> apply)
    {
        if (BoutiqueTheme.FramedCheckbox(configuration.Current, label, ref value))
        {
            apply(value);
            configuration.Save();
        }
    }

    private void DrawTooltipModeButton(string label, bool requiresShift)
    {
        var selected = configuration.Current.TooltipsRequireShift == requiresShift;
        if (BoutiqueTheme.FramedButton(
                configuration.Current,
                $"{label}##TooltipMode{requiresShift}",
                new Vector2(-1f, 0f)))
        {
            configuration.Current.TooltipsRequireShift = requiresShift;
            configuration.Save();
        }

        if (!selected)
        {
            return;
        }

        var minimum = ImGui.GetItemRectMin();
        var maximum = ImGui.GetItemRectMax();
        var size = maximum.Y - minimum.Y;
        var start = new Vector2(maximum.X - (size * 0.68f), minimum.Y + (size * 0.52f));
        var middle = start + new Vector2(size * 0.14f, size * 0.16f);
        var end = middle + new Vector2(size * 0.28f, -size * 0.34f);
        var color = ImGui.ColorConvertFloat4ToU32(BoutiqueTheme.Available);
        ImGui.GetWindowDrawList().AddLine(start, middle, color, 2.5f);
        ImGui.GetWindowDrawList().AddLine(middle, end, color, 2.5f);
    }

    private bool DrawNumericSliderInt(string id, ref int value, int minimum, int maximum)
    {
        using var numericFont = fonts.Numeric.Push();
        var changed = ImGui.SliderInt(id, ref value, minimum, maximum, "%d%%");
        BoutiqueTheme.DrawLastGoldControlFrame(configuration.Current);
        return changed;
    }

    private void DrawCombatSafetySettings()
    {
        var disableInCombat = configuration.Current.DisableInCombat;
        if (BoutiqueTheme.FramedCheckbox(
                configuration.Current,
                "Close and disable the Boutique during combat",
                ref disableInCombat))
        {
            configuration.Current.DisableInCombat = disableInCombat;
            configuration.Save();
        }

        ImGui.TextWrapped(
            disableInCombat
                ? "Enabled (default): entering combat closes the window, returns the actor to game state, and blocks reopening until combat ends."
                : "Disabled: the Boutique may remain open and function during combat.");
    }

    private void DrawWardrobeSynchronizationSettings()
    {
        var automaticallySync = configuration.Current.AutomaticallySyncWardrobe;
        if (BoutiqueTheme.FramedCheckbox(
                configuration.Current,
                "Automatically Sync Wardrobe",
                ref automaticallySync))
        {
            configuration.Current.AutomaticallySyncWardrobe = automaticallySync;
            configuration.Save();
        }

        ImGui.TextWrapped(
            "Automatically updates Crystal Wardrobe after FFXIV finishes applying a local-player gearset change. Reset Character remains available for manual synchronization.");
    }

    private void DrawUiProfileSettings()
    {
        ImGui.TextWrapped(
            "Choose the complete visual profile used by every Boutique window. This changes presentation only; all browsing, Designs, Wardrobe, dyes, safety, and session behavior remain identical.");
        ImGui.Spacing();
        if (ImGui.BeginTable(
                "##UiThemeProfiles",
                2,
                ImGuiTableFlags.SizingStretchSame | ImGuiTableFlags.NoSavedSettings))
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            DrawUiProfileButton("SIMPLE", UiThemeProfile.Default);
            ImGui.TableNextColumn();
            DrawUiProfileButton("THE CRYSTARIUM BOUTIQUE THEME", UiThemeProfile.Crystarium);
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            DrawUiProfileButton("Simple Crystarium Boutique", UiThemeProfile.SimpleCrystarium);
            ImGui.EndTable();
        }

        var crystarium = BoutiqueTheme.IsCrystariumProfile(configuration.Current);
        var simpleCrystarium = BoutiqueTheme.IsSimpleCrystariumProfile(configuration.Current);
        ImGui.TextColored(
            crystarium
                ? BoutiqueTheme.CrystariumFrame
                : simpleCrystarium
                    ? BoutiqueTheme.SimpleCrystariumSilverGrey
                    : BoutiqueTheme.MutedText,
            crystarium
                ? "THE CRYSTARIUM BOUTIQUE THEME is active: one continuous blue stained-glass background with raised dark-metal panels, controls, dividers, and borders. Transparency settings remain active."
                : simpleCrystarium
                    ? "Simple Crystarium Boutique is active: the SIMPLE layout with the Crystarium stained-glass backdrop and a restrained silver, steel, charcoal, and soft-white palette."
                    : "SIMPLE is active: your existing background, border, bar, button, filter, tooltip, transparency, and layout preferences are used exactly as saved.");
        ImGui.Spacing();
        using (fonts.Helper.Push())
        {
            ImGui.TextWrapped(
                "New installations begin with THE CRYSTARIUM BOUTIQUE THEME. Switching themes never overwrites SIMPLE colors, so players can return to their exact saved presentation at any time.");
        }
    }

    private void DrawUiProfileButton(string label, UiThemeProfile profile)
    {
        if (BoutiqueTheme.FramedButton(
                configuration.Current,
                $"{label}##UiThemeProfile{profile}",
                new Vector2(-1f, 0f)))
        {
            configuration.Current.UiProfile = profile;
            configuration.Save();
        }

        if (configuration.Current.UiProfile != profile)
        {
            return;
        }

        var minimum = ImGui.GetItemRectMin();
        var maximum = ImGui.GetItemRectMax();
        var size = maximum.Y - minimum.Y;
        var start = new Vector2(maximum.X - (size * 0.68f), minimum.Y + (size * 0.52f));
        var middle = start + new Vector2(size * 0.14f, size * 0.16f);
        var end = middle + new Vector2(size * 0.28f, -size * 0.34f);
        var color = ImGui.ColorConvertFloat4ToU32(BoutiqueTheme.Available);
        ImGui.GetWindowDrawList().AddLine(start, middle, color, 2.5f);
        ImGui.GetWindowDrawList().AddLine(middle, end, color, 2.5f);
    }

    private void SetGridLayout(int rows, int columns)
    {
        configuration.Current.AppearanceGridRows = rows;
        configuration.Current.AppearanceGridColumns = columns;
        configuration.Save();
        browser.SetPageSize(rows * columns);
    }

    private void SaveConfigurationAfterSliderEdit()
    {
        if (ImGui.IsItemDeactivatedAfterEdit())
        {
            configuration.Save();
        }
    }

    private static float GetSettingsControlWidth()
        => Math.Max(1f, Math.Min(420f, ImGui.GetContentRegionAvail().X));

    private void DrawDebugSection()
    {
        ImGui.TextColored(BoutiqueTheme.GetHeadingColor(configuration.Current), "DEBUG");
        ImGui.TextWrapped(
            "Local read-only diagnostics for troubleshooting and future development. This section does not upload logs, contact a remote service, or mutate appearance state.");
        ImGui.Separator();

        var page = browser.CurrentPage;
        ImGui.TextColored(BoutiqueTheme.MutedText, "CURRENT STATE");
        ImGui.TextUnformatted($"Session: {session.State} · Dirty: {session.IsDirty}");
        ImGui.TextUnformatted($"Session ID: {session.SessionId?.ToString("N") ?? "None"}");
        ImGui.TextUnformatted($"Selected slot: {EquipmentSlotDefinitions.GetDisplayName(browser.SelectedSlot)}");
        ImGui.TextUnformatted($"Content group: {browser.SelectedContentGroupKey}");
        ImGui.TextUnformatted(
            $"Page: {page?.DisplayPage ?? 0}/{page?.TotalPages ?? 0} · Results: {(page?.TotalResults ?? 0):N0}");
        ImGui.TextUnformatted(
            $"Active class/job ID: {browser.ActiveClassJobId?.ToString(CultureInfo.InvariantCulture) ?? "Unavailable"}");
        ImGui.TextUnformatted(
            $"UI draw: {averageDrawMilliseconds:F3} avg · {peakDrawMilliseconds:F3} peak ms");

        if (catalogError is not null)
        {
            ImGui.TextColored(BoutiqueTheme.Unavailable, "Catalog diagnostic:");
            ImGui.TextWrapped(catalogError);
        }

        if (previewError is not null)
        {
            ImGui.TextColored(BoutiqueTheme.Unavailable, "Last preview error:");
            ImGui.TextWrapped(previewError.ToString());
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.TextColored(BoutiqueTheme.MutedText, "CHANGED EQUIPMENT STATE");
        if (sessionController.PreviewEquipmentBySlot.Count == 0)
        {
            ImGui.TextColored(BoutiqueTheme.MutedText, "No Boutique-changed slots.");
        }
        else
        {
            foreach (var equipment in sessionController.PreviewEquipmentBySlot.Values.OrderBy(item => item.Slot))
            {
                ImGui.TextWrapped(
                    $"• {EquipmentSlotDefinitions.GetDisplayName(equipment.Slot)}: {equipment.DisplayName} · item {equipment.Appearance.SourceItemId?.ToString(CultureInfo.InvariantCulture) ?? "custom"} · dyes {equipment.GetStain(0).Value}/{equipment.GetStain(1).Value}");
            }
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.TextColored(BoutiqueTheme.MutedText, "RECENT SESSION TRANSITIONS");
        foreach (var transition in session.TransitionHistory.Reverse())
        {
            ImGui.TextWrapped(
                $"• {transition.Timestamp:HH:mm:ss} · {transition.From} → {transition.To} · {transition.Reason}");
        }

    }

    private void DrawCompactStatus()
    {
        ImGui.TextColored(BoutiqueTheme.MutedText, "RUNTIME STATUS");
        ImGui.TextUnformatted($"Session: {session.State}");
        DrawDependencyRow("Glamourer", appearanceService.Status);
        DrawDependencyRow("Penumbra", penumbraService.Status);
        if (appearanceService.DetectedVersion is not null)
        {
            ImGui.TextColored(BoutiqueTheme.MutedText, $"Glamourer API: {appearanceService.DetectedVersion}");
        }

        if (previewStatus is not null)
        {
            var color = previewStatusIsError ? BoutiqueTheme.Unavailable : BoutiqueTheme.Available;
            ImGui.PushStyleColor(ImGuiCol.Text, color);
            ImGui.TextWrapped(previewStatus);
            ImGui.PopStyleColor();
        }

        if (configuration.Current.ShowTechnicalStatus && previewError?.TechnicalContext is not null)
        {
            ImGui.TextColored(BoutiqueTheme.MutedText, "Preview diagnostic:");
            ImGui.TextWrapped(previewError.TechnicalContext);
        }

        ImGui.Spacing();
    }

    private void DrawTechnicalStatusSettings()
    {
        ImGui.TextColored(BoutiqueTheme.MutedText, "TECHNICAL STATUS");
        var showTechnicalStatus = configuration.Current.ShowTechnicalStatus;
        if (BoutiqueTheme.FramedCheckbox(
                configuration.Current,
                "Show technical status",
                ref showTechnicalStatus))
        {
            configuration.Current.ShowTechnicalStatus = showTechnicalStatus;
            configuration.Save();
        }

        if (configuration.Current.ShowTechnicalStatus)
        {
            var page = browser.CurrentPage;
            ImGui.TextColored(BoutiqueTheme.MutedText, $"Items: {itemRepository.SourceItemCount:N0}");
            ImGui.TextColored(BoutiqueTheme.MutedText, $"Indexed looks: {itemRepository.AppearanceCount:N0}");
            ImGui.TextColored(
                BoutiqueTheme.MutedText,
                $"Mogstation items: {itemRepository.MogStationSourceItemCount:N0}");
            ImGui.TextColored(BoutiqueTheme.MutedText, $"Dyes: {stainCount:N0}");
            ImGui.TextColored(BoutiqueTheme.MutedText, $"Designs: {loadoutPanel.Count:N0}");
            ImGui.TextColored(
                BoutiqueTheme.MutedText,
                $"Grid: {configuration.Current.AppearanceGridRows} x {configuration.Current.AppearanceGridColumns} · {browser.PageSize} per page");
            ImGui.TextColored(
                BoutiqueTheme.MutedText,
                $"Active class/job ID: {browser.ActiveClassJobId?.ToString(CultureInfo.InvariantCulture) ?? "Unavailable"}");
            if (page is not null)
            {
                var hasAutomaticWeaponFilter = browser.ActiveClassJobId.HasValue
                    && browser.SelectedSlot is EquipmentSlot.MainHand or EquipmentSlot.OffHand;
                var querySource = browser.Filter.IsDefault && !hasAutomaticWeaponFilter
                    ? "indexed"
                    : page.UsedCachedFilter ? "cache hit" : "cache miss";
                ImGui.TextColored(BoutiqueTheme.MutedText, $"Filtered looks: {page.TotalResults:N0}");
                ImGui.TextColored(
                    BoutiqueTheme.MutedText,
                    $"Query: {page.QueryDuration.TotalMilliseconds:F3} ms · {querySource}");
            }

            if (sessionController.TryGetPreviewEquipment(browser.SelectedSlot, out var equipment))
            {
                ImGui.TextColored(
                    BoutiqueTheme.MutedText,
                    $"Selected dyes: {equipment.GetStain(0).Value}, {equipment.GetStain(1).Value}");
            }

            ImGui.TextColored(
                BoutiqueTheme.MutedText,
                $"UI: {averageDrawMilliseconds:F3} avg · {peakDrawMilliseconds:F3} peak ms");
            ImGui.TextColored(BoutiqueTheme.MutedText, $"Transitions: {session.TransitionHistory.Count}");
        }
    }

    private void RecordDrawDuration(double elapsedMilliseconds)
    {
        if (!hasDrawTiming)
        {
            averageDrawMilliseconds = elapsedMilliseconds;
            peakDrawMilliseconds = elapsedMilliseconds;
            hasDrawTiming = true;
            return;
        }

        averageDrawMilliseconds = (averageDrawMilliseconds * 0.95) + (elapsedMilliseconds * 0.05);
        peakDrawMilliseconds = Math.Max(elapsedMilliseconds, peakDrawMilliseconds * 0.995);
    }

    private void DrawWorkspace()
    {
        if (!ImGui.BeginChild(
                "##BoutiqueWorkspace",
                Vector2.Zero,
                !BoutiqueTheme.UsesTransparentParentContent(configuration.Current),
                ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
        {
            ImGui.EndChild();
            return;
        }

        BoutiqueScaleStyle.ApplyResponsiveWindowFontScale(
            configuration.Current.InterfaceScalePercent,
            BoutiqueTheme.DefaultWindowSize);

        var page = browser.CurrentPage;

        if (catalogError is not null)
        {
            ImGui.TextColored(BoutiqueTheme.Unavailable, "The local item catalog is unavailable.");
            ImGui.TextWrapped(catalogError);
        }
        else
        {
            DrawBrowserFilterPanel();
            ImGui.Spacing();
            DrawEquipmentSlotPanel();
            ImGui.Spacing();
            page = browser.CurrentPage;
            if (page is null)
            {
                ImGui.TextColored(BoutiqueTheme.Unavailable, "No appearance data is available for this slot.");
            }
            else
            {
                DrawEquipmentGrid(page);
                var ruleTop = ImGui.GetCursorPosY();
                BoutiqueTheme.DrawProfileHorizontalRule(
                    configuration.Current,
                    BoutiqueTheme.GetHorizontalRuleColor(configuration.Current),
                    BoutiqueTheme.ProminentRuleThickness);
                ImGui.SetCursorPosY(
                    ruleTop
                        + BoutiqueTheme.GetProfileHorizontalRuleThickness(
                            configuration.Current,
                            BoutiqueTheme.ProminentRuleThickness)
                        + BoutiqueTheme.PreviousItemButtonMargin);
                DrawPageControls(page);
            }
        }

        ImGui.EndChild();
    }

    private void DrawBrowserFilterPanel()
    {
        var style = ImGui.GetStyle();
        var padding = Math.Max(4f, ImGui.GetFrameHeight() * 0.16f);
        var contentHeight = (ImGui.GetFrameHeight() * 2f)
            + style.ItemSpacing.Y
            + (style.CellPadding.Y * 2f);
        DrawBoutiquePanel(
            "##BoutiqueFilterPanel",
            contentHeight + (padding * 2f) + 2f,
            padding,
            DrawBrowserFilters);
    }

    private void DrawEquipmentSlotPanel()
    {
        var style = ImGui.GetStyle();
        var padding = Math.Max(4f, ImGui.GetFrameHeight() * 0.16f);
        float slotFrameHeight;
        using (fonts.Navigation.Push())
        {
            slotFrameHeight = ImGui.GetFrameHeight();
        }

        var contentHeight = (slotFrameHeight * 2f)
            + (style.CellPadding.Y * 4f);
        DrawBoutiquePanel(
            "##BoutiqueEquipmentSlotPanel",
            contentHeight + (padding * 2f) + 2f,
            padding,
            DrawBoutiqueSlotButtons);
    }

    private void DrawBoutiquePanel(
        string id,
        float height,
        float padding,
        Action drawContent)
    {
        var borderColor = BoutiqueTheme.GetPanelBorderColor(configuration.Current);
        ImGui.PushStyleColor(ImGuiCol.ChildBg, Vector4.Zero);
        ImGui.PushStyleColor(ImGuiCol.Border, borderColor);
        ImGui.PushStyleVar(
            ImGuiStyleVar.ChildBorderSize,
            BoutiqueTheme.GetBoutiquePanelBorderThickness(configuration.Current));
        ImGui.PushStyleVar(
            ImGuiStyleVar.ChildRounding,
            BoutiqueTheme.GetCardRounding(configuration.Current));
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(padding));
        if (ImGui.BeginChild(
                id,
                new Vector2(0f, height),
                true,
                ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
        {
            drawContent();
        }

        ImGui.EndChild();
        ImGui.PopStyleVar(3);
        ImGui.PopStyleColor(2);
    }

    private void DrawBrowserFilters()
    {
        var filtersChanged = false;
        var filterBackground = BoutiqueTheme.GetFilterBackgroundColor(configuration.Current);
        var filterText = BoutiqueTheme.GetBoutiqueTabTextColor(configuration.Current);
        ImGui.PushStyleColor(ImGuiCol.FrameBg, filterBackground);
        ImGui.PushStyleColor(
            ImGuiCol.FrameBgHovered,
            BoutiqueTheme.GetHoveredControlColor(filterBackground));
        ImGui.PushStyleColor(
            ImGuiCol.FrameBgActive,
            BoutiqueTheme.GetActiveControlColor(filterBackground));
        ImGui.PushStyleColor(ImGuiCol.Text, filterText);
        var filterHintText = filterText;
        filterHintText.W *= 0.72f;
        ImGui.PushStyleColor(ImGuiCol.TextDisabled, filterHintText);
        if (ImGui.BeginTable(
                "##BoutiqueControlRow",
                8,
                ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.NoSavedSettings))
        {
            ImGui.TableSetupColumn("Expansion", ImGuiTableColumnFlags.WidthStretch, 1.25f);
            ImGui.TableSetupColumn("JobRole", ImGuiTableColumnFlags.WidthStretch, 1.15f);
            ImGui.TableSetupColumn("DyeSupport", ImGuiTableColumnFlags.WidthStretch, 1.10f);
            ImGui.TableSetupColumn("Level", ImGuiTableColumnFlags.WidthFixed, CalculateLevelFilterWidth());
            ImGui.TableSetupColumn("Search", ImGuiTableColumnFlags.WidthStretch, 0.80f);
            ImGui.TableSetupColumn("Clear", ImGuiTableColumnFlags.WidthFixed, ImGui.GetFrameHeight());
            ImGui.TableSetupColumn("Designs", ImGuiTableColumnFlags.WidthStretch, 1.25f);
            ImGui.TableSetupColumn("Actions", ImGuiTableColumnFlags.WidthStretch, 1.45f);
            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            DrawExpansionSelector();
            ImGui.TableNextColumn();
            filtersChanged |= DrawJobRoleFilter();
            ImGui.TableNextColumn();
            filtersChanged |= DrawDyeSupportFilter();
            ImGui.TableNextColumn();
            filtersChanged |= DrawMaximumEquipLevelFilter();
            ImGui.TableNextColumn();
            DrawFilterHeading("SEARCH");
            var searchWidth = (ImGui.CalcTextSize("Item").X * 3f)
                + (ImGui.GetStyle().FramePadding.X * 2f);
            ImGui.SetNextItemWidth(Math.Min(ImGui.GetContentRegionAvail().X, searchWidth));
            var searchChanged = ImGui.InputTextWithHint(
                "##AppearanceSearch",
                "Item",
                ref searchText,
                160);
            BoutiqueTheme.DrawLastGoldControlFrame(configuration.Current);
            filtersChanged |= searchChanged;
            ImGui.TableNextColumn();
            ImGui.Dummy(new Vector2(0f, ImGui.GetTextLineHeight()));
            ImGui.BeginDisabled(browser.Filter.IsDefault);
            ImGui.PushStyleColor(ImGuiCol.Button, BoutiqueTheme.ClearRed);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, BoutiqueTheme.ClearRedHovered);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, BoutiqueTheme.ClearRedActive);
            ImGui.PushStyleColor(
                ImGuiCol.Text,
                BoutiqueTheme.GetBoutiqueTabTextColor(configuration.Current));
            if (BoutiqueTheme.FramedButton(
                    configuration.Current,
                    "X##ClearAppearanceFilters",
                    new Vector2(ImGui.GetFrameHeight())))
            {
                searchText = string.Empty;
                maximumEquipLevelText = string.Empty;
                maximumEquipLevel = null;
                dyeSupportFilter = DyeSupportFilter.Any;
                jobRoleFilter = JobRoleFilter.Any;
                filtersChanged = true;
            }

            ImGui.PopStyleColor(4);
            ImGui.EndDisabled();
            ImGui.TableNextColumn();
            loadoutPanel.DrawBoutiqueDesignSelector();
            ImGui.TableNextColumn();
            DrawBoutiqueActions();
            ImGui.EndTable();
        }

        ImGui.PopStyleColor(5);

        if (filtersChanged)
        {
            browser.ApplyFilter(new CatalogFilter(
                searchText,
                null,
                maximumEquipLevel,
                null,
                null,
                dyeSupportFilter,
                jobRoleFilter));
        }

        loadoutPanel.DrawBoutiquePopups();
    }

    private bool DrawMaximumEquipLevelFilter()
    {
        DrawFilterHeading("LEVEL");
        var previousText = maximumEquipLevelText;
        ImGui.SetNextItemWidth(-1f);
        var changed = ImGui.InputTextWithHint(
            "##MaximumEquipLevel",
            "All",
            ref maximumEquipLevelText,
            8,
            ImGuiInputTextFlags.CharsDecimal);
        BoutiqueTheme.DrawLastGoldControlFrame(configuration.Current);
        if (!changed)
        {
            return false;
        }

        if (!TryParseMaximumEquipLevel(maximumEquipLevelText, out var parsedLevel, out var normalizedText))
        {
            maximumEquipLevelText = previousText;
            return false;
        }

        maximumEquipLevelText = normalizedText;
        maximumEquipLevel = parsedLevel;
        return true;
    }

    private float CalculateLevelFilterWidth()
    {
        var inputWidth = ImGui.CalcTextSize("100").X + (ImGui.GetStyle().FramePadding.X * 2f);
        float headingWidth;
        using (fonts.SectionHeading.Push())
        {
            headingWidth = ImGui.CalcTextSize("LEVEL").X;
        }

        return Math.Max(inputWidth, headingWidth);
    }

    internal static bool TryParseMaximumEquipLevel(
        string text,
        out byte? maximumLevel,
        out string normalizedText)
    {
        var trimmed = text.Trim();
        if (trimmed.Length == 0)
        {
            maximumLevel = null;
            normalizedText = string.Empty;
            return true;
        }

        if (!long.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            maximumLevel = null;
            normalizedText = text;
            return false;
        }

        if (parsed <= 0)
        {
            maximumLevel = null;
            normalizedText = string.Empty;
            return true;
        }

        maximumLevel = (byte)Math.Min(parsed, byte.MaxValue);
        normalizedText = maximumLevel.Value.ToString(CultureInfo.InvariantCulture);
        return true;
    }

    private void DrawExpansionSelector()
    {
        DrawFilterHeading("EXPANSION");
        var selectedGroup = browser.AvailableContentGroups
            .FirstOrDefault(group => group.Key == browser.SelectedContentGroupKey);
        var preview = selectedGroup?.DisplayName ?? "No indexed items";
        ImGui.SetNextItemWidth(-1f);
        if (!BoutiqueTheme.BeginResponsiveCombo(
                configuration.Current,
                "##ExpansionSelector",
                preview,
                BoutiqueTheme.GetBoutiqueTabTextColor(configuration.Current)))
        {
            return;
        }

        foreach (var group in browser.AvailableContentGroups)
        {
            var selected = group.Key == browser.SelectedContentGroupKey;
            if (ImGui.Selectable(group.DisplayName, selected))
            {
                browser.SelectContentGroup(group.Key);
            }

            if (selected)
            {
                ImGui.SetItemDefaultFocus();
            }
        }

        BoutiqueTheme.EndResponsiveCombo();
    }

    private bool DrawDyeSupportFilter()
    {
        DrawFilterHeading("DYE SLOTS");
        ImGui.SetNextItemWidth(-1f);
        if (!BoutiqueTheme.BeginResponsiveCombo(
                configuration.Current,
                "##DyeSupportFilter",
                DyeFilterLabels[(int)dyeSupportFilter],
                BoutiqueTheme.GetBoutiqueTabTextColor(configuration.Current)))
        {
            return false;
        }

        var changed = false;
        foreach (var value in Enum.GetValues<DyeSupportFilter>())
        {
            var selected = dyeSupportFilter == value;
            if (ImGui.Selectable(DyeFilterLabels[(int)value], selected))
            {
                dyeSupportFilter = value;
                changed = true;
            }

            if (selected)
            {
                ImGui.SetItemDefaultFocus();
            }
        }

        BoutiqueTheme.EndResponsiveCombo();
        return changed;
    }

    private bool DrawJobRoleFilter()
    {
        DrawFilterHeading("ROLE");
        ImGui.SetNextItemWidth(-1f);
        if (!BoutiqueTheme.BeginResponsiveCombo(
                configuration.Current,
                "##JobRoleFilter",
                JobRoleFilterLabels[(int)jobRoleFilter],
                BoutiqueTheme.GetBoutiqueTabTextColor(configuration.Current)))
        {
            return false;
        }

        var changed = false;
        foreach (var value in Enum.GetValues<JobRoleFilter>())
        {
            var selected = jobRoleFilter == value;
            if (ImGui.Selectable(JobRoleFilterLabels[(int)value], selected))
            {
                jobRoleFilter = value;
                changed = true;
            }

            if (selected)
            {
                ImGui.SetItemDefaultFocus();
            }
        }

        BoutiqueTheme.EndResponsiveCombo();
        return changed;
    }

    private enum BoutiqueTab
    {
        Boutique,
        Favorites,
        Designs,
        Settings,
        Help,
        Info,
    }

    private void DrawFilterHeading(string text)
    {
        using var headingFont = fonts.SectionHeading.Push();
        ImGui.TextColored(BoutiqueTheme.GetBoutiqueTabTextColor(configuration.Current), text);
    }

    private void DrawEquipmentGrid(CatalogPage page)
    {
        var availableHeight = ImGui.GetContentRegionAvail().Y;
        PageFooterLayout footerLayout;
        using (fonts.Numeric.Push())
        {
            footerLayout = CalculatePageFooterLayout(page, ImGui.GetContentRegionAvail().X);
        }
        var footerReserve = BoutiqueTheme.GetProfileHorizontalRuleThickness(
                configuration.Current,
                BoutiqueTheme.ProminentRuleThickness)
            + footerLayout.Height
            + (BoutiqueTheme.PreviousItemButtonMargin * 2f)
            + ImGui.GetStyle().ItemSpacing.Y;
        var gridHeight = Math.Max(
            BoutiqueTheme.MinimumGridHeight,
            availableHeight - footerReserve);

        if (!ImGui.BeginChild(
                "##EquipmentGridRegion",
                new Vector2(0f, gridHeight),
                false,
                ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
        {
            ImGui.EndChild();
            return;
        }

        var crystariumProfile = BoutiqueTheme.IsCrystariumProfile(configuration.Current);
        if (crystariumProfile)
        {
            var padding = ImGui.GetStyle().CellPadding;
            ImGui.PushStyleVar(
                ImGuiStyleVar.CellPadding,
                padding * BoutiqueTheme.CrystariumGridSpacingScale);
        }

        var gridColumns = configuration.Current.AppearanceGridColumns;
        var gridRows = Math.Max(browser.PageSize / gridColumns, 1);
        var cellPadding = ImGui.GetStyle().CellPadding.Y * gridRows * 2f;
        var tileHeight = Math.Max(
            1f,
            (ImGui.GetContentRegionAvail().Y - cellPadding) / gridRows);

        if (ImGui.BeginTable(
                "##EquipmentGrid",
                gridColumns,
                ImGuiTableFlags.SizingStretchSame | ImGuiTableFlags.NoPadInnerX))
        {
            for (var index = 0; index < browser.PageSize; index++)
            {
                if (index % gridColumns == 0)
                {
                    ImGui.TableNextRow(ImGuiTableRowFlags.None, tileHeight);
                }

                ImGui.TableSetColumnIndex(index % gridColumns);
                if (index < page.Items.Length)
                {
                    DrawItemTile(page.Items[index], index, tileHeight);
                }
                else
                {
                    DrawEmptyTile(index, tileHeight);
                }
            }

            ImGui.EndTable();
        }

        if (crystariumProfile)
        {
            ImGui.PopStyleVar();
        }

        if (ImGui.IsWindowHovered(ImGuiHoveredFlags.ChildWindows))
        {
            var wheel = ImGui.GetIO().MouseWheel;
            if (wheel < 0f)
            {
                browser.MovePage(1);
            }
            else if (wheel > 0f)
            {
                browser.MovePage(-1);
            }
        }

        ImGui.EndChild();
    }

    private void DrawItemTile(
        AppearanceEntry entry,
        int index,
        float tileHeight,
        EquipmentSlot? previewSlot = null,
        string idPrefix = "Equipment",
        Guid? favoriteListContext = null)
    {
        var crystariumProfile = BoutiqueTheme.IsCrystariumProfile(configuration.Current);
        var transparentTileContainer
            = BoutiqueTheme.UsesTransparentItemTileContainer(configuration.Current);
        var pushedTileColors = 0;
        if (transparentTileContainer)
        {
            ImGui.PushStyleColor(ImGuiCol.ChildBg, Vector4.Zero);
            ImGui.PushStyleColor(ImGuiCol.Border, Vector4.Zero);
            pushedTileColors = 2;
        }

        if (crystariumProfile)
        {
            ImGui.PushStyleVar(
                ImGuiStyleVar.WindowPadding,
                new Vector2(BoutiqueTheme.CrystariumTilePadding));
        }

        var visible = ImGui.BeginChild(
            $"##{idPrefix}Tile{index}",
            new Vector2(0f, tileHeight),
            true,
            ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);

        if (visible)
        {
            var available = ImGui.GetContentRegionAvail();
            var availableIconWidth = crystariumProfile
                ? available.X / CrystariumMetalFrameRenderer.FramedSizeScale
                : available.X - BoutiqueTheme.TileInset;
            var availableIconHeight = crystariumProfile
                ? available.Y / CrystariumMetalFrameRenderer.FramedSizeScale
                : available.Y - BoutiqueTheme.TileInset;
            var iconSize = Math.Max(
                1f,
                Math.Min(
                    crystariumProfile
                        ? BoutiqueTheme.CrystariumPreferredIconSize
                        : BoutiqueTheme.PreferredIconSize,
                    Math.Min(availableIconWidth, availableIconHeight)));
            var frameExtent = crystariumProfile
                ? CrystariumMetalFrameRenderer.OuterExtentFor(iconSize)
                : 0f;
            var framedIconSize = crystariumProfile
                ? CrystariumMetalFrameRenderer.FramedSizeFor(iconSize)
                : iconSize;
            var cursor = ImGui.GetCursorPos();
            ImGui.SetCursorPos(new Vector2(
                cursor.X
                    + Math.Max((available.X - framedIconSize) / 2f, 0f)
                    + frameExtent,
                cursor.Y
                    + Math.Max((available.Y - framedIconSize) / 2f, 0f)
                    + frameExtent));
            var iconPosition = ImGui.GetCursorScreenPos();
            DrawItemIcon(entry.PrimaryItem.IconId, iconSize);
            var hovered = ImGui.IsItemHovered();
            DrawItemIconBorder(iconPosition, iconSize);
            var isFavorite = previewSlot is null
                && configuration.Current.Favorites.Contains(entry.PrimaryItem.ItemId);
            if (BoutiqueTheme.ShouldDrawFavoriteFrame(configuration.Current, isFavorite))
            {
                DrawFavoriteFrame(iconPosition, iconSize, frameExtent, crystariumProfile);
            }

            var restriction = GetItemRestriction(entry.PrimaryItem);
            if (restriction != ItemEquipRestrictionReason.None)
            {
                BoutiqueTheme.DrawIconInteractionGlow(
                    ImGui.GetWindowDrawList(),
                    iconPosition,
                    iconPosition + new Vector2(iconSize),
                    crystariumProfile
                        ? BoutiqueTheme.CrystariumRestrictedItem
                        : BoutiqueTheme.RestrictedItem,
                    frameExtent,
                    outerOnly: crystariumProfile);
            }
            else if (hovered)
            {
                BoutiqueTheme.DrawIconInteractionGlow(
                    ImGui.GetWindowDrawList(),
                    iconPosition,
                    iconPosition + new Vector2(iconSize),
                    BoutiqueTheme.GetSelectedItemHighlightColor(configuration.Current),
                    frameExtent,
                    outerOnly: crystariumProfile);
            }

            if (BoutiqueTheme.ShouldDrawFavoriteBadge(isFavorite))
            {
                DrawFavoriteBadge(iconPosition, iconSize);
            }

            if (hovered && ImGui.IsMouseReleased(ImGuiMouseButton.Left))
            {
                if (previewSlot is null)
                {
                    browser.SelectAppearance(entry.AppearanceKey);
                }

                PreviewItem(entry, previewSlot);
            }

            if (hovered && ImGui.IsMouseReleased(ImGuiMouseButton.Right))
            {
                ImGui.OpenPopup($"##FavoriteItem{entry.PrimaryItem.ItemId}");
            }

            DrawFavoriteManagementPopup(
                entry.PrimaryItem,
                previewSlot is not null,
                favoriteListContext);

            if (hovered)
            {
                DrawItemOverlay(entry, previewSlot ?? browser.SelectedSlot, iconPosition, iconSize);
            }
        }

        ImGui.EndChild();
        if (crystariumProfile)
        {
            ImGui.PopStyleVar();
        }

        if (pushedTileColors > 0)
        {
            ImGui.PopStyleColor(pushedTileColors);
        }
    }

    private void DrawItemIconBorder(Vector2 iconPosition, float iconSize)
    {
        var drawList = ImGui.GetWindowDrawList();
        if (BoutiqueTheme.IsCrystariumProfile(configuration.Current))
        {
            CrystariumMetalFrameRenderer.Draw(
                drawList,
                iconPosition,
                iconPosition + new Vector2(iconSize),
                crystariumItemFrameTexture);
            return;
        }

        // Center the outer stroke 2.5 px inside the image so it never protrudes
        // past the icon's own rounded corner bounds.
        var minimum = iconPosition + new Vector2(2.5f);
        var maximum = iconPosition + new Vector2(iconSize - 2.5f);
        drawList.AddRect(
            minimum,
            maximum,
            ImGui.ColorConvertFloat4ToU32(
                BoutiqueTheme.GetItemBorderShadowColor(configuration.Current)),
            BoutiqueTheme.ControlRounding - 1f,
            ImDrawFlags.None,
            5f);
        drawList.AddRect(
            minimum + new Vector2(1.25f),
            maximum - new Vector2(1.25f),
            ImGui.ColorConvertFloat4ToU32(
                BoutiqueTheme.GetItemBorderColor(configuration.Current)),
            BoutiqueTheme.ControlRounding - 3f,
            ImDrawFlags.None,
            2.5f);
    }

    private void DrawFavoriteFrame(
        Vector2 iconPosition,
        float iconSize,
        float frameExtent,
        bool outerOnly)
    {
        BoutiqueTheme.DrawIconInteractionGlow(
            ImGui.GetWindowDrawList(),
            iconPosition,
            iconPosition + new Vector2(iconSize),
            BoutiqueTheme.GetFavoriteItemFrameColor(configuration.Current),
            frameExtent,
            outerOnly);
    }

    private void DrawFavoriteBadge(Vector2 iconPosition, float iconSize)
    {
        var texture = favoriteBadgeTexture.GetWrapOrDefault();
        if (texture is null)
        {
            return;
        }

        var badgeSize = Math.Max(1f, MathF.Round(iconSize * 0.2f));
        var inset = Math.Max(2f, MathF.Round(iconSize * 0.025f));
        var badgeMinimum = new Vector2(
            MathF.Round(iconPosition.X + iconSize - badgeSize - inset),
            MathF.Round(iconPosition.Y + inset));
        ImGui.GetWindowDrawList().AddImage(
            texture.Handle,
            badgeMinimum,
            badgeMinimum + new Vector2(badgeSize));
    }

    private ItemEquipRestrictionReason GetItemRestriction(EquipmentItem item)
    {
        if (!playerState.IsLoaded)
        {
            return ItemEquipRestrictionReason.None;
        }

        var race = playerState.Race.RowId switch
        {
            1 => CharacterRaceMask.Hyur,
            2 => CharacterRaceMask.Elezen,
            3 => CharacterRaceMask.Lalafell,
            4 => CharacterRaceMask.Miqote,
            5 => CharacterRaceMask.Roegadyn,
            6 => CharacterRaceMask.AuRa,
            7 => CharacterRaceMask.Hrothgar,
            8 => CharacterRaceMask.Viera,
            _ => CharacterRaceMask.None,
        };
        var sex = playerState.Sex == Dalamud.Game.Player.Sex.Male
            ? CharacterSexMask.Male
            : CharacterSexMask.Female;
        return item.EquipRestrictions.Evaluate(new CharacterEquipContext(
            race,
            sex,
            playerState.GrandCompany.RowId));
    }

    private void PreviewItem(AppearanceEntry entry, EquipmentSlot? previewSlot = null)
    {
        var item = entry.PrimaryItem;
        var slot = previewSlot ?? browser.SelectedSlot;
        var appearance = AppearanceSelection.WithoutStains(
            new AppearanceId(item.ItemId),
            item.ItemId);
        var result = sessionController.PreviewEquipment(
            slot,
            appearance,
            item.DyeChannelCount,
            item.Name,
            item.IconId,
            item.HasLinkedOffHandComponent,
            ResolveRequestedVisorState(slot, ImGui.GetIO().KeyCtrl));
        if (result.IsSuccess)
        {
            loadoutPanel.NotifyManualChange();
        }

        previewStatusIsError = !result.IsSuccess;
        previewError = result.Error;
        previewStatus = result.IsSuccess
            ? $"Previewing: {item.Name}"
            : result.Error?.Message ?? "The selected appearance could not be previewed.";
    }

    internal static bool? ResolveRequestedVisorState(EquipmentSlot slot, bool controlPressed)
        => slot == EquipmentSlot.Head ? !controlPressed : null;

    private void HandleLoadoutFeedback(LoadoutFeedback feedback)
    {
        previewStatusIsError = !feedback.IsSuccess;
        previewError = feedback.Error;
        previewStatus = feedback.Message;
    }

    internal void HandleEquipmentCardFeedback(EquipmentCardFeedback feedback)
    {
        if (feedback.IsSuccess)
        {
            loadoutPanel.NotifyManualChange();
        }

        previewStatusIsError = !feedback.IsSuccess;
        previewError = feedback.Error;
        previewStatus = feedback.Message;
    }

    private void DrawBoutiqueSlotButtons()
    {
        using var slotFont = fonts.Navigation.Push();
        if (!ImGui.BeginTable(
                "##BoutiqueSlotButtons",
                6,
                ImGuiTableFlags.SizingStretchSame
                | ImGuiTableFlags.NoSavedSettings
                | ImGuiTableFlags.NoPadInnerX))
        {
            return;
        }

        var buttonColor = BoutiqueTheme.GetBoutiqueButtonColor(configuration.Current);
        var buttonTextColor = BoutiqueTheme.GetBoutiqueButtonTextColor(configuration.Current);
        ImGui.PushStyleColor(ImGuiCol.Button, buttonColor);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, BoutiqueTheme.GetHoveredControlColor(buttonColor));
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, BoutiqueTheme.GetActiveControlColor(buttonColor));
        ImGui.PushStyleColor(ImGuiCol.Text, buttonTextColor);
        for (var index = 0; index < BoutiqueSlotButtons.Length; index++)
        {
            if (index % 6 == 0)
            {
                ImGui.TableNextRow();
            }

            ImGui.TableSetColumnIndex(index % 6);
            var definition = BoutiqueSlotButtons[index];
            var selected = definition.Slot == browser.SelectedSlot;
            if (selected)
            {
                ImGui.PushStyleColor(
                    ImGuiCol.Button,
                    BoutiqueTheme.GetSelectedSlotButtonColor(configuration.Current));
                ImGui.PushStyleColor(
                    ImGuiCol.Border,
                    BoutiqueTheme.GetSelectedSlotBorderColor(configuration.Current));
                ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 2f);
            }

            if (BoutiqueTheme.FramedButton(
                    configuration.Current,
                    $"##BoutiqueSlot{definition.Slot}",
                    new Vector2(-1f, 0f)))
            {
                browser.SelectSlot(definition.Slot);
            }

            DrawBoutiqueSlotButtonContent(index, definition.DisplayName);

            if (selected)
            {
                ImGui.PopStyleVar();
                ImGui.PopStyleColor(2);
            }
        }

        ImGui.PopStyleColor(4);
        ImGui.EndTable();
    }

    private void DrawBoutiqueSlotButtonContent(int spriteIndex, string label)
    {
        var minimum = ImGui.GetItemRectMin();
        var maximum = ImGui.GetItemRectMax();
        var drawList = ImGui.GetWindowDrawList();
        var texture = equipmentSlotTexture.GetWrapOrDefault();
        var iconSize = Math.Max(
            1f,
            Math.Min(maximum.Y - minimum.Y - 4f, ImGui.GetTextLineHeight() + 2f));
        var iconMinimum = new Vector2(
            minimum.X + 5f,
            minimum.Y + ((maximum.Y - minimum.Y - iconSize) / 2f));
        if (texture is not null)
        {
            const int spriteColumns = 4;
            const int spriteRows = 3;
            var column = spriteIndex % spriteColumns;
            var row = spriteIndex / spriteColumns;
            var uv0 = new Vector2(column / (float)spriteColumns, row / (float)spriteRows);
            var uv1 = new Vector2(
                (column + 1f) / spriteColumns,
                (row + 1f) / spriteRows);
            drawList.AddImage(
                texture.Handle,
                iconMinimum,
                iconMinimum + new Vector2(iconSize),
                uv0,
                uv1);
        }

        var textMinimum = new Vector2(
            iconMinimum.X + iconSize + ImGui.GetStyle().ItemInnerSpacing.X,
            minimum.Y + 1f - Math.Max(ImGui.GetTextLineHeight() * 0.07f, 1f));
        var textMaximum = new Vector2(
            maximum.X - ImGui.GetStyle().FramePadding.X,
            maximum.Y - 1f - Math.Max(ImGui.GetTextLineHeight() * 0.07f, 1f));
        BoutiqueTheme.DrawFittedText(
            drawList,
            label,
            textMinimum,
            textMaximum,
            BoutiqueTheme.GetBoutiqueButtonTextColor(configuration.Current),
            minimumScale: 0.22f);
    }

    private void DrawBoutiqueActions()
    {
        var wardrobeColor = BoutiqueTheme.GetWardrobeButtonColor(configuration.Current);
        ImGui.PushStyleColor(ImGuiCol.Button, wardrobeColor);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, BoutiqueTheme.GetHoveredControlColor(wardrobeColor));
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, BoutiqueTheme.GetActiveControlColor(wardrobeColor));
        ImGui.PushStyleColor(ImGuiCol.Text, BoutiqueTheme.GetWardrobeButtonTextColor(configuration.Current));
        if (BoutiqueTheme.FramedButton(
                configuration.Current,
                "##CrystalWardrobe",
                new Vector2(-1f, 0f)))
        {
            toggleWardrobe();
        }

        DrawCrystalWardrobeButtonContent();
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Open or close the movable Crystal Wardrobe equipment-card window.");
        }

        ImGui.PopStyleColor(4);
        var saveColor = BoutiqueTheme.GetSaveButtonColor(configuration.Current);
        ImGui.PushStyleColor(ImGuiCol.Button, saveColor);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, BoutiqueTheme.GetHoveredControlColor(saveColor));
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, BoutiqueTheme.GetActiveControlColor(saveColor));
        ImGui.PushStyleColor(ImGuiCol.Text, BoutiqueTheme.GetSaveButtonTextColor(configuration.Current));
        loadoutPanel.DrawBoutiqueSaveButton(includeLabelSpacing: false);
        ImGui.PopStyleColor(4);
    }

    private void DrawCrystalWardrobeButtonContent()
    {
        const string label = "Crystal Wardrobe";
        var minimum = ImGui.GetItemRectMin();
        var maximum = ImGui.GetItemRectMax();
        var availableWidth = Math.Max(maximum.X - minimum.X, 1f);
        var iconSize = Math.Max(
            4f,
            Math.Min(
                Math.Min(ImGui.GetTextLineHeight() + 2f, maximum.Y - minimum.Y - 4f),
                availableWidth * 0.22f));
        var spacing = ImGui.GetStyle().ItemInnerSpacing.X;
        var drawList = ImGui.GetWindowDrawList();
        var iconMinimum = new Vector2(
            maximum.X - ImGui.GetStyle().FramePadding.X - iconSize,
            minimum.Y + ((maximum.Y - minimum.Y - iconSize) / 2f));
        BoutiqueTheme.DrawFittedText(
            drawList,
            label,
            new Vector2(minimum.X + 2f, minimum.Y + 1f),
            new Vector2(iconMinimum.X - spacing, maximum.Y - 1f),
            BoutiqueTheme.GetWardrobeButtonTextColor(configuration.Current),
            minimumScale: 0.22f);

        var texture = crystalWardrobeTexture.GetWrapOrDefault();
        if (texture is null)
        {
            return;
        }

        drawList.AddImage(
            texture.Handle,
            iconMinimum,
            iconMinimum + new Vector2(iconSize),
            Vector2.Zero,
            Vector2.One,
            ImGui.ColorConvertFloat4ToU32(BoutiqueTheme.GetWardrobeButtonTextColor(configuration.Current)));
    }

    private void DrawHelpTab()
    {
        var workspaceStyleColors = PushSimpleCrystariumWorkspaceStyle();
        if (!ImGui.BeginChild(
                "##HelpWorkspace",
                Vector2.Zero,
                !BoutiqueTheme.UsesTransparentParentContent(configuration.Current)))
        {
            ImGui.EndChild();
            PopSimpleCrystariumWorkspaceStyle(workspaceStyleColors);
            return;
        }

        using (fonts.Navigation.Push())
        {
            ImGui.TextColored(BoutiqueTheme.GetHeadingColor(configuration.Current), "HELP");
        }

        using (fonts.Helper.Push())
        {
            ImGui.TextWrapped("Local guidance for Boutique controls, saved designs, themes, UI options, and the optional Eorzea Collection importer.");
        }

        ImGui.Separator();
        if (DrawHelpSectionHeader(
                "GETTING STARTED / ITEM CONTROLS",
                "Boutique",
                ref helpBoutiqueExpanded))
        {
            DrawHelpPoints(HelpGuideContent.BoutiquePoints);
        }

        if (DrawHelpSectionHeader(
                "CRYSTAL WARDROBE / DYES",
                "Wardrobe",
                ref helpWardrobeExpanded))
        {
            DrawHelpPoints(HelpGuideContent.WardrobePoints);
        }

        if (DrawHelpSectionHeader(
                "FAVORITES",
                "Favorites",
                ref helpFavoritesExpanded))
        {
            DrawHelpPoints(HelpGuideContent.FavoritePoints);
        }

        if (DrawHelpSectionHeader(
                "SAVING / EXPORTING / IMPORTING CUSTOM BOUTIQUE DESIGNS",
                "Designs",
                ref helpDesignsExpanded))
        {
            DrawHelpPoints(HelpGuideContent.DesignPoints);
        }

        if (DrawHelpSectionHeader(
                "EORZEA COLLECTION INTEGRATION: IMPORTING GLAMOURS",
                "Eorzea",
                ref helpEorzeaExpanded))
        {
            DrawHelpPoints(HelpGuideContent.EorzeaPoints);
        }

        if (DrawHelpSectionHeader(
                "THEMES / UI OPTIONS / ACCESSIBILITY",
                "Themes",
                ref helpThemesExpanded))
        {
            DrawHelpPoints(HelpGuideContent.ThemePoints);
        }

        ImGui.Separator();
        var showWelcomeGuide = configuration.Current.ShowWelcomeGuideOnOpen;
        if (BoutiqueTheme.FramedCheckbox(
                configuration.Current,
                "Show Welcome Guide When Boutique Opens",
                ref showWelcomeGuide))
        {
            configuration.Current.ShowWelcomeGuideOnOpen = showWelcomeGuide;
            configuration.Save();
        }

        ImGui.EndChild();
        PopSimpleCrystariumWorkspaceStyle(workspaceStyleColors);
    }

    private bool DrawHelpSectionHeader(string title, string id, ref bool expanded)
    {
        ImGui.Spacing();
        bool clicked;
        using (fonts.SectionHeading.Push())
        {
            clicked = BoutiqueTheme.FramedButton(
                configuration.Current,
                $"{(expanded ? "-  " : string.Empty)}{title}##HelpSection{id}",
                new Vector2(-1f, 0f));
        }

        if (clicked)
        {
            expanded = !expanded;
        }

        return expanded;
    }

    private static void DrawHelpPoint(string text)
    {
        ImGui.Bullet();
        ImGui.SameLine();
        ImGui.TextWrapped(text);
    }

    private static void DrawHelpPoints(IEnumerable<string> points)
    {
        foreach (var point in points)
        {
            DrawHelpPoint(point);
        }
    }

    private static EquipmentSlotDefinition FindSlotDefinition(EquipmentSlot slot)
        => EquipmentSlotDefinitions.All.Single(definition => definition.Slot == slot);

    private void DrawEmptyTile(int index, float tileHeight)
    {
        var transparentTileContainer
            = BoutiqueTheme.UsesTransparentItemTileContainer(configuration.Current);
        if (transparentTileContainer)
        {
            ImGui.PushStyleColor(ImGuiCol.ChildBg, Vector4.Zero);
            ImGui.PushStyleColor(ImGuiCol.Border, Vector4.Zero);
        }

        if (ImGui.BeginChild(
                $"##EquipmentTileEmpty{index}",
                new Vector2(0f, tileHeight),
                true,
                ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
        {
            var available = ImGui.GetContentRegionAvail();
            ImGui.SetCursorPos(new Vector2(
                ImGui.GetCursorPosX() + Math.Max(available.X / 2f, 0f),
                ImGui.GetCursorPosY() + Math.Max(available.Y / 2f, 0f)));
            ImGui.TextColored(BoutiqueTheme.MutedText, "·");
        }

        ImGui.EndChild();
        if (transparentTileContainer)
        {
            ImGui.PopStyleColor(2);
        }
    }

    private int PushSimpleCrystariumWorkspaceStyle()
    {
        if (!BoutiqueTheme.UsesTransparentParentContent(configuration.Current))
        {
            return 0;
        }

        ImGui.PushStyleColor(ImGuiCol.ChildBg, Vector4.Zero);
        ImGui.PushStyleColor(ImGuiCol.Border, Vector4.Zero);
        return 2;
    }

    private static void PopSimpleCrystariumWorkspaceStyle(int colorCount)
    {
        if (colorCount > 0)
        {
            ImGui.PopStyleColor(colorCount);
        }
    }

    private void DrawItemIcon(uint iconId, float iconSize)
    {
        var lookup = new GameIconLookup(iconId, itemHq: false, hiRes: true);
        if (textureProvider.TryGetFromGameIcon(lookup, out var sharedTexture))
        {
            var texture = sharedTexture.GetWrapOrDefault();
            if (texture is not null)
            {
                ImGui.Image(texture.Handle, new Vector2(iconSize));
                return;
            }
        }

        ImGui.Dummy(new Vector2(iconSize));
    }

    private void DrawItemOverlay(
        AppearanceEntry entry,
        EquipmentSlot slot,
        Vector2 iconPosition,
        float iconSize)
    {
        if (!BoutiqueTheme.ShouldShowItemTooltip(configuration.Current))
        {
            return;
        }

        var item = entry.PrimaryItem;
        ItemTooltipRenderer.Draw(
            item,
            itemRepository.GetItemsSharingModel(item.ModelFamilyKey, slot),
            iconPosition,
            iconSize,
            configuration.Current,
            itemRarityColors,
            fonts);
    }

    private void DrawPageControls(CatalogPage page)
    {
        using var numericFont = fonts.Numeric.Push();
        var rowStart = ImGui.GetCursorPos();
        var rowScreenStart = ImGui.GetCursorScreenPos();
        var rowWidth = ImGui.GetContentRegionAvail().X;
        var layout = CalculatePageFooterLayout(page, rowWidth);
        var canMoveBack = page.PageIndex > 0;
        var canMoveForward = page.PageIndex + 1 < page.TotalPages;
        const string firstLabel = "<< First";
        const string previousLabel = "< Previous";
        const string nextLabel = "Next >";
        const string lastLabel = "Last >>";
        const string separator = "|";
        var pageLabel = $"Page {page.DisplayPage} / {page.TotalPages}";
        var spacing = ImGui.GetStyle().ItemSpacing.X
            * BoutiqueTheme.PageControlScale
            * layout.Scale;
        var buttonHeight = ImGui.GetFrameHeight()
            * BoutiqueTheme.PageControlScale
            * layout.Scale;
        var controlY = rowStart.Y + ((layout.Height - buttonHeight) / 2f);
        var controlScreenY = rowScreenStart.Y + ((layout.Height - buttonHeight) / 2f);
        var controlX = rowStart.X + Math.Max((rowWidth - layout.NavigationWidth) / 2f, 0f);
        var controlScreenX = rowScreenStart.X + Math.Max((rowWidth - layout.NavigationWidth) / 2f, 0f);
        var drawList = ImGui.GetWindowDrawList();
        var buttonColor = BoutiqueTheme.GetBoutiqueButtonColor(configuration.Current);
        var buttonTextColor = BoutiqueTheme.GetBoutiqueButtonTextColor(configuration.Current);

        float GetTextWidth(string text)
            => ImGui.CalcTextSize(text).X * layout.FontScale;

        float GetButtonWidth(string text)
            => (ImGui.CalcTextSize(text).X + (ImGui.GetStyle().FramePadding.X * 2f))
                * BoutiqueTheme.PageControlScale
                * layout.Scale;

        void Advance(float width)
        {
            controlX += width + spacing;
            controlScreenX += width + spacing;
        }

        void DrawFooterText(string text, Vector4 color)
        {
            var width = GetTextWidth(text);
            BoutiqueTheme.DrawFittedText(
                drawList,
                text,
                new Vector2(controlScreenX, controlScreenY),
                new Vector2(controlScreenX + width, controlScreenY + buttonHeight),
                color,
                layout.FontScale,
                minimumScale: 0.15f);
            Advance(width);
        }

        bool DrawFooterButton(string id, string text, bool enabled)
        {
            var width = GetButtonWidth(text);
            ImGui.SetCursorPos(new Vector2(controlX, controlY));
            ImGui.BeginDisabled(!enabled);
            var clicked = BoutiqueTheme.FittedTextButton(
                configuration.Current,
                id,
                text,
                new Vector2(width, buttonHeight),
                layout.FontScale,
                minimumFontScale: 0.15f,
                textColor: buttonTextColor);
            ImGui.EndDisabled();
            Advance(width);
            return clicked;
        }

        ImGui.PushStyleColor(ImGuiCol.Button, buttonColor);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, BoutiqueTheme.GetHoveredControlColor(buttonColor));
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, BoutiqueTheme.GetActiveControlColor(buttonColor));

        DrawFooterText(pageLabel, buttonTextColor);
        if (DrawFooterButton("##FirstBoutiquePage", firstLabel, canMoveBack))
        {
            browser.MovePage(-page.PageIndex);
        }

        DrawFooterText(separator, buttonTextColor);
        if (DrawFooterButton("##PreviousBoutiquePage", previousLabel, canMoveBack))
        {
            browser.MovePage(-1);
        }

        DrawFooterText(separator, buttonTextColor);
        if (DrawFooterButton("##NextBoutiquePage", nextLabel, canMoveForward))
        {
            browser.MovePage(1);
        }

        DrawFooterText(separator, buttonTextColor);
        if (DrawFooterButton("##LastBoutiquePage", lastLabel, canMoveForward))
        {
            browser.MovePage(page.TotalPages - page.PageIndex - 1);
        }

        ImGui.PopStyleColor(3);

        DrawPreviousItemButton(rowStart, rowWidth, layout);
        ImGui.SetCursorPosY(Math.Max(
            ImGui.GetCursorPosY(),
            rowStart.Y + layout.Height + BoutiqueTheme.PreviousItemButtonMargin));
    }

    private static PageFooterLayout CalculatePageFooterLayout(CatalogPage page, float rowWidth)
    {
        const string firstLabel = "<< First";
        const string previousLabel = "< Previous";
        const string nextLabel = "Next >";
        const string lastLabel = "Last >>";
        const string separator = "|";
        var pageLabel = $"Page {page.DisplayPage} / {page.TotalPages}";
        var desiredScale = BoutiqueTheme.PageControlScale;
        var framePadding = ImGui.GetStyle().FramePadding.X * 2f;
        var spacing = ImGui.GetStyle().ItemSpacing.X;
        var navigationBaseWidth = (ImGui.CalcTextSize(pageLabel).X * desiredScale)
            + ((ImGui.CalcTextSize(firstLabel).X + framePadding) * desiredScale)
            + ((ImGui.CalcTextSize(previousLabel).X + framePadding) * desiredScale)
            + ((ImGui.CalcTextSize(nextLabel).X + framePadding) * desiredScale)
            + ((ImGui.CalcTextSize(lastLabel).X + framePadding) * desiredScale)
            + (ImGui.CalcTextSize(separator).X * 3f * desiredScale)
            + (spacing * 7f * desiredScale);
        var scalableWidth = navigationBaseWidth
            + ((BoutiqueTheme.PreviousItemButtonSize + spacing) * 2f);
        var widthWithoutMargins = Math.Max(
            rowWidth - (BoutiqueTheme.PreviousItemButtonMargin * 2f),
            1f);
        var scale = Math.Clamp(
            widthWithoutMargins / Math.Max(scalableWidth, 1f),
            0.22f,
            1f);
        var fontScale = desiredScale * scale;
        var undoSize = BoutiqueTheme.PreviousItemButtonSize * scale;
        var navigationWidth = navigationBaseWidth * scale;
        var buttonHeight = ImGui.GetFrameHeight() * desiredScale * scale;
        return new PageFooterLayout(
            scale,
            fontScale,
            Math.Max(undoSize, buttonHeight),
            navigationWidth,
            undoSize);
    }

    internal static PageFooterLayout CalculatePreviousItemOnlyFooterLayout(float rowWidth)
    {
        var widthWithoutMargins = Math.Max(
            rowWidth - (BoutiqueTheme.PreviousItemButtonMargin * 2f),
            1f);
        var scale = Math.Clamp(
            widthWithoutMargins / BoutiqueTheme.PreviousItemButtonSize,
            0.22f,
            1f);
        var undoSize = BoutiqueTheme.PreviousItemButtonSize * scale;
        return new PageFooterLayout(
            scale,
            scale,
            undoSize,
            0f,
            undoSize);
    }

    private void DrawPreviousItemButton(
        Vector2 rowStart,
        float rowWidth,
        PageFooterLayout layout)
    {
        var buttonSize = Math.Max(1f, MathF.Round(layout.UndoSize));
        var buttonColor = BoutiqueTheme.GetBoutiqueButtonColor(configuration.Current);
        ImGui.SetCursorPos(new Vector2(
            MathF.Round(rowStart.X
                + Math.Max(
                    rowWidth - buttonSize - BoutiqueTheme.PreviousItemButtonMargin,
                    0f)),
            MathF.Round(rowStart.Y + ((layout.Height - buttonSize) / 2f))));
        ImGui.PushID("PreviousBoutiqueItem");
        ImGui.PushStyleColor(ImGuiCol.Button, buttonColor);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, BoutiqueTheme.GetHoveredControlColor(buttonColor));
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, BoutiqueTheme.GetActiveControlColor(buttonColor));
        ImGui.BeginDisabled(!sessionController.CanUndoEquipmentPreview);
        var texture = previousItemTexture.GetWrapOrDefault();
        var clicked = texture is not null
            ? ImGui.ImageButton(
                texture.Handle,
                new Vector2(buttonSize),
                Vector2.Zero,
                Vector2.One,
                0,
                buttonColor,
                Vector4.One)
            : ImGui.Button("Undo", new Vector2(buttonSize));
        ImGui.EndDisabled();
        ImGui.PopStyleColor(3);

        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
        {
            ImGui.SetTooltip(sessionController.CanUndoEquipmentPreview
                ? "Return to the previous item selected in the Boutique."
                : "No earlier Boutique item selection is available.");
        }

        if (clicked)
        {
            var result = sessionController.UndoLastEquipmentPreview();
            if (result.IsSuccess)
            {
                browser.ClearAppearanceSelection();
                loadoutPanel.NotifyManualChange();
            }

            previewStatusIsError = !result.IsSuccess;
            previewError = result.Error;
            previewStatus = result.IsSuccess
                ? "Returned to the previous Boutique item selection."
                : result.Error?.Message ?? "The previous Boutique item could not be restored.";
        }

        ImGui.PopID();
    }

    internal readonly record struct PageFooterLayout(
        float Scale,
        float FontScale,
        float Height,
        float NavigationWidth,
        float UndoSize);

    private static void DrawDependencyRow(string name, DependencyStatus status)
    {
        var color = status == DependencyStatus.Available ? BoutiqueTheme.Available : BoutiqueTheme.Unavailable;
        ImGui.TextColored(color, $"{name}: {status}");
    }

}
