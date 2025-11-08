using System;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Game.Command;
using TheCrystariumBoutique.UI;

namespace TheCrystariumBoutique;

public sealed class Plugin : IDalamudPlugin
{
    public string Name => "The Crystarium Boutique";

    private readonly WindowSystem _windows = new("The Crystarium Boutique");
    private readonly Configuration _config;
    private readonly ItemRepository _repo;
    private readonly TryOnService _tryOn;
    private readonly MainWindow _main;

    private const string CommandTcb = "/tcb";
    private const string CommandBoutique = "/boutique";
    private const string CommandDiag = "/tcbdiag";

    public Plugin(IDalamudPluginInterface pi, ICommandManager commandManager)
    {
        pi.Create<Svc>();

        _config = (Svc.PluginInterface.GetPluginConfig() as Configuration) ?? new Configuration();
        _config.Initialize(Svc.PluginInterface);

        _repo = new ItemRepository();
        _tryOn = new TryOnService();

        _main = new MainWindow(_config, _repo, _tryOn)
        {
            IsOpen = _config.OpenOnLogin
        };
        _windows.AddWindow(_main);

        Svc.PluginInterface.UiBuilder.Draw += DrawUI;
        Svc.PluginInterface.UiBuilder.OpenConfigUi += ToggleMainWindow;

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
            HelpMessage = "Show The Crystarium Boutique TryOn diagnostics."
        });
    }

    private void OnCommand(string command, string args)
        => ToggleMainWindow();

    private void ToggleMainWindow()
        => _main.IsOpen = !_main.IsOpen;

    private void DrawUI()
        => _windows.Draw();

    private void OnDiag(string cmd, string args)
    {
        TryOnDiagnostics.PrintStatus();
#if ENABLE_TRYON
        Svc.Chat.Print("[Boutique] ENABLE_TRYON is defined.");
#else
        Svc.Chat.Print("[Boutique] ENABLE_TRYON is NOT defined.");
#endif
    }

    public void Dispose()
    {
        Svc.Commands.RemoveHandler(CommandTcb);
        Svc.Commands.RemoveHandler(CommandBoutique);
        Svc.PluginInterface.UiBuilder.Draw -= DrawUI;
        Svc.PluginInterface.UiBuilder.OpenConfigUi -= ToggleMainWindow;
        _repo.Dispose();
        Svc.Commands.RemoveHandler(CommandDiag);
    }
}
