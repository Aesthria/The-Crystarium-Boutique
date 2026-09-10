using System.Collections.Immutable;
using System.Globalization;
using System.Numerics;
using CrystariumBoutique.Configuration;
using CrystariumBoutique.Core.Appearance;
using CrystariumBoutique.Core.Catalog;
using CrystariumBoutique.Core.Dyes;
using CrystariumBoutique.Core.Errors;
using CrystariumBoutique.Core.ExternalDesigns;
using CrystariumBoutique.Core.Loadouts;
using CrystariumBoutique.Core.Sessions;
using CrystariumBoutique.Integrations;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures;
using Dalamud.Plugin.Services;

namespace CrystariumBoutique.Ui.Components;

internal sealed record LoadoutFeedback(
    bool IsSuccess,
    string Message,
    BoutiqueError? Error);

internal sealed class LoadoutPanel
{
    internal const string ImportDesignLabel = "Import Design";
    internal const string ExportDesignLabel = "Export Design";
    internal const string ImportDesignTooltip =
        "Clicking this button loads a Design string shared by another user and saves it to your Designs list so you can try it yourself.";

    private const string SavePopupId = "##BoutiqueSaveLoadout";
    private const string ImportPopupId = "##BoutiqueImportLoadout";
    private const string EorzeaImportPopupId = "##BoutiqueEorzeaCollectionImport";
    private const float SavePopupWidth = 480f;
    private const float DesignListWidth = 300f;
    private const float DesignCardMinimumHeight = 168f;
    private const float DesignDyeFieldHeight = 25f;
    private static readonly EquipmentSlotDefinition[] DesignCardLayout =
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

    private readonly LoadoutLibrary library;
    private readonly BoutiqueSessionController sessionController;
    private readonly EquipmentBrowserController browser;
    private readonly EorzeaCollectionImportService eorzeaCollection;
    private readonly IStainRepository stains;
    private readonly IItemRepository itemRepository;
    private readonly ITextureProvider textureProvider;
    private readonly ItemRarityColorResolver itemRarityColors;
    private readonly ConfigurationStore configuration;
    private readonly BoutiqueFontSet fonts;
    private readonly Action<LoadoutFeedback> reportFeedback;
    private readonly Action<BoutiqueLoadout> showReference;
    private readonly string? loadError;
    private Guid? selectedLoadoutId;
    private LoadoutEditMode editMode;
    private LoadoutConfirmation confirmation;
    private string saveName = string.Empty;
    private string saveNotes = string.Empty;
    private string editName = string.Empty;
    private string importPayload = string.Empty;
    private string eorzeaCollectionUrl = string.Empty;
    private Guid? activeLoadoutId;
    private bool eorzeaCollectionActive;
    private bool openEorzeaImportPopup;
    private bool openBoutiqueSavePopup;
    private Task<Result<BoutiqueLoadout>>? eorzeaImportTask;
    private string? toolbarNotice;

    public LoadoutPanel(
        LoadoutLibrary library,
        BoutiqueSessionController sessionController,
        EquipmentBrowserController browser,
        EorzeaCollectionImportService eorzeaCollection,
        IStainRepository stains,
        IItemRepository itemRepository,
        ITextureProvider textureProvider,
        ItemRarityColorResolver itemRarityColors,
        ConfigurationStore configuration,
        BoutiqueFontSet fonts,
        Action<LoadoutFeedback> reportFeedback,
        Action<BoutiqueLoadout> showReference,
        string? loadError)
    {
        this.library = library ?? throw new ArgumentNullException(nameof(library));
        this.sessionController = sessionController ?? throw new ArgumentNullException(nameof(sessionController));
        this.browser = browser ?? throw new ArgumentNullException(nameof(browser));
        this.eorzeaCollection = eorzeaCollection ?? throw new ArgumentNullException(nameof(eorzeaCollection));
        this.stains = stains ?? throw new ArgumentNullException(nameof(stains));
        this.itemRepository = itemRepository ?? throw new ArgumentNullException(nameof(itemRepository));
        this.textureProvider = textureProvider ?? throw new ArgumentNullException(nameof(textureProvider));
        this.itemRarityColors = itemRarityColors ?? throw new ArgumentNullException(nameof(itemRarityColors));
        this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        this.fonts = fonts ?? throw new ArgumentNullException(nameof(fonts));
        this.reportFeedback = reportFeedback ?? throw new ArgumentNullException(nameof(reportFeedback));
        this.showReference = showReference ?? throw new ArgumentNullException(nameof(showReference));
        this.loadError = loadError;
    }

    public int Count => library.Loadouts.Length;

    public void NotifyManualChange()
    {
        activeLoadoutId = null;
        eorzeaCollectionActive = false;
    }

    public void ResetActiveDesign()
    {
        activeLoadoutId = null;
        eorzeaCollectionActive = false;
    }

