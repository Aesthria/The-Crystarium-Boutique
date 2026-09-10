using System.Numerics;
using CrystariumBoutique.Configuration;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;

namespace CrystariumBoutique.Ui;

internal sealed class WelcomeGuideWindow : Window
{
    private readonly ConfigurationStore configuration;
    private readonly BoutiqueFontSet fonts;
    private bool applyOpeningSize = true;

    public WelcomeGuideWindow(ConfigurationStore configuration, BoutiqueFontSet fonts)
        : base("Welcome to The Crystarium Boutique###CrystariumBoutiqueWelcome")
    {
        this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        this.fonts = fonts ?? throw new ArgumentNullException(nameof(fonts));
        Size = WelcomeGuideLayout.PreferredWindowSize;
        SizeCondition = ImGuiCond.Appearing;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = WelcomeGuideLayout.MinimumWindowSize,
            MaximumSize = new Vector2(760f, float.MaxValue),
        };
        IsOpen = false;
    }

    public void Show()
    {
        if (!IsOpen)
        {
            applyOpeningSize = true;
        }

        IsOpen = true;
    }

    public override void PreDraw()
    {
        var bounds = WelcomeGuideLayout.CalculateBounds(
            ImGui.GetMainViewport().WorkSize,
            ImGuiHelpers.GlobalScale);
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = bounds.MinimumSize,
            MaximumSize = bounds.MaximumSize,
        };

        if (applyOpeningSize)
        {
            Size = bounds.OpeningSize;
            SizeCondition = ImGuiCond.Appearing;
            applyOpeningSize = false;
        }
    }

    public override void Draw()
    {
        BoutiqueScaleStyle.ApplyUserWindowFontScale(
            configuration.Current.InterfaceScalePercent);
        using var bodyFont = fonts.Body.Push();

        using (fonts.Navigation.Push())
        {
            ImGui.TextColored(
                BoutiqueTheme.GetHeadingColor(configuration.Current),
                "Welcome to The Crystarium Boutique");
        }

        ImGui.Separator();
        foreach (var point in HelpGuideContent.WelcomePoints)
        {
            DrawBullet(point);
        }
        ImGui.Spacing();
        ImGui.TextWrapped(
            "For more information about character customization and interface options, see the Help tab.");
        ImGui.Spacing();
        ImGui.TextWrapped(
            "You can stop this guide from appearing every time the Boutique opens by turning it off in the Help tab.");
        ImGui.Spacing();
        if (BoutiqueTheme.FramedButton(configuration.Current, "Continue", new Vector2(-1f, 0f)))
        {
            IsOpen = false;
        }
    }

    private static void DrawBullet(string text)
    {
        ImGui.Bullet();
        ImGui.SameLine();
        ImGui.TextWrapped(text);
    }
}
