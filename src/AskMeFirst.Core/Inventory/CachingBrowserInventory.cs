using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Models;

namespace AskMeFirst.Core.Inventory;

public sealed class CachingBrowserInventory(
    IBrowserInventory inner,
    IDiscoveryCache cache) : IBrowserInventory
{
    private IReadOnlyList<Browser>? snapshot;

    public IReadOnlyList<Browser> Discover()
    {
        if (snapshot is not null)
        {
            return snapshot;
        }
        IReadOnlyList<Browser>? cached = cache.TryRead();
        if (cached is not null)
        {
            snapshot = cached;
        }
        else
        {
            snapshot = inner.Discover();
            cache.Write(snapshot);
        }
        return snapshot;
    }

    public Browser? FindById(string id)
    {
        return Discover().FirstOrDefault(b => string.Equals(b.Id, id, StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<Browser> Refresh()
    {
        snapshot = inner.Refresh();
        cache.Write(snapshot);
        return snapshot;
    }
}