    public void DrawBoutiqueDesignSelector()
    {
        using (fonts.SectionHeading.Push())
        {
            ImGui.TextColored(
                BoutiqueTheme.GetBoutiqueTabTextColor(configuration.Current),
                "DESIGNS");
        }
        ImGui.SetNextItemWidth(-1f);
        var activeDesignName = GetActiveDesignName();
        if (!BoutiqueTheme.BeginResponsiveCombo(
                configuration.Current,
                "##BoutiqueDesignSelector",
                activeDesignName,
                BoutiqueTheme.GetBoutiqueTabTextColor(configuration.Current)))
        {
            return;
        }

        var canApply = sessionController.Session.State == BoutiqueSessionState.Active;
        ImGui.BeginDisabled(!canApply);
        ImGui.PushStyleColor(ImGuiCol.Text, BoutiqueTheme.GetHeadingColor(configuration.Current));
        var importEorzeaCollection = ImGui.Selectable("Eorzea Collection Import from URL");
        ImGui.PopStyleColor();
        if (importEorzeaCollection)
        {
            var clipboard = ImGui.GetClipboardText()?.Trim() ?? string.Empty;
            eorzeaCollectionUrl = EorzeaCollectionImport
                .ConvertToApiUri(clipboard).IsSuccess
                ? clipboard
                : string.Empty;
            openEorzeaImportPopup = true;
        }

        ImGui.EndDisabled();
        ImGui.Separator();

        var noCustomDesign = activeLoadoutId is null && !eorzeaCollectionActive;
        ImGui.BeginDisabled(!canApply);
        if (ImGui.Selectable("No Design", noCustomDesign))
        {
            var result = sessionController.ResetAll();
            if (result.IsSuccess)
            {
                activeLoadoutId = null;
                eorzeaCollectionActive = false;
                browser.ClearAppearanceSelection();
            }

            Report(result, result.IsSuccess
                ? "Returned to the session opening appearance."
                : "The current design could not be cleared.");
        }

        ImGui.EndDisabled();
        foreach (var loadout in library.Loadouts)
        {
            var selected = activeLoadoutId == loadout.Id;
            ImGui.BeginDisabled(!canApply);
            if (ImGui.Selectable($"{loadout.Name}##BoutiqueDesign{loadout.Id:N}", selected))
            {
                ApplyDesign(loadout);
            }

            ImGui.EndDisabled();
        }

        BoutiqueTheme.EndResponsiveCombo();
    }

    public void DrawBoutiqueSaveButton(bool includeLabelSpacing = true)
    {
        if (includeLabelSpacing)
        {
            ImGui.Dummy(new Vector2(0f, ImGui.GetTextLineHeight()));
        }

        ImGui.BeginDisabled(sessionController.ChangedSlotCount == 0 || loadError is not null);
        if (BoutiqueTheme.FittedTextButton(
                configuration.Current,
                "##BoutiqueSave",
                "Save Design",
                new Vector2(-1f, 0f),
                minimumFontScale: 0.22f))
        {
            saveName = SuggestNewName();
            saveNotes = string.Empty;
            openBoutiqueSavePopup = true;
        }

        ImGui.EndDisabled();
    }

    public void DrawBoutiquePopups()
    {
        CompleteEorzeaImport();
        if (openEorzeaImportPopup)
        {
            ImGui.OpenPopup(EorzeaImportPopupId);
            openEorzeaImportPopup = false;
        }

        if (openBoutiqueSavePopup)
        {
            ImGui.OpenPopup(SavePopupId);
            openBoutiqueSavePopup = false;
        }

        DrawEorzeaImportPopup();
        DrawSavePopup();
    }

    public void DrawDesignsTab()
    {
        if (!ImGui.BeginChild(
                "##DesignsWorkspace",
                Vector2.Zero,
                true,
                ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
        {
            ImGui.EndChild();
            return;
        }

        EnsureSelection();
        DrawDesignToolbar(GetSelected());
        BoutiqueTheme.DrawProfileHorizontalRule(
            configuration.Current,
            BoutiqueTheme.GetHorizontalRuleColor(configuration.Current));
        if (!string.IsNullOrWhiteSpace(toolbarNotice))
        {
            ImGui.TextColored(BoutiqueTheme.Available, toolbarNotice);
        }
        DrawSavePopup();
        DrawImportPopup();
        if (GetSelected() is { } selectedForEdit)
        {
            DrawInlineEdit(selectedForEdit);
            DrawConfirmation(selectedForEdit);
        }

        var footerHeight = ImGui.GetFrameHeight() + ImGui.GetStyle().ItemSpacing.Y;
        var contentHeight = Math.Max(1f, ImGui.GetContentRegionAvail().Y - footerHeight);
        if (ImGui.BeginChild("##DesignLibraryBody", new Vector2(0f, contentHeight), false))
        {
            DrawDesignLibrary();
        }

        ImGui.EndChild();
        DrawGenerateListFooter(GetSelected());
        ImGui.EndChild();
    }

    private void DrawDesignLibrary()
    {

        if (loadError is not null)
        {
            ImGui.TextColored(BoutiqueTheme.Unavailable, "The local design library is unavailable.");
            ImGui.TextWrapped(loadError);
            return;
        }

        var read = library.LastRead;
        if (read is { Warnings.Length: > 0 })
        {
            ImGui.TextColored(BoutiqueTheme.Unavailable, "Some saved designs required recovery:");
            foreach (var warning in read.Warnings)
            {
                ImGui.TextWrapped($"• {warning}");
            }

            ImGui.Separator();
        }

        if (library.Loadouts.IsEmpty)
        {
            selectedLoadoutId = null;
            ImGui.TextColored(BoutiqueTheme.MutedText, "No saved designs yet.");
            ImGui.TextWrapped("Preview equipment and save it, or import a shared Boutique design.");
            return;
        }

        if (ImGui.BeginChild("##DesignList", new Vector2(DesignListWidth, 0f), true))
        {
            using (fonts.SectionHeading.Push())
            {
                ImGui.TextColored(BoutiqueTheme.MutedText, "SAVED DESIGNS");
            }
            BoutiqueTheme.DrawProfileHorizontalRule(
                configuration.Current,
                BoutiqueTheme.GetDecorativeRuleColor(configuration.Current),
                2f);
            foreach (var loadout in library.Loadouts)
            {
                var selected = loadout.Id == selectedLoadoutId;
                if (ImGui.Selectable($"{loadout.Name}##{loadout.Id:N}", selected))
                {
                    selectedLoadoutId = loadout.Id;
                    editMode = LoadoutEditMode.None;
                    confirmation = LoadoutConfirmation.None;
                }
            }
        }

        ImGui.EndChild();
        ImGui.SameLine();
        if (ImGui.BeginChild("##DesignDetails", Vector2.Zero, true))
        {
            using (fonts.SectionHeading.Push())
            {
                ImGui.TextColored(BoutiqueTheme.MutedText, "SAVED OUTFIT INFO");
            }
            BoutiqueTheme.DrawProfileHorizontalRule(
                configuration.Current,
                BoutiqueTheme.GetDecorativeRuleColor(configuration.Current),
                2f);
            if (GetSelected() is { } selected)
            {
                DrawDetails(selected);
            }
        }

        ImGui.EndChild();
    }

    private void DrawGenerateListFooter(BoutiqueLoadout? selected)
    {
        const string label = "Generate List";
        var buttonWidth = ImGui.CalcTextSize(label).X + (ImGui.GetStyle().FramePadding.X * 2f);
        ImGui.SetCursorPosX(
            ImGui.GetCursorPosX() + Math.Max(ImGui.GetContentRegionAvail().X - buttonWidth, 0f));
        var buttonColor = BoutiqueTheme.GetNeutralActionButtonColor(configuration.Current);
        ImGui.PushStyleColor(ImGuiCol.Button, buttonColor);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, BoutiqueTheme.GetHoveredControlColor(buttonColor));
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, BoutiqueTheme.GetActiveControlColor(buttonColor));
        ImGui.PushStyleColor(ImGuiCol.Text, Vector4.One);
        ImGui.BeginDisabled(selected is null);
        if (BoutiqueTheme.FramedButton(
                configuration.Current,
                label,
                new Vector2(buttonWidth, 0f)) && selected is not null)
        {
            showReference(selected);
        }

        ImGui.EndDisabled();
        ImGui.PopStyleColor(4);
    }

