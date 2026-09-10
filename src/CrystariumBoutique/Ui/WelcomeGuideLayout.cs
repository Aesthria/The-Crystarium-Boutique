using System.Numerics;

namespace CrystariumBoutique.Ui;

internal static class WelcomeGuideLayout
{
    public static readonly Vector2 PreferredWindowSize = new(620f, 820f);
    public static readonly Vector2 MinimumWindowSize = new(480f, 360f);

    private const float MaximumWindowWidth = 760f;
    private const float WorkAreaClearance = 48f;

    public static WelcomeGuideBounds CalculateBounds(Vector2 workAreaSize, float globalScale)
    {
        var safeScale = globalScale > 0f && float.IsFinite(globalScale)
            ? globalScale
            : 1f;
        var logicalWorkArea = workAreaSize / safeScale;
        var available = new Vector2(
            Math.Max(logicalWorkArea.X - WorkAreaClearance, 1f),
            Math.Max(logicalWorkArea.Y - WorkAreaClearance, 1f));
        var maximum = new Vector2(
            Math.Min(MaximumWindowWidth, available.X),
            available.Y);
        var minimum = Vector2.Min(MinimumWindowSize, maximum);
        var opening = Vector2.Min(PreferredWindowSize, maximum);

        return new WelcomeGuideBounds(
            opening,
            minimum,
            maximum,
            opening.Y < PreferredWindowSize.Y);
    }
}

internal readonly record struct WelcomeGuideBounds(
    Vector2 OpeningSize,
    Vector2 MinimumSize,
    Vector2 MaximumSize,
    bool UsesConstrainedHeightFallback);
