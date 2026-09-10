using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures;

namespace CrystariumBoutique.Ui;

/// <summary>
/// Draws the layered weathered-steel surround used by Crystarium Boutique grid icons.
/// </summary>
internal static class CrystariumMetalFrameRenderer
{
    // The generated 1254 px asset has a measured 1106 px transparent opening
    // along each straight centerline. Corner gussets intentionally extend farther
    // inward to cover the game icon's exposed rounded corners.
    // Scaling the complete frame from this ratio keeps that opening flush with
    // the game icon at every responsive icon size.
    private const float TransparentOpeningRatio = 1106f / 1254f;

    public const float FramedSizeScale = 1f / TransparentOpeningRatio;

    public static float OuterExtentFor(float contentSize)
        => contentSize * (FramedSizeScale - 1f) / 2f;

    public static float FramedSizeFor(float contentSize)
        => contentSize * FramedSizeScale;

    public static void Draw(
        ImDrawListPtr drawList,
        Vector2 contentMinimum,
        Vector2 contentMaximum,
        ISharedImmediateTexture frameTexture)
    {
        var outerExtent = OuterExtentFor(contentMaximum.X - contentMinimum.X);
        var visualMinimum = contentMinimum - new Vector2(outerExtent);
        var visualMaximum = contentMaximum + new Vector2(outerExtent);
        if (frameTexture.GetWrapOrDefault() is { } texture)
        {
            drawList.AddImage(texture.Handle, visualMinimum, visualMaximum);
            return;
        }

        // A restrained one-piece fallback avoids restoring the old layered frame
        // during the first frame before the generated texture is ready.
        drawList.AddRect(
            visualMinimum,
            visualMaximum,
            ImGui.ColorConvertFloat4ToU32(BoutiqueTheme.CrystariumSteelDark),
            BoutiqueTheme.ControlRounding + 6f,
            ImDrawFlags.None,
            11f);
    }
}
