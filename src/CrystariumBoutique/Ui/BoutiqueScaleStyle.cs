using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace CrystariumBoutique.Ui;

/// <summary>
/// Applies Boutique-owned font scaling to the current window without changing
/// Dalamud's global style or participating in font-atlas rebuilds.
/// </summary>
internal static class BoutiqueScaleStyle
{
    private const float MinimumResponsiveScale = 0.85f;
    private const float MaximumResponsiveScale = 1.60f;
    private const float MinimumFinalScale = 0.65f;
    private const float MaximumFinalScale = 3f;

    [ThreadStatic]
    private static float currentBaseScale;

    public static void ApplyResponsiveWindowFontScale(
        int interfaceScalePercent,
        Vector2 referenceWindowSize)
    {
        var currentSize = ImGui.GetWindowSize();
        var widthRatio = Math.Max(currentSize.X / Math.Max(referenceWindowSize.X, 1f), 0.01f);
        var heightRatio = Math.Max(currentSize.Y / Math.Max(referenceWindowSize.Y, 1f), 0.01f);
        var responsiveScale = Math.Clamp(
            MathF.Sqrt(widthRatio * heightRatio),
            MinimumResponsiveScale,
            MaximumResponsiveScale);
        var userScale = Math.Clamp(interfaceScalePercent, 75, 200) / 100f;
        currentBaseScale = Math.Clamp(
            responsiveScale * userScale,
            MinimumFinalScale,
            MaximumFinalScale);
        ImGui.SetWindowFontScale(currentBaseScale);
    }

    public static void ApplyUserWindowFontScale(int interfaceScalePercent)
    {
        currentBaseScale = Math.Clamp(interfaceScalePercent, 75, 200) / 100f;
        ImGui.SetWindowFontScale(currentBaseScale);
    }

    public static void ApplyRelativeWindowFontScale(float relativeScale = 1f)
    {
        var baseScale = currentBaseScale > 0f ? currentBaseScale : 1f;
        ImGui.SetWindowFontScale(Math.Clamp(
            baseScale * Math.Max(relativeScale, 0.01f),
            MinimumFinalScale,
            MaximumFinalScale));
    }
}
