using Dalamud.Interface.GameFonts;
using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Plugin;

namespace CrystariumBoutique.Ui;

public sealed class BoutiqueFontSet : IDisposable
{
    public BoutiqueFontSet(IDalamudPluginInterface pluginInterface)
    {
        ArgumentNullException.ThrowIfNull(pluginInterface);
        var atlas = pluginInterface.UiBuilder.FontAtlas;
        MainTitle = atlas.NewGameFontHandle(new GameFontStyle(GameFontFamily.TrumpGothic, 23f));
        Navigation = atlas.NewGameFontHandle(new GameFontStyle(GameFontFamily.Jupiter, 20f));
        SectionHeading = atlas.NewGameFontHandle(new GameFontStyle(GameFontFamily.Jupiter, 16f));
        Body = atlas.NewGameFontHandle(new GameFontStyle(GameFontFamily.Axis, 14f));
        Helper = atlas.NewGameFontHandle(new GameFontStyle(GameFontFamily.Axis, 12f));
        Numeric = atlas.NewGameFontHandle(new GameFontStyle(GameFontFamily.MiedingerMid, 14f));
        Tooltip = atlas.NewGameFontHandle(new GameFontStyle(GameFontFamily.Jupiter, 16f));
        TooltipItemName = atlas.NewGameFontHandle(new GameFontStyle(GameFontFamily.Jupiter, 25.6f));
        TooltipSmall = atlas.NewGameFontHandle(new GameFontStyle(GameFontFamily.Jupiter, 14f));
    }

    public IFontHandle MainTitle { get; }

    public IFontHandle Navigation { get; }

    public IFontHandle SectionHeading { get; }

    public IFontHandle Body { get; }

    public IFontHandle Helper { get; }

    public IFontHandle Numeric { get; }

    public IFontHandle Tooltip { get; }

    public IFontHandle TooltipItemName { get; }

    public IFontHandle TooltipSmall { get; }

    public void Dispose()
    {
        TooltipSmall.Dispose();
        TooltipItemName.Dispose();
        Tooltip.Dispose();
        Numeric.Dispose();
        Helper.Dispose();
        Body.Dispose();
        SectionHeading.Dispose();
        Navigation.Dispose();
        MainTitle.Dispose();
    }
}
