using System.Collections.Immutable;
using CrystariumBoutique.Core.Appearance;
using CrystariumBoutique.Core.Catalog;

namespace CrystariumBoutique.Core.Tests.Catalog;

public sealed class CatalogFilteringTests
{
    [Fact]
    public void SearchNormalizationFoldsCaseDiacriticsPunctuationAndWhitespace()
    {
        var normalized = CatalogSearch.Normalize("  ÉTOILE's—Hat  ");

        Assert.Equal("etoile s hat", normalized);
    }

    [Fact]
    public void SearchMatchesAllTermsInAnyOrder()
    {
        var catalog = new EquipmentCatalog(
        [
            CreateItem(1, "Augmented Hellhound Cane", equipLevel: 90),
            CreateItem(2, "Unrelated Staff", equipLevel: 90),
        ]);

        var page = catalog.GetPage(
            EquipmentSlot.Head,
            "ew",
            0,
            Filter(search: "cane augmented"));

        Assert.Equal("Augmented Hellhound Cane", Assert.Single(page.Items).PrimaryItem.Name);
    }

    [Fact]
    public void SearchCanPromoteAMatchingSourceWithoutLosingAppearanceIdentity()
    {
        var appearance = new AppearanceKey(42, 7);
        var catalog = new EquipmentCatalog(
        [
            CreateItem(1, "Alpha Hood", appearanceKey: appearance),
            CreateItem(2, "Crème Chapeau", appearanceKey: appearance),
        ]);

        var page = catalog.GetPage(
            EquipmentSlot.Head,
            "arr",
            0,
            Filter(search: "creme"));

        var entry = Assert.Single(page.Items);
        Assert.Equal(appearance, entry.AppearanceKey);
        Assert.Equal("Crème Chapeau", entry.PrimaryItem.Name);
        Assert.Equal(2, entry.SourceItems.Length);
    }

    [Fact]
    public void LevelItemLevelAndDyeFiltersApplyToTheSameSourceItem()
    {
        var catalog = new EquipmentCatalog(
        [
            CreateItem(1, "Low", equipLevel: 20, itemLevel: 30, dyeChannels: 2),
            CreateItem(2, "One Dye", equipLevel: 50, itemLevel: 100, dyeChannels: 1),
            CreateItem(3, "Match", equipLevel: 60, itemLevel: 200, dyeChannels: 2),
            CreateItem(4, "High", equipLevel: 90, itemLevel: 700, dyeChannels: 2),
        ]);

        var page = catalog.GetPage(
            EquipmentSlot.Head,
            "hw",
            0,
            new CatalogFilter(string.Empty, 55, 65, 150, 250, DyeSupportFilter.TwoChannels));

        Assert.Equal("Match", Assert.Single(page.Items).PrimaryItem.Name);
    }

    [Fact]
    public void MaximumEquipLevelUsesAnInclusiveRequiredLevelUpperBound()
    {
        var catalog = new EquipmentCatalog(
        [
            CreateItem(1, "Level 1", equipLevel: 1),
            CreateItem(2, "Level 30", equipLevel: 30),
            CreateItem(3, "Level 60", equipLevel: 60),
            CreateItem(4, "Level 61", equipLevel: 61),
            CreateItem(5, "Level 100", equipLevel: 100),
        ]);

        var page = catalog.GetPage(
            EquipmentSlot.Head,
            SpecialContentGroups.AllExpansions.Key,
            0,
            CatalogFilter.Default with { MaximumEquipLevel = 60 });

        Assert.Equal([1u, 2u, 3u], page.Items.Select(entry => entry.PrimaryItem.ItemId).Order());
    }

    [Fact]
    public void MaximumEquipLevelSeventyRejectsOnlyHigherRequiredLevels()
    {
        var catalog = new EquipmentCatalog(
        [
            CreateItem(1, "Level 70", equipLevel: 70),
            CreateItem(2, "Level 71", equipLevel: 71),
        ]);

        var page = catalog.GetPage(
            EquipmentSlot.Head,
            SpecialContentGroups.AllExpansions.Key,
            0,
            CatalogFilter.Default with { MaximumEquipLevel = 70 });

        Assert.Equal(1u, Assert.Single(page.Items).PrimaryItem.ItemId);
    }

