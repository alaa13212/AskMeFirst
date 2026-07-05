namespace AskMeFirst.Core.Routing;

public sealed record RoutingDecision
{
    public required string BrowserId { get; init; }

    public string? ProfileId { get; init; }

    public bool Private { get; init; }

    public bool StripTracking { get; init; } = true;

    public bool Unshorten { get; init; }
}