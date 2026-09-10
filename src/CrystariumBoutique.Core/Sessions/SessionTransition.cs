namespace CrystariumBoutique.Core.Sessions;

public sealed record SessionTransition(
    BoutiqueSessionState From,
    BoutiqueSessionState To,
    DateTimeOffset Timestamp,
    string Reason);
