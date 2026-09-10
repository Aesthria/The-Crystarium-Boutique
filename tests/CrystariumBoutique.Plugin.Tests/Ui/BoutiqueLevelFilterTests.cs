using CrystariumBoutique.Ui;

namespace CrystariumBoutiquePlugin.Tests.Ui;

public sealed class BoutiqueLevelFilterTests
{
    [Theory]
    [InlineData("1", 1)]
    [InlineData("30", 30)]
    [InlineData("60", 60)]
    [InlineData("70", 70)]
    [InlineData("100", 100)]
    [InlineData("110", 110)]
    public void PositiveLevelProducesAnUpperBound(string text, byte expected)
    {
        var valid = BoutiqueWindow.TryParseMaximumEquipLevel(text, out var level, out var normalized);

        Assert.True(valid);
        Assert.Equal(expected, level);
        Assert.Equal(text, normalized);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("0")]
    [InlineData("-1")]
    public void EmptyZeroOrNegativeLevelRestoresAll(string text)
    {
        var valid = BoutiqueWindow.TryParseMaximumEquipLevel(text, out var level, out var normalized);

        Assert.True(valid);
        Assert.Null(level);
        Assert.Empty(normalized);
    }

    [Theory]
    [InlineData("sixty")]
    [InlineData("60x")]
    public void InvalidLevelIsRejectedWithoutProducingAFilter(string text)
    {
        var valid = BoutiqueWindow.TryParseMaximumEquipLevel(text, out var level, out var normalized);

        Assert.False(valid);
        Assert.Null(level);
        Assert.Equal(text, normalized);
    }

    [Fact]
    public void ValuesBeyondTheLuminaByteFieldClampSafely()
    {
        var valid = BoutiqueWindow.TryParseMaximumEquipLevel("999", out var level, out var normalized);

        Assert.True(valid);
        Assert.Equal(byte.MaxValue, level);
        Assert.Equal("255", normalized);
    }
}
