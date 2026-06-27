namespace AskMeFirst.Core.Config;

public sealed record AppConfig
{
    public Settings Settings { get; init; } = new();

    public IReadOnlyList<BrowserSpec> Browsers { get; init; } = [];
}