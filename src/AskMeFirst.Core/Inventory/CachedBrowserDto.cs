namespace AskMeFirst.Core.Inventory;

public sealed record CachedBrowserDto(
    string Id,
    string DisplayName,
    string ExecutablePath,
    string? IconName,
    string? FlatpakAppId);
