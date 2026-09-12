using CrystariumBoutique.Configuration;
using CrystariumBoutique.Core.Catalog;
using CrystariumBoutique.Core.Dyes;
using CrystariumBoutique.Core.Errors;
using CrystariumBoutique.Core.Integrations;
using CrystariumBoutique.Core.Loadouts;
using CrystariumBoutique.Core.Sessions;
using CrystariumBoutique.Data;
using CrystariumBoutique.Integrations;
using CrystariumBoutique.Integrations.GlamourerIpc;
using CrystariumBoutique.Integrations.PenumbraIpc;
using CrystariumBoutique.Ui;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace CrystariumBoutique;

public sealed class Plugin : IDalamudPlugin
{
    private static readonly string[] CommandNames = ["/boutique", "/tcb", "/cb"];

    private readonly IDalamudPluginInterface pluginInterface;
    private readonly ICommandManager commandManager;
    private readonly IPluginLog pluginLog;
    private readonly IClientState clientState;
    private readonly IPlayerState playerState;
    private readonly ICondition condition;
    private readonly ConfigurationStore configuration;
    private readonly WindowSystem windowSystem = new("CrystariumBoutique");
    private readonly IContextAwareAppearanceService appearanceService;
    private readonly IPenumbraService penumbraService;
    private readonly PenumbraRedrawService? penumbraServiceLifetime;
    private readonly IDependencyAvailabilityRefresher? appearanceDependency;
    private readonly IDependencyAvailabilityRefresher? penumbraDependency;
    private readonly DependencyRecoveryCoordinator dependencyRecovery;
    private readonly EorzeaCollectionImportService eorzeaCollectionImportService;
    private readonly BoutiqueSessionController sessionController;
    private readonly EquipmentBrowserController browser;
    private readonly BoutiqueFontSet fonts;
    private readonly DesignReferenceWindow designReferenceWindow;
    private readonly WelcomeGuideWindow welcomeGuideWindow;
    private readonly BoutiqueWindow boutiqueWindow;
    private readonly BoutiqueWardrobeWindow boutiqueWardrobeWindow;
    private readonly WardrobeAutoSyncCoordinator? wardrobeAutoSync;
    private bool lastGposeState;
    private bool gposeHandoffPending;
    private bool gposeHandoffFailureReported;
    private bool lastAppearanceServiceAvailable;
    private bool combatLockActive;
    private bool disposed;

