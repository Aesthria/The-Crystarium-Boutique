using CrystariumBoutique.Core.Catalog;

namespace CrystariumBoutique.Core.Tests.Catalog;

public sealed class ItemMarketabilityResolverTests
{
    [Theory]
    [InlineData(42u, false, true)]
    [InlineData(0u, false, false)]
    [InlineData(42u, true, false)]
    public void RequiresASearchCategoryAndTradableItem(
        uint searchCategoryId,
        bool isUntradable,
        bool expected)
        => Assert.Equal(
            expected,
            ItemMarketabilityResolver.IsMarketable(searchCategoryId, isUntradable));
}
