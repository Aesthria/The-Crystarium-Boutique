using CrystariumBoutique.Core.Catalog;

namespace CrystariumBoutique.Core.Tests.Catalog;

public sealed class ItemAvailabilitySupplementTests
{
    [Fact]
    public void PackagedSupplementUsesUniqueExactItemIds()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "item-availability-supplement.json");

        var supplement = ItemAvailabilitySupplement.Load(path);

        Assert.Equal(20, supplement.Records.Count);
        Assert.Equal(2, supplement.ExactGroups.Length);
        Assert.Equal(1473, supplement.AllItemIds.Count);
        Assert.All(supplement.Records, pair => Assert.Equal(pair.Key, pair.Value.ItemId));
        Assert.All(supplement.Records.Values, record => Assert.True(record.Excluded));
        Assert.Contains(13879u, supplement.Records.Keys);
        Assert.Contains(374u, supplement.AllItemIds);
        Assert.Contains(887u, supplement.AllItemIds);
        Assert.Contains(366u, supplement.AllItemIds);
        Assert.DoesNotContain(2995u, supplement.AllItemIds);
        Assert.DoesNotContain(3306u, supplement.AllItemIds);
        Assert.DoesNotContain(15132u, supplement.AllItemIds);
        Assert.DoesNotContain(1601u, supplement.AllItemIds);
    }

    [Fact]
    public void WeatheredExclusionsRemainExactAndRetainCurrentItems()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "item-availability-supplement.json");
        var supplement = ItemAvailabilitySupplement.Load(path);
        var weathered = Assert.Single(
            supplement.ExactGroups,
            group => group.Category == "Legacy 1.x Weathered Starter Equipment");

        Assert.Equal(47, weathered.ItemIds.Count);
        Assert.Contains(366u, weathered.ItemIds);
        Assert.DoesNotContain(2995u, supplement.AllItemIds);
        Assert.DoesNotContain(3306u, supplement.AllItemIds);
        Assert.DoesNotContain(15132u, supplement.AllItemIds);
    }

    [Fact]
    public void AllCurrentLuminaDatedEquipmentIdsAreExplicitlyExcluded()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "item-availability-supplement.json");
        var supplement = ItemAvailabilitySupplement.Load(path);
        var dated = Assert.Single(
            supplement.ExactGroups,
            group => group.Category == "Legacy 1.x Dated Equipment");

        Assert.Equal(1406, dated.ItemIds.Count);
        Assert.Contains(887u, dated.ItemIds);
        Assert.Contains(1521u, dated.ItemIds);
        Assert.Contains(1600u, dated.ItemIds);
        Assert.Contains(101u, supplement.Records.Keys);
        Assert.Contains(944u, supplement.Records.Keys);
    }
}
