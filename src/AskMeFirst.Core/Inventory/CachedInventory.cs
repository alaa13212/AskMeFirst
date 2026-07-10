using AskMeFirst.Core.Models;

namespace AskMeFirst.Core.Inventory;

public sealed record CachedBrowserDto(
    string Id,
    string DisplayName,
    string ExecutablePath,
    string? IconName,
    string? FlatpakAppId);

public sealed record CachedInventory(
    int Version,
    DateTimeOffset GeneratedAt,
    string Platform,
    IReadOnlyList<CachedBrowserDto> Browsers);
