using CrystariumBoutique.Ui;

namespace CrystariumBoutiquePlugin.Tests.Ui;

public sealed class FavoritesRevertLayoutTests
{
    [Fact]
    public void WideFavoritesFooterUsesApprovedBoutiqueRevertSize()
    {
        var layout = BoutiqueWindow.CalculatePreviousItemOnlyFooterLayout(500f);

        Assert.Equal(BoutiqueTheme.PreviousItemButtonSize, layout.UndoSize);
        Assert.Equal(layout.UndoSize, layout.Height);
    }

    [Fact]
    public void NarrowFavoritesFooterScalesWithoutGrowingOrClipping()
    {
        const float rowWidth = 50f;

        var layout = BoutiqueWindow.CalculatePreviousItemOnlyFooterLayout(rowWidth);

        Assert.True(layout.UndoSize < BoutiqueTheme.PreviousItemButtonSize);
        Assert.True(layout.UndoSize + (BoutiqueTheme.PreviousItemButtonMargin * 2f) <= rowWidth + 0.001f);
    }
}
