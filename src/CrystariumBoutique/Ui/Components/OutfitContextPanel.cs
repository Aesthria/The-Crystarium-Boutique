using System.Collections.Immutable;
using System.Globalization;
using System.Numerics;
using CrystariumBoutique.Configuration;
using CrystariumBoutique.Core.Appearance;
using CrystariumBoutique.Core.Catalog;
using CrystariumBoutique.Core.Dyes;
using CrystariumBoutique.Core.Errors;
using CrystariumBoutique.Core.Sessions;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures;
using Dalamud.Plugin.Services;

namespace CrystariumBoutique.Ui.Components;

internal enum WardrobeLayout
{
    Horizontal,
    Vertical,
}

internal sealed record EquipmentCardFeedback(
    bool IsSuccess,
    string Message,
    BoutiqueError? Error);

internal sealed class OutfitContextPanel
{
    private const int CardColumns = 6;
    private const float DyePopupWidth = 520f;
    private const float DyeSwatchSize = 34f;
    private const float DyeButtonHeight = 28f;
    private static readonly EquipmentSlotDefinition[] HorizontalCardLayout =
    [
        FindDefinition(EquipmentSlot.MainHand),
        FindDefinition(EquipmentSlot.Head),
        FindDefinition(EquipmentSlot.Body),
        FindDefinition(EquipmentSlot.Hands),
        FindDefinition(EquipmentSlot.Legs),
        FindDefinition(EquipmentSlot.Feet),
        FindDefinition(EquipmentSlot.OffHand),
        FindDefinition(EquipmentSlot.Ears),
        FindDefinition(EquipmentSlot.Neck),
        FindDefinition(EquipmentSlot.Wrists),
        FindDefinition(EquipmentSlot.RightRing),
        FindDefinition(EquipmentSlot.LeftRing),
    ];
    private static readonly EquipmentSlotDefinition[] VerticalCardLayout =
    [
        FindDefinition(EquipmentSlot.MainHand),
        FindDefinition(EquipmentSlot.OffHand),
        FindDefinition(EquipmentSlot.Head),
        FindDefinition(EquipmentSlot.Ears),
        FindDefinition(EquipmentSlot.Body),
        FindDefinition(EquipmentSlot.Neck),
        FindDefinition(EquipmentSlot.Hands),
        FindDefinition(EquipmentSlot.Wrists),
        FindDefinition(EquipmentSlot.Legs),
        FindDefinition(EquipmentSlot.RightRing),
        FindDefinition(EquipmentSlot.Feet),
        FindDefinition(EquipmentSlot.LeftRing),
    ];

    private readonly IStainRepository stains;
    private readonly IItemRepository itemRepository;
    private readonly BoutiqueSessionController sessionController;
    private readonly EquipmentBrowserController browser;
    private readonly ITextureProvider textureProvider;
    private readonly ItemRarityColorResolver itemRarityColors;
    private readonly ConfigurationStore configuration;
    private readonly BoutiqueFontSet fonts;
    private readonly ISharedImmediateTexture unavailableDyeTexture;
    private readonly Action<EquipmentCardFeedback> reportFeedback;
    private readonly RecentStainHistory recentStains = new();
    private ImmutableArray<StainDefinition> filteredStains;
    private string dyeSearchText = string.Empty;
    private StainGroup? selectedDyeGroup;
    private byte selectedDyeChannel;

