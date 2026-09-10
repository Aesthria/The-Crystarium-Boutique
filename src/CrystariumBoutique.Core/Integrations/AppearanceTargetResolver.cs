using CrystariumBoutique.Core.Errors;

namespace CrystariumBoutique.Core.Integrations;

public static class AppearanceTargetResolver
{
    private const int NormalPlayerObjectIndex = 0;

    public static Result<int> Resolve(
        bool isGposing,
        int? gposeTargetObjectIndex,
        bool isLocalPlayerTarget = true,
        int? discoveredLocalPlayerObjectIndex = null)
    {
        if (!isGposing)
        {
            return Result.Success(NormalPlayerObjectIndex);
        }

        var hasSupportedGposeTarget = gposeTargetObjectIndex is > NormalPlayerObjectIndex;
        if (hasSupportedGposeTarget && isLocalPlayerTarget)
        {
            return Result.Success(gposeTargetObjectIndex.GetValueOrDefault());
        }

        if (discoveredLocalPlayerObjectIndex.HasValue)
        {
            return Result.Success(discoveredLocalPlayerObjectIndex.Value);
        }

        if (!hasSupportedGposeTarget)
        {
            return Result.Failure<int>(new BoutiqueError(
                BoutiqueErrorCode.PlayerUnavailable,
                "The Boutique is waiting for your GPose character to become available.",
                "Neither a matching GPose player object nor a supported GPose target was available."));
        }

        return Result.Failure<int>(new BoutiqueError(
            BoutiqueErrorCode.InvalidGameState,
            "The Boutique could not identify your character in GPose yet.",
            "The supported GPose target did not match the logged-in local player and no matching GPose player object was discovered."));
    }

    public static int ResolveRestoreTarget(
        bool boundInGpose,
        bool isCurrentlyGposing,
        int boundObjectIndex)
        => boundInGpose && !isCurrentlyGposing
            ? NormalPlayerObjectIndex
            : boundObjectIndex;

    public static bool MatchesLocalPlayer(
        string? targetName,
        string? localPlayerName,
        uint? targetHomeWorldId,
        uint? localPlayerHomeWorldId)
    {
        if (string.IsNullOrWhiteSpace(targetName)
            || string.IsNullOrWhiteSpace(localPlayerName)
            || !string.Equals(targetName, localPlayerName, StringComparison.Ordinal))
        {
            return false;
        }

        return !targetHomeWorldId.HasValue
            || !localPlayerHomeWorldId.HasValue
            || targetHomeWorldId.Value == 0
            || localPlayerHomeWorldId.Value == 0
            || targetHomeWorldId.Value == localPlayerHomeWorldId.Value;
    }
}
