using CrystariumBoutique.Ui.Components;

namespace CrystariumBoutiquePlugin.Tests.Ui;

public sealed class DesignActionTextTests
{
    [Fact]
    public void PublicDesignActionsUseExplicitLabels()
    {
        Assert.Equal("Import Design", LoadoutPanel.ImportDesignLabel);
        Assert.Equal("Export Design", LoadoutPanel.ExportDesignLabel);
    }

    [Fact]
    public void ImportTooltipExplainsSharedStringAndLocalSave()
    {
        Assert.Contains("Design string shared by another user", LoadoutPanel.ImportDesignTooltip);
        Assert.Contains("saves it to your Designs list", LoadoutPanel.ImportDesignTooltip);
        Assert.Contains("try it yourself", LoadoutPanel.ImportDesignTooltip);
    }
}
