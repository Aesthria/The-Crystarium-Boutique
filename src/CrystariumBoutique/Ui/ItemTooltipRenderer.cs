using System.Collections.Immutable;
using System.Globalization;
using System.Numerics;
using CrystariumBoutique.Configuration;
using CrystariumBoutique.Core.Catalog;
using Dalamud.Bindings.ImGui;

namespace CrystariumBoutique.Ui;

internal static class ItemTooltipRenderer
{
    private const int MaximumDisplayedVendors = 3;
    private const int MaximumDisplayedQuests = 2;
    private const float MinimumTooltipWidth = 320f;
    private const float MaximumTooltipWidth = 760f;
    private const float HeaderColumnGap = 28f;

    public static void Draw(
        EquipmentItem item,
        ImmutableArray<EquipmentItem> modelFamilyItems,
        Vector2 iconPosition,
        float iconSize,
        PluginConfiguration configuration,
        ItemRarityColorResolver rarityColors,
        BoutiqueFontSet fonts)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(rarityColors);
        ArgumentNullException.ThrowIfNull(fonts);

        var transparency = Math.Clamp(configuration.TooltipTransparencyPercent, 0, 100);
        var backgroundAlpha = 1f - (transparency / 100f);
        var fontScale = Math.Clamp(configuration.TooltipFontScalePercent, 75, 200) / 100f;
        var background = new Vector4(BoutiqueTheme.TooltipBackground, backgroundAlpha);
        var localSources = ItemAcquisitionFormatter.GetLocalSources(item);
        var enabledSources = localSources
            .Where(source => IsEnabled(source.Kind, configuration))
            .ToImmutableArray();
        var sharedItems = configuration.TooltipShowSharedModels
            ? GetSharedItems(item, modelFamilyItems, configuration.TooltipSharedModelLimit)
            : ImmutableArray<EquipmentItem>.Empty;
        var acquisitionLines = BuildAcquisitionLines(
            localSources,
            enabledSources,
            sharedItems,
            configuration);

        using var tooltipFont = fonts.Tooltip.Push();
        var tooltipWidth = CalculateWidth(item, acquisitionLines, sharedItems, fontScale, fonts);
        PositionTooltip(
            iconPosition,
            iconSize,
            tooltipWidth,
            EstimateHeight(acquisitionLines.Length, sharedItems.Length, fontScale));

