using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Models;
using AskMeConfig = AskMeFirst.Core.Config.Config;

namespace AskMeFirst.Core;

public sealed class UrlRouter
{
    private readonly IBrowserInventory _inventory;
    private readonly IUrlLauncher _launcher;
    private readonly ILogger _logger;
    private readonly AskMeConfig _config;

    public UrlRouter(
        IBrowserInventory inventory,
        IUrlLauncher launcher,
        ILogger logger,
        AskMeConfig config)
    {
        _inventory = inventory;
        _launcher = launcher;
        _logger = logger;
        _config = config;
    }

    public int Route(Uri url, string? browserId)
    {
        IReadOnlyList<Browser> browsers = _inventory.Discover();
        if (browsers.Count == 0)
        {
            _logger.LogError("No browsers discovered on this system.");
            return 2;
        }

        Browser? chosen = ResolveBrowser(browserId, browsers);
        if (chosen is null)
        {
            _logger.LogError(
                $"Browser '{browserId}' not found. " +
                $"Discovered: {string.Join(", ", browsers.Select(b => b.Id))}");
            return 3;
        }

        _logger.LogInfo($"Routing {url} → {chosen.DisplayName} ({chosen.ExecutablePath})");
        _launcher.Launch(chosen, url);
        return 0;
    }

    private Browser? ResolveBrowser(string? browserId, IReadOnlyList<Browser> browsers)
    {
        if (browserId is null or "system")
        {
            _logger.LogInfo("No --browser specified; using first discovered browser.");
            return browsers[0];
        }
        return _inventory.FindById(browserId);
    }
}
