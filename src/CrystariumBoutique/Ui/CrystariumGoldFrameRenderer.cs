using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Plugin.Services;

namespace CrystariumBoutique.Ui;

/// <summary>
/// Draws the exact antique-gold control and tooltip frames supplied with the
/// Crystarium theme. The source regions are sliced so corners keep their shape
/// while straight sections expand with responsive controls and tooltips.
/// </summary>
internal static class CrystariumGoldFrameRenderer
{
    private static readonly SourceRegion ControlSource = new(
        new Vector2(54f, 48f),
        new Vector2(1486f, 298f),
        new Vector4(105f, 50f, 105f, 50f));

    private static readonly SourceRegion TooltipSource = new(
        new Vector2(133f, 362f),
        new Vector2(1406f, 961f),
        new Vector4(96f, 96f, 96f, 96f));

    private static ISharedImmediateTexture? frameTexture;

    public static void Initialize(ITextureProvider textureProvider, string assetDirectory)
    {
        ArgumentNullException.ThrowIfNull(textureProvider);
        ArgumentException.ThrowIfNullOrWhiteSpace(assetDirectory);
        frameTexture = textureProvider.GetFromFile(
            Path.Combine(assetDirectory, "images", "crystarium-gold-frames.png"));
    }

    public static void Reset()
        => frameTexture = null;

    public static void DrawControl(
        ImDrawListPtr drawList,
        Vector2 minimum,
        Vector2 maximum)
    {
        if (frameTexture?.GetWrapOrDefault() is not { } texture)
        {
            return;
        }

        var size = maximum - minimum;
        if (size.X <= 2f || size.Y <= 2f)
        {
            return;
        }

        var sourceHeight = ControlSource.Maximum.Y - ControlSource.Minimum.Y;
        var requestedCap = size.Y * (ControlSource.Slices.X / sourceHeight);
        var cap = Math.Min(requestedCap, size.X / 2f);
        var verticalCap = size.Y * (ControlSource.Slices.Y / sourceHeight);
        DrawNineSlice(
            drawList,
            texture,
            minimum,
            maximum,
            ControlSource,
            new Vector4(cap, verticalCap, cap, verticalCap));
    }

    public static void DrawTooltip(
        ImDrawListPtr drawList,
        Vector2 minimum,
        Vector2 maximum)
    {
        if (frameTexture?.GetWrapOrDefault() is not { } texture)
        {
            return;
        }

        var size = maximum - minimum;
        if (size.X <= 8f || size.Y <= 8f)
        {
            return;
        }

        var corner = Math.Clamp(Math.Min(size.X, size.Y) * 0.075f, 12f, 24f);
        corner = Math.Min(corner, Math.Min(size.X, size.Y) / 2f);
        DrawNineSlice(
            drawList,
            texture,
            minimum,
            maximum,
            TooltipSource,
            new Vector4(corner, corner, corner, corner));
        DrawTooltipSideRails(drawList, texture, minimum, maximum, corner);
    }

