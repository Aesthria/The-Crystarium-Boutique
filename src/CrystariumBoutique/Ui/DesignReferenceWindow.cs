using System.Collections.Immutable;
using System.Globalization;
using System.Numerics;
using CrystariumBoutique.Configuration;
using CrystariumBoutique.Core.Catalog;
using CrystariumBoutique.Core.Loadouts;
using CrystariumBoutique.Core.Sessions;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;

namespace CrystariumBoutique.Ui;

internal sealed class DesignReferenceWindow : Window
{
    private const float IconSize = 54f;
    private const float ItemCardHeight = 112f;
    private static readonly Vector2 DefaultWindowSize = new(520f, 620f);
    private readonly ITextureProvider textureProvider;
    private readonly IGameGui gameGui;
    private readonly IItemRepository itemRepository;
    private readonly ItemRarityColorResolver itemRarityColors;
    private readonly ConfigurationStore configuration;
    private readonly BoutiqueFontSet fonts;
    private readonly ISharedImmediateTexture crystariumBackgroundTexture;
    private BoutiqueLoadout? design;
    private ulong assignedHoveredItem;
    private bool customBackgroundPushed;
    private CrystariumProfileStyleStack profileStyleStack;
    private IDisposable? titleFontScope;

    public DesignReferenceWindow(
        ITextureProvider textureProvider,
        IGameGui gameGui,
        IItemRepository itemRepository,
        ItemRarityColorResolver itemRarityColors,
        ConfigurationStore configuration,
        BoutiqueFontSet fonts,
        string assetDirectory)
        : base("Boutique Design List###CrystariumBoutiqueDesignReference")
    {
        this.textureProvider = textureProvider ?? throw new ArgumentNullException(nameof(textureProvider));
        this.gameGui = gameGui ?? throw new ArgumentNullException(nameof(gameGui));
        this.itemRepository = itemRepository ?? throw new ArgumentNullException(nameof(itemRepository));
        this.itemRarityColors = itemRarityColors ?? throw new ArgumentNullException(nameof(itemRarityColors));
        this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        this.fonts = fonts ?? throw new ArgumentNullException(nameof(fonts));
        ArgumentException.ThrowIfNullOrWhiteSpace(assetDirectory);
        crystariumBackgroundTexture = textureProvider.GetFromFile(
            Path.Combine(assetDirectory, "images", "crystarium-stained-glass.png"));
        Size = DefaultWindowSize;
        SizeCondition = ImGuiCond.FirstUseEver;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(360f, 280f),
            MaximumSize = new Vector2(float.MaxValue),
        };
        IsOpen = false;
    }

    public void Show(BoutiqueLoadout loadout)
    {
        design = loadout ?? throw new ArgumentNullException(nameof(loadout));
        IsOpen = true;
    }

    public override void Draw()
    {
        BoutiqueScaleStyle.ApplyResponsiveWindowFontScale(
            configuration.Current.InterfaceScalePercent,
            DefaultWindowSize);
        using var bodyFont = fonts.Body.Push();
        PluginBackgroundStyle.DrawCrystariumBackdrop(
            configuration.Current,
            crystariumBackgroundTexture);
        if (design is null)
        {
            ImGui.TextColored(BoutiqueTheme.MutedText, "No design is selected.");
            ClearOwnedHoveredItem();
            return;
        }

        ImGui.TextColored(BoutiqueTheme.GetHeadingColor(configuration.Current), design.Name);
        ImGui.TextColored(
            BoutiqueTheme.MutedText,
            $"{design.Equipment.Length:N0} item{(design.Equipment.Length == 1 ? string.Empty : "s")} · movable and resizable reference");
        if (design.Notes is not null)
        {
            ImGui.TextWrapped(design.Notes);
        }

        ImGui.Separator();
        ulong hoveredItem = 0;
        if (ImGui.BeginChild("##DesignReferenceItems", Vector2.Zero, false))
        {
            foreach (var equipment in design.Equipment)
            {
                if (ImGui.BeginChild(
                        $"##DesignReference{equipment.Slot}",
                        new Vector2(0f, ItemCardHeight),
                        true,
                        ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
                {
                    DrawIcon(equipment.IconId);
                    if (ImGui.IsItemHovered() && equipment.Appearance.SourceItemId is > 0)
                    {
                        hoveredItem = equipment.Appearance.SourceItemId.Value;
                        if (BoutiqueTheme.ShouldShowItemTooltip(configuration.Current))
                        {
                            if (itemRepository.TryGetItem(
                                    equipment.Appearance.SourceItemId.Value,
                                    equipment.Slot,
                                    out var item))
                            {
                                ItemTooltipRenderer.Draw(
                                    item,
                                    itemRepository.GetItemsSharingModel(
                                        item.ModelFamilyKey,
                                        equipment.Slot),
                                    ImGui.GetItemRectMin(),
                                    IconSize,
                                    configuration.Current,
                                    itemRarityColors,
                                    fonts);
                            }
                            else
                            {
                                ImGui.SetTooltip($"{equipment.DisplayName}\nItem ID {hoveredItem.ToString(CultureInfo.InvariantCulture)}");
                            }
                        }
                    }

                    ImGui.SameLine();
                    ImGui.BeginGroup();
                    ImGui.TextColored(
                        BoutiqueTheme.MutedText,
                        EquipmentSlotDefinitions.GetDisplayName(equipment.Slot));
                    ImGui.TextWrapped(equipment.DisplayName);
                    if (equipment.DyeChannelCount > 0)
                    {
                        ImGui.TextColored(
                            BoutiqueTheme.MutedText,
                            $"Dyes: {GetStainValue(equipment, 0)} / {GetStainValue(equipment, 1)}");
                    }

                    ImGui.EndGroup();
                }

                ImGui.EndChild();
            }
        }

        ImGui.EndChild();
        UpdateHoveredItem(hoveredItem);
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
    }

    public override void PostDraw()
    {
        ImGui.PopStyleVar();
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
        => ClearOwnedHoveredItem();

    public void Reset()
    {
        IsOpen = false;
        design = null;
        ClearOwnedHoveredItem();
    }

    private void DrawIcon(uint iconId)
    {
        var lookup = new GameIconLookup(iconId, itemHq: false, hiRes: true);
        if (textureProvider.TryGetFromGameIcon(lookup, out var sharedTexture)
            && sharedTexture.GetWrapOrDefault() is { } texture)
        {
            ImGui.Image(texture.Handle, new Vector2(IconSize));
            return;
        }

        ImGui.Dummy(new Vector2(IconSize));
    }

    private void UpdateHoveredItem(ulong itemId)
    {
        if (itemId > 0)
        {
            gameGui.HoveredItem = itemId;
            assignedHoveredItem = itemId;
            return;
        }

        ClearOwnedHoveredItem();
    }

    private void ClearOwnedHoveredItem()
    {
        if (assignedHoveredItem > 0 && gameGui.HoveredItem == assignedHoveredItem)
        {
            gameGui.HoveredItem = 0;
        }

        assignedHoveredItem = 0;
    }

    private static string GetStainValue(LoadoutEquipmentState equipment, byte channel)
        => channel >= equipment.DyeChannelCount
            ? "—"
            : equipment.Appearance.Stains.Length > channel
                ? equipment.Appearance.Stains[channel].Value.ToString(CultureInfo.InvariantCulture)
                : "0";
}
