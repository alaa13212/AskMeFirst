using System.Globalization;
using AskMeFirst.Core.Abstractions;

namespace AskMeFirst.Core.Routing;

public sealed class UnshortenTaskBuilder(
    IUnshortener unshortener,
    IShortenerDomainList shortenerDomains,
    TrackingStripper stripper,
    ILogger logger) : IUnshortenTaskBuilder
{
    public Task<string?>? Build(Uri url)
    {
        if (string.IsNullOrEmpty(url.Host) || !shortenerDomains.IsKnown(url.Host))
        {
            return null;
        }
        return ResolveAndStripAsync(url, CancellationToken.None);
    }

    private async Task<string?> ResolveAndStripAsync(Uri url, CancellationToken ct)
    {
        try
        {
            string? resolved = await unshortener.ResolveAsync(url, ct).ConfigureAwait(false);
            if (resolved is null)
            {
                return null;
            }
            if (!Uri.TryCreate(resolved, UriKind.Absolute, out Uri? resolvedUri))
            {
                return null;
            }
            return stripper.Strip(resolvedUri).ToString();
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (Exception ex)
        {
            logger.LogWarn(FormattableString.Invariant($"Unshorten failed for {url}: {ex.Message}"));
            return null;
        }
    }
}
