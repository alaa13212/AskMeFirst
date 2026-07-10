using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Models;

namespace AskMeFirst.Core.Inventory;

public sealed class CachingBrowserInventory(
    IBrowserInventory inner,
    IDiscoveryCache cache) : IBrowserInventory
{
    private readonly object gate = new();
    private IReadOnlyList<Browser>? snapshot;

    public IReadOnlyList<Browser> Discover()
    {
        if (snapshot is not null)
        {
            return snapshot;
        }
        lock (gate)
        {
            if (snapshot is not null)
            {
                return snapshot;
            }
            IReadOnlyList<Browser>? cached = cache.TryRead();
            if (cached is not null)
            {
                snapshot = cached;
                return snapshot;
            }
            IReadOnlyList<Browser> fresh = inner.Discover();
            cache.Write(fresh);
            snapshot = fresh;
            return snapshot;
        }
    }

    public Browser? FindById(string id)
    {
        return Discover().FirstOrDefault(b => string.Equals(b.Id, id, StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<Browser> Refresh()
    {
        lock (gate)
        {
            IReadOnlyList<Browser> fresh = inner.Refresh();
            cache.Write(fresh);
            snapshot = fresh;
            return snapshot;
        }
    }
}
