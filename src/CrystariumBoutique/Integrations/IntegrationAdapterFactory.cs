namespace CrystariumBoutique.Integrations;

internal static class IntegrationAdapterFactory
{
    public static TService Create<TClient, TService>(
        Func<TClient> createClient,
        Func<TClient, TService> createService)
        where TClient : IDisposable
    {
        ArgumentNullException.ThrowIfNull(createClient);
        ArgumentNullException.ThrowIfNull(createService);

        var client = createClient();
        try
        {
            return createService(client);
        }
        catch
        {
            client.Dispose();
            throw;
        }
    }
}
