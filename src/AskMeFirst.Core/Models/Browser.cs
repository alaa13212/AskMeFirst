using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Launch;

namespace AskMeFirst.Core.Models;

public sealed record Browser
{
    public required string Id { get; init; }

    public required string DisplayName { get; init; }

    public required string ExecutablePath { get; init; }

    public string? IconName { get; init; }

    public IBrowserLaunchStrategy LaunchStrategy { get; init; } = DefaultLaunchStrategy.Instance;

    public BrowserProfile? Profile { get; init; }

    public bool NewWindow { get; init; }

    public string? FlatpakAppId { get; init; }
}