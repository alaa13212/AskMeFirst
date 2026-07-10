using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using AskMeFirst.Core.Abstractions;

namespace AskMeFirst.Core.Routing;

[SuppressMessage("Design", "CA1001", Justification = "DI singleton; lifetime is the app lifetime.")]
public sealed class HttpUnshortener : IUnshortener
{
    private const string UserAgent = "AskMeFirst/1.0 (Unshortening)";

    private readonly HttpClient client;
    private readonly ILogger logger;

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
            logger.LogWarn(FormattableString.Invariant($"Unshorten failed for {url}: {ex.Message}"));
            return null;
        }
    }
}
