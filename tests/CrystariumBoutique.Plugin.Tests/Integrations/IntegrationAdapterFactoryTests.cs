using CrystariumBoutique.Integrations;

namespace CrystariumBoutiquePlugin.Tests.Integrations;

public sealed class IntegrationAdapterFactoryTests
{
    [Fact]
    public void SuccessfulConstructionTransfersClientOwnershipToService()
    {
        var client = new DisposableClient();

        var service = IntegrationAdapterFactory.Create(
            () => client,
            value => new OwningService(value));

        Assert.Same(client, service.Client);
        Assert.Equal(0, client.DisposeCount);

        service.Dispose();
        Assert.Equal(1, client.DisposeCount);
    }

    [Fact]
    public void FailedConstructionDisposesPartiallyOwnedClientExactlyOnce()
    {
        var client = new DisposableClient();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            IntegrationAdapterFactory.Create<DisposableClient, OwningService>(
                () => client,
                _ => throw new InvalidOperationException("Modeled fatal adapter construction failure.")));

        Assert.Equal("Modeled fatal adapter construction failure.", exception.Message);
        Assert.Equal(1, client.DisposeCount);
    }

    private sealed class OwningService(DisposableClient client) : IDisposable
    {
        public DisposableClient Client { get; } = client;

        public void Dispose() => Client.Dispose();
    }

    private sealed class DisposableClient : IDisposable
    {
        public int DisposeCount { get; private set; }

        public void Dispose() => DisposeCount++;
    }
}
