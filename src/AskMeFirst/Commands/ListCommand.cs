using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Commands;
using AskMeFirst.Core.Models;
using Microsoft.Extensions.DependencyInjection;

namespace AskMeFirst.Commands;

public sealed class ListCommand : ICommand
{
    public string Name => "--list";
    public string Usage => "--list";
    public string Description => "List discovered browsers and their profiles.";

    public Task<int> Execute(string[] args, CommandContext ctx)
    {
        IBrowserInventory inventory = ctx.Services.GetRequiredService<IBrowserInventory>();
        IBrowserProfileDetector profiles = ctx.Services.GetRequiredService<IBrowserProfileDetector>();
        IReadOnlyList<Browser> browsers = inventory.Discover();
        if (browsers.Count == 0)
        {
            Console.WriteLine("No browsers discovered on this system.");
            return Task.FromResult(0);
        }

        Console.WriteLine($"Discovered {browsers.Count} browser(s):");
        foreach (Browser b in browsers)
        {
            Console.WriteLine($"  {b.Id,-12} {b.DisplayName,-24} {b.ExecutablePath}");
            foreach (BrowserProfile profile in profiles.Detect(b))
            {
                string marker = profile.IsDefault ? "*" : " ";
                Console.WriteLine(
                    $"      {marker} {profile.DirectoryName,-20} {profile.Name}");
            }
        }
        return Task.FromResult(0);
    }
}