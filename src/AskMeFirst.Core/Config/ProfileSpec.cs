namespace AskMeFirst.Core.Config;

public sealed record ProfileSpec
{
    public required string Id { get; init; }

    public required string BrowserId { get; init; }

    public string? Name { get; init; }

    public string? Directory { get; init; }

    public string? DisplayName { get; init; }
}