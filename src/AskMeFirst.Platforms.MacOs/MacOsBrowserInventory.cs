using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Launch;
using AskMeFirst.Core.Models;

namespace AskMeFirst.Platforms.MacOs;

public sealed class MacOsBrowserInventory : IBrowserInventory
{
    private static readonly (string Id, string DisplayName, string AppName)[] Known =
    [
        ("chrome", "Google Chrome", "Google Chrome"),
        ("firefox", "Mozilla Firefox", "Firefox"),
        ("safari", "Safari", "Safari"),
        ("edge", "Microsoft Edge", "Microsoft Edge"),
        ("brave", "Brave", "Brave Browser"),
        ("opera", "Opera", "Opera"),
        ("vivaldi", "Vivaldi", "Vivaldi"),
        ("arc", "Arc", "Arc"),
    ];

    public IReadOnlyList<Browser> Discover()
    {
        List<Browser> discovered = [];
        foreach ((string id, string displayName, string appName) in Known)
        {
            string appPath = $"/Applications/{appName}.app";
            if (Directory.Exists(appPath))
            {
                discovered.Add(new Browser
                {
                    Id = id,
                    DisplayName = displayName,
                    ExecutablePath = appPath,
                    LaunchStrategy = BrowserLaunchStrategies.For(id),
                });
            }
        }
        return discovered;
    }

    public Browser? FindById(string id)
    {
        return Discover().FirstOrDefault(b =>
            string.Equals(b.Id, id, StringComparison.OrdinalIgnoreCase));
    }
}