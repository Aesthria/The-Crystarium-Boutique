using System.Collections.Immutable;
using System.Globalization;
using CrystariumBoutique.Core.Appearance;
using CrystariumBoutique.Core.Errors;
using CrystariumBoutique.Core.Loadouts;
using CrystariumBoutique.Core.Sessions;

namespace CrystariumBoutique.Core.Tests.Loadouts;

public sealed class LoadoutExchangeCodecTests
{
    [Fact]
    public void SharedDesignRoundTripsAllPortableFields()
    {
        var timestamp = DateTimeOffset.Parse(
            "2026-08-26T12:00:00Z",
            CultureInfo.InvariantCulture);
        var source = new BoutiqueLoadout(
            LoadoutSchema.CurrentVersion,
            Guid.NewGuid(),
            "Sky Pirate",
            timestamp,
            timestamp,
            [new LoadoutEquipmentState(
                EquipmentSlot.Head,
                new AppearanceSelection(new AppearanceId(44001), 44001, [new StainId(12), new StainId(34)]),
                2,
                "Sky Pirate's Cap",
                1234)],
            ["pirate", "blue"],
            "Shared locally.");

        var payload = LoadoutExchangeCodec.Encode(source);
        var result = LoadoutExchangeCodec.Decode(payload);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(source.Name, result.Value.Name);
        var equipment = Assert.Single(result.Value.Equipment);
        var sourceEquipment = Assert.Single(source.Equipment);
        Assert.Equal(sourceEquipment.Slot, equipment.Slot);
        Assert.Equal(sourceEquipment.Appearance.AppearanceId, equipment.Appearance.AppearanceId);
        Assert.Equal(sourceEquipment.Appearance.SourceItemId, equipment.Appearance.SourceItemId);
        Assert.Equal(
            sourceEquipment.Appearance.Stains.Select(stain => stain.Value),
            equipment.Appearance.Stains.Select(stain => stain.Value));
        Assert.Equal(sourceEquipment.DyeChannelCount, equipment.DyeChannelCount);
        Assert.Equal(sourceEquipment.DisplayName, equipment.DisplayName);
        Assert.Equal(sourceEquipment.IconId, equipment.IconId);
        Assert.Equal(source.Tags.AsEnumerable(), result.Value.Tags.AsEnumerable());
        Assert.Equal(source.Notes, result.Value.Notes);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-design")]
    [InlineData("TCB-DESIGN-1:not-base64")]
    public void InvalidSharedTextFailsWithoutCreatingARecord(string payload)
    {
        var result = LoadoutExchangeCodec.Decode(payload);

        Assert.False(result.IsSuccess);
        Assert.Equal(BoutiqueErrorCode.LoadoutInvalid, result.Error?.Code);
    }
}