    private static void DrawTooltipSideRails(
        ImDrawListPtr drawList,
        IDalamudTextureWrap texture,
        Vector2 minimum,
        Vector2 maximum,
        float corner)
    {
        const float sourceRailWidth = 32f;
        var destinationRailWidth = Math.Clamp((maximum.X - minimum.X) * 0.012f, 4f, 7f);
        var verticalInset = Math.Max(corner * 0.72f, 8f);
        var destinationTop = minimum.Y + verticalInset;
        var destinationBottom = maximum.Y - verticalInset;
        if (destinationBottom <= destinationTop)
        {
            return;
        }

        var inverseTextureSize = new Vector2(1f / texture.Width, 1f / texture.Height);
        var sourceTop = TooltipSource.Minimum.Y + TooltipSource.Slices.Y;
        var sourceBottom = TooltipSource.Maximum.Y - TooltipSource.Slices.W;
        var tint = ImGui.ColorConvertFloat4ToU32(
            new Vector4(1f, 1f, 1f, ImGui.GetStyle().Alpha));

        drawList.AddImage(
            texture.Handle,
            minimum + new Vector2(1f, verticalInset),
            new Vector2(minimum.X + 1f + destinationRailWidth, destinationBottom),
            new Vector2(TooltipSource.Minimum.X, sourceTop) * inverseTextureSize,
            new Vector2(TooltipSource.Minimum.X + sourceRailWidth, sourceBottom)
                * inverseTextureSize,
            tint);
        drawList.AddImage(
            texture.Handle,
            new Vector2(maximum.X - 1f - destinationRailWidth, destinationTop),
            maximum - new Vector2(1f, verticalInset),
            new Vector2(TooltipSource.Maximum.X - sourceRailWidth, sourceTop)
                * inverseTextureSize,
            new Vector2(TooltipSource.Maximum.X, sourceBottom) * inverseTextureSize,
            tint);

        // The supplied side pixels are extremely narrow compared with the long top and bottom
        // rails. Reinforce their opaque center inside the popup clip rectangle so the vertical
        // frame remains continuous at every tooltip size and UI scale.
        var styleAlpha = ImGui.GetStyle().Alpha;
        var shadow = ImGui.ColorConvertFloat4ToU32(
            new Vector4(0.16f, 0.105f, 0.025f, styleAlpha));
        var gold = ImGui.ColorConvertFloat4ToU32(
            new Vector4(0.56f, 0.39f, 0.10f, styleAlpha));
        var highlight = ImGui.ColorConvertFloat4ToU32(
            new Vector4(0.78f, 0.61f, 0.25f, styleAlpha));
        var leftX = minimum.X + 3.5f;
        var rightX = maximum.X - 3.5f;
        var top = destinationTop - 1f;
        var bottom = destinationBottom + 1f;
        foreach (var x in new[] { leftX, rightX })
        {
            drawList.AddLine(new Vector2(x, top), new Vector2(x, bottom), shadow, 6f);
            drawList.AddLine(new Vector2(x, top), new Vector2(x, bottom), gold, 3.5f);
            drawList.AddLine(
                new Vector2(x - 0.75f, top),
                new Vector2(x - 0.75f, bottom),
                highlight,
                1f);
        }
    }

    private static void DrawNineSlice(
        ImDrawListPtr drawList,
        IDalamudTextureWrap texture,
        Vector2 destinationMinimum,
        Vector2 destinationMaximum,
        SourceRegion source,
        Vector4 destinationSlices)
    {
        var sourceX = new[]
        {
            source.Minimum.X,
            source.Minimum.X + source.Slices.X,
            source.Maximum.X - source.Slices.Z,
            source.Maximum.X,
        };
        var sourceY = new[]
        {
            source.Minimum.Y,
            source.Minimum.Y + source.Slices.Y,
            source.Maximum.Y - source.Slices.W,
            source.Maximum.Y,
        };
        var destinationX = new[]
        {
            destinationMinimum.X,
            destinationMinimum.X + destinationSlices.X,
            destinationMaximum.X - destinationSlices.Z,
            destinationMaximum.X,
        };
        var destinationY = new[]
        {
            destinationMinimum.Y,
            destinationMinimum.Y + destinationSlices.Y,
            destinationMaximum.Y - destinationSlices.W,
            destinationMaximum.Y,
        };
        var inverseTextureSize = new Vector2(1f / texture.Width, 1f / texture.Height);
        var tint = ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, ImGui.GetStyle().Alpha));

        for (var row = 0; row < 3; row++)
        {
            for (var column = 0; column < 3; column++)
            {
                if (row == 1 && column == 1)
                {
                    continue;
                }

                var cellMinimum = new Vector2(destinationX[column], destinationY[row]);
                var cellMaximum = new Vector2(destinationX[column + 1], destinationY[row + 1]);
                if (cellMaximum.X <= cellMinimum.X || cellMaximum.Y <= cellMinimum.Y)
                {
                    continue;
                }

                var uvMinimum = new Vector2(sourceX[column], sourceY[row]) * inverseTextureSize;
                var uvMaximum = new Vector2(sourceX[column + 1], sourceY[row + 1]) * inverseTextureSize;
                drawList.AddImage(
                    texture.Handle,
                    cellMinimum,
                    cellMaximum,
                    uvMinimum,
                    uvMaximum,
                    tint);
            }
        }
    }

    private readonly record struct SourceRegion(
        Vector2 Minimum,
        Vector2 Maximum,
        Vector4 Slices);
}
