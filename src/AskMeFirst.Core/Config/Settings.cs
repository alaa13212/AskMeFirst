namespace AskMeFirst.Core.Config;

public sealed record Settings
{
    public bool StripTracking { get; init; } = true;

    public bool Unshorten { get; init; }

    public TimeSpan InventoryCacheTtl { get; init; } = TimeSpan.FromHours(24);

    public TimeSpan UnshortenTimeout { get; init; } = TimeSpan.FromMilliseconds(1000);
}