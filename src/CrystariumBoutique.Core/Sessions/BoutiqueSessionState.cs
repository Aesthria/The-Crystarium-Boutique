namespace CrystariumBoutique.Core.Sessions;

public enum BoutiqueSessionState
{
    Closed,
    Initializing,
    WaitingForCharacter,
    Active,
    Restoring,
    Committing,
    DependencyUnavailable,
    RestoreFailed,
    Suspended,
}
