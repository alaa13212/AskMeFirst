namespace AskMeFirst.Core.Inventory;

public sealed record CachedInventory(
    int Version,
    DateTimeOffset GeneratedAt,
    IReadOnlyList<CachedBrowserDto> Browsers);
