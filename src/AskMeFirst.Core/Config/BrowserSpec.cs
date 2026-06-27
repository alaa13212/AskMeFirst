namespace AskMeFirst.Core.Config;

public sealed record BrowserSpec
{
    public string Id { get; init; } = "";

    public string DisplayName { get; init; } = "";

    public string Executable { get; init; } = "auto";

    public string? Profile { get; init; }
}