    private void DrawEorzeaImportPopup()
    {
        ImGui.SetNextWindowSizeConstraints(
            new Vector2(620f, 0f),
            new Vector2(620f, float.MaxValue));
        if (!ImGui.BeginPopup(EorzeaImportPopupId))
        {
            return;
        }

        ImGui.TextColored(BoutiqueTheme.GetHeadingColor(configuration.Current), "EORZEA COLLECTION IMPORT");
        ImGui.TextWrapped(
            "Paste a public Eorzea Collection glamour URL. The imported outfit is applied to the current Boutique session and is not saved until you choose Save Design.");
        ImGui.SetNextItemWidth(-1f);
        ImGui.InputTextWithHint(
            "##EorzeaCollectionUrl",
            "https://ffxiv.eorzeacollection.com/glamour/12345/name",
            ref eorzeaCollectionUrl,
            2048);
        BoutiqueTheme.DrawLastGoldControlFrame(configuration.Current);

        var isImporting = eorzeaImportTask is not null;
        var validUrl = EorzeaCollectionImport.ConvertToApiUri(eorzeaCollectionUrl).IsSuccess;
        ImGui.BeginDisabled(isImporting || !validUrl);
        if (BoutiqueTheme.FramedButton(configuration.Current, "Import and Apply"))
        {
            eorzeaImportTask = eorzeaCollection.ImportAsync(eorzeaCollectionUrl);
        }

        ImGui.EndDisabled();
        ImGui.SameLine();
        if (BoutiqueTheme.FramedButton(configuration.Current, "Paste Clipboard"))
        {
            eorzeaCollectionUrl = ImGui.GetClipboardText()?.Trim() ?? string.Empty;
        }

        ImGui.SameLine();
        if (BoutiqueTheme.FramedButton(configuration.Current, "Close"))
        {
            ImGui.CloseCurrentPopup();
        }

        if (isImporting)
        {
            ImGui.TextColored(BoutiqueTheme.MutedText, "Downloading and resolving the design…");
        }
        else if (eorzeaCollectionUrl.Length > 0 && !validUrl)
        {
            ImGui.TextColored(
                BoutiqueTheme.Unavailable,
                "Use a full public ffxiv.eorzeacollection.com/glamour/... URL.");
        }

        ImGui.EndPopup();
    }

    private void CompleteEorzeaImport()
    {
        if (eorzeaImportTask is null || !eorzeaImportTask.IsCompleted)
        {
            return;
        }

        Result<BoutiqueLoadout> importResult;
        try
        {
            importResult = eorzeaImportTask.GetAwaiter().GetResult();
        }
        catch (Exception exception)
        {
            importResult = Result.Failure<BoutiqueLoadout>(new BoutiqueError(
                BoutiqueErrorCode.ExternalImportFailed,
                "The Eorzea Collection glamour could not be imported.",
                $"{exception.GetType().Name}: {exception.Message}"));
        }

        eorzeaImportTask = null;
        if (!importResult.IsSuccess || importResult.Value is null)
        {
            reportFeedback(new LoadoutFeedback(
                false,
                importResult.Error?.Message ?? "The Eorzea Collection glamour could not be imported.",
                importResult.Error));
            return;
        }

        var applyResult = sessionController.ApplyLoadout(importResult.Value);
        if (applyResult.IsSuccess)
        {
            activeLoadoutId = null;
            eorzeaCollectionActive = true;
            browser.ClearAppearanceSelection();
        }

        Report(
            applyResult,
            applyResult.IsSuccess
                ? $"Imported and applied: {EorzeaCollectionImport.DefaultDesignName}"
                : "The imported design could not be applied.");
    }

    private void ApplyDesign(BoutiqueLoadout loadout)
    {
        var result = sessionController.ApplyLoadout(loadout);
        if (result.IsSuccess)
        {
            activeLoadoutId = loadout.Id;
            eorzeaCollectionActive = false;
            selectedLoadoutId = loadout.Id;
            browser.ClearAppearanceSelection();
        }

        Report(result, result.IsSuccess
            ? $"Loaded: {loadout.Name}"
            : "The selected design could not be applied.");
    }

