using System.Numerics;
using CrystariumBoutique.Configuration;
using Dalamud.Bindings.ImGui;

namespace CrystariumBoutique.Ui;

internal readonly record struct CrystariumProfileStyleStack(int ColorCount, int VariableCount)
{
    public void Pop()
    {
        if (VariableCount > 0)
        {
            ImGui.PopStyleVar(VariableCount);
        }

        if (ColorCount > 0)
        {
            ImGui.PopStyleColor(ColorCount);
        }
    }
}

internal static class CrystariumProfileStyle
{
    public static CrystariumProfileStyleStack Push(PluginConfiguration configuration)
    {
        if (BoutiqueTheme.IsSimpleCrystariumProfile(configuration))
        {
            return PushSimpleCrystarium(configuration);
        }

        if (!BoutiqueTheme.IsCrystariumProfile(configuration))
        {
            return default;
        }

        var glass = BoutiqueTheme.CrystariumGlass;
        var glassHovered = BoutiqueTheme.CrystariumGlassHovered;
        var glassActive = BoutiqueTheme.CrystariumGlassActive;
        var frame = BoutiqueTheme.CrystariumFrame;
        var frameHovered = BoutiqueTheme.CrystariumFrameHovered;
        var frameActive = BoutiqueTheme.CrystariumFrameActive;
        var panel = BoutiqueTheme.CrystariumPanel;
        var text = BoutiqueTheme.GetBoutiqueTabTextColor(configuration);
        var mutedText = BoutiqueTheme.CrystariumMutedText;
        var horizontalRule = BoutiqueTheme.GetHorizontalRuleColor(configuration);
        var transparentPanel = panel;
        transparentPanel.W = 0.40f;

        PushColor(ImGuiCol.Text, text);
        PushColor(ImGuiCol.TextDisabled, mutedText);
        PushColor(ImGuiCol.ChildBg, transparentPanel);
        PushColor(ImGuiCol.PopupBg, new Vector4(0.075f, 0.09f, 0.105f, 0.99f));
        PushColor(ImGuiCol.Border, frameActive);
        PushColor(ImGuiCol.BorderShadow, new Vector4(0.005f, 0.007f, 0.009f, 0.92f));
        PushColor(ImGuiCol.FrameBg, panel);
        PushColor(ImGuiCol.FrameBgHovered, glassHovered);
        PushColor(ImGuiCol.FrameBgActive, glassActive);
        PushColor(ImGuiCol.TitleBg, new Vector4(0.025f, 0.095f, 0.145f, 1f));
        PushColor(ImGuiCol.TitleBgActive, new Vector4(0.035f, 0.145f, 0.215f, 1f));
        PushColor(ImGuiCol.TitleBgCollapsed, new Vector4(0.018f, 0.070f, 0.110f, 1f));
        PushColor(ImGuiCol.ScrollbarBg, new Vector4(0.035f, 0.04f, 0.045f, 0.92f));
        PushColor(ImGuiCol.ScrollbarGrab, glass);
        PushColor(ImGuiCol.ScrollbarGrabHovered, glassHovered);
        PushColor(ImGuiCol.ScrollbarGrabActive, frameActive);
        PushColor(ImGuiCol.CheckMark, BoutiqueTheme.CrystariumGlow);
        PushColor(ImGuiCol.SliderGrab, frameHovered);
        PushColor(ImGuiCol.SliderGrabActive, BoutiqueTheme.CrystariumGlow);
        PushColor(ImGuiCol.Button, glass);
        PushColor(ImGuiCol.ButtonHovered, glassHovered);
        PushColor(ImGuiCol.ButtonActive, glassActive);
        PushColor(ImGuiCol.Header, glass);
        PushColor(ImGuiCol.HeaderHovered, glassHovered);
        PushColor(ImGuiCol.HeaderActive, glassActive);
        PushColor(ImGuiCol.Tab, new Vector4(0.12f, 0.14f, 0.16f, 0.50f));
        PushColor(ImGuiCol.TabHovered, BoutiqueTheme.CrystariumFrameHovered);
        PushColor(ImGuiCol.TabActive, BoutiqueTheme.CrystariumGlassHovered);
        PushColor(ImGuiCol.TabUnfocused, new Vector4(0.07f, 0.08f, 0.09f, 0.50f));
        PushColor(ImGuiCol.TabUnfocusedActive, BoutiqueTheme.CrystariumGlassActive);
        PushColor(ImGuiCol.Separator, horizontalRule);
        PushColor(ImGuiCol.SeparatorHovered, BoutiqueTheme.GetHoveredControlColor(horizontalRule));
        PushColor(ImGuiCol.SeparatorActive, BoutiqueTheme.GetActiveControlColor(horizontalRule));

        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 2f);
        ImGui.PushStyleVar(ImGuiStyleVar.ChildBorderSize, 1f);
        ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 0f);
        ImGui.PushStyleVar(
            ImGuiStyleVar.FramePadding,
            ImGui.GetStyle().FramePadding + new Vector2(2f, 1f));
        return new CrystariumProfileStyleStack(33, 4);
    }

    private static CrystariumProfileStyleStack PushSimpleCrystarium(
        PluginConfiguration configuration)
    {
        var charcoal = BoutiqueTheme.SimpleCrystariumDeepCharcoal;
        var darkSteel = BoutiqueTheme.SimpleCrystariumDarkSteel;
        var mediumSteel = BoutiqueTheme.SimpleCrystariumMediumSteel;
        var gold = BoutiqueTheme.GetSimpleThemeGoldColor(configuration);
        var blueGrey = BoutiqueTheme.SimpleCrystariumMutedBlueGrey;

        PushColor(ImGuiCol.Text, gold);
        PushColor(ImGuiCol.TextDisabled, gold with { W = 0.65f });
        PushColor(ImGuiCol.ChildBg, charcoal with { W = 0.72f });
        PushColor(ImGuiCol.PopupBg, charcoal with { W = 0.98f });
        PushColor(ImGuiCol.Border, gold);
        PushColor(ImGuiCol.BorderShadow, charcoal);
        PushColor(ImGuiCol.FrameBg, darkSteel);
        PushColor(ImGuiCol.FrameBgHovered, mediumSteel);
        PushColor(ImGuiCol.FrameBgActive, blueGrey);
        PushColor(ImGuiCol.TitleBg, charcoal);
        PushColor(ImGuiCol.TitleBgActive, darkSteel);
        PushColor(ImGuiCol.TitleBgCollapsed, charcoal);
        PushColor(ImGuiCol.ScrollbarBg, charcoal with { W = 0.92f });
        PushColor(ImGuiCol.ScrollbarGrab, darkSteel);
        PushColor(ImGuiCol.ScrollbarGrabHovered, mediumSteel);
        PushColor(ImGuiCol.ScrollbarGrabActive, blueGrey);
        PushColor(ImGuiCol.CheckMark, gold);
        PushColor(ImGuiCol.SliderGrab, gold with { W = 0.82f });
        PushColor(ImGuiCol.SliderGrabActive, gold);
        PushColor(ImGuiCol.Button, darkSteel);
        PushColor(ImGuiCol.ButtonHovered, mediumSteel);
        PushColor(ImGuiCol.ButtonActive, blueGrey);
        PushColor(ImGuiCol.Header, darkSteel);
        PushColor(ImGuiCol.HeaderHovered, mediumSteel);
        PushColor(ImGuiCol.HeaderActive, blueGrey);
        PushColor(ImGuiCol.Tab, darkSteel with { W = 0.78f });
        PushColor(ImGuiCol.TabHovered, mediumSteel);
        PushColor(ImGuiCol.TabActive, blueGrey);
        PushColor(ImGuiCol.TabUnfocused, charcoal with { W = 0.72f });
        PushColor(ImGuiCol.TabUnfocusedActive, darkSteel);
        PushColor(ImGuiCol.Separator, gold);
        PushColor(ImGuiCol.SeparatorHovered, BoutiqueTheme.GetHoveredControlColor(gold));
        PushColor(ImGuiCol.SeparatorActive, BoutiqueTheme.GetActiveControlColor(gold));

        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 1f);
        ImGui.PushStyleVar(ImGuiStyleVar.ChildBorderSize, 1f);
        ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 1f);
        return new CrystariumProfileStyleStack(33, 3);
    }

    private static void PushColor(ImGuiCol target, Vector4 color)
        => ImGui.PushStyleColor(target, color);
}
