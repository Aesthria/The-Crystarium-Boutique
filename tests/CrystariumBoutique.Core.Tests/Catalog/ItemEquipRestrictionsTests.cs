using CrystariumBoutique.Core.Catalog;

namespace CrystariumBoutique.Core.Tests.Catalog;

public sealed class ItemEquipRestrictionsTests
{
    [Fact]
    public void DefaultRestrictionsAllowEveryKnownCharacter()
    {
        var result = default(ItemEquipRestrictions).Evaluate(new CharacterEquipContext(
            CharacterRaceMask.Viera,
            CharacterSexMask.Female,
            3));

        Assert.Equal(ItemEquipRestrictionReason.None, result);
    }

    [Fact]
    public void EvaluateReturnsEveryMismatchedRestriction()
    {
        var restrictions = new ItemEquipRestrictions(
            CharacterRaceMask.Hyur | CharacterRaceMask.Elezen,
            CharacterSexMask.Male,
            2);

        var result = restrictions.Evaluate(new CharacterEquipContext(
            CharacterRaceMask.Lalafell,
            CharacterSexMask.Female,
            1));

        Assert.Equal(
            ItemEquipRestrictionReason.Race
                | ItemEquipRestrictionReason.Sex
                | ItemEquipRestrictionReason.GrandCompany,
            result);
    }

    [Fact]
    public void EvaluateAllowsMatchingRaceSexAndGrandCompany()
    {
        var restrictions = new ItemEquipRestrictions(
            CharacterRaceMask.Miqote,
            CharacterSexMask.Female,
            3);

        var result = restrictions.Evaluate(new CharacterEquipContext(
            CharacterRaceMask.Miqote,
            CharacterSexMask.Female,
            3));

        Assert.Equal(ItemEquipRestrictionReason.None, result);
    }
}
