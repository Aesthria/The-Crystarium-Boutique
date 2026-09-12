namespace CrystariumBoutique.Integrations;

internal sealed class DependencyRecoveryCoordinator
{
    internal static readonly TimeSpan DefaultProbeInterval = TimeSpan.FromMilliseconds(750);

    private readonly IDependencyAvailabilityRefresher? appearanceDependency;
    private readonly IDependencyAvailabilityRefresher? penumbraDependency;
    private readonly Func<DateTimeOffset> utcNow;
    private readonly TimeSpan probeInterval;
    private DateTimeOffset nextProbeAt = DateTimeOffset.MinValue;

    public DependencyRecoveryCoordinator(
        IDependencyAvailabilityRefresher? appearanceDependency,
        IDependencyAvailabilityRefresher? penumbraDependency)
        : this(appearanceDependency, penumbraDependency, () => DateTimeOffset.UtcNow, DefaultProbeInterval)
    {
    }

    internal DependencyRecoveryCoordinator(
        IDependencyAvailabilityRefresher? appearanceDependency,
        IDependencyAvailabilityRefresher? penumbraDependency,
        Func<DateTimeOffset> utcNow,
        TimeSpan probeInterval)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(probeInterval, TimeSpan.Zero);
        this.appearanceDependency = appearanceDependency;
        this.penumbraDependency = penumbraDependency;
        this.utcNow = utcNow ?? throw new ArgumentNullException(nameof(utcNow));
        this.probeInterval = probeInterval;
    }

    public bool RefreshForOpen()
    {
        var changed = Refresh(appearanceDependency, onlyWhenUnavailable: false)
            | Refresh(penumbraDependency, onlyWhenUnavailable: false);
        nextProbeAt = utcNow() + probeInterval;
        return changed;
    }

    public bool ProbeIfDue()
    {
        if (!NeedsProbe(appearanceDependency) && !NeedsProbe(penumbraDependency))
        {
            return false;
        }

        var now = utcNow();
        if (now < nextProbeAt)
        {
            return false;
        }

        nextProbeAt = now + probeInterval;
        return Refresh(appearanceDependency, onlyWhenUnavailable: true)
            | Refresh(penumbraDependency, onlyWhenUnavailable: true);
    }

    private static bool NeedsProbe(IDependencyAvailabilityRefresher? dependency)
        => dependency is not null && !dependency.IsAvailable;

    private static bool Refresh(
        IDependencyAvailabilityRefresher? dependency,
        bool onlyWhenUnavailable)
        => dependency is not null
            && (!onlyWhenUnavailable || !dependency.IsAvailable)
            && dependency.RefreshAvailability();
}
