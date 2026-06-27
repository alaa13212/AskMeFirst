using AskMeFirst.Core.Models;

namespace AskMeFirst.Core.Abstractions;

public interface IBrowserInventory
{
    IReadOnlyList<Browser> Discover();

    Browser? FindById(string id);
}
