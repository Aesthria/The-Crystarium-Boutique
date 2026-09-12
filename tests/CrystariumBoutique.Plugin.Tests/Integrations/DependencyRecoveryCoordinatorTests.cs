using System.Text.Json;
using CrystariumBoutique.Integrations;

namespace CrystariumBoutiquePlugin.Tests.Integrations;

public sealed class DependencyRecoveryCoordinatorTests
{
    private static readonly TimeSpan Interval = TimeSpan.FromMilliseconds(750);

    [Fact]
    public void AvailableDependenciesAreNotPeriodicallyProbed()
    {
        var glamourer = new FakeDependency(isAvailable: true, providerAvailable: true);
        var penumbra = new FakeDependency(isAvailable: true, providerAvailable: true);
        var now = DateTimeOffset.UnixEpoch;
        var coordinator = Create(glamourer, penumbra, () => now);

        Assert.False(coordinator.ProbeIfDue());
        now += TimeSpan.FromMinutes(1);
        Assert.False(coordinator.ProbeIfDue());
        Assert.Equal(0, glamourer.RefreshCount);
        Assert.Equal(0, penumbra.RefreshCount);
    }

    [Fact]
    public void UnavailableDependenciesAreProbedImmediatelyThenThrottled()
    {
        var glamourer = new FakeDependency(isAvailable: false, providerAvailable: false);
        var now = DateTimeOffset.UnixEpoch;
        var coordinator = Create(glamourer, null, () => now);

        Assert.False(coordinator.ProbeIfDue());
        Assert.Equal(1, glamourer.RefreshCount);

        now += Interval - TimeSpan.FromMilliseconds(1);
        Assert.False(coordinator.ProbeIfDue());
        Assert.Equal(1, glamourer.RefreshCount);

        now += TimeSpan.FromMilliseconds(1);
        Assert.False(coordinator.ProbeIfDue());
        Assert.Equal(2, glamourer.RefreshCount);
    }

    [Fact]
    public void MissedInitializedEventIsRecoveredByPeriodicProbe()
    {
        var glamourer = new FakeDependency(isAvailable: false, providerAvailable: false);
        var now = DateTimeOffset.UnixEpoch;
        var coordinator = Create(glamourer, null, () => now);
        coordinator.ProbeIfDue();

        glamourer.ProviderAvailable = true;
        now += Interval;

        Assert.True(coordinator.ProbeIfDue());
        Assert.True(glamourer.IsAvailable);
        Assert.Equal(1, glamourer.TransitionCount);
    }

    [Fact]
    public void ImmediateOpenRefreshDoesNotWaitForPeriodicThrottle()
    {
        var glamourer = new FakeDependency(isAvailable: false, providerAvailable: false);
        var now = DateTimeOffset.UnixEpoch;
        var coordinator = Create(glamourer, null, () => now);
        coordinator.ProbeIfDue();
        glamourer.ProviderAvailable = true;

        Assert.True(coordinator.RefreshForOpen());
        Assert.True(glamourer.IsAvailable);
        Assert.Equal(2, glamourer.RefreshCount);
    }

    [Fact]
    public void PenumbraLateAvailabilityIsRecoveredIndependently()
    {
        var glamourer = new FakeDependency(isAvailable: true, providerAvailable: true);
        var penumbra = new FakeDependency(isAvailable: false, providerAvailable: true);
        var coordinator = Create(glamourer, penumbra, () => DateTimeOffset.UnixEpoch);

        Assert.True(coordinator.ProbeIfDue());
        Assert.Equal(0, glamourer.RefreshCount);
        Assert.Equal(1, penumbra.RefreshCount);
        Assert.True(penumbra.IsAvailable);
    }

    [Fact]
    public void AvailableDisposedAvailableCycleRecoversWithoutDuplicateTransitions()
    {
        var glamourer = new FakeDependency(isAvailable: true, providerAvailable: true);
        var now = DateTimeOffset.UnixEpoch;
        var coordinator = Create(glamourer, null, () => now);

        glamourer.SetUnavailable();
        glamourer.ProviderAvailable = false;
        Assert.False(coordinator.ProbeIfDue());

        glamourer.ProviderAvailable = true;
        now += Interval;
        Assert.True(coordinator.ProbeIfDue());
        Assert.True(glamourer.IsAvailable);

        now += Interval;
        Assert.False(coordinator.ProbeIfDue());
        Assert.Equal(1, glamourer.TransitionCount);
    }

