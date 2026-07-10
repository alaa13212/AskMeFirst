using AskMeFirst.Core.Abstractions;

namespace AskMeFirst.Core.Routing;

public sealed class HttpUnshortener : IUnshortener, IDisposable
{
    private const string UserAgent = "AskMeFirst/1.0 (Unshortening)";

    private readonly HttpClient client;
    private readonly ILogger logger;
    private bool _disposed;

    public HttpUnshortener(HttpMessageHandler handler, TimeSpan timeout, ILogger logger)
    {
        client = new HttpClient(handler) { Timeout = timeout };
        client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
        this.logger = logger;
    }

    public async Task<string?> ResolveAsync(Uri url, CancellationToken ct)
    {
        try
        {
            using HttpRequestMessage req = new(HttpMethod.Head, url);
            using HttpResponseMessage resp = await client.SendAsync(
                req, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode)
            {
                return null;
            }
            return resp.RequestMessage?.RequestUri?.ToString();
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarn($"Unshorten failed for {url}: {ex.Message}");
            return null;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;
        client.Dispose();
    }
}