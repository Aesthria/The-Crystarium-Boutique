using CrystariumBoutique.Core.Catalog;

namespace CrystariumBoutique.Core.Tests.Catalog;

public sealed class ContentGroupResolverTests
{
    [Theory]
    [InlineData(1, "arr")]
    [InlineData(50, "arr")]
    [InlineData(51, "hw")]
    [InlineData(70, "sb")]
    [InlineData(80, "shb")]
    [InlineData(90, "ew")]
    [InlineData(100, "dt")]
    public void ResolveMapsKnownExpansionEraLevelBands(byte level, string expectedKey)
    {
        var group = ContentGroupResolver.Resolve(level);

        Assert.Equal(expectedKey, group.Key);
    }

    [Fact]
    public void ResolveCreatesFutureLevelBandWithoutFixedExpansionCount()
    {
        var group = ContentGroupResolver.Resolve(107);

        Assert.Equal("levels-101-110", group.Key);
        Assert.Equal("Levels 101–110", group.DisplayName);
        Assert.Equal(101, group.MinimumEquipLevel);
        Assert.Equal(110, group.MaximumEquipLevel);
    }

    [Fact]
    public void ResolveKeepsUnlevelledItemsInOtherGroup()
    {
        var group = ContentGroupResolver.Resolve(0);

        Assert.Equal("other", group.Key);
    }
}
