using System.Text.Json;
using System.Text.Json.Serialization;

namespace AskMeFirst.Core.Config;

public sealed record Settings
{
    public string? DefaultBrowserId { get; init; }

    public bool StripTracking { get; init; } = true;

    public bool Unshorten { get; init; }

    public int InventoryCacheHours { get; init; } = 24;

    public int UnshortenTimeoutMs { get; init; } = 1000;
}

public sealed record BrowserSpec
{
    public string Id { get; init; } = "";

    public string DisplayName { get; init; } = "";

    public string Executable { get; init; } = "auto";

    public string? Profile { get; init; }
}

public sealed record Config
{
    public Settings Settings { get; init; } = new();

    public IReadOnlyList<BrowserSpec> Browsers { get; init; } = [];
}

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    ReadCommentHandling = JsonCommentHandling.Skip,
    AllowTrailingCommas = true,
    PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(Config))]
internal partial class ConfigJsonContext : JsonSerializerContext
{
}
