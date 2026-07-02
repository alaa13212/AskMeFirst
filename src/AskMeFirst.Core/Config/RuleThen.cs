namespace AskMeFirst.Core.Config;

public sealed record RuleThen
{
    public string Browser { get; init; } = "";

    public string? ProfileId { get; init; }

    public bool NewWindow { get; init; }

    public bool Private { get; init; }

    public bool StripTracking { get; init; } = true;

    public bool Unshorten { get; init; }
}