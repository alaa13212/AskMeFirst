using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Models;

namespace AskMeFirst.MgmtUi.Services;

public sealed record DetectedBrowser(
    Browser Browser,
    IReadOnlyList<BrowserProfile> Profiles,
    DateTimeOffset DiscoveredAt);

public interface IBrowserDiscovery
{
    IReadOnlyList<DetectedBrowser> Browsers { get; }

    DateTimeOffset? LastRefreshedAt { get; }

    void Refresh();

    bool IsConfigured(Browser browser);
}

public sealed class NullBrowserDiscovery : IBrowserDiscovery
{
    public static readonly NullBrowserDiscovery Instance = new();

    private NullBrowserDiscovery()
    {
    }

    public IReadOnlyList<DetectedBrowser> Browsers => [];

    public DateTimeOffset? LastRefreshedAt => null;

    public void Refresh()
    {
    }

    public bool IsConfigured(Browser browser) => false;
}

public sealed class BrowserDiscoveryAdapter : IBrowserDiscovery
{
    private readonly IBrowserInventory _inventory;
    private readonly IBrowserProfileDetector? _profiles;
    private readonly ILogger _logger;

    public BrowserDiscoveryAdapter(IBrowserInventory inventory, IBrowserProfileDetector? profiles, ILogger logger)
    {
        _inventory = inventory;
        _profiles = profiles;
        _logger = logger;
    }

    public IReadOnlyList<DetectedBrowser> Browsers { get; private set; } = [];

    public DateTimeOffset? LastRefreshedAt { get; private set; }

    public void Refresh()
    {
        try
        {
            IReadOnlyList<Browser> browsers = _inventory.Refresh();
            List<DetectedBrowser> list = [];
            foreach (Browser browser in browsers)
            {
                IReadOnlyList<BrowserProfile> profiles = _profiles?.Detect(browser) ?? [];
                list.Add(new DetectedBrowser(browser, profiles, DateTimeOffset.Now));
            }
            Browsers = list;
            LastRefreshedAt = DateTimeOffset.Now;
            _logger.LogInfo($"Discovered {list.Count} browser(s).");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Browser discovery failed: {ex.Message}");
        }
    }

    public bool IsConfigured(Browser browser)
    {
        return !string.IsNullOrEmpty(browser.Id);
    }
}
