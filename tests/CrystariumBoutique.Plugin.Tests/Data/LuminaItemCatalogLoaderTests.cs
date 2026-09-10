using CrystariumBoutique.Core.Catalog;
using CrystariumBoutique.Data;

namespace CrystariumBoutiquePlugin.Tests.Data;

public sealed class LuminaItemCatalogLoaderTests
{
    [Fact]
    public void CurrentGeneratedSchemaMapsBeastmasterToFirstUnnamedJobColumn()
    {
        var classJobs = new[]
        {
            new LuminaItemCatalogLoader.ClassJobDefinition(1, "GLA", false),
            new LuminaItemCatalogLoader.ClassJobDefinition(36, "BLU", true),
            new LuminaItemCatalogLoader.ClassJobDefinition(42, "PCT", false),
            new LuminaItemCatalogLoader.ClassJobDefinition(43, "BST", true),
        };

        var properties = LuminaItemCatalogLoader.MapClassJobCategoryProperties(classJobs);

        Assert.Equal("GLA", properties[1].Name);
        Assert.Equal("BLU", properties[36].Name);
        Assert.Equal("PCT", properties[42].Name);
        Assert.Equal("Unknown0", properties[43].Name);
    }

    [Fact]
    public void LimitedRoleIsAddedToSharedJobEquipmentWithoutRemovingStandardRole()
    {
        var classJobs = new[]
        {
            new LuminaItemCatalogLoader.ClassJobDefinition(20, "MNK", false),
            new LuminaItemCatalogLoader.ClassJobDefinition(43, "BST", true),
        };
        var eligibility = default(ClassJobMask).Add(20).Add(43);

        var roles = LuminaItemCatalogLoader.IncludeLimitedRole(
            EquipmentRoles.MeleeDps,
            eligibility,
            classJobs);

        Assert.True(roles.HasFlag(EquipmentRoles.MeleeDps));
        Assert.True(roles.HasFlag(EquipmentRoles.Limited));
    }

    [Fact]
    public void LimitedRoleIsNotAddedWithoutEligibleLimitedJob()
    {
        var classJobs = new[]
        {
            new LuminaItemCatalogLoader.ClassJobDefinition(20, "MNK", false),
            new LuminaItemCatalogLoader.ClassJobDefinition(43, "BST", true),
        };

        var roles = LuminaItemCatalogLoader.IncludeLimitedRole(
            EquipmentRoles.MeleeDps,
            default(ClassJobMask).Add(20),
            classJobs);

        Assert.Equal(EquipmentRoles.MeleeDps, roles);
    }

    [Fact]
    public void BlueMageRemainsDataDrivenLimitedJob()
    {
        var classJobs = new[]
        {
            new LuminaItemCatalogLoader.ClassJobDefinition(25, "BLM", false),
            new LuminaItemCatalogLoader.ClassJobDefinition(36, "BLU", true),
        };

        var roles = LuminaItemCatalogLoader.IncludeLimitedRole(
            EquipmentRoles.None,
            default(ClassJobMask).Add(36),
            classJobs);

        Assert.Equal(EquipmentRoles.Limited, roles);
    }
}
