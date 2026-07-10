namespace AskMeFirst.Core.Config;

public sealed record RuleWhen
{
    public IReadOnlyList<string>? UrlMatchesAny { get; init; }

    public IReadOnlyList<string>? UrlMatchesAll { get; init; }

    public string? UrlRegex { get; init; }

    public IReadOnlyList<string>? SchemeIn { get; init; }
}
