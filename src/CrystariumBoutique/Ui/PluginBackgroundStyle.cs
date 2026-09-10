using System.Numerics;
using CrystariumBoutique.Configuration;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures;

namespace CrystariumBoutique.Ui;

internal static class PluginBackgroundStyle
{
    public static bool Push(PluginConfiguration configuration)
    {
        if (BoutiqueTheme.UsesCrystariumBackdrop(configuration))
        {
            var background = BoutiqueTheme.IsSimpleCrystariumProfile(configuration)
                ? BoutiqueTheme.SimpleCrystariumDeepCharcoal
                : new Vector4(0.02f, 0.05f, 0.08f, 1f);
            ImGui.PushStyleColor(ImGuiCol.WindowBg, background);
            return true;
        }

        if (!configuration.UseCustomBackgroundColor)
        {
            return false;
        }

        ImGui.PushStyleColor(ImGuiCol.WindowBg, GetColor(configuration));
        return true;
    }

    public static Vector4 GetColor(PluginConfiguration configuration)
        => new(
            Math.Clamp(configuration.BackgroundColorRed, 0, 255) / 255f,
            Math.Clamp(configuration.BackgroundColorGreen, 0, 255) / 255f,
            Math.Clamp(configuration.BackgroundColorBlue, 0, 255) / 255f,
            1f);

    public static void DrawCrystariumBackdrop(
        PluginConfiguration configuration,
        ISharedImmediateTexture texture)
    {
        if (!BoutiqueTheme.UsesCrystariumBackdrop(configuration)
            || texture.GetWrapOrDefault() is not { } wrap
            || wrap.Width <= 0
            || wrap.Height <= 0)
        {
            return;
        }

        var position = ImGui.GetWindowPos();
        var size = ImGui.GetWindowSize();
        if (size.X <= 0f || size.Y <= 0f)
        {
            return;
        }

        var uvMinimum = Vector2.Zero;
        var uvMaximum = Vector2.One;
        var textureAspect = wrap.Width / (float)wrap.Height;
        var windowAspect = size.X / size.Y;
        if (textureAspect > windowAspect)
        {
            var visibleWidth = windowAspect / textureAspect;
            uvMinimum.X = (1f - visibleWidth) / 2f;
            uvMaximum.X = 1f - uvMinimum.X;
        }
        else
        {
            var visibleHeight = textureAspect / windowAspect;
            uvMinimum.Y = (1f - visibleHeight) / 2f;
            uvMaximum.Y = 1f - uvMinimum.Y;
        }

        var backgroundVisibility = 1f - (Math.Clamp(
            configuration.BackgroundTransparencyPercent,
            0,
            100) / 100f);
        var windowVisibility = 1f - (Math.Clamp(
            configuration.PluginTransparencyPercent,
            0,
            90) / 100f);
        var visibility = backgroundVisibility * windowVisibility;
        if (visibility <= 0f)
        {
            return;
        }

        var maximum = position + size;
        var drawList = ImGui.GetWindowDrawList();
        drawList.AddImage(
            wrap.Handle,
            position,
            maximum,
            uvMinimum,
            uvMaximum,
            ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, 0.86f * visibility)));
        var darkness = Math.Clamp(
            configuration.CrystariumBackgroundDarknessPercent,
            0,
            100) / 100f;
        var shadeAlpha = (0.16f + (0.84f * darkness)) * visibility;
        drawList.AddRectFilled(
            position,
            maximum,
            ImGui.ColorConvertFloat4ToU32(new Vector4(0.01f, 0.015f, 0.02f, shadeAlpha)));
        var outline = BoutiqueTheme.IsSimpleCrystariumProfile(configuration)
            ? BoutiqueTheme.GetSimpleThemeGoldColor(configuration)
            : BoutiqueTheme.CrystariumFrameHovered;
        drawList.AddRect(
            position + Vector2.One,
            maximum - Vector2.One,
            ImGui.ColorConvertFloat4ToU32(new Vector4(0.015f, 0.02f, 0.025f, 0.96f * visibility)),
            3f,
            ImDrawFlags.None,
            5f);
        drawList.AddRect(
            position + new Vector2(3f),
            maximum - new Vector2(3f),
            ImGui.ColorConvertFloat4ToU32(new Vector4(
                outline.X,
                outline.Y,
                outline.Z,
                0.84f * visibility)),
            2f,
            ImDrawFlags.None,
            1f);
    }
}
