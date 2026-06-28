namespace AskMeFirst.Core.Config;

public sealed record AppConfig
{
    public Settings Settings { get; init; } = new();

    public IReadOnlyList<BrowserSpec> Browsers { get; init; } = [];

    public IReadOnlyList<ProfileSpec> Profiles { get; init; } = [];

    public IReadOnlyList<Rule> Rules { get; init; } = [];

    public IReadOnlyList<string> TrackingParamsExtra { get; init; } = [];

    public bool TrackingParamsOverride { get; init; }

    public IReadOnlyList<string> UnshortenDomainsExtra { get; init; } = [];

    public bool UnshortenDomainsOverride { get; init; }
}