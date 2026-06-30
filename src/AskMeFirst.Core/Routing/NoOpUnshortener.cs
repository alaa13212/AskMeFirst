namespace AskMeFirst.Core.Routing;

public sealed class NoOpUnshortener : IUnshortener
{
    public Task<string?> ResolveAsync(Uri url, CancellationToken ct) => Task.FromResult<string?>(null);
}