    public Plugin(
        IDalamudPluginInterface pluginInterface,
        ICommandManager commandManager,
        IPluginLog pluginLog,
        IDataManager dataManager,
        ITextureProvider textureProvider,
        IClientState clientState,
        IPlayerState playerState,
        IObjectTable objectTable,
        ICondition condition,
        ITargetManager targetManager,
        IGameGui gameGui)
    {
        this.pluginInterface = pluginInterface ?? throw new ArgumentNullException(nameof(pluginInterface));
        this.commandManager = commandManager ?? throw new ArgumentNullException(nameof(commandManager));
        this.pluginLog = pluginLog ?? throw new ArgumentNullException(nameof(pluginLog));
        this.clientState = clientState ?? throw new ArgumentNullException(nameof(clientState));
        this.playerState = playerState ?? throw new ArgumentNullException(nameof(playerState));
        this.condition = condition ?? throw new ArgumentNullException(nameof(condition));

        lastGposeState = clientState.IsGPosing;
        pluginInterface.UiBuilder.DisableGposeUiHide = true;
        try
        {
            var redrawService = IntegrationAdapterFactory.Create(
                () => new PenumbraIpcClient(pluginInterface),
                ipcClient => new PenumbraRedrawService(ipcClient, pluginLog));
            penumbraService = redrawService;
            penumbraServiceLifetime = redrawService;
            penumbraDependency = redrawService;
        }
        catch (Exception exception)
        {
            pluginLog.Warning(
                exception,
                "Boutique could not initialize its optional Penumbra redraw adapter.");
            penumbraService = new UnavailablePenumbraService();
        }

        configuration = new ConfigurationStore(pluginInterface);
        var assetDirectory = pluginInterface.AssemblyLocation.DirectoryName ?? AppContext.BaseDirectory;
        var acquisitionSupplementPath = Path.Combine(
            assetDirectory,
            "data",
            "item-acquisition-supplement.json");
        var availabilitySupplementPath = Path.Combine(
            assetDirectory,
            "data",
            "item-availability-supplement.json");
        var catalogResult = new LuminaItemCatalogLoader(
            dataManager,
            acquisitionSupplementPath,
            availabilitySupplementPath).Load();
        var catalog = catalogResult.Value?.Catalog ?? EquipmentCatalog.Empty;
        var catalogError = catalogResult.Error?.ToString();
        try
        {
            var glamourerService = IntegrationAdapterFactory.Create(
                () => new GlamourerIpcClient(pluginInterface),
                ipcClient => new GlamourerAppearanceService(
                    ipcClient,
                    penumbraService,
                    pluginLog,
                    clientState,
                    targetManager,
                    playerState,
                    objectTable,
                    catalog));
            appearanceService = glamourerService;
            appearanceDependency = glamourerService;
        }
        catch (Exception exception)
        {
            pluginLog.Warning(
                exception,
                "Boutique could not initialize its Glamourer IPC adapter and will remain browse-only.");
            appearanceService = new UnavailableAppearanceService();
        }
        dependencyRecovery = new DependencyRecoveryCoordinator(
            appearanceDependency,
            penumbraDependency);
        var itemRarityColors = new ItemRarityColorResolver(dataManager);
        var stainCatalogResult = new LuminaStainCatalogLoader(dataManager).Load();
        var stainCatalog = stainCatalogResult.Value?.Catalog ?? StainCatalog.Empty;
        var stainCatalogError = stainCatalogResult.Error?.ToString();
        eorzeaCollectionImportService = new EorzeaCollectionImportService(catalog, stainCatalog);
        var loadoutLibrary = new LoadoutLibrary(new JsonLoadoutStore(
            Path.Combine(pluginInterface.ConfigDirectory.FullName, "loadouts")));
        var loadoutInitializeResult = loadoutLibrary.Initialize();
        var loadoutError = loadoutInitializeResult.Error?.ToString();
        browser = new EquipmentBrowserController(
            catalog,
            configuration.Current.AppearanceGridColumns * configuration.Current.AppearanceGridRows);
        RefreshActiveClassJob();
        sessionController = new BoutiqueSessionController(appearanceService, itemRepository: catalog);
        lastAppearanceServiceAvailable = appearanceService.IsAvailable;
        fonts = new BoutiqueFontSet(pluginInterface);
        CrystariumGoldFrameRenderer.Initialize(textureProvider, assetDirectory);
        designReferenceWindow = new DesignReferenceWindow(
            textureProvider,
            gameGui,
            catalog,
            itemRarityColors,
            configuration,
            fonts,
            assetDirectory);
        welcomeGuideWindow = new WelcomeGuideWindow(configuration, fonts);
        boutiqueWindow = new BoutiqueWindow(
            sessionController,
            appearanceService,
            penumbraService,
            configuration,
            catalog,
            browser,
            textureProvider,
            itemRarityColors,
            playerState,
            catalogError,
            stainCatalog,
            loadoutLibrary,
            loadoutError,
            eorzeaCollectionImportService,
            fonts,
            assetDirectory,
            designReferenceWindow.Show,
            ToggleWardrobe,
            welcomeGuideWindow.Show,
            CloseSessionFromWindow);
        boutiqueWardrobeWindow = new BoutiqueWardrobeWindow(
            stainCatalog,
            catalog,
            sessionController,
            browser,
            textureProvider,
            configuration,
            itemRarityColors,
            fonts,
            assetDirectory,
            boutiqueWindow.HandleEquipmentCardFeedback);

        windowSystem.AddWindow(boutiqueWindow);
        windowSystem.AddWindow(boutiqueWardrobeWindow);
        windowSystem.AddWindow(designReferenceWindow);
        windowSystem.AddWindow(welcomeGuideWindow);
        if (appearanceService is ILocalPlayerGearsetFinalizationSource gearsetFinalizationSource)
        {
            wardrobeAutoSync = new WardrobeAutoSyncCoordinator(
                gearsetFinalizationSource,
                () => configuration.Current.AutomaticallySyncWardrobe
                    && boutiqueWindow.IsOpen
                    && sessionController.Session.State == BoutiqueSessionState.Active
                    && appearanceService.IsAvailable
                    && !clientState.IsGPosing,
                () => sessionController.Session.IsDirty,
                sessionController.RelinquishPreviewForSynchronization,
                CaptureSynchronizedWardrobeState,
                RefreshSynchronizedWardrobeState);
        }

        foreach (var commandName in CommandNames)
        {
            commandManager.AddHandler(commandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "Open The Crystarium Boutique.",
                ShowInHelp = true,
            });
        }
        pluginInterface.UiBuilder.Draw += DrawUi;
        pluginInterface.UiBuilder.OpenMainUi += OpenMainUi;
        pluginInterface.UiBuilder.OpenConfigUi += OpenSettingsUi;
        clientState.ClassJobChanged += OnClassJobChanged;

