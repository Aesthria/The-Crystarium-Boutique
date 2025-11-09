using System;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using TheCrystariumBoutique.Data;
using TheCrystariumBoutique.UI;

namespace TheCrystariumBoutique;

public sealed class Plugin : IDalamudPlugin
{
    public string Name => "The Crystarium Boutique";

    private const string CommandTcb = "/tcb";
    private const string CommandBoutique = "/boutique";
    private const string CommandDiag = "/tcbdiag";

    private readonly WindowSystem _windows = new("TheCrystariumBoutique");
    private readonly Configuration _config;
    private readonly ItemRepository? _repo;
    private readonly TryOnService? _tryOn;
    private readonly MainWindow? _main;

    public Plugin(IDalamudPluginInterface pluginInterface)
    {
        try
        {
            // Wire up service container
            pluginInterface.Create<Svc>();

            // Load config
            _config = (Svc.PluginInterface.GetPluginConfig() as Configuration) ?? new Configuration();
            _config.Initialize(Svc.PluginInterface);

            // Initialize dependencies safely
            try
            {
                _repo = new ItemRepository();
            }
            catch (Exception ex)
            {
                SafeLog($"Failed to create ItemRepository: {ex}");
            }

            try
            {
                _tryOn = new TryOnService();
            }
            catch (Exception ex)
            {
                SafeLog($"Failed to create TryOnService: {ex}");
            }

            // Create main window if dependencies are ready
            if (_repo != null && _tryOn != null)
            {
                _main = new MainWindow(_config, _repo, _tryOn)
                {
                    IsOpen = _config.OpenOnLogin
                };
                _windows.AddWindow(_main);
            }
            else
            {
                SafeLog("MainWindow not created because dependencies were missing.");
            }

            // UI hooks
            Svc.PluginInterface.UiBuilder.Draw += DrawUI;
            Svc.PluginInterface.UiBuilder.OpenConfigUi += ToggleMainWindow;
            Svc.PluginInterface.UiBuilder.OpenMainUi += ToggleMainWindow;

            // Commands
            Svc.Commands.AddHandler(CommandTcb, new CommandInfo(OnCommand)
            {
                HelpMessage = "Open or close The Crystarium Boutique."
            });

            Svc.Commands.AddHandler(CommandBoutique, new CommandInfo(OnCommand)
            {
                HelpMessage = "Open or close The Crystarium Boutique."
            });

            Svc.Commands.AddHandler(CommandDiag, new CommandInfo(OnDiag)
            {
                HelpMessage = "Show The Crystarium Boutique diagnostics."
            });

            SafeLog("The Crystarium Boutique initialized.");
        }
        catch (Exception ex)
        {
            // Ensure no exceptions escape the constructor
            SafeLog("Fatal error during plugin initialization:\n" + ex);
        }
    }

    private void OnCommand(string command, string args)
        => ToggleMainWindow();

    private void ToggleMainWindow()
    {
        if (_main != null)
        {
            _main.IsOpen = !_main.IsOpen;
        }
        else
        {
            SafePrint("[Boutique] UI not available (initialization failed).");
        }
    }

    private void DrawUI()
        => _windows.Draw();

    private void OnDiag(string cmd, string args)
    {
        SafePrint("[Boutique] Diagnostics:");
        SafePrint(_repo != null
            ? " - ItemRepository: initialized"
            : " - ItemRepository: not initialized");
        SafePrint(_tryOn != null
            ? " - TryOnService: initialized"
            : " - TryOnService: not initialized");
    }

    private static void SafePrint(string message)
    {
        try
        {
            Svc.Chat?.Print(message);
        }
        catch
        {
            // ignore logging errors
        }
    }

    private static void SafeLog(string message)
    {
        try
        {
            Svc.PluginLog?.Information(message);
        }
        catch
        {
            // ignore logging errors
        }
    }

    public void Dispose()
    {
        try
        {
            Svc.Commands.RemoveHandler(CommandTcb);
            Svc.Commands.RemoveHandler(CommandBoutique);
            Svc.Commands.RemoveHandler(CommandDiag);
        }
        catch
        {
        }

        try
        {
            Svc.PluginInterface.UiBuilder.Draw -= DrawUI;
            Svc.PluginInterface.UiBuilder.OpenConfigUi -= ToggleMainWindow;
            Svc.PluginInterface.UiBuilder.OpenMainUi -= ToggleMainWindow;
        }
        catch
        {
        }

        try
        {
            _repo?.Dispose();
        }
        catch
        {
        }
    }
}
