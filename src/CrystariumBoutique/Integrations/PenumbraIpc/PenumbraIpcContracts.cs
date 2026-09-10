using CrystariumBoutique.Core.Integrations;

namespace CrystariumBoutique.Integrations.PenumbraIpc;

// Wire-contract facts verified against the locally installed Penumbra 1.7.1.1
// public API (breaking/feature version 5.19). These are Boutique-owned
// interoperability types and do not require Penumbra.Api or Luna at runtime.
internal enum PenumbraIpcRedrawType
{
    Redraw = 0,
    AfterGpose = 1,
}

internal readonly record struct PenumbraIpcAvailability(
    DependencyStatus Status,
    int? BreakingVersion,
    int? FeatureVersion,
    string? Detail = null)
{
    public string? Version => BreakingVersion.HasValue && FeatureVersion.HasValue
        ? $"{BreakingVersion.Value}.{FeatureVersion.Value}"
        : null;
}
