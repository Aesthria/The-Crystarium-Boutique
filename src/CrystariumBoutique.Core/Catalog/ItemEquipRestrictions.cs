namespace CrystariumBoutique.Core.Catalog;

[Flags]
public enum CharacterRaceMask : ushort
{
    None = 0,
    Hyur = 1 << 0,
    Elezen = 1 << 1,
    Lalafell = 1 << 2,
    Miqote = 1 << 3,
    Roegadyn = 1 << 4,
    AuRa = 1 << 5,
    Hrothgar = 1 << 6,
    Viera = 1 << 7,
    All = Hyur | Elezen | Lalafell | Miqote | Roegadyn | AuRa | Hrothgar | Viera,
}

[Flags]
public enum CharacterSexMask : byte
{
    None = 0,
    Male = 1 << 0,
    Female = 1 << 1,
    All = Male | Female,
}

[Flags]
public enum ItemEquipRestrictionReason : byte
{
    None = 0,
    Race = 1 << 0,
    Sex = 1 << 1,
    GrandCompany = 1 << 2,
}

public readonly record struct CharacterEquipContext(
    CharacterRaceMask Race,
    CharacterSexMask Sex,
    uint GrandCompanyId);

public readonly record struct ItemEquipRestrictions(
    CharacterRaceMask Races,
    CharacterSexMask Sexes,
    uint GrandCompanyId)
{
    public ItemEquipRestrictionReason Evaluate(CharacterEquipContext character)
    {
        var result = ItemEquipRestrictionReason.None;
        if (Races != CharacterRaceMask.None
            && character.Race != CharacterRaceMask.None
            && (Races & character.Race) == 0)
        {
            result |= ItemEquipRestrictionReason.Race;
        }

        if (Sexes != CharacterSexMask.None
            && character.Sex != CharacterSexMask.None
            && (Sexes & character.Sex) == 0)
        {
            result |= ItemEquipRestrictionReason.Sex;
        }

        if (GrandCompanyId != 0 && GrandCompanyId != character.GrandCompanyId)
        {
            result |= ItemEquipRestrictionReason.GrandCompany;
        }

        return result;
    }
}
