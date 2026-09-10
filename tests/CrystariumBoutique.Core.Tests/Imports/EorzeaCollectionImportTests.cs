using System.Collections.Immutable;
using CrystariumBoutique.Core.Appearance;
using CrystariumBoutique.Core.Catalog;
using CrystariumBoutique.Core.Dyes;
using CrystariumBoutique.Core.ExternalDesigns;

namespace CrystariumBoutique.Core.Tests.ExternalDesigns;

public sealed class EorzeaCollectionImportTests
{
    [Fact]
    public void PublicGlamourUrlConvertsToTheRestrictedApiEndpoint()
    {
        var result = EorzeaCollectionImport.ConvertToApiUri(
            "https://ffxiv.eorzeacollection.com/glamour/346278/071326");

        Assert.True(result.IsSuccess);
        Assert.Equal(
            "https://ffxiv.eorzeacollection.com/api/glamour/346278",
            result.Value!.AbsoluteUri);
        Assert.False(EorzeaCollectionImport.ConvertToApiUri(
            "http://ffxiv.eorzeacollection.com/glamour/346278/071326").IsSuccess);
        Assert.False(EorzeaCollectionImport.ConvertToApiUri(
            "https://example.com/glamour/346278/071326").IsSuccess);
        Assert.False(EorzeaCollectionImport.ConvertToApiUri(
            "https://ffxiv.eorzeacollection.com/api/glamour/346278").IsSuccess);
    }

    [Fact]
    public void ApiResponseParsesSupportedSlotsAndDyeSlugs()
    {
        const string json = """
            {
              "id": 346278,
              "name": "071326",
              "gear": {
                "head": { "name": "Scion Hearer's Hood", "dyes": "soot-black,ruby-red" },
                "body": { "name": "Neo Citizen's Hooded Tunic", "dyes": "ruby-red,pure-white" },
                "weapon": null,
                "fashion": { "name": "Ignored", "dyes": "none" }
              }
            }
            """;

        var result = EorzeaCollectionImport.Parse(json);

        Assert.True(result.IsSuccess);
        Assert.Equal(346278, result.Value!.Id);
        Assert.Equal("071326", result.Value.Name);
        Assert.Equal(2, result.Value.Equipment.Length);
        var head = result.Value.Equipment.Single(item => item.Slot == EquipmentSlot.Head);
        Assert.Equal("Scion Hearer's Hood", head.ItemName);
        Assert.Equal(["soot-black", "ruby-red"], head.DyeNames);
    }

    [Fact]
    public void ResolverAddsLinkedOffHandAndEmperorItemsForEveryOmittedVisibleSlot()
    {
        var weapon = Item(
            50000,
            "Test Katana",
            [EquipmentSlot.MainHand, EquipmentSlot.OffHand],
            dyeChannels: 2,
            modelSub: 9);
        var body = Item(50001, "Neo Citizen's Hooded Tunic", [EquipmentSlot.Body], dyeChannels: 2);
        var catalog = new EquipmentCatalog(
        [
            weapon,
            body,
            Item(10032, "The Emperor's New Hat", [EquipmentSlot.Head], emptyAppearance: true),
            Item(10033, "The Emperor's New Robe", [EquipmentSlot.Body], emptyAppearance: true),
            Item(10034, "The Emperor's New Gloves", [EquipmentSlot.Hands], emptyAppearance: true),
            Item(10035, "The Emperor's New Breeches", [EquipmentSlot.Legs], emptyAppearance: true),
            Item(10036, "The Emperor's New Boots", [EquipmentSlot.Feet], emptyAppearance: true),
            Item(9293, "The Emperor's New Earrings", [EquipmentSlot.Ears], emptyAppearance: true),
            Item(9292, "The Emperor's New Necklace", [EquipmentSlot.Neck], emptyAppearance: true),
            Item(9294, "The Emperor's New Bracelet", [EquipmentSlot.Wrists], emptyAppearance: true),
            Item(9295, "The Emperor's New Ring", [EquipmentSlot.RightRing, EquipmentSlot.LeftRing], emptyAppearance: true),
        ]);
        var stains = new StainCatalog(
        [
            new StainDefinition(new StainId(17), "Ruby Red", default, StainGroup.Red, 1, false),
            new StainDefinition(new StainId(2), "Pure White", default, StainGroup.Neutral, 1, false),
        ]);
        var source = new EorzeaCollectionDesign(
            346278,
            "Source Name",
            [
                new EorzeaCollectionEquipment(
                    EquipmentSlot.MainHand,
                    "Test Katana",
                    ["ruby-red", string.Empty]),
                new EorzeaCollectionEquipment(
                    EquipmentSlot.Body,
                    "Neo Citizen's Hooded Tunic",
                    ["ruby-red", "pure-white"]),
            ]);

        var result = EorzeaCollectionImport.Resolve(
            source,
            catalog,
            stains,
            "https://ffxiv.eorzeacollection.com/glamour/346278/071326");

        Assert.True(result.IsSuccess, result.Error?.ToString());
        Assert.Equal(EorzeaCollectionImport.DefaultDesignName, result.Value!.Name);
        Assert.Equal(12, result.Value.Equipment.Length);
        var offHand = result.Value.Equipment.Single(item => item.Slot == EquipmentSlot.OffHand);
        Assert.Equal(50000u, offHand.Appearance.SourceItemId);
        var importedBody = result.Value.Equipment.Single(item => item.Slot == EquipmentSlot.Body);
        Assert.Equal([new StainId(17), new StainId(2)], importedBody.Appearance.Stains);
        Assert.Equal(
            10032u,
            result.Value.Equipment.Single(item => item.Slot == EquipmentSlot.Head).Appearance.SourceItemId);
        Assert.Equal(
            10032u,
            result.Value.Equipment.Single(item => item.Slot == EquipmentSlot.Head).IconId);
        Assert.Equal(
            9295u,
            result.Value.Equipment.Single(item => item.Slot == EquipmentSlot.LeftRing).Appearance.SourceItemId);
    }

    [Fact]
    public void ResolverCanApplyKnownEmperorIdsWhenInvisibleRowsAreAbsentFromTheBrowserCatalog()
    {
        var body = Item(50001, "Test Coat", [EquipmentSlot.Body]);
        var catalog = new EquipmentCatalog([body]);
        var source = new EorzeaCollectionDesign(
            7,
            "Sparse Outfit",
            [new EorzeaCollectionEquipment(EquipmentSlot.Body, "Test Coat", [])]);

        var result = EorzeaCollectionImport.Resolve(
            source,
            catalog,
            StainCatalog.Empty,
            "https://ffxiv.eorzeacollection.com/glamour/7/sparse-outfit");

        Assert.True(result.IsSuccess, result.Error?.ToString());
        Assert.Equal(10, result.Value!.Equipment.Length);
        var head = result.Value.Equipment.Single(item => item.Slot == EquipmentSlot.Head);
        Assert.Equal(10032u, head.Appearance.SourceItemId);
        Assert.Equal("The Emperor's New Hat", head.DisplayName);
    }

    private static EquipmentItem Item(
        uint itemId,
        string name,
        ImmutableArray<EquipmentSlot> slots,
        byte dyeChannels = 0,
        ulong modelSub = 0,
        bool emptyAppearance = false)
        => new(
            itemId,
            name,
            "Equipment",
            1,
            1,
            itemId,
            dyeChannels,
            emptyAppearance ? default : new AppearanceKey(itemId, modelSub),
            ContentGroupResolver.Resolve(1),
            slots);
}
