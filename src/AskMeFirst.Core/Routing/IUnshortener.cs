namespace AskMeFirst.Core.Routing;

public interface IUnshortener
{
    Task<string?> ResolveAsync(Uri url, CancellationToken ct);
}