    private string GetActiveDesignName()
    {
        if (eorzeaCollectionActive)
        {
            return EorzeaCollectionImport.DefaultDesignName;
        }

        return activeLoadoutId.HasValue
            && library.TryGet(activeLoadoutId.Value, out var loadout)
                ? loadout.Name
                : "No Design";
    }

    private void DrawSavePopup()
    {
        ImGui.SetNextWindowSizeConstraints(
            new Vector2(SavePopupWidth, 0f),
            new Vector2(SavePopupWidth, float.MaxValue));
        if (!ImGui.BeginPopup(SavePopupId))
        {
            return;
        }

        ImGui.TextColored(BoutiqueTheme.GetHeadingColor(configuration.Current), "SAVE CURRENT DESIGN");
        ImGui.TextColored(
            BoutiqueTheme.MutedText,
            $"{sessionController.ChangedSlotCount} changed slots will be saved.");
        ImGui.SetNextItemWidth(-1f);
        ImGui.InputTextWithHint(
            "##SaveLoadoutName",
            "Design name",
            ref saveName,
            LoadoutSchema.MaximumNameLength + 1);
        BoutiqueTheme.DrawLastGoldControlFrame(configuration.Current);
        ImGui.SetNextItemWidth(-1f);
        ImGui.InputTextWithHint(
            "##SaveLoadoutNotes",
            "Notes (optional)",
            ref saveNotes,
            LoadoutSchema.MaximumNotesLength + 1);
        BoutiqueTheme.DrawLastGoldControlFrame(configuration.Current);

        var canSave = sessionController.ChangedSlotCount > 0
            && !string.IsNullOrWhiteSpace(saveName);
        ImGui.BeginDisabled(!canSave);
        if (BoutiqueTheme.FramedButton(configuration.Current, "Save Design"))
        {
            var result = library.Create(
                saveName,
                sessionController.PreviewEquipmentBySlot.Values,
                saveNotes);
            if (result.IsSuccess && result.Value is not null)
            {
                selectedLoadoutId = result.Value.Id;
                activeLoadoutId = result.Value.Id;
                eorzeaCollectionActive = false;
                Report(result, $"Saved design: {result.Value.Name}");
                ImGui.CloseCurrentPopup();
            }
            else
            {
                Report(result, "The current design could not be saved.");
            }
        }

        ImGui.EndDisabled();
        ImGui.SameLine();
        if (BoutiqueTheme.FramedButton(configuration.Current, "Cancel"))
        {
            ImGui.CloseCurrentPopup();
        }

        ImGui.EndPopup();
    }

    private void DrawDetails(BoutiqueLoadout selected)
    {
        using (fonts.Body.Push())
        {
            ImGui.TextUnformatted(selected.Name);
        }
        BoutiqueTheme.DrawProfileHorizontalRule(
            configuration.Current,
            BoutiqueTheme.GetDecorativeRuleColor(configuration.Current),
            2f);
        using (fonts.Numeric.Push())
        {
            ImGui.TextColored(
                BoutiqueTheme.MutedText,
                $"Created | {selected.CreatedAtUtc.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture)}"
                    + " << | >> "
                    + $"Updated | {selected.UpdatedAtUtc.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture)}");
        }
        if (!string.IsNullOrWhiteSpace(selected.Notes))
        {
            ImGui.Spacing();
            DrawNotesField(selected.Notes);
        }

        ImGui.Spacing();
        BoutiqueTheme.DrawProfileHorizontalRule(
            configuration.Current,
            BoutiqueTheme.GetHorizontalRuleColor(configuration.Current));
        using (fonts.SectionHeading.Push())
        {
            ImGui.TextColored(BoutiqueTheme.MutedText, "DESIGN ITEM LIST");
        }
        BoutiqueTheme.DrawProfileHorizontalRule(
            configuration.Current,
            BoutiqueTheme.GetHorizontalRuleColor(configuration.Current));
        ImGui.Spacing();
        DrawReadOnlyDesignCards(selected);
    }