    [Fact]
    public void MaximumEquipLevelComposesWithExpansionRoleDyeAndSearch()
    {
        var catalog = new EquipmentCatalog(
        [
            CreateItem(1, "Target Coat", equipLevel: 80, dyeChannels: 2, roles: EquipmentRoles.Tank),
            CreateItem(2, "Target Coat Too High", equipLevel: 90, dyeChannels: 2, roles: EquipmentRoles.Tank),
            CreateItem(3, "Target Coat Wrong Role", equipLevel: 80, dyeChannels: 2, roles: EquipmentRoles.Healer),
            CreateItem(4, "Target Coat Wrong Dye", equipLevel: 80, dyeChannels: 1, roles: EquipmentRoles.Tank),
            CreateItem(5, "Unrelated Coat", equipLevel: 80, dyeChannels: 2, roles: EquipmentRoles.Tank),
        ]);

        var page = catalog.GetPage(
            EquipmentSlot.Head,
            "shb",
            0,
            CatalogFilter.Default with
            {
                SearchText = "target",
                MaximumEquipLevel = 80,
                DyeSupport = DyeSupportFilter.TwoChannels,
                JobRole = JobRoleFilter.Tank,
            });

        Assert.Equal(1u, Assert.Single(page.Items).PrimaryItem.ItemId);
    }

    [Theory]
    [InlineData(DyeSupportFilter.None, 1u)]
    [InlineData(DyeSupportFilter.SingleChannel, 2u)]
    [InlineData(DyeSupportFilter.TwoChannels, 3u)]
    public void DyeSlotFiltersMatchExactChannelCounts(DyeSupportFilter filter, uint expectedItemId)
    {
        var catalog = new EquipmentCatalog(
        [
            CreateItem(1, "No Dye", dyeChannels: 0),
            CreateItem(2, "One Dye", dyeChannels: 1),
            CreateItem(3, "Two Dyes", dyeChannels: 2),
        ]);

        var page = catalog.GetPage(
            EquipmentSlot.Head,
            "arr",
            0,
            new CatalogFilter(string.Empty, null, null, null, null, filter));

        Assert.Equal(expectedItemId, Assert.Single(page.Items).PrimaryItem.ItemId);
    }

    [Fact]
    public void FilteringPreservesTwentyFourItemPagination()
    {
        var items = Enumerable.Range(1, 52)
            .Select(index => CreateItem(
                (uint)index,
                index % 2 == 0 ? $"Keep {index}" : $"Skip {index}"));
        var catalog = new EquipmentCatalog(items);
        var filter = Filter(search: "keep");

        var first = catalog.GetPage(EquipmentSlot.Head, "arr", 0, filter);
        var second = catalog.GetPage(EquipmentSlot.Head, "arr", 1, filter);

        Assert.Equal(24, first.Items.Length);
        Assert.Equal(2, second.Items.Length);
        Assert.Equal(26, first.TotalResults);
        Assert.Equal(2, first.TotalPages);
    }

    [Theory]
    [InlineData(JobRoleFilter.Tank, 1u)]
    [InlineData(JobRoleFilter.Healer, 2u)]
    [InlineData(JobRoleFilter.MagicalRangedDps, 3u)]
    [InlineData(JobRoleFilter.Crafter, 4u)]
    [InlineData(JobRoleFilter.Limited, 5u)]
    public void JobRoleFilterMatchesVerifiedRoleFlags(JobRoleFilter filter, uint expectedItemId)
    {
        var catalog = new EquipmentCatalog(
        [
            CreateItem(1, "Tank", roles: EquipmentRoles.Tank),
            CreateItem(2, "Healer", roles: EquipmentRoles.Healer),
            CreateItem(3, "Caster", roles: EquipmentRoles.MagicalRangedDps),
            CreateItem(4, "Crafter", roles: EquipmentRoles.Crafter),
            CreateItem(5, "Limited", roles: EquipmentRoles.Limited),
        ]);

        var page = catalog.GetPage(
            EquipmentSlot.Head,
            "arr",
            0,
            new CatalogFilter(string.Empty, null, null, null, null, DyeSupportFilter.Any, filter));

        Assert.Equal(expectedItemId, Assert.Single(page.Items).PrimaryItem.ItemId);
    }

