namespace CrystariumBoutique.Integrations;

internal interface ILocalPlayerGearsetFinalizationSource
{
    event Action? LocalPlayerGearsetFinalized;

    event Action? LocalPlayerGameStateReverted;
}