    private static void DrawNotesField(string notes)
    {
        var label = $"NOTES | {notes}";
        const float padding = 8f;
        var wrapWidth = Math.Max(1f, ImGui.GetContentRegionAvail().X - (padding * 2f));
        var textHeight = ImGui.CalcTextSize(label, false, wrapWidth).Y;
        ImGui.PushStyleColor(ImGuiCol.ChildBg, Vector4.Zero);
        ImGui.PushStyleColor(ImGuiCol.Border, BoutiqueTheme.LightGreyBorder);
        ImGui.PushStyleVar(ImGuiStyleVar.ChildBorderSize, 1f);
        ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, BoutiqueTheme.ControlRounding / 2f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(padding, padding / 2f));
        if (ImGui.BeginChild(
                "##SavedDesignNotes",
                new Vector2(0f, textHeight + padding),
                true,
                ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
        {
            ImGui.TextWrapped(label);
        }

        ImGui.EndChild();
        ImGui.PopStyleVar(3);
        ImGui.PopStyleColor(2);
    }

    private void DrawReadOnlyDesignCards(BoutiqueLoadout selected)
    {
        var equipmentBySlot = selected.Equipment.ToDictionary(equipment => equipment.Slot);
        const int columns = 6;
        const int rows = 2;
        var tablePadding = ImGui.GetStyle().CellPadding.Y * rows * 2f;
        var cardHeight = Math.Max(
            DesignCardMinimumHeight,
            (ImGui.GetContentRegionAvail().Y - tablePadding) / rows);
        if (!ImGui.BeginTable(
                "##SavedDesignCards",
                columns,
                ImGuiTableFlags.SizingStretchSame
                | ImGuiTableFlags.NoPadInnerX
                | ImGuiTableFlags.NoSavedSettings))
        {
            return;
        }

        for (var index = 0; index < DesignCardLayout.Length; index++)
        {
            if (index % columns == 0)
            {
                ImGui.TableNextRow(ImGuiTableRowFlags.None, cardHeight);
            }

            ImGui.TableSetColumnIndex(index % columns);
            var definition = DesignCardLayout[index];
            equipmentBySlot.TryGetValue(definition.Slot, out var equipment);
            DrawReadOnlyDesignCard(definition, equipment, cardHeight);
        }

        ImGui.EndTable();
    }

    private void DrawReadOnlyDesignCard(
        EquipmentSlotDefinition definition,
        LoadoutEquipmentState? equipment,
        float height)
    {
        ImGui.PushStyleColor(
            ImGuiCol.ChildBg,
            BoutiqueTheme.GetDesignCardBackground(configuration.Current));
        ImGui.PushStyleColor(
            ImGuiCol.Border,
            BoutiqueTheme.GetDesignCardBorder(configuration.Current));
        ImGui.PushStyleVar(
            ImGuiStyleVar.ChildBorderSize,
            BoutiqueTheme.GetCardBorderThickness(configuration.Current));
        ImGui.PushStyleVar(
            ImGuiStyleVar.ChildRounding,
            BoutiqueTheme.GetCardRounding(configuration.Current));
        var visible = ImGui.BeginChild(
            $"##SavedDesignCard{definition.Slot}",
            new Vector2(0f, height),
            true,
            ImGuiWindowFlags.NoScrollbar
            | ImGuiWindowFlags.NoScrollWithMouse);
        if (visible)
        {
            using (fonts.SectionHeading.Push())
            {
                ImGui.TextUnformatted(definition.DisplayName);
            }

            ImGui.PushStyleColor(ImGuiCol.Text, GetSavedItemColor(equipment, definition.Slot));
            using (fonts.Body.Push())
            {
                ImGui.TextWrapped(equipment?.DisplayName ?? "Not saved");
            }

            ImGui.PopStyleColor();
            BoutiqueTheme.DrawProfileHorizontalRule(
                configuration.Current,
                BoutiqueTheme.GetDecorativeRuleColor(configuration.Current),
                1.5f);

            var iconRegionHeight = Math.Max(
                1f,
                ImGui.GetContentRegionAvail().Y
                    - DesignDyeFieldHeight
                    - ImGui.GetStyle().ItemSpacing.Y);
            DrawReadOnlyDesignIcon(definition.Slot, equipment, iconRegionHeight);
            DrawReadOnlyDesignDyes(definition.Slot, equipment);
        }

        ImGui.EndChild();
        ImGui.PopStyleVar(2);
        ImGui.PopStyleColor(2);
    }

    private void DrawReadOnlyDesignIcon(
        EquipmentSlot slot,
        LoadoutEquipmentState? equipment,
        float height)
    {
        if (ImGui.BeginChild(
                $"##SavedDesignIcon{slot}",
                new Vector2(0f, height),
                false,
                ImGuiWindowFlags.NoScrollbar
                | ImGuiWindowFlags.NoScrollWithMouse))
        {
            var available = ImGui.GetContentRegionAvail();
            var iconSize = Math.Max(1f, Math.Min(available.X - 5f, available.Y - 5f));
            var cursor = ImGui.GetCursorPos();
            ImGui.SetCursorPos(new Vector2(
                cursor.X + Math.Max((available.X - iconSize) / 2f, 0f),
                cursor.Y + Math.Max((available.Y - iconSize) / 2f, 0f)));
            var iconId = equipment?.IconId ?? 0;
            if (iconId > 0
                && textureProvider.TryGetFromGameIcon(
                    new GameIconLookup(iconId, itemHq: false, hiRes: true),
                    out var sharedTexture)
                && sharedTexture.GetWrapOrDefault() is { } texture)
            {
                ImGui.Image(texture.Handle, new Vector2(iconSize));
                var iconMinimum = ImGui.GetItemRectMin();
                var iconMaximum = ImGui.GetItemRectMax();
                if (ImGui.IsItemHovered())
                {
                    BoutiqueTheme.DrawIconInteractionGlow(
                        ImGui.GetWindowDrawList(),
                        iconMinimum,
                        iconMaximum,
                        BoutiqueTheme.GetSelectedItemHighlightColor(configuration.Current),
                        outerOnly: BoutiqueTheme.IsCrystariumProfile(configuration.Current));
                    if (BoutiqueTheme.ShouldShowItemTooltip(configuration.Current)
                        && equipment?.Appearance.SourceItemId is { } sourceItemId
                        && itemRepository.TryGetItem(sourceItemId, slot, out var item))
                    {
                        ItemTooltipRenderer.Draw(
                            item,
                            itemRepository.GetItemsSharingModel(item.ModelFamilyKey, slot),
                            iconMinimum,
                            iconSize,
                            configuration.Current,
                            itemRarityColors,
                            fonts);
                    }
                }
            }
            else
            {
                var marker = ImGui.CalcTextSize("--");
                ImGui.SetCursorPos(new Vector2(
                    cursor.X + Math.Max((available.X - marker.X) / 2f, 0f),
                    cursor.Y + Math.Max((available.Y - marker.Y) / 2f, 0f)));
                ImGui.TextColored(BoutiqueTheme.MutedText, "--");
            }
        }

        ImGui.EndChild();
    }

    private void DrawReadOnlyDesignDyes(EquipmentSlot slot, LoadoutEquipmentState? equipment)
    {
        var spacing = ImGui.GetStyle().ItemSpacing.X;
        var width = Math.Max(1f, (ImGui.GetContentRegionAvail().X - spacing) / 2f);
        DrawReadOnlyDyeField(slot, equipment, 0, width);
        ImGui.SameLine(0f, spacing);
        DrawReadOnlyDyeField(slot, equipment, 1, width);
    }

    private void DrawReadOnlyDyeField(
        EquipmentSlot slot,
        LoadoutEquipmentState? equipment,
        byte channel,
        float width)
    {
        var supported = equipment is not null && channel < equipment.DyeChannelCount;
        var stainId = supported && equipment!.Appearance.Stains.Length > channel
            ? equipment.Appearance.Stains[channel]
            : StainId.None;
        var stain = supported && stains.TryGet(stainId, out var definition)
            ? definition
            : null;
        var label = !supported
            ? "—"
            : stain?.Name ?? "None";
        var background = stain is null
            ? BoutiqueTheme.WarmCharcoal
            : new Vector4(
                stain.Color.Red / 255f,
                stain.Color.Green / 255f,
                stain.Color.Blue / 255f,
                1f);
        var textColor = !supported
            ? BoutiqueTheme.MarketUnavailable
            : GetContrastingTextColor(background);

        ImGui.PushStyleColor(ImGuiCol.ChildBg, background);
        ImGui.PushStyleColor(ImGuiCol.Border, BoutiqueTheme.AntiqueGoldShadow);
        ImGui.PushStyleVar(ImGuiStyleVar.ChildBorderSize, 1f);
        ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, BoutiqueTheme.ControlRounding / 2f);
        if (ImGui.BeginChild(
                $"##SavedDesignDye{slot}{channel}",
                new Vector2(width, DesignDyeFieldHeight),
                true,
                ImGuiWindowFlags.NoScrollbar
                | ImGuiWindowFlags.NoScrollWithMouse
                | ImGuiWindowFlags.NoInputs))
        {
            BoutiqueScaleStyle.ApplyRelativeWindowFontScale();
            var available = ImGui.GetContentRegionAvail();
            var textWidth = ImGui.CalcTextSize(label).X;
            if (textWidth > available.X)
            {
                BoutiqueScaleStyle.ApplyRelativeWindowFontScale(Math.Clamp(
                    available.X / textWidth,
                    0.28f,
                    1f));
            }

            var textSize = ImGui.CalcTextSize(label);
            ImGui.SetCursorPos(new Vector2(
                ImGui.GetCursorPosX() + Math.Max((available.X - textSize.X) / 2f, 0f),
                ImGui.GetCursorPosY() + Math.Max((available.Y - textSize.Y) / 2f, 0f)));
            ImGui.TextColored(textColor, label);
        }

        ImGui.EndChild();
        ImGui.PopStyleVar(2);
        ImGui.PopStyleColor(2);
    }

