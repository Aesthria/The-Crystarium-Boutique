using CrystariumBoutique.Core.Errors;

namespace CrystariumBoutique.Core.Loadouts;

public interface ILoadoutStore
{
    Result<LoadoutStoreSnapshot> ReadAll();

    Result Write(BoutiqueLoadout loadout);

    Result Delete(Guid loadoutId);
}
