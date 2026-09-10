using CrystariumBoutique.Ui;

namespace CrystariumBoutiquePlugin.Tests.Ui;

public sealed class HelpGuideContentTests
{
    private static readonly string AllContent = string.Join(
        '\n',
        HelpGuideContent.WelcomePoints
            .Concat(HelpGuideContent.BoutiquePoints)
            .Concat(HelpGuideContent.WardrobePoints)
            .Concat(HelpGuideContent.FavoritePoints)
            .Concat(HelpGuideContent.DesignPoints)
            .Concat(HelpGuideContent.EorzeaPoints)
            .Concat(HelpGuideContent.ThemePoints));

    [Fact]
    public void CurrentCommandsAreDocumentedWithoutObsoleteCommand()
    {
        Assert.Contains("/boutique", AllContent, StringComparison.Ordinal);
        Assert.Contains("/tcb", AllContent, StringComparison.Ordinal);
        Assert.Contains("/cb", AllContent, StringComparison.Ordinal);
        Assert.DoesNotContain("/cboutique", AllContent, StringComparison.Ordinal);
    }

    [Fact]
    public void VisorControlsDescribeTheVisibleResult()
    {
        Assert.Contains("optional visor piece hidden", AllContent, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("full or alternate visor appearance", AllContent, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("VisorState", AllContent, StringComparison.Ordinal);
    }

    [Fact]
    public void WardrobeDyesFavoritesDesignsAndThemesAreCurrent()
    {
        Assert.Contains("Automatically Sync Wardrobe", AllContent, StringComparison.Ordinal);
        Assert.Contains("Reset Character", AllContent, StringComparison.Ordinal);
        Assert.Contains("Clear All Dye Slots", AllContent, StringComparison.Ordinal);
        Assert.Contains("Right Click", AllContent, StringComparison.Ordinal);
        Assert.Contains("Save Design", AllContent, StringComparison.Ordinal);
        Assert.Contains("Import Design", AllContent, StringComparison.Ordinal);
        Assert.Contains("Export Design", AllContent, StringComparison.Ordinal);
        Assert.Contains("Design string", AllContent, StringComparison.Ordinal);
        Assert.Contains("modify", AllContent, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Simple Crystarium Boutique", AllContent, StringComparison.Ordinal);
        Assert.DoesNotContain("Details tab", AllContent, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Refresh Slots", AllContent, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void WardrobeRightClickRemovalIsDocumentedInWelcomeAndHelp()
    {
        Assert.Contains(
            HelpGuideContent.WelcomePoints,
            point => point.Contains("Right Click", StringComparison.Ordinal)
                && point.Contains("equipped item card", StringComparison.OrdinalIgnoreCase)
                && point.Contains("remove", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(
            HelpGuideContent.WardrobePoints,
            point => point.Contains("Right Click", StringComparison.Ordinal)
                && point.Contains("equipped item card", StringComparison.OrdinalIgnoreCase)
                && point.Contains("remove", StringComparison.OrdinalIgnoreCase));
    }
}
