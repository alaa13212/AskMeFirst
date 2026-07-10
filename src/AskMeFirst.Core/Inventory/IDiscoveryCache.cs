using AskMeFirst.Core.Models;

namespace AskMeFirst.Core.Inventory;

public interface IDiscoveryCache
{
    IReadOnlyList<Browser>? TryRead();

    void Write(IReadOnlyList<Browser> browsers);

    DateTimeOffset? LastGenerated { get; }
}