    [Theory]
    [InlineData(JobRoleFilter.MeleeDps)]
    [InlineData(JobRoleFilter.Limited)]
    public void SharedBeastmasterArmorAppearsInEveryCompatibleRole(JobRoleFilter role)
    {
        var catalog = new EquipmentCatalog(
        [
            CreateItem(
                50745,
                "Beastmaster's Coat +1",
                roles: EquipmentRoles.MeleeDps | EquipmentRoles.Limited,
                classJobs: default(ClassJobMask).Add(20).Add(43)),
        ]);

        var page = catalog.GetPage(
            EquipmentSlot.Head,
            "arr",
            0,
            CatalogFilter.Default with { JobRole = role });

        Assert.Equal(50745u, Assert.Single(page.Items).PrimaryItem.ItemId);
    }

    [Theory]
    [InlineData(JobRoleFilter.MagicalRangedDps)]
    [InlineData(JobRoleFilter.Limited)]
    public void SharedBlueMageCasterArmorAppearsInEveryCompatibleRole(JobRoleFilter role)
    {
        var catalog = new EquipmentCatalog(
        [
            CreateItem(
                40000,
                "Shared Caster Coat",
                roles: EquipmentRoles.MagicalRangedDps | EquipmentRoles.Limited,
                classJobs: default(ClassJobMask).Add(25).Add(36)),
        ]);

        var page = catalog.GetPage(
            EquipmentSlot.Head,
            "arr",
            0,
            CatalogFilter.Default with { JobRole = role });

        Assert.Equal(40000u, Assert.Single(page.Items).PrimaryItem.ItemId);
    }

    [Fact]
    public void RepeatedFilteredQueriesUseTheBoundedResultCache()
    {
        var catalog = new EquipmentCatalog(
            Enumerable.Range(1, 1000)
                .Select(index => CreateItem((uint)index, $"Indexed Item {index}")));
        var filter = Filter(search: "indexed 99");

        var first = catalog.GetPage(EquipmentSlot.Head, "arr", 0, filter);
        var second = catalog.GetPage(EquipmentSlot.Head, "arr", 0, filter);

        Assert.False(first.UsedCachedFilter);
        Assert.True(second.UsedCachedFilter);
        Assert.True(second.QueryDuration < TimeSpan.FromMilliseconds(50));
    }

    [Fact]
    public void ClassJobMaskSupportsBothBitRangesAndRejectsUnknownIds()
    {
        var mask = default(ClassJobMask)
            .Add(0)
            .Add(19)
            .Add(70)
            .Add(128);

        Assert.True(mask.Contains(19));
        Assert.True(mask.Contains(70));
        Assert.False(mask.Contains(0));
        Assert.False(mask.Contains(20));
        Assert.False(mask.Contains(128));
    }

    [Fact]
    public void ActiveClassJobHidesIncompatibleWeapons()
    {
        var catalog = new EquipmentCatalog(
        [
            CreateItem(
                1,
                "Paladin Sword",
                slot: EquipmentSlot.MainHand,
                classJobs: default(ClassJobMask).Add(19)),
            CreateItem(
                2,
                "Warrior Axe",
                slot: EquipmentSlot.MainHand,
                classJobs: default(ClassJobMask).Add(21)),
        ]);

        var page = catalog.GetPage(
            EquipmentSlot.MainHand,
            "arr",
            0,
            CatalogFilter.Default with { ActiveClassJobId = 19 });

        Assert.Equal("Paladin Sword", Assert.Single(page.Items).PrimaryItem.Name);
    }