    private Vector4 GetSavedItemColor(LoadoutEquipmentState? equipment, EquipmentSlot slot)
        => equipment?.Appearance.SourceItemId is { } itemId
            && itemRepository.TryGetItem(itemId, slot, out var item)
                ? itemRarityColors.GetColor(item)
                : BoutiqueTheme.MutedText;

    private static Vector4 GetContrastingTextColor(Vector4 color)
    {
        var luminance = (0.2126f * color.X) + (0.7152f * color.Y) + (0.0722f * color.Z);
        return luminance > 0.52f
            ? new Vector4(0.05f, 0.05f, 0.05f, 1f)
            : new Vector4(0.96f, 0.96f, 0.96f, 1f);
    }

    private void DrawDesignToolbar(BoutiqueLoadout? selected)
    {
        using var toolbarFont = fonts.Navigation.Push();
        var openSavePopup = false;
        var openImportPopup = false;
        if (!ImGui.BeginTable(
                "##DesignToolbar",
                4,
                ImGuiTableFlags.SizingStretchSame | ImGuiTableFlags.NoSavedSettings))
        {
            return;
        }

        var editColor = BoutiqueTheme.IsCrystariumProfile(configuration.Current)
            ? BoutiqueTheme.GetBoutiqueButtonColor(configuration.Current)
            : BoutiqueTheme.GetNeutralActionButtonColor(configuration.Current);
        var editHovered = BoutiqueTheme.GetHoveredControlColor(editColor);
        var editActive = BoutiqueTheme.GetActiveControlColor(editColor);
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.BeginDisabled(sessionController.ChangedSlotCount == 0 || loadError is not null);
        if (DrawDesignActionButton(
                "Save Design",
                BoutiqueTheme.Eucalyptus,
                BoutiqueTheme.EucalyptusHovered,
                BoutiqueTheme.EucalyptusActive))
        {
            saveName = SuggestNewName();
            saveNotes = string.Empty;
            openSavePopup = true;
        }

        ImGui.EndDisabled();
        ImGui.TableNextColumn();
        ImGui.BeginDisabled(selected is null);
        if (DrawDesignActionButton(
                "Rename",
                editColor,
                editHovered,
                editActive)
            && selected is not null)
        {
            editMode = LoadoutEditMode.Rename;
            editName = selected.Name;
            confirmation = LoadoutConfirmation.None;
        }

        ImGui.TableNextColumn();
        if (DrawDesignActionButton(
                "Duplicate",
                editColor,
                editHovered,
                editActive)
            && selected is not null)
        {
            editMode = LoadoutEditMode.Duplicate;
            editName = library.SuggestDuplicateName(selected);
            confirmation = LoadoutConfirmation.None;
        }

        ImGui.EndDisabled();
        ImGui.TableNextColumn();
        ImGui.BeginDisabled(loadError is not null);
        var importClicked = DrawDesignActionButton(
                ImportDesignLabel,
                BoutiqueTheme.Terracotta,
                BoutiqueTheme.TerracottaHovered,
                BoutiqueTheme.TerracottaActive);
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
        {
            ImGui.SetTooltip(ImportDesignTooltip);
        }

        if (importClicked)
        {
            importPayload = ImGui.GetClipboardText() ?? string.Empty;
            openImportPopup = true;
        }

        ImGui.EndDisabled();
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        var canLoad = selected is not null && sessionController.Session.State == BoutiqueSessionState.Active;
        ImGui.BeginDisabled(!canLoad);
        if (DrawDesignActionButton(
                "Load",
                BoutiqueTheme.SteelBlue,
                BoutiqueTheme.SteelBlueHovered,
                BoutiqueTheme.SteelBlueActive)
            && selected is not null)
        {
            var result = sessionController.ApplyLoadout(selected);
            if (result.IsSuccess)
            {
                browser.ClearAppearanceSelection();
                activeLoadoutId = selected.Id;
                eorzeaCollectionActive = false;
            }

            Report(result, result.IsSuccess
                ? $"Loaded: {selected.Name}"
                : "The selected design could not be applied.");
        }

        ImGui.EndDisabled();
        ImGui.TableNextColumn();
        ImGui.BeginDisabled(selected is null || sessionController.ChangedSlotCount == 0);
        if (DrawDesignActionButton(
                "Overwrite",
                editColor,
                editHovered,
                editActive))
        {
            confirmation = LoadoutConfirmation.Overwrite;
            editMode = LoadoutEditMode.None;
        }

        ImGui.EndDisabled();
        ImGui.TableNextColumn();
        ImGui.BeginDisabled(selected is null);
        if (DrawDesignActionButton(
                "Delete",
                BoutiqueTheme.ClearRed,
                BoutiqueTheme.ClearRedHovered,
                BoutiqueTheme.ClearRedActive))
        {
            confirmation = LoadoutConfirmation.Delete;
            editMode = LoadoutEditMode.None;
        }

        ImGui.EndDisabled();
        ImGui.TableNextColumn();
        ImGui.BeginDisabled(selected is null);
        var exportClicked = DrawDesignActionButton(
                ExportDesignLabel,
                BoutiqueTheme.Terracotta,
                BoutiqueTheme.TerracottaHovered,
                BoutiqueTheme.TerracottaActive);
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
        {
            ImGui.SetTooltip(
                "Clicking this button adds the Design string to your clipboard. Paste it to share.");
        }

        if (exportClicked && selected is not null)
        {
            ImGui.SetClipboardText(LoadoutExchangeCodec.Encode(selected));
            toolbarNotice = $"Copied {selected.Name} to the clipboard. Paste it to share.";
            reportFeedback(new LoadoutFeedback(true, toolbarNotice, null));
        }

        ImGui.EndDisabled();
        ImGui.EndTable();
        if (openSavePopup)
        {
            ImGui.OpenPopup(SavePopupId);
        }

        if (openImportPopup)
        {
            ImGui.OpenPopup(ImportPopupId);
        }
    }