        if (catalogResult.Value is { } load)
        {
            pluginLog.Information(
                "Loaded Boutique catalog from {ExaminedRows} Item rows: {SourceItems} equipment items, {Appearances} indexed appearances, {FittingShopItemIds} local FittingShop item IDs, {MogStationItems} matching equipment items, and {AcquisitionSources} local acquisition sources across {AcquisitionItems} items in {ElapsedMilliseconds} ms.",
                load.ExaminedRows,
                load.Catalog.SourceItemCount,
                load.Catalog.AppearanceCount,
                load.FittingShopItemIds,
                load.Catalog.MogStationSourceItemCount,
                load.AcquisitionSources,
                load.AcquisitionIndexedItems,
                load.Duration.TotalMilliseconds);
            if (load.FittingShopWarning is not null)
            {
                pluginLog.Warning(
                    "The local FittingShop sheet could not be indexed; Mogstation grouping is unavailable: {Warning}",
                    load.FittingShopWarning);
            }

            foreach (var warning in load.AcquisitionWarnings)
            {
                pluginLog.Warning(
                    "A local acquisition source could not be indexed and will be omitted from tooltips: {Warning}",
                    warning);
            }

            if (load.SupplementalAcquisitionSources > 0)
            {
                pluginLog.Information(
                    "Loaded {SupplementalSources} verified local duty acquisition sources for game build {GameBuild}.",
                    load.SupplementalAcquisitionSources,
                    load.SupplementalGameBuild);
            }
        }
        else
        {
            pluginLog.Error("Boutique catalog unavailable: {Error}", catalogError ?? "Unknown data load error.");
        }

        if (stainCatalogResult.Value is { } stainLoad)
        {
            pluginLog.Information(
                "Loaded Boutique dye catalog from {ExaminedRows} Stain rows: {Stains} named dyes in {ElapsedMilliseconds} ms.",
                stainLoad.ExaminedRows,
                stainLoad.Catalog.Count,
                stainLoad.Duration.TotalMilliseconds);
        }
        else
        {
            pluginLog.Error(
                "Boutique dye catalog unavailable: {Error}",
                stainCatalogError ?? "Unknown dye data load error.");
        }

        if (loadoutInitializeResult.IsSuccess && loadoutLibrary.LastRead is { } loadoutRead)
        {
            pluginLog.Information(
                "Loaded {Loadouts} local Boutique loadouts; migrated {Migrated} and quarantined {Quarantined} records.",
                loadoutLibrary.Loadouts.Length,
                loadoutRead.MigratedCount,
                loadoutRead.QuarantinedCount);
            foreach (var warning in loadoutRead.Warnings)
            {
                pluginLog.Warning("Boutique loadout warning: {Warning}", warning);
            }
        }
        else
        {
            pluginLog.Error(
                "Boutique loadout library unavailable: {Error}",
                loadoutError ?? "Unknown local loadout error.");
        }

        pluginLog.Information(
            "Crystarium Boutique {Version} initialized.",
            BoutiqueVersion.SemanticVersion);
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        boutiqueWindow.IsOpen = false;
        boutiqueWardrobeWindow.IsOpen = false;
        designReferenceWindow.Reset();

        var closeResult = sessionController.Close();
        if (!closeResult.IsSuccess)
        {
            pluginLog.Error(
                "Boutique session cleanup failed: {Error}",
                closeResult.Error?.ToString() ?? "Unknown cleanup error.");
        }

