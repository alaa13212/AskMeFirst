namespace AskMeFirst.Core.Inventory;

public sealed record CachedInventory(
    DateTimeOffset GeneratedAt,
    IReadOnlyList<CachedBrowserDto> Browsers);
