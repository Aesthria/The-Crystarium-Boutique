using CrystariumBoutique.Core.Appearance;
using CrystariumBoutique.Ui;

namespace CrystariumBoutiquePlugin.Tests.Ui;

public sealed class BoutiqueInputMappingTests
{
    [Fact]
    public void NormalHeadLeftClickRequestsVisorOn()
        => Assert.True(BoutiqueWindow.ResolveRequestedVisorState(
            EquipmentSlot.Head,
            controlPressed: false));

    [Fact]
    public void ControlHeadLeftClickRequestsVisorOff()
        => Assert.False(BoutiqueWindow.ResolveRequestedVisorState(
            EquipmentSlot.Head,
            controlPressed: true));

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NonHeadLeftClickDoesNotRequestVisorState(bool controlPressed)
        => Assert.Null(BoutiqueWindow.ResolveRequestedVisorState(
            EquipmentSlot.Body,
            controlPressed));
}
