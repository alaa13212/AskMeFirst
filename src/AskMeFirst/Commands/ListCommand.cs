using AskMeFirst.Core.Commands;
using AskMeFirst.Core.Models;

namespace AskMeFirst.Commands;

public sealed class ListCommand : ICommand
{
    public string Name => "--list";
    public string Usage => "--list";
    public string Description => "List discovered browsers and their profiles.";

    public int Execute(string[] args, CommandContext ctx)
    {
        IReadOnlyList<Browser> browsers = ctx.Inventory.Discover();
        if (browsers.Count == 0)
        {
            Console.WriteLine("No browsers discovered on this system.");
            return 0;
        }

        Console.WriteLine($"Discovered {browsers.Count} browser(s):");
        foreach (Browser b in browsers)
        {
            Console.WriteLine($"  {b.Id,-12} {b.DisplayName,-24} {b.ExecutablePath}");
            foreach (BrowserProfile profile in ctx.Profiles.Detect(b.Id))
            {
                string marker = profile.IsDefault ? "*" : " ";
                Console.WriteLine(
                    $"      {marker} {profile.DirectoryName,-20} {profile.Name}");
            }
        }
        return 0;
    }
}