    public OutfitContextPanel(
        IStainRepository stains,
        IItemRepository itemRepository,
        BoutiqueSessionController sessionController,
        EquipmentBrowserController browser,
        ITextureProvider textureProvider,
        ItemRarityColorResolver itemRarityColors,
        ConfigurationStore configuration,
        BoutiqueFontSet fonts,
        string assetDirectory,
        Action<EquipmentCardFeedback> reportFeedback)
    {
        this.stains = stains ?? throw new ArgumentNullException(nameof(stains));
        this.itemRepository = itemRepository ?? throw new ArgumentNullException(nameof(itemRepository));
        this.sessionController = sessionController ?? throw new ArgumentNullException(nameof(sessionController));
        this.browser = browser ?? throw new ArgumentNullException(nameof(browser));
        this.textureProvider = textureProvider ?? throw new ArgumentNullException(nameof(textureProvider));
        this.itemRarityColors = itemRarityColors ?? throw new ArgumentNullException(nameof(itemRarityColors));
        this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        this.fonts = fonts ?? throw new ArgumentNullException(nameof(fonts));
        ArgumentException.ThrowIfNullOrWhiteSpace(assetDirectory);
        unavailableDyeTexture = textureProvider.GetFromFile(
            Path.Combine(assetDirectory, "images", "unavailable-dye.png"));
        this.reportFeedback = reportFeedback ?? throw new ArgumentNullException(nameof(reportFeedback));
        filteredStains = stains.All;
    }

    public void DrawEquipmentCards(float height, WardrobeLayout layout)
    {
        var cardLayout = GetCardLayout(layout);
        var columns = layout == WardrobeLayout.Vertical ? 2 : CardColumns;
        if (!ImGui.BeginTable(
                "##BoutiqueEquipmentCards",
                columns,
                ImGuiTableFlags.SizingStretchSame
                | ImGuiTableFlags.NoPadInnerX
                | ImGuiTableFlags.NoSavedSettings))
        {
            return;
        }

        for (var index = 0; index < cardLayout.Count; index++)
        {
            if (index % columns == 0)
            {
                ImGui.TableNextRow(ImGuiTableRowFlags.None, height);
            }

            ImGui.TableSetColumnIndex(index % columns);
            DrawEquipmentCard(cardLayout[index], height);
        }

        ImGui.EndTable();
    }

    internal static IReadOnlyList<EquipmentSlotDefinition> GetCardLayout(WardrobeLayout layout)
        => layout == WardrobeLayout.Vertical
            ? VerticalCardLayout
            : HorizontalCardLayout;

