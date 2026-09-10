using CrystariumBoutique.Core.Integrations;
using CrystariumBoutique.Integrations.PenumbraIpc;

namespace CrystariumBoutiquePlugin.Tests.Integrations;

public sealed class PenumbraIpcClientTests
{
    [Fact]
    public void MissingProviderIsUnavailable()
    {
        using var client = new PenumbraIpcClient(new FakeTransport
        {
            HasVersionFunction = false,
        });

        var availability = client.DetectAvailability();

        Assert.Equal(DependencyStatus.Unavailable, availability.Status);
        Assert.Null(availability.Version);
    }

    [Theory]
    [InlineData(5, 0, DependencyStatus.Available)]
    [InlineData(5, 18, DependencyStatus.Available)]
    [InlineData(4, 99, DependencyStatus.Incompatible)]
    [InlineData(6, 0, DependencyStatus.Incompatible)]
    public void BreakingVersionPolicyIsFailClosed(
        int breakingVersion,
        int featureVersion,
        DependencyStatus expected)
    {
        using var client = new PenumbraIpcClient(new FakeTransport
        {
            Version = (breakingVersion, featureVersion),
        });

        var availability = client.DetectAvailability();

        Assert.Equal(expected, availability.Status);
        Assert.Equal($"{breakingVersion}.{featureVersion}", availability.Version);
    }

    [Fact]
    public void MissingRedrawEndpointIsIncompatible()
    {
        using var client = new PenumbraIpcClient(new FakeTransport
        {
            HasRedrawFunction = false,
        });

        Assert.Equal(DependencyStatus.Incompatible, client.DetectAvailability().Status);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void RedrawPreservesObjectIndexAndVerifiedWireValue(
        int expectedWireValue)
    {
        var transport = new FakeTransport();
        using var client = new PenumbraIpcClient(transport);

        client.RedrawObject(214, (PenumbraIpcRedrawType)expectedWireValue);

        Assert.Equal((214, expectedWireValue), transport.Redraw);
    }

    [Fact]
    public void LifecycleEventsAreForwardedAndUnsubscribed()
    {
        var transport = new FakeTransport();
        var client = new PenumbraIpcClient(transport);
        var initialized = 0;
        var disposed = 0;
        client.ProviderInitialized += () => initialized++;
        client.ProviderDisposed += () => disposed++;

        transport.RaiseInitialized();
        transport.RaiseDisposed();
        client.Dispose();
        transport.RaiseInitialized();
        transport.RaiseDisposed();

        Assert.Equal(1, initialized);
        Assert.Equal(1, disposed);
        Assert.True(transport.WasDisposed);
    }

    [Fact]
    public void PluginAssemblyHasNoPenumbraApiOrLunaReference()
    {
        var references = typeof(PenumbraIpcClient).Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.DoesNotContain("Penumbra.Api", references);
        Assert.DoesNotContain("Luna", references);
    }

    private sealed class FakeTransport : PenumbraIpcClient.IPenumbraIpcTransport
    {
        public event Action? Initialized;

        public event Action? Disposed;

        public bool HasVersionFunction { get; init; } = true;

        public bool HasRedrawFunction { get; init; } = true;

        public (int BreakingVersion, int FeatureVersion) Version { get; init; } = (5, 18);

        public (int ObjectIndex, int RedrawType)? Redraw { get; private set; }

        public bool WasDisposed { get; private set; }

        public (int BreakingVersion, int FeatureVersion) InvokeVersion()
            => Version;

        public void RedrawObject(int objectIndex, int redrawType)
            => Redraw = (objectIndex, redrawType);

        public void Dispose()
            => WasDisposed = true;

        public void RaiseInitialized()
            => Initialized?.Invoke();

        public void RaiseDisposed()
            => Disposed?.Invoke();
    }
}
