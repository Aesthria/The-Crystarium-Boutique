using System.Net;
using System.Net.Http.Headers;
using System.Text;
using CrystariumBoutique.Core.Catalog;
using CrystariumBoutique.Core.Dyes;
using CrystariumBoutique.Core.Errors;
using CrystariumBoutique.Core.ExternalDesigns;
using CrystariumBoutique.Core.Loadouts;

namespace CrystariumBoutique.Integrations;

public sealed class EorzeaCollectionImportService : IDisposable
{
    private const int MaximumResponseBytes = 2 * 1024 * 1024;
    private const string BrowserUserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 "
        + "(KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36";

    private readonly IItemRepository items;
    private readonly IStainRepository stains;
    private readonly HttpClient httpClient;

    public EorzeaCollectionImportService(IItemRepository items, IStainRepository stains)
    {
        this.items = items ?? throw new ArgumentNullException(nameof(items));
        this.stains = stains ?? throw new ArgumentNullException(nameof(stains));
        httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(20),
        };
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(BrowserUserAgent);
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<Result<BoutiqueLoadout>> ImportAsync(
        string userFacingUrl,
        CancellationToken cancellationToken = default)
    {
        var uriResult = EorzeaCollectionImport.ConvertToApiUri(userFacingUrl);
        if (!uriResult.IsSuccess || uriResult.Value is null)
        {
            return Result.Failure<BoutiqueLoadout>(uriResult.Error!);
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, uriResult.Value);
            using var response = await httpClient.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken)
                .ConfigureAwait(false);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                return Failure(
                    "Eorzea Collection did not return the requested glamour.",
                    $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}");
            }

            if (response.Content.Headers.ContentLength is > MaximumResponseBytes)
            {
                return Failure(
                    "The Eorzea Collection response was unexpectedly large.",
                    $"ContentLength={response.Content.Headers.ContentLength}");
            }

            await using var source = await response.Content.ReadAsStreamAsync(cancellationToken)
                .ConfigureAwait(false);
            using var destination = new MemoryStream();
            var buffer = new byte[16 * 1024];
            while (true)
            {
                var read = await source.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
                if (read == 0)
                {
                    break;
                }

                if (destination.Length + read > MaximumResponseBytes)
                {
                    return Failure(
                        "The Eorzea Collection response was unexpectedly large.",
                        $"Response exceeded {MaximumResponseBytes} bytes.");
                }

                await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken)
                    .ConfigureAwait(false);
            }

            var parseResult = EorzeaCollectionImport.Parse(
                Encoding.UTF8.GetString(destination.GetBuffer(), 0, checked((int)destination.Length)));
            if (!parseResult.IsSuccess || parseResult.Value is null)
            {
                return Result.Failure<BoutiqueLoadout>(parseResult.Error!);
            }

            return EorzeaCollectionImport.Resolve(
                parseResult.Value,
                items,
                stains,
                userFacingUrl.Trim());
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            return Failure("Eorzea Collection did not respond before the import timed out.", exception.Message);
        }
        catch (HttpRequestException exception)
        {
            return Failure("The Eorzea Collection glamour could not be downloaded.", exception.Message);
        }
        catch (Exception exception)
        {
            return Failure(
                "The Eorzea Collection glamour could not be imported.",
                $"{exception.GetType().Name}: {exception.Message}");
        }
    }

    public void Dispose()
        => httpClient.Dispose();

    private static Result<BoutiqueLoadout> Failure(string message, string technicalContext)
        => Result.Failure<BoutiqueLoadout>(new BoutiqueError(
            BoutiqueErrorCode.ExternalImportFailed,
            message,
            technicalContext));
}