    private void DrawEquipmentCard(EquipmentSlotDefinition definition, float height)
    {
        var selected = definition.Slot == browser.SelectedSlot;
        var changed = sessionController.TryGetPreviewEquipment(definition.Slot, out var equipment);
        var cardBackground = BoutiqueTheme.GetWardrobeCardBackground(
            configuration.Current,
            selected,
            ImGui.GetStyle().Colors[(int)ImGuiCol.ChildBg]);

        ImGui.PushStyleColor(ImGuiCol.ChildBg, cardBackground);
        ImGui.PushStyleColor(
            ImGuiCol.Border,
            BoutiqueTheme.GetWardrobeCardBorderColor(configuration.Current));
        ImGui.PushStyleVar(
            ImGuiStyleVar.ChildBorderSize,
            BoutiqueTheme.GetCardBorderThickness(configuration.Current));
        ImGui.PushStyleVar(
            ImGuiStyleVar.ChildRounding,
            BoutiqueTheme.GetCardRounding(configuration.Current));

        var visible = ImGui.BeginChild(
            $"##EquipmentCard{definition.Slot}",
            new Vector2(0f, height),
            true,
            ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
        if (visible)
        {
            DrawCardHeader(definition, changed ? equipment : null);

            var available = ImGui.GetContentRegionAvail();
            var iconRegionHeight = Math.Max(
                1f,
                available.Y - DyeButtonHeight - ImGui.GetStyle().ItemSpacing.Y);
            DrawCardIconRegion(definition.Slot, changed ? equipment!.IconId : 0, iconRegionHeight);
            var dyeControlsHovered = DrawDyeButtons(definition.Slot, changed ? equipment : null);
            DrawDyePopup(definition.Slot);

            var dyePopupOpen = ImGui.IsPopupOpen(GetDyePopupId(definition.Slot));
            if (!dyeControlsHovered
                && !dyePopupOpen
                && ImGui.IsWindowHovered(ImGuiHoveredFlags.ChildWindows))
            {
                if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
                {
                    browser.SelectSlot(definition.Slot);
                }

                if (equipment is not null && ImGui.IsMouseReleased(ImGuiMouseButton.Right))
                {
                    ClearEquipment(definition.Slot);
                }

                if (BoutiqueTheme.ShouldShowItemTooltip(configuration.Current))
                {
                    DrawCardTooltip(definition, changed ? equipment : null);
                }
            }
        }

        ImGui.EndChild();
        ImGui.PopStyleVar(2);
        ImGui.PopStyleColor(2);
    }

    private void DrawCardHeader(
        EquipmentSlotDefinition definition,
        PreviewEquipmentState? equipment)
    {
        using (fonts.SectionHeading.Push())
        {
            ImGui.TextUnformatted(definition.DisplayName);
        }

        ImGui.PushStyleColor(
            ImGuiCol.Text,
            equipment is null
                ? BoutiqueTheme.GetSecondaryTextColor(configuration.Current)
                : itemRarityColors.GetColor(equipment, definition.Slot, itemRepository));
        using (fonts.Body.Push())
        {
            ImGui.TextWrapped(equipment?.DisplayName ?? "Original appearance");
        }
        ImGui.PopStyleColor();
        BoutiqueTheme.DrawProfileHorizontalRule(
            configuration.Current,
            BoutiqueTheme.GetWardrobeCardDividerColor(configuration.Current),
            2f);
    }

    private void DrawCardIconRegion(EquipmentSlot slot, uint iconId, float height)
    {
        var transparentIconWell = BoutiqueTheme.UsesCrystariumBackdrop(configuration.Current);
        if (transparentIconWell)
        {
            ImGui.PushStyleColor(ImGuiCol.ChildBg, Vector4.Zero);
        }

        if (ImGui.BeginChild(
                $"##EquipmentCardIconRegion{slot}",
                new Vector2(0f, height),
                false,
                ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
        {
            var available = ImGui.GetContentRegionAvail();
            var iconSize = Math.Max(
                1f,
                Math.Min(
                    available.X - BoutiqueTheme.EquipmentCardInset,
                    available.Y - BoutiqueTheme.EquipmentCardInset));
            var cursor = ImGui.GetCursorPos();
            ImGui.SetCursorPos(new Vector2(
                cursor.X + Math.Max((available.X - iconSize) / 2f, 0f),
                cursor.Y + Math.Max((available.Y - iconSize) / 2f, 0f)));
            DrawCardIcon(iconId, iconSize);
        }

        ImGui.EndChild();
        if (transparentIconWell)
        {
            ImGui.PopStyleColor();
        }
    }

    private void DrawCardIcon(uint iconId, float iconSize)
    {
        if (iconId > 0
            && textureProvider.TryGetFromGameIcon(
                new GameIconLookup(iconId, itemHq: false, hiRes: true),
                out var sharedTexture))
        {
            var texture = sharedTexture.GetWrapOrDefault();
            if (texture is not null)
            {
                ImGui.Image(texture.Handle, new Vector2(iconSize));
                return;
            }
        }

        if (ImGui.BeginChild(
                "##OriginalAppearanceIcon",
                new Vector2(iconSize),
                true,
                ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
        {
            var textSize = ImGui.CalcTextSize("--");
            var available = ImGui.GetContentRegionAvail();
            ImGui.SetCursorPos(new Vector2(
                ImGui.GetCursorPosX() + Math.Max((available.X - textSize.X) / 2f, 0f),
                ImGui.GetCursorPosY() + Math.Max((available.Y - textSize.Y) / 2f, 0f)));
            ImGui.TextColored(BoutiqueTheme.GetSecondaryTextColor(configuration.Current), "--");
        }

        ImGui.EndChild();
    }

    private bool DrawDyeButtons(EquipmentSlot slot, PreviewEquipmentState? equipment)
    {
        var hovered = false;
        var spacing = ImGui.GetStyle().ItemSpacing.X;
        var buttonWidth = Math.Max(1f, (ImGui.GetContentRegionAvail().X - spacing) / 2f);
        for (byte channel = 0; channel < 2; channel++)
        {
            if (channel > 0)
            {
                ImGui.SameLine(0f, spacing);
            }

            var supported = equipment is not null && channel < equipment.DyeChannelCount;
            var stainId = supported ? equipment!.GetStain(channel) : StainId.None;
            var stain = stains.TryGet(stainId, out var definition) ? definition : null;
            var label = stain?.Name ?? $"Dye {channel + 1}";
            if (supported)
            {
                var buttonColor = stain is null
                    ? new Vector4(0.24f, 0.24f, 0.24f, 1f)
                    : ToVector(stain.Color);
                ImGui.PushStyleColor(ImGuiCol.Button, buttonColor);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, buttonColor);
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, buttonColor);
                ImGui.PushStyleColor(ImGuiCol.Text, GetContrastingTextColor(buttonColor));
                if (DrawAdaptiveDyeButton(
                        label,
                        $"##EquipmentCardDye{slot}{channel}",
                        buttonWidth))
                {
                    selectedDyeChannel = channel;
                    ImGui.OpenPopup(GetDyePopupId(slot));
                }

                var dyeHovered = ImGui.IsItemHovered();
                hovered |= dyeHovered;
                if (dyeHovered && ImGui.IsMouseReleased(ImGuiMouseButton.Right))
                {
                    selectedDyeChannel = channel;
                    ApplyStain(slot, StainId.None, null);
                }

                ImGui.PopStyleColor(4);
            }
            else
            {
                ImGui.BeginDisabled();
                BoutiqueTheme.FramedButton(
                    configuration.Current,
                    $"##EquipmentCardDye{slot}{channel}",
                    new Vector2(buttonWidth, DyeButtonHeight));
                ImGui.EndDisabled();
                hovered |= ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled);
                DrawUnsupportedDyeCross();
                if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                {
                    ImGui.SetTooltip($"Dye {channel + 1} is unavailable for this item.");
                }
            }
        }

        return hovered;
    }

    private bool DrawAdaptiveDyeButton(string label, string id, float buttonWidth)
    {
        var availableTextWidth = Math.Max(
            1f,
            buttonWidth - (ImGui.GetStyle().FramePadding.X * 2f));
        var textWidth = ImGui.CalcTextSize(label).X;
        var fittedScale = textWidth <= availableTextWidth
            ? BoutiqueTheme.OutfitCardFontScale
            : Math.Clamp(
                BoutiqueTheme.OutfitCardFontScale * (availableTextWidth / textWidth),
                0.28f,
                BoutiqueTheme.OutfitCardFontScale);
        BoutiqueScaleStyle.ApplyRelativeWindowFontScale(fittedScale);
        var clicked = BoutiqueTheme.FramedButton(
            configuration.Current,
            $"{label}{id}",
            new Vector2(buttonWidth, DyeButtonHeight));
        BoutiqueScaleStyle.ApplyRelativeWindowFontScale();
        return clicked;
    }

    private void DrawUnsupportedDyeCross()
    {
        var minimum = ImGui.GetItemRectMin();
        var maximum = ImGui.GetItemRectMax();
        var texture = unavailableDyeTexture.GetWrapOrDefault();
        if (texture is not null)
        {
            var iconSize = Math.Min(20f, Math.Min(maximum.X - minimum.X, maximum.Y - minimum.Y) - 4f);
            var iconMinimum = new Vector2(
                minimum.X + ((maximum.X - minimum.X - iconSize) / 2f),
                minimum.Y + ((maximum.Y - minimum.Y - iconSize) / 2f));
            ImGui.GetWindowDrawList().AddImage(
                texture.Handle,
                iconMinimum,
                iconMinimum + new Vector2(iconSize));
            return;
        }

        const string fallback = "X";
        var labelSize = ImGui.CalcTextSize(fallback);
        ImGui.GetWindowDrawList().AddText(
            new Vector2(
                minimum.X + ((maximum.X - minimum.X - labelSize.X) / 2f),
                minimum.Y + ((maximum.Y - minimum.Y - labelSize.Y) / 2f)),
            ImGui.ColorConvertFloat4ToU32(BoutiqueTheme.MarketUnavailable),
            fallback);
    }

    private void DrawDyePopup(EquipmentSlot slot)
    {
        ImGui.SetNextWindowSizeConstraints(
            new Vector2(DyePopupWidth, 480f),
            new Vector2(DyePopupWidth, 720f));
        if (!ImGui.BeginPopup(GetDyePopupId(slot)))
        {
            return;
        }

        if (!sessionController.TryGetPreviewEquipment(slot, out var equipment)
            || equipment.DyeChannelCount == 0)
        {
            ImGui.TextColored(
                BoutiqueTheme.Unavailable,
                "This card no longer contains an item with a supported dye slot.");
            if (BoutiqueTheme.FramedButton(configuration.Current, "Close"))
            {
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
            return;
        }

        selectedDyeChannel = Math.Min(
            selectedDyeChannel,
            (byte)(equipment.DyeChannelCount - 1));
        ImGui.TextColored(
            BoutiqueTheme.GetHeadingColor(configuration.Current),
            $"DYE {selectedDyeChannel + 1}");
        ImGui.TextWrapped(equipment.DisplayName);
        DrawDyeChannelTabs(equipment);

        var currentStain = equipment.GetStain(selectedDyeChannel);
        ImGui.TextColored(
            BoutiqueTheme.GetSecondaryTextColor(configuration.Current),
            $"Current: {GetStainName(currentStain)}");
        var searchChanged = ImGui.InputTextWithHint(
                "##EquipmentCardDyeSearch",
                "Search dye names",
                ref dyeSearchText,
                80);
        BoutiqueTheme.DrawLastGoldControlFrame(configuration.Current);
        if (searchChanged)
        {
            RefreshDyeFilter();
        }

        ImGui.SetNextItemWidth(210f);
        DrawDyeGroupSelector();
        ImGui.SameLine();
        if (BoutiqueTheme.FramedButton(configuration.Current, "Clear dye slot"))
        {
            ApplyStain(slot, StainId.None, null);
        }

        ImGui.SameLine();
        if (BoutiqueTheme.FramedButton(configuration.Current, "Close"))
        {
            ImGui.CloseCurrentPopup();
        }

        DrawRecentStains(slot, currentStain);
        ImGui.Separator();
        if (ImGui.BeginChild("##EquipmentCardDyeSwatches", Vector2.Zero, false))
        {
            DrawFilteredStains(slot, currentStain);
        }

        ImGui.EndChild();
        ImGui.EndPopup();
    }

    private void DrawDyeChannelTabs(PreviewEquipmentState equipment)
    {
        for (byte channel = 0; channel < equipment.DyeChannelCount; channel++)
        {
            if (channel > 0)
            {
                ImGui.SameLine();
            }

            var selected = selectedDyeChannel == channel;
            if (selected)
            {
                ImGui.PushStyleColor(
                    ImGuiCol.Button,
                    BoutiqueTheme.GetSelectedSlotButtonColor(configuration.Current));
                ImGui.PushStyleColor(
                    ImGuiCol.ButtonHovered,
                    BoutiqueTheme.GetSelectedSlotBorderColor(configuration.Current));
            }

            if (BoutiqueTheme.FramedButton(
                    configuration.Current,
                    $"Dye {channel + 1}##EquipmentCardDyeTab{channel}"))
            {
                selectedDyeChannel = channel;
            }

            if (selected)
            {
                ImGui.PopStyleColor(2);
            }
        }
    }

    private void DrawDyeGroupSelector()
    {
        var preview = selectedDyeGroup.HasValue
            ? StainGroups.GetDisplayName(selectedDyeGroup.Value)
            : "All color groups";
        if (!BoutiqueTheme.BeginFramedCombo(
                configuration.Current,
                "##EquipmentCardDyeGroup",
                preview))
        {
            return;
        }

        if (ImGui.Selectable("All color groups", selectedDyeGroup is null))
        {
            selectedDyeGroup = null;
            RefreshDyeFilter();
        }

        foreach (var group in StainGroups.All)
        {
            if (!stains.All.Any(stain => stain.Group == group))
            {
                continue;
            }

            var selected = selectedDyeGroup == group;
            if (ImGui.Selectable(StainGroups.GetDisplayName(group), selected))
            {
                selectedDyeGroup = group;
                RefreshDyeFilter();
            }

            if (selected)
            {
                ImGui.SetItemDefaultFocus();
            }
        }

        ImGui.EndCombo();
    }

    private void DrawRecentStains(EquipmentSlot slot, StainId currentStain)
    {
        if (recentStains.Items.Count == 0)
        {
            return;
        }

        ImGui.Spacing();
        ImGui.TextColored(BoutiqueTheme.GetSecondaryTextColor(configuration.Current), "RECENT");
        for (var index = 0; index < recentStains.Items.Count; index++)
        {
            if (!stains.TryGet(recentStains.Items[index], out var stain))
            {
                continue;
            }

            if (index > 0)
            {
                ImGui.SameLine();
            }

            DrawDyeSwatch(slot, stain, currentStain, $"Recent{index}");
        }
    }

    private void DrawFilteredStains(EquipmentSlot slot, StainId currentStain)
    {
        if (filteredStains.IsEmpty)
        {
            ImGui.TextColored(
                BoutiqueTheme.GetSecondaryTextColor(configuration.Current),
                "No dyes match the current search and group.");
            return;
        }

        foreach (var group in StainGroups.All)
        {
            if (selectedDyeGroup.HasValue && selectedDyeGroup.Value != group)
            {
                continue;
            }

            var groupStains = filteredStains.Where(stain => stain.Group == group).ToArray();
            if (groupStains.Length == 0)
            {
                continue;
            }

            ImGui.TextColored(
                BoutiqueTheme.GetSecondaryTextColor(configuration.Current),
                StainGroups.GetDisplayName(group));
            var columns = Math.Max(
                1,
                (int)(ImGui.GetContentRegionAvail().X
                    / (DyeSwatchSize + ImGui.GetStyle().ItemSpacing.X)));
            for (var index = 0; index < groupStains.Length; index++)
            {
                if (index % columns != 0)
                {
                    ImGui.SameLine();
                }

                var stain = groupStains[index];
                DrawDyeSwatch(slot, stain, currentStain, $"Catalog{stain.Id.Value}");
            }

            ImGui.Spacing();
        }
    }

    private void DrawDyeSwatch(
        EquipmentSlot slot,
        StainDefinition stain,
        StainId currentStain,
        string idSuffix)
    {
        var selected = stain.Id == currentStain;
        if (selected)
        {
            ImGui.PushStyleColor(
                ImGuiCol.Border,
                BoutiqueTheme.GetSelectedSlotBorderColor(configuration.Current));
            ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 3f);
        }

        var clicked = ImGui.ColorButton(
            $"##EquipmentCardDyeSwatch{idSuffix}",
            ToVector(stain.Color),
            ImGuiColorEditFlags.NoTooltip | ImGuiColorEditFlags.NoDragDrop,
            new Vector2(DyeSwatchSize));

        if (selected)
        {
            ImGui.PopStyleVar();
            ImGui.PopStyleColor();
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            ImGui.TextUnformatted(stain.Name);
            ImGui.TextColored(
                BoutiqueTheme.GetSecondaryTextColor(configuration.Current),
                $"Dye ID {stain.Id.Value.ToString(CultureInfo.InvariantCulture)}");
            if (stain.IsMetallic)
            {
                ImGui.TextColored(BoutiqueTheme.GetHeadingColor(configuration.Current), "Metallic");
            }

            ImGui.EndTooltip();
        }

        if (clicked)
        {
            ApplyStain(slot, stain.Id, stain);
        }
    }

    private void ApplyStain(EquipmentSlot slot, StainId stainId, StainDefinition? stain)
    {
        var result = sessionController.PreviewDye(slot, selectedDyeChannel, stainId);
        if (result.IsSuccess)
        {
            recentStains.Record(stainId);
        }

        reportFeedback(new EquipmentCardFeedback(
            result.IsSuccess,
            result.IsSuccess
                ? $"Dye {selectedDyeChannel + 1}: {stain?.Name ?? "None"}"
                : result.Error?.Message ?? "The selected dye could not be previewed.",
            result.Error));
    }

    private void ClearEquipment(EquipmentSlot slot)
    {
        var result = sessionController.ClearEquipment(slot);
        if (result.IsSuccess)
        {
            browser.ClearAppearanceSelection();
        }

        reportFeedback(new EquipmentCardFeedback(
            result.IsSuccess,
            result.IsSuccess
                ? $"Cleared {EquipmentSlotDefinitions.GetDisplayName(slot)} from the Boutique preview."
                : result.Error?.Message ?? "The selected equipment slot could not be cleared.",
            result.Error));
    }

    private void RefreshDyeFilter()
        => filteredStains = stains.Search(dyeSearchText, selectedDyeGroup);

    private void DrawCardTooltip(
        EquipmentSlotDefinition definition,
        PreviewEquipmentState? equipment)
    {
        if (equipment?.Appearance.SourceItemId is { } sourceItemId
            && itemRepository.TryGetItem(sourceItemId, definition.Slot, out var item))
        {
            ItemTooltipRenderer.Draw(
                item,
                itemRepository.GetItemsSharingModel(item.ModelFamilyKey, definition.Slot),
                ImGui.GetWindowPos(),
                Math.Max(1f, Math.Min(ImGui.GetWindowSize().X, ImGui.GetWindowSize().Y)),
                configuration.Current,
                itemRarityColors,
                fonts);
            return;
        }

        ImGui.BeginTooltip();
        ImGui.TextUnformatted(definition.DisplayName);
        ImGui.Separator();
        if (equipment is null)
        {
            ImGui.TextColored(
                BoutiqueTheme.GetSecondaryTextColor(configuration.Current),
                "Current: Session opening appearance");
        }
        else
        {
            ImGui.TextUnformatted($"Current: {equipment.DisplayName}");
            ImGui.TextColored(
                BoutiqueTheme.GetSecondaryTextColor(configuration.Current),
                GetDyeSummary(equipment));
        }

        ImGui.Spacing();
        ImGui.TextColored(
            BoutiqueTheme.GetSecondaryTextColor(configuration.Current),
            "Click to browse this slot.");
        ImGui.EndTooltip();
    }

    private string GetDyeSummary(PreviewEquipmentState equipment)
    {
        if (equipment.DyeChannelCount == 0)
        {
            return "Dyes: Not supported";
        }

        var names = new string[equipment.DyeChannelCount];
        for (byte channel = 0; channel < equipment.DyeChannelCount; channel++)
        {
            names[channel] = GetStainName(equipment.GetStain(channel));
        }

        return $"Dyes: {string.Join(" / ", names)}";
    }

    private string GetStainName(StainId stainId)
        => stainId.IsNone
            ? "None"
            : stains.TryGet(stainId, out var stain) ? stain.Name : $"Dye {stainId.Value}";

    private static string GetDyePopupId(EquipmentSlot slot)
        => $"##EquipmentCardDyePicker{slot}";

    private static EquipmentSlotDefinition FindDefinition(EquipmentSlot slot)
        => EquipmentSlotDefinitions.All.Single(definition => definition.Slot == slot);

    private static Vector4 ToVector(StainColor color)
        => new(color.Red / 255f, color.Green / 255f, color.Blue / 255f, 1f);

    private static Vector4 GetContrastingTextColor(Vector4 color)
    {
        var luminance = (0.2126f * color.X) + (0.7152f * color.Y) + (0.0722f * color.Z);
        return luminance > 0.52f
            ? new Vector4(0.05f, 0.05f, 0.05f, 1f)
            : new Vector4(0.96f, 0.96f, 0.96f, 1f);
    }
}