    private bool DrawDesignActionButton(
        string label,
        Vector4 color,
        Vector4 hovered,
        Vector4 active)
    {
        ImGui.PushStyleColor(ImGuiCol.Button, color);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, hovered);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, active);
        ImGui.PushStyleColor(ImGuiCol.Text, Vector4.One);
        var clicked = BoutiqueTheme.FramedButton(
            configuration.Current,
            label,
            new Vector2(-1f, 0f));
        ImGui.PopStyleColor(4);
        return clicked;
    }

    private void DrawImportPopup()
    {
        ImGui.SetNextWindowSizeConstraints(
            new Vector2(680f, 0f),
            new Vector2(680f, float.MaxValue));
        if (!ImGui.BeginPopup(ImportPopupId))
        {
            return;
        }

        ImGui.TextColored(BoutiqueTheme.GetHeadingColor(configuration.Current), "IMPORT SHARED DESIGN");
        ImGui.TextWrapped("Paste a TCB-DESIGN-1 string. Imported designs are validated and saved as a new local design.");
        ImGui.InputTextMultiline(
            "##ImportedDesignPayload",
            ref importPayload,
            131_073,
            new Vector2(-1f, 220f));
        BoutiqueTheme.DrawLastGoldControlFrame(configuration.Current);
        if (BoutiqueTheme.FramedButton(configuration.Current, "Paste Clipboard"))
        {
            importPayload = ImGui.GetClipboardText() ?? string.Empty;
        }

        ImGui.SameLine();
        ImGui.BeginDisabled(string.IsNullOrWhiteSpace(importPayload));
        if (BoutiqueTheme.FramedButton(configuration.Current, "Import Design"))
        {
            var decodeResult = LoadoutExchangeCodec.Decode(importPayload);
            if (decodeResult.IsSuccess && decodeResult.Value is not null)
            {
                var importResult = library.Import(decodeResult.Value);
                if (importResult.IsSuccess && importResult.Value is not null)
                {
                    selectedLoadoutId = importResult.Value.Id;
                    Report(importResult, $"Imported design: {importResult.Value.Name}");
                    ImGui.CloseCurrentPopup();
                }
                else
                {
                    Report(importResult, "The shared design could not be imported.");
                }
            }
            else
            {
                reportFeedback(new LoadoutFeedback(
                    false,
                    decodeResult.Error?.Message ?? "The shared design could not be read.",
                    decodeResult.Error));
            }
        }

        ImGui.EndDisabled();
        ImGui.SameLine();
        if (BoutiqueTheme.FramedButton(configuration.Current, "Cancel"))
        {
            ImGui.CloseCurrentPopup();
        }

        ImGui.EndPopup();
    }

    private void DrawInlineEdit(BoutiqueLoadout selected)
    {
        if (editMode == LoadoutEditMode.None)
        {
            return;
        }

        ImGui.Spacing();
        ImGui.TextColored(
            BoutiqueTheme.MutedText,
            editMode == LoadoutEditMode.Rename ? "RENAME DESIGN" : "DUPLICATE DESIGN");
        ImGui.SetNextItemWidth(-1f);
        ImGui.InputText(
            "##EditLoadoutName",
            ref editName,
            LoadoutSchema.MaximumNameLength + 1);
        BoutiqueTheme.DrawLastGoldControlFrame(configuration.Current);
        ImGui.BeginDisabled(string.IsNullOrWhiteSpace(editName));
        var neutralColor = BoutiqueTheme.GetNeutralActionButtonColor(configuration.Current);
        if (DrawDesignActionButton(
                "Confirm name",
                neutralColor,
                BoutiqueTheme.GetHoveredControlColor(neutralColor),
                BoutiqueTheme.GetActiveControlColor(neutralColor)))
        {
            var result = editMode == LoadoutEditMode.Rename
                ? library.Rename(selected.Id, editName)
                : library.Duplicate(selected.Id, editName);
            if (result.IsSuccess && result.Value is not null)
            {
                selectedLoadoutId = result.Value.Id;
                Report(
                    result,
                    editMode == LoadoutEditMode.Rename
                        ? $"Renamed design: {result.Value.Name}"
                        : $"Duplicated design: {result.Value.Name}");
                editMode = LoadoutEditMode.None;
            }
            else
            {
                Report(result, "The design name change could not be saved.");
            }
        }

        ImGui.EndDisabled();
        if (DrawDesignActionButton(
                "Cancel edit",
                neutralColor,
                BoutiqueTheme.GetHoveredControlColor(neutralColor),
                BoutiqueTheme.GetActiveControlColor(neutralColor)))
        {
            editMode = LoadoutEditMode.None;
        }
    }

    private void DrawConfirmation(BoutiqueLoadout selected)
    {
        if (confirmation == LoadoutConfirmation.None)
        {
            return;
        }

        ImGui.Spacing();
        ImGui.Separator();
        if (confirmation == LoadoutConfirmation.Overwrite)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, BoutiqueTheme.Unavailable);
            ImGui.TextWrapped(
                $"Replace \"{selected.Name}\" with the current {sessionController.ChangedSlotCount}-slot preview?");
            ImGui.PopStyleColor();
            if (BoutiqueTheme.FramedButton(
                    configuration.Current,
                    "Confirm overwrite",
                    new Vector2(-1f, 0f)))
            {
                var result = library.Overwrite(
                    selected.Id,
                    sessionController.PreviewEquipmentBySlot.Values);
                Report(result, result.IsSuccess
                    ? $"Overwrote design: {selected.Name}"
                    : "The design could not be overwritten.");
                if (result.IsSuccess)
                {
                    confirmation = LoadoutConfirmation.None;
                }
            }
        }
        else
        {
            ImGui.PushStyleColor(ImGuiCol.Text, BoutiqueTheme.Unavailable);
            ImGui.TextWrapped($"Delete \"{selected.Name}\" from the visible design library?");
            ImGui.PopStyleColor();
            if (DrawDesignActionButton(
                    "Confirm delete",
                    BoutiqueTheme.ClearRed,
                    BoutiqueTheme.ClearRedHovered,
                    BoutiqueTheme.ClearRedActive))
            {
                var result = library.Delete(selected.Id);
                Report(result, result.IsSuccess
                    ? $"Deleted design: {selected.Name}"
                    : "The design could not be deleted.");
                if (result.IsSuccess)
                {
                    selectedLoadoutId = library.Loadouts.IsEmpty ? null : library.Loadouts[0].Id;
                    confirmation = LoadoutConfirmation.None;
                }
            }
        }

        if (DrawDesignActionButton(
                "Cancel action",
                BoutiqueTheme.SteelBlue,
                BoutiqueTheme.SteelBlueHovered,
                BoutiqueTheme.SteelBlueActive))
        {
            confirmation = LoadoutConfirmation.None;
        }
    }

    private void EnsureSelection()
    {
        if (selectedLoadoutId is null
            || !library.TryGet(selectedLoadoutId.Value, out _))
        {
            selectedLoadoutId = library.Loadouts.IsEmpty ? null : library.Loadouts[0].Id;
        }
    }

    private BoutiqueLoadout? GetSelected()
        => selectedLoadoutId.HasValue
            && library.TryGet(selectedLoadoutId.Value, out var loadout)
                ? loadout
                : null;

    private string SuggestNewName()
    {
        var number = Count + 1;
        var candidate = $"Boutique Design {number}";
        while (library.Loadouts.Any(loadout => string.Equals(
                   loadout.Name,
                   candidate,
                   StringComparison.OrdinalIgnoreCase)))
        {
            candidate = $"Boutique Design {++number}";
        }

        return candidate;
    }

    private static EquipmentSlotDefinition FindDefinition(EquipmentSlot slot)
        => EquipmentSlotDefinitions.All.Single(definition => definition.Slot == slot);

    private void Report(Result result, string message)
        => reportFeedback(new LoadoutFeedback(
            result.IsSuccess,
            result.IsSuccess ? message : result.Error?.Message ?? message,
            result.Error));

    private void Report(Result<BoutiqueLoadout> result, string message)
        => reportFeedback(new LoadoutFeedback(
            result.IsSuccess,
            result.IsSuccess ? message : result.Error?.Message ?? message,
            result.Error));

    private enum LoadoutEditMode
    {
        None,
        Rename,
        Duplicate,
    }

    private enum LoadoutConfirmation
    {
        None,
        Overwrite,
        Delete,
    }
}
