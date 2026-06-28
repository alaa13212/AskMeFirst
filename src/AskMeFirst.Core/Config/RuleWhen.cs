namespace AskMeFirst.Core.Config;

public sealed record RuleWhen
{
    public IReadOnlyList<string>? ProcessIn { get; init; }

    public IReadOnlyList<string>? UrlMatchesAny { get; init; }

    public IReadOnlyList<string>? UrlMatchesAll { get; init; }

    public string? UrlRegex { get; init; }

    public IReadOnlyList<string>? SchemeIn { get; init; }

    public string? TimeBetween { get; init; }

    public IReadOnlyList<string>? WeekdayIn { get; init; }

    public bool? BrowserRunning { get; init; }
}