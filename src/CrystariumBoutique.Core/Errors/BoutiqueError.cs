namespace CrystariumBoutique.Core.Errors;

public sealed record BoutiqueError(
    BoutiqueErrorCode Code,
    string Message,
    string? TechnicalContext = null,
    bool IsRecoverable = true)
{
    public override string ToString()
        => TechnicalContext is null
            ? $"{Code}: {Message}"
            : $"{Code}: {Message} ({TechnicalContext})";
}