    [Theory]
    [InlineData(EquipmentSlot.MainHand, 50749u)]
    [InlineData(EquipmentSlot.OffHand, 50750u)]
    public void ActiveBeastmasterCanBrowseCompatibleWeaponsAndShields(
        EquipmentSlot slot,
        uint expectedItemId)
    {
        const uint beastmasterClassJobId = 43;
        var catalog = new EquipmentCatalog(
        [
            CreateItem(
                expectedItemId,
                slot == EquipmentSlot.MainHand ? "Beastmaster's Hand Axe +1" : "Beastmaster's Hoplon +1",
                slot: slot,
                roles: EquipmentRoles.Limited,
                classJobs: default(ClassJobMask).Add(beastmasterClassJobId)),
            CreateItem(
                expectedItemId + 1000,
                "Incompatible Weapon",
                slot: slot,
                classJobs: default(ClassJobMask).Add(19)),
        ]);

        var page = catalog.GetPage(
            slot,
            "arr",
            0,
            CatalogFilter.Default with
            {
                ActiveClassJobId = beastmasterClassJobId,
                JobRole = JobRoleFilter.Limited,
            });

        Assert.Equal(expectedItemId, Assert.Single(page.Items).PrimaryItem.ItemId);
    }

    [Theory]
    [InlineData(EquipmentSlot.MainHand, 50749u, 51736u)]
    [InlineData(EquipmentSlot.OffHand, 50750u, 51737u)]
    public void StarterAndNonStarterBeastmasterEquipmentUseTheSameBrowseRules(
        EquipmentSlot slot,
        uint starterItemId,
        uint nonStarterItemId)
    {
        const uint beastmasterClassJobId = 43;
        var beastmaster = default(ClassJobMask).Add(beastmasterClassJobId);
        var catalog = new EquipmentCatalog(
        [
            CreateItem(
                starterItemId,
                "Beastmaster Starter",
                slot: slot,
                appearanceKey: new AppearanceKey(starterItemId, 0),
                roles: EquipmentRoles.Limited,
                classJobs: beastmaster),
            CreateItem(
                nonStarterItemId,
                "Beastmaster Current",
                slot: slot,
                appearanceKey: new AppearanceKey(nonStarterItemId, 0),
                roles: EquipmentRoles.Limited,
                classJobs: beastmaster),
        ]);

        var page = catalog.GetPage(
            slot,
            "arr",
            0,
            CatalogFilter.Default with
            {
                ActiveClassJobId = beastmasterClassJobId,
                JobRole = JobRoleFilter.Limited,
            });

        Assert.Equal(2, page.TotalResults);
        Assert.Contains(page.Items, entry => entry.PrimaryItem.ItemId == starterItemId);
        Assert.Contains(page.Items, entry => entry.PrimaryItem.ItemId == nonStarterItemId);
    }

    [Fact]
    public void BeastmasterLimitedSearchAndLevelFiltersCompose()
    {
        var beastmaster = default(ClassJobMask).Add(43);
        var catalog = new EquipmentCatalog(
        [
            CreateItem(
                50745,
                "Beastmaster's Furs",
                equipLevel: 50,
                roles: EquipmentRoles.MeleeDps | EquipmentRoles.Limited,
                classJobs: beastmaster),
            CreateItem(
                51745,
                "Future Beastmaster Furs",
                equipLevel: 60,
                appearanceKey: new AppearanceKey(51745, 0),
                roles: EquipmentRoles.MeleeDps | EquipmentRoles.Limited,
                classJobs: beastmaster),
        ]);

        var page = catalog.GetPage(
            EquipmentSlot.Head,
            "arr",
            0,
            CatalogFilter.Default with
            {
                SearchText = "beastmaster furs",
                MaximumEquipLevel = 50,
                JobRole = JobRoleFilter.Limited,
            });

        Assert.Equal(50745u, Assert.Single(page.Items).PrimaryItem.ItemId);
    }

