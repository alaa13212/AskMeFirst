namespace AskMeFirst.Core.Routing;

public sealed record RoutingContext
{
    public required Uri Url { get; init; }

    public required string Host { get; init; }

    public required string HostPath { get; init; }

    public required DateTimeOffset Now { get; init; }

    public string? ExplicitBrowserId { get; init; }

    public string? ExplicitProfileId { get; init; }

    public static RoutingContext Create(
        Uri url,
        DateTimeOffset now,
        string? explicitBrowserId = null,
        string? explicitProfileId = null)
    {
        string host = url.Host;
        string path = url.AbsolutePath;
        string hostPath = path == "/" ? host : host + path;
        return new RoutingContext
        {
            Url = url,
            Host = host,
            HostPath = hostPath,
            Now = now,
            ExplicitBrowserId = explicitBrowserId,
            ExplicitProfileId = explicitProfileId,
        };
    }
}
