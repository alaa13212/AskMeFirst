using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Commands;
using AskMeFirst.Core.Models;

namespace AskMeFirst.Commands;

public sealed class RefreshCommand : ICommand
{
    public string Name => "refresh";
    public string Usage => "refresh";
    public string Description => "Re-scan installed browsers and rewrite the discovery cache.";

    public Task<int> Execute(string[] args, CommandContext ctx)
    {
        IBrowserInventory inventory = ctx.Resolve<IBrowserInventory>();
        IReadOnlyList<Browser> browsers = inventory.Refresh();
        int total = 0;
        foreach (Browser _ in browsers)
        {
            total++;
        }
        Console.WriteLine($"Cache refreshed: {total} browser(s).");
        return Task.FromResult(0);
    }
}
