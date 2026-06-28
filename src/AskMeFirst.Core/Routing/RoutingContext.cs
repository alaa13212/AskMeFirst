namespace AskMeFirst.Core.Routing;

public sealed record RoutingContext
{
    public required Uri Url { get; init; }

    public required string Host { get; init; }

    public required string HostPath { get; init; }

    public string? SourceProcess { get; init; }

    public required DateTimeOffset Now { get; init; }

    public bool IsTargetBrowserRunning { get; init; }

    public string? ExplicitBrowserId { get; init; }

    public string? ExplicitProfileId { get; init; }

    public static RoutingContext Create(
        Uri url,
        string? sourceProcess,
        DateTimeOffset now,
        bool isRunning = false,
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
            SourceProcess = sourceProcess,
            Now = now,
            IsTargetBrowserRunning = isRunning,
            ExplicitBrowserId = explicitBrowserId,
            ExplicitProfileId = explicitProfileId,
        };
    }
}