        ImGui.SetNextWindowSizeConstraints(
            new Vector2(tooltipWidth, 0f),
            new Vector2(tooltipWidth, Math.Max(180f, ImGui.GetIO().DisplaySize.Y - 24f)));
        ImGui.PushStyleColor(ImGuiCol.WindowBg, background);
        ImGui.PushStyleColor(
            ImGuiCol.Border,
            BoutiqueTheme.IsCrystariumProfile(configuration)
                ? BoutiqueTheme.CrystariumGold
                : BoutiqueTheme.GetItemBorderColor(configuration));
        var crystariumFrame = BoutiqueTheme.IsCrystariumProfile(configuration);
        ImGui.PushStyleColor(
            ImGuiCol.Text,
            crystariumFrame
                ? BoutiqueTheme.CrystariumText
                : BoutiqueTheme.IsSimpleCrystariumProfile(configuration)
                    ? BoutiqueTheme.SimpleCrystariumSoftWhite
                    : Vector4.One);
        ImGui.PushStyleVar(
            ImGuiStyleVar.WindowPadding,
            crystariumFrame
                ? new Vector2(14f, 6f)
                : new Vector2(BoutiqueTheme.TooltipPadding + 2f));
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, crystariumFrame ? 0f : 2f);
        ImGui.PushStyleVar(ImGuiStyleVar.PopupBorderSize, crystariumFrame ? 0f : 2f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, crystariumFrame ? 10f : 5f);
        ImGui.PushStyleVar(ImGuiStyleVar.PopupRounding, crystariumFrame ? 10f : 5f);

        ImGui.BeginTooltip();
        BoutiqueScaleStyle.ApplyRelativeWindowFontScale(fontScale);
        DrawHeader(item, rarityColors.GetColor(item), fonts);
        ImGui.Separator();
        DrawAcquisition(acquisitionLines, configuration);
        if (!sharedItems.IsEmpty)
        {
            ImGui.Spacing();
            ImGui.Separator();
            DrawSharedAppearances(sharedItems, configuration, rarityColors, fonts);
        }

        BoutiqueTheme.DrawTooltipGoldFrame(configuration);

        ImGui.EndTooltip();
        ImGui.PopStyleVar(5);
        ImGui.PopStyleColor(3);
    }

    private static void DrawHeader(
        EquipmentItem item,
        Vector4 rarityColor,
        BoutiqueFontSet fonts)
    {
        var stats = $"LV {item.EquipLevel} | ILV {item.ItemLevel}";
        var id = $"ID {item.ItemId.ToString(CultureInfo.InvariantCulture)}";
        var dyeSlots = item.DyeChannelCount == 0
            ? "Dye slots none"
            : $"Dye slots {item.DyeChannelCount.ToString(CultureInfo.InvariantCulture)}";
        float statsWidth;
        using (fonts.TooltipSmall.Push())
        {
            statsWidth = Math.Max(
                ImGui.CalcTextSize(stats).X,
                Math.Max(ImGui.CalcTextSize(id).X, ImGui.CalcTextSize(dyeSlots).X)) + 6f;
        }

        if (!ImGui.BeginTable(
                "##AcquisitionTooltipHeader",
                3,
                ImGuiTableFlags.SizingStretchProp
                | ImGuiTableFlags.NoSavedSettings))
        {
            return;
        }

        ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch, 1f);
        ImGui.TableSetupColumn("Gap", ImGuiTableColumnFlags.WidthFixed, HeaderColumnGap);
        ImGui.TableSetupColumn("Stats", ImGuiTableColumnFlags.WidthFixed, statsWidth);
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        using (fonts.TooltipItemName.Push())
        {
            ImGui.PushStyleColor(ImGuiCol.Text, rarityColor);
            ImGui.TextWrapped(item.Name);
            ImGui.PopStyleColor();
        }

        ImGui.TableNextColumn();
        ImGui.Dummy(new Vector2(HeaderColumnGap, 1f));
        ImGui.TableNextColumn();
        using (fonts.TooltipSmall.Push())
        {
            DrawRightAligned(stats);
            DrawRightAligned(id);
            DrawRightAligned(dyeSlots);
        }

        ImGui.EndTable();
    }

    private static void DrawAcquisition(
        ImmutableArray<AcquisitionLine> lines,
        PluginConfiguration configuration)
    {
        ImGui.TextColored(
            BoutiqueTheme.IsSimpleCrystariumProfile(configuration)
                ? BoutiqueTheme.SimpleCrystariumLightSilver
                : BoutiqueTheme.AntiqueGold,
            "HOW TO OBTAIN");
        foreach (var line in lines)
        {
            var color = line.Kind is { } kind
                ? GetSourceColor(kind, configuration)
                : BoutiqueTheme.GetSecondaryTextColor(configuration);
            if (line.Kind == ItemAcquisitionKind.DutyDrop)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, color);
                ImGui.TextWrapped(line.Text);
                ImGui.PopStyleColor();
                continue;
            }

            ImGui.TextColored(color, ">");
            ImGui.SameLine();
            ImGui.TextWrapped(line.Text);
        }
    }

    private static void DrawSharedAppearances(
        ImmutableArray<EquipmentItem> sharedItems,
        PluginConfiguration configuration,
        ItemRarityColorResolver rarityColors,
        BoutiqueFontSet fonts)
    {
        ImGui.TextColored(
            BoutiqueTheme.IsSimpleCrystariumProfile(configuration)
                ? BoutiqueTheme.SimpleCrystariumLightSilver
                : BoutiqueTheme.AntiqueGold,
            "OTHER ITEMS SHARING THIS MODEL");
        if (!ImGui.BeginTable(
                "##SharedAppearanceSources",
                2,
                ImGuiTableFlags.SizingStretchProp
                | ImGuiTableFlags.NoSavedSettings
                | ImGuiTableFlags.NoPadInnerX))
        {
            return;
        }

        ImGui.TableSetupColumn("Item", ImGuiTableColumnFlags.WidthStretch, 1.3f);
        ImGui.TableSetupColumn("Availability", ImGuiTableColumnFlags.WidthStretch, 1f);
        foreach (var sharedItem in sharedItems)
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.PushStyleColor(ImGuiCol.Text, rarityColors.GetColor(sharedItem));
            ImGui.TextWrapped(sharedItem.Name);
            ImGui.PopStyleColor();
            ImGui.TableNextColumn();
            using (fonts.TooltipSmall.Push())
            {
                ImGui.TextWrapped(GetSharedSourceSummary(sharedItem, configuration));
            }
        }

        ImGui.EndTable();
    }

    private static ImmutableArray<AcquisitionLine> BuildAcquisitionLines(
        ImmutableArray<ItemAcquisitionSource> localSources,
        ImmutableArray<ItemAcquisitionSource> enabledSources,
        ImmutableArray<EquipmentItem> sharedItems,
        PluginConfiguration configuration)
    {
        if (enabledSources.IsEmpty)
        {
            return
            [
                new AcquisitionLine(
                    localSources.IsEmpty
                        ? sharedItems.Any(shared => ItemAcquisitionFormatter
                            .GetLocalSources(shared)
                            .Any(source => IsEnabled(source.Kind, configuration)))
                            ? "Exact source is not exposed by installed local game sheets; verified sources for items sharing this model are listed below."
                            : "Exact source is not exposed by installed local game sheets."
                        : "Acquisition sources are hidden by the current tooltip settings.",
                    null),
            ];
        }

        var displayedSources = ImmutableArray.CreateBuilder<ItemAcquisitionSource>();
        var displayedVendors = 0;
        var displayedQuests = 0;
        foreach (var source in enabledSources)
        {
            if (source.Kind == ItemAcquisitionKind.Vendor)
            {
                if (displayedVendors >= MaximumDisplayedVendors)
                {
                    continue;
                }

                displayedVendors++;
            }
            else if (source.Kind == ItemAcquisitionKind.Quest)
            {
                if (displayedQuests >= MaximumDisplayedQuests)
                {
                    continue;
                }

                displayedQuests++;
            }

            displayedSources.Add(source);
        }

        var formatted = displayedSources
            .Select(source => new AcquisitionLine(
                ItemAcquisitionFormatter.Format(source, configuration.TooltipAcquisitionDetail),
                source.Kind))
            .DistinctBy(line => line.Text, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return formatted.ToImmutableArray();
    }

    internal static ImmutableArray<EquipmentItem> GetSharedItems(
        EquipmentItem item,
        ImmutableArray<EquipmentItem> modelFamilyItems,
        int limit)
    {
        if (modelFamilyItems.IsDefaultOrEmpty)
        {
            return ImmutableArray<EquipmentItem>.Empty;
        }

        return modelFamilyItems
            .Where(source => source.ItemId != item.ItemId)
            .DistinctBy(source => source.ItemId)
            .Take(Math.Clamp(limit, 1, 24))
            .ToImmutableArray();
    }

    private static string GetSharedSourceSummary(
        EquipmentItem item,
        PluginConfiguration configuration)
    {
        var sources = ItemAcquisitionFormatter.GetLocalSources(item)
            .Where(candidate => IsEnabled(candidate.Kind, configuration))
            .Select(source => ItemAcquisitionFormatter.GetCompactLabel(source.Kind))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return sources.Length == 0
            ? "[Source unavailable]"
            : $"[{string.Join(", ", sources)}]";
    }

    private static bool IsEnabled(ItemAcquisitionKind kind, PluginConfiguration configuration)
        => kind switch
        {
            ItemAcquisitionKind.Vendor => configuration.TooltipShowVendors,
            ItemAcquisitionKind.MarketBoard => configuration.TooltipShowMarketBoard,
            ItemAcquisitionKind.Crafting => configuration.TooltipShowCrafting,
            ItemAcquisitionKind.Quest => configuration.TooltipShowQuests,
            ItemAcquisitionKind.Achievement => configuration.TooltipShowAchievements,
            ItemAcquisitionKind.SeasonalEvent => configuration.TooltipShowSeasonalEvents,
            ItemAcquisitionKind.OnlineStore => configuration.TooltipShowOnlineStore,
            ItemAcquisitionKind.DutyDrop => configuration.TooltipShowDutyDrops,
            _ => false,
        };

    private static Vector4 GetSourceColor(
        ItemAcquisitionKind kind,
        PluginConfiguration configuration)
        => kind switch
        {
            ItemAcquisitionKind.MarketBoard => BoutiqueTheme.Available,
            ItemAcquisitionKind.OnlineStore => BoutiqueTheme.Unavailable,
            ItemAcquisitionKind.SeasonalEvent => BoutiqueTheme.IsSimpleCrystariumProfile(configuration)
                ? BoutiqueTheme.SimpleCrystariumLightSilver
                : BoutiqueTheme.AntiqueGold,
            _ => BoutiqueTheme.IsSimpleCrystariumProfile(configuration)
                ? BoutiqueTheme.SimpleCrystariumSoftWhite
                : Vector4.One,
        };

    private static float CalculateWidth(
        EquipmentItem item,
        ImmutableArray<AcquisitionLine> acquisitionLines,
        ImmutableArray<EquipmentItem> sharedItems,
        float fontScale,
        BoutiqueFontSet fonts)
    {
        float statsWidth;
        using (fonts.TooltipSmall.Push())
        {
            statsWidth = ImGui.CalcTextSize($"LV {item.EquipLevel} | ILV {item.ItemLevel}").X;
        }

        float itemNameWidth;
        using (fonts.TooltipItemName.Push())
        {
            itemNameWidth = ImGui.CalcTextSize(item.Name).X;
        }

        var headerWidth = itemNameWidth + statsWidth + HeaderColumnGap + 34f;
        var contentWidth = acquisitionLines
            .Select(line => ImGui.CalcTextSize(line.Text).X + 34f)
            .Concat(sharedItems.Select(shared =>
                ImGui.CalcTextSize(shared.Name).X
                + ImGui.CalcTextSize("Mogstation - Online Store").X
                + 50f))
            .DefaultIfEmpty(0f)
            .Max();
        return Math.Clamp(
            Math.Max(headerWidth, contentWidth) * fontScale,
            MinimumTooltipWidth,
            MaximumTooltipWidth);
    }

    private static float EstimateHeight(int acquisitionLines, int sharedItems, float fontScale)
        => (140f + (acquisitionLines * 26f) + (sharedItems * 31f)) * fontScale;

    private static void PositionTooltip(
        Vector2 iconPosition,
        float iconSize,
        float tooltipWidth,
        float estimatedHeight)
    {
        const float cornerGap = 4f;
        var displaySize = ImGui.GetIO().DisplaySize;
        var placeRight = iconPosition.X + iconSize + cornerGap + tooltipWidth
            <= displaySize.X - BoutiqueTheme.TooltipPadding;
        var placeAbove = iconPosition.Y + cornerGap - estimatedHeight
            >= BoutiqueTheme.TooltipPadding;
        var anchor = new Vector2(
            placeRight ? iconPosition.X + iconSize + cornerGap : iconPosition.X - cornerGap,
            placeAbove ? iconPosition.Y + cornerGap : iconPosition.Y + iconSize - cornerGap);
        var pivot = new Vector2(placeRight ? 0f : 1f, placeAbove ? 1f : 0f);
        ImGui.SetNextWindowPos(anchor, ImGuiCond.Always, pivot);
    }

    private static void DrawRightAligned(string text)
    {
        var cursorX = ImGui.GetCursorPosX();
        var availableWidth = ImGui.GetContentRegionAvail().X;
        var width = ImGui.CalcTextSize(text).X;
        ImGui.SetCursorPosX(cursorX + Math.Max(availableWidth - width, 0f));
        ImGui.TextUnformatted(text);
    }

    private sealed record AcquisitionLine(string Text, ItemAcquisitionKind? Kind);
}
