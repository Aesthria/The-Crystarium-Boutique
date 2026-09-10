namespace CrystariumBoutique.Core.Errors;

public enum BoutiqueErrorCode
{
    DependencyUnavailable,
    UnsupportedApiVersion,
    PlayerUnavailable,
    InvalidGameState,
    InvalidItem,
    InvalidDye,
    AppearanceApplyFailed,
    AppearanceCaptureFailed,
    RestoreFailed,
    GposeTransitionFailed,
    DataLoadFailed,
    ConfigurationCorrupt,
    LoadoutInvalid,
    LoadoutNotFound,
    LoadoutPersistenceFailed,
    ExternalImportFailed,
    UnknownIntegrationFailure,
}