        pluginInterface.UiBuilder.OpenConfigUi -= OpenSettingsUi;
        pluginInterface.UiBuilder.OpenMainUi -= OpenMainUi;
        pluginInterface.UiBuilder.Draw -= DrawUi;
        clientState.ClassJobChanged -= OnClassJobChanged;
        foreach (var commandName in CommandNames)
        {
            commandManager.RemoveHandler(commandName);
        }
        windowSystem.RemoveAllWindows();
        CrystariumGoldFrameRenderer.Reset();
        fonts.Dispose();
        eorzeaCollectionImportService.Dispose();
        wardrobeAutoSync?.Dispose();
        appearanceService.Dispose();
        penumbraServiceLifetime?.Dispose();
        pluginLog.Information("Crystarium Boutique disposed cleanly.");
    }

    private void OnCommand(string command, string arguments)
    {
        if (boutiqueWindow.IsOpen)
        {
            boutiqueWindow.IsOpen = false;
            boutiqueWindow.ResetAfterClose();
            CloseSessionFromWindow();
            return;
        }

        OpenMainUi();
    }

    private void OpenMainUi()
    {
        if (IsCombatBlocked())
        {
            pluginLog.Information("Boutique open request ignored while the default combat safety lock is active.");
            return;
        }

        dependencyRecovery.RefreshForOpen();
        HandleAppearanceDependencyTransition();
        RefreshActiveClassJob();

        if (sessionController.Session.State == BoutiqueSessionState.RestoreFailed)
        {
            var retryResult = sessionController.Close();
            if (!retryResult.IsSuccess)
            {
                pluginLog.Error(
                    "Boutique could not reopen because original-state recovery is still failing: {Error}",
                    retryResult.Error?.ToString() ?? "Unknown recovery error.");
                return;
            }
        }

        if (sessionController.Session.State == BoutiqueSessionState.Closed)
        {
            var openResult = sessionController.Open();
            if (!openResult.IsSuccess)
            {
                pluginLog.Warning(
                    "Boutique opened in degraded mode: {Error}",
                    openResult.Error?.ToString() ?? "Unknown integration error.");
            }
        }

        boutiqueWindow.IsOpen = true;
        boutiqueWardrobeWindow.IsOpen = true;
    }

    private void OpenSettingsUi()
    {
        OpenMainUi();
        if (boutiqueWindow.IsOpen)
        {
            boutiqueWindow.RequestSettingsTab();
        }
    }

    private void DrawUi()
    {
        dependencyRecovery.ProbeIfDue();
        HandleAppearanceDependencyTransition();
        HandleGposeTransition();
        if (IsCombatBlocked())
        {
            CloseForCombatIfNeeded();
            return;
        }

        combatLockActive = false;
        HandlePendingWardrobeSynchronization();
        windowSystem.Draw();
    }

    private void HandlePendingWardrobeSynchronization()
    {
        var result = wardrobeAutoSync?.ConsumePending();
        if (result is null)
        {
            return;
        }

        if (result.IsSuccess)
        {
            pluginLog.Information(
                "Crystal Wardrobe synchronized from the finalized local-player gearset state.");
        }
        else
        {
            pluginLog.Warning(
                "Crystal Wardrobe automatic gearset synchronization failed; Reset Character remains available: {Error}",
                result.Error?.ToString() ?? "Unknown synchronization error.");
        }
    }

    private Result CaptureSynchronizedWardrobeState()
        => CompleteWardrobeSynchronization(sessionController.CaptureSynchronizedGameState());

    private Result RefreshSynchronizedWardrobeState()
        => CompleteWardrobeSynchronization(sessionController.RefreshSlots());

    private Result CompleteWardrobeSynchronization(Result result)
    {
        if (result.IsSuccess)
        {
            browser.ClearAppearanceSelection();
        }

        return result;
    }

    private void HandleAppearanceDependencyTransition()
    {
        var isAvailable = appearanceService.IsAvailable;
        if (isAvailable == lastAppearanceServiceAvailable)
        {
            return;
        }

        lastAppearanceServiceAvailable = isAvailable;
        if (!isAvailable)
        {
            if (sessionController.Session.State == BoutiqueSessionState.Active)
            {
                var interruptedResult = sessionController.MarkDependencyUnavailable(
                    "Glamourer was unloaded during an active Boutique preview session.");
                if (interruptedResult.IsSuccess)
                {
                    pluginLog.Warning(
                        "Boutique preserved its interrupted preview session and will restore game state before resuming when Glamourer becomes available again.");
                }
                else
                {
                    pluginLog.Error(
                        "Boutique could not preserve its session after Glamourer was unloaded: {Error}",
                        interruptedResult.Error?.ToString() ?? "Unknown session interruption error.");
                }
            }

            return;
        }

        if (sessionController.Session.State == BoutiqueSessionState.RestoreFailed)
        {
            var closeResult = sessionController.Close();
            if (!closeResult.IsSuccess)
            {
                pluginLog.Error(
                    "Boutique detected Glamourer again, but interrupted-session restoration still failed: {Error}",
                    closeResult.Error?.ToString() ?? "Unknown restoration error.");
                return;
            }

            if (!boutiqueWindow.IsOpen)
            {
                pluginLog.Information(
                    "Boutique restored the interrupted session after Glamourer became available again.");
                return;
            }

            var reopenResult = sessionController.Open();
            if (!reopenResult.IsSuccess)
            {
                pluginLog.Warning(
                    "Boutique restored the interrupted session, but could not open a new preview session: {Error}",
                    reopenResult.Error?.ToString() ?? "Unknown session activation error.");
            }

            return;
        }

        if (!boutiqueWindow.IsOpen
            || sessionController.Session.State != BoutiqueSessionState.DependencyUnavailable)
        {
            return;
        }

        var retryResult = sessionController.RetryOpenAfterDependencyAvailable();
        if (retryResult.IsSuccess)
        {
            pluginLog.Information(
                "Boutique restored any interrupted preview and upgraded the open browse-only window to an active session after Glamourer became available.");
        }
        else
        {
            pluginLog.Warning(
                "Boutique detected Glamourer, but could not upgrade the open browse-only window: {Error}",
                retryResult.Error?.ToString() ?? "Unknown session activation error.");
        }
    }

    private void HandleGposeTransition()
    {
        var isGposing = clientState.IsGPosing;
        if (isGposing != lastGposeState)
        {
            lastGposeState = isGposing;
            appearanceService.PrepareForContextTransition();
            if (sessionController.Session.State == BoutiqueSessionState.Active)
            {
                gposeHandoffPending = true;
                gposeHandoffFailureReported = false;
                pluginLog.Information(
                    "Boutique is handing its active appearance session {Direction} without closing or clearing preview state.",
                    isGposing ? "into GPose" : "back to normal play");
            }
        }

        if (!gposeHandoffPending)
        {
            return;
        }

        if (sessionController.Session.State != BoutiqueSessionState.Active)
        {
            gposeHandoffPending = false;
            return;
        }

        var rebindResult = appearanceService.RebindToCurrentContext();
        if (!rebindResult.IsSuccess)
        {
            if (!gposeHandoffFailureReported)
            {
                gposeHandoffFailureReported = true;
                pluginLog.Warning(
                    "Boutique is waiting to hand off its active session {Direction}: {Error}",
                    isGposing ? "to the local GPose actor" : "to the normal local player",
                    rebindResult.Error?.ToString() ?? "Unknown actor-binding error.");
            }

            return;
        }

        var transferResult = sessionController.TransferPreviewToCurrentActor();
        gposeHandoffPending = false;
        if (transferResult.IsSuccess)
        {
            pluginLog.Information(
                "Boutique carried {ChangedSlots} changed equipment slots {Direction} without closing the session.",
                sessionController.ChangedSlotCount,
                isGposing ? "into GPose" : "back to normal play");
        }
        else
        {
            pluginLog.Error(
                "Boutique GPose session handoff failed after actor binding: {Error}",
                transferResult.Error?.ToString() ?? "Unknown preview-transfer error.");
        }
    }

    private bool IsCombatBlocked()
        => configuration.Current.DisableInCombat && condition[ConditionFlag.InCombat];

    private void CloseForCombatIfNeeded()
    {
        if (combatLockActive)
        {
            return;
        }

        combatLockActive = true;
        if (!boutiqueWindow.IsOpen && sessionController.Session.State == BoutiqueSessionState.Closed)
        {
            return;
        }

        boutiqueWindow.IsOpen = false;
        boutiqueWardrobeWindow.IsOpen = false;
        designReferenceWindow.Reset();
        boutiqueWindow.ResetAfterClose();
        var closeResult = sessionController.Close();
        if (closeResult.IsSuccess)
        {
            pluginLog.Information("Boutique closed and its original appearance restored because combat began.");
        }
        else
        {
            pluginLog.Error(
                "Boutique combat-safety restoration failed: {Error}",
                closeResult.Error?.ToString() ?? "Unknown combat-safety restoration error.");
        }
    }

    private void OnClassJobChanged(uint classJobId)
        => browser.SetActiveClassJob(classJobId);

    private void RefreshActiveClassJob()
        => browser.SetActiveClassJob(playerState.IsLoaded ? playerState.ClassJob.RowId : null);

    private void CloseSessionFromWindow()
    {
        boutiqueWardrobeWindow.IsOpen = false;
        welcomeGuideWindow.IsOpen = false;
        designReferenceWindow.Reset();
        var closeResult = sessionController.Close();
        if (!closeResult.IsSuccess)
        {
            pluginLog.Error(
                "Boutique session close failed: {Error}",
                closeResult.Error?.ToString() ?? "Unknown close error.");
        }
    }

    private void ToggleWardrobe()
    {
        if (!boutiqueWindow.IsOpen || sessionController.Session.State != BoutiqueSessionState.Active)
        {
            return;
        }

        boutiqueWardrobeWindow.IsOpen = !boutiqueWardrobeWindow.IsOpen;
    }
}