    [Fact]
    public void RepeatedUnavailableProbesDoNotReportStateChanges()
    {
        var glamourer = new FakeDependency(isAvailable: false, providerAvailable: false);
        var now = DateTimeOffset.UnixEpoch;
        var coordinator = Create(glamourer, null, () => now);

        for (var index = 0; index < 4; index++)
        {
            Assert.False(coordinator.ProbeIfDue());
            now += Interval;
        }

        Assert.Equal(4, glamourer.RefreshCount);
        Assert.Equal(0, glamourer.TransitionCount);
    }

    [Fact]
    public void RepeatedOpenRefreshesDoNotReportDuplicateStateChanges()
    {
        var glamourer = new FakeDependency(isAvailable: true, providerAvailable: true);
        var coordinator = Create(glamourer, null, () => DateTimeOffset.UnixEpoch);

        Assert.False(coordinator.RefreshForOpen());
        Assert.False(coordinator.RefreshForOpen());
        Assert.Equal(2, glamourer.RefreshCount);
        Assert.Equal(0, glamourer.TransitionCount);
    }

    [Fact]
    public void PluginOpenRefreshPrecedesSessionOpenAndDrawProbePrecedesTransitionHandling()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "src",
            "CrystariumBoutique",
            "Plugin.cs"));
        var openStart = source.IndexOf("private void OpenMainUi()", StringComparison.Ordinal);
        var drawStart = source.IndexOf("private void DrawUi()", StringComparison.Ordinal);
        var openRefresh = source.IndexOf("dependencyRecovery.RefreshForOpen();", openStart, StringComparison.Ordinal);
        var sessionOpen = source.IndexOf("sessionController.Open();", openStart, StringComparison.Ordinal);
        var drawProbe = source.IndexOf("dependencyRecovery.ProbeIfDue();", drawStart, StringComparison.Ordinal);
        var transition = source.IndexOf("HandleAppearanceDependencyTransition();", drawProbe, StringComparison.Ordinal);

        Assert.True(openStart >= 0);
        Assert.True(openRefresh > openStart);
        Assert.True(sessionOpen > openRefresh);
        Assert.True(drawProbe > drawStart);
        Assert.True(transition > drawProbe);
    }

    [Fact]
    public void DistributedAndRepositoryManifestsUseModestNegativeLoadPriority()
    {
        var root = FindRepositoryRoot();
        using var repositoryManifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "repo.json")));
        using var distributedManifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root,
            "src",
            "CrystariumBoutique",
            "CrystariumBoutique.json")));

        Assert.Equal(-100, repositoryManifest.RootElement[0].GetProperty("LoadPriority").GetInt32());
        Assert.Equal(-100, distributedManifest.RootElement.GetProperty("LoadPriority").GetInt32());
    }

    private static DependencyRecoveryCoordinator Create(
        IDependencyAvailabilityRefresher? appearance,
        IDependencyAvailabilityRefresher? penumbra,
        Func<DateTimeOffset> utcNow)
        => new(appearance, penumbra, utcNow, Interval);

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "repo.json")))
            {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException("Could not locate the Crystarium Boutique repository root.");
    }

    private sealed class FakeDependency(bool isAvailable, bool providerAvailable)
        : IDependencyAvailabilityRefresher
    {
        public bool IsAvailable { get; private set; } = isAvailable;

        public bool ProviderAvailable { get; set; } = providerAvailable;

        public int RefreshCount { get; private set; }

        public int TransitionCount { get; private set; }

        public bool RefreshAvailability()
        {
            RefreshCount++;
            if (IsAvailable == ProviderAvailable)
            {
                return false;
            }

            IsAvailable = ProviderAvailable;
            TransitionCount++;
            return true;
        }

        public void SetUnavailable()
            => IsAvailable = false;
    }
}