    [Theory]
    [InlineData(JobRoleFilter.Tank)]
    [InlineData(JobRoleFilter.Healer)]
    [InlineData(JobRoleFilter.MeleeDps)]
    [InlineData(JobRoleFilter.PhysicalRangedDps)]
    [InlineData(JobRoleFilter.MagicalRangedDps)]
    public void BeastmasterOnlyEquipmentDoesNotLeakIntoUnrelatedRoles(JobRoleFilter role)
    {
        var catalog = new EquipmentCatalog(
        [
            CreateItem(
                50749,
                "Beastmaster Only",
                roles: EquipmentRoles.Limited,
                classJobs: default(ClassJobMask).Add(43)),
        ]);

        var page = catalog.GetPage(
            EquipmentSlot.Head,
            "arr",
            0,
            CatalogFilter.Default with { JobRole = role });

        Assert.Empty(page.Items);
    }

    [Fact]
    public void ActiveClassJobDoesNotRestrictNonWeaponSlots()
    {
        var catalog = new EquipmentCatalog(
        [
            CreateItem(1, "First Hat"),
            CreateItem(2, "Second Hat"),
        ]);

        var page = catalog.GetPage(
            EquipmentSlot.Head,
            "arr",
            0,
            CatalogFilter.Default with { ActiveClassJobId = 19 });

        Assert.Equal(2, page.TotalResults);
    }

    [Fact]
    public void ExactAvailabilityExclusionHidesOnlyTheMarkedItemFromBrowsing()
    {
        var excluded = CreateItem(10, "Dated Bronze Dagger") with { IsExcludedFromBrowsing = true };
        var similarlyNamed = CreateItem(11, "Replica Dated Bronze Dagger");
        var catalog = new EquipmentCatalog([excluded, similarlyNamed]);

        var page = catalog.GetPage(EquipmentSlot.Head, "arr", 0, CatalogFilter.Default);

        Assert.Equal(11u, Assert.Single(page.Items).PrimaryItem.ItemId);
        Assert.True(catalog.TryGetItem(10, EquipmentSlot.Head, out _));
    }

    [Fact]
    public void ValidCurrentSearchResultAlsoRemainsInNormalBrowsing()
    {
        var current = CreateItem(20, "Current Visual Variant");
        var catalog = new EquipmentCatalog([current]);

        var normal = catalog.GetPage(EquipmentSlot.Head, "arr", 0, CatalogFilter.Default);
        var search = catalog.GetPage(EquipmentSlot.Head, "arr", 0, Filter("visual variant"));

        Assert.Equal(20u, Assert.Single(normal.Items).PrimaryItem.ItemId);
        Assert.Equal(20u, Assert.Single(search.Items).PrimaryItem.ItemId);
    }

    [Fact]
    public void MissingAcquisitionDataDoesNotAffectCatalogEligibility()
    {
        var withoutAcquisition = CreateItem(30, "Legitimate Unsourced Item") with
        {
            AcquisitionSources = ImmutableArray<ItemAcquisitionSource>.Empty,
        };
        var catalog = new EquipmentCatalog([withoutAcquisition]);

        var page = catalog.GetPage(EquipmentSlot.Head, "arr", 0, CatalogFilter.Default);

        Assert.Equal(30u, Assert.Single(page.Items).PrimaryItem.ItemId);
    }

    private static CatalogFilter Filter(string search)
        => new(search, null, null, null, null, DyeSupportFilter.Any);

    private static EquipmentItem CreateItem(
        uint itemId,
        string name,
        byte equipLevel = 50,
        uint itemLevel = 100,
        byte dyeChannels = 1,
        AppearanceKey? appearanceKey = null,
        EquipmentRoles roles = EquipmentRoles.None,
        EquipmentSlot slot = EquipmentSlot.Head,
        ClassJobMask classJobs = default)
    {
        var group = ContentGroupResolver.Resolve(equipLevel);
        return new EquipmentItem(
            itemId,
            name,
            "Test Equipment",
            equipLevel,
            itemLevel,
            itemId,
            dyeChannels,
            appearanceKey ?? new AppearanceKey(itemId, 0),
            group,
            ImmutableArray.Create(slot),
            roles,
            false,
            classJobs);
    }
}
