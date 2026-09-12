using CrystariumBoutique;

namespace CrystariumBoutiquePlugin.Tests;

public sealed class BoutiqueVersionTests
{
    [Fact]
    public void FinalReleaseIdentityUsesNeutralVersionLabel()
    {
        Assert.Equal("0.1.3", BoutiqueVersion.SemanticVersion);
        Assert.Equal("v0.1.3", BoutiqueVersion.DisplayLabel);
    }
}
