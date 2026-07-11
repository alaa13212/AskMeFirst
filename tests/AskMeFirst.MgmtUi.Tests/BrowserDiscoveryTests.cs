using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Config;
using AskMeFirst.Core.Models;
using AskMeFirst.MgmtUi.Areas;
using AskMeFirst.MgmtUi.Services;
using AskMeFirst.MgmtUi.ViewModels;
using Xunit;

namespace AskMeFirst.MgmtUi.Tests;

public class BrowserDiscoveryTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _configPath;
    private readonly FakeLogger _logger = new();

    public BrowserDiscoveryTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"askmefirst-discovery-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _configPath = Path.Combine(_tempDir, "config.json");
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    [Fact]
    public void StubBrowserInventory_Refresh_PopulatesAdapterBrowsers()
    {
        IBrowserInventory inventory = new StubInventory(
            MakeBrowser("firefox", "Mozilla Firefox", "/usr/bin/firefox"),
            MakeBrowser("chrome", "Google Chrome", "/usr/bin/chromium"));

        BrowserDiscoveryAdapter adapter = new(inventory, null, _logger);
        adapter.Refresh();

        Assert.Equal(2, adapter.Browsers.Count);
        Assert.Equal("firefox", adapter.Browsers[0].Browser.Id);
        Assert.Equal("Mozilla Firefox", adapter.Browsers[0].Browser.DisplayName);
        Assert.NotNull(adapter.LastRefreshedAt);
    }

    [Fact]
    public void StubBrowserInventory_WithProfileDetector_AttachesProfiles()
    {
        IBrowserInventory inventory = new StubInventory(
            MakeBrowser("firefox", "Firefox", "/ff"));
        BrowserProfile profile = new("Work", "/home/u/.firefox/work", IsDefault: true);
        IBrowserProfileDetector detector = new StubDetector(("firefox", profile));

        BrowserDiscoveryAdapter adapter = new(inventory, detector, _logger);
        adapter.Refresh();

        Assert.Single(adapter.Browsers);
        DetectedBrowser detected = adapter.Browsers[0];
        Assert.Equal("firefox", detected.Browser.Id);
        Assert.Single(detected.Profiles);
        Assert.Equal("Work", detected.Profiles[0].Name);
    }

    [Fact]
    public void BrowserArea_PopulatesDetectedRows_ExcludingConfigured()
    {
        JsonConfigPersister persister = new(_configPath, _logger);
        SeedConfigWith(persister, MakeBrowserSpec("firefox", "Firefox", "/ff"));
        BrowserDiscoveryAdapter discovery = new(
            new StubInventory(
                MakeBrowser("firefox", "Firefox", "/ff"),
                MakeBrowser("chrome", "Chrome", "/chromium")),
            null, _logger);
        discovery.Refresh();

        MgmtUiWindowViewModel window = BuildVm(persister, discovery);
        window.DrillTo(AreaId.Browsers);
        BrowserDetailViewModel detail = (BrowserDetailViewModel) window.ActiveDetail!;

        Assert.Single(detail.Rows);
        Assert.Single(detail.ExtraRows);
        DetectedBrowserRowViewModel extra = (DetectedBrowserRowViewModel) detail.ExtraRows[0];
        Assert.Equal("chrome", extra.Id);
        Assert.True(extra.HasAddButton);
    }

    [Fact]
    public void DetectedBrowser_AddToConfigCommand_PersistsBrowserSpec()
    {
        JsonConfigPersister persister = new(_configPath, _logger);
        BrowserDiscoveryAdapter discovery = new(
            new StubInventory(MakeBrowser("chrome", "Chrome", "/chromium")),
            null, _logger);
        discovery.Refresh();
        MgmtUiWindowViewModel window = BuildVm(persister, discovery);
        window.DrillTo(AreaId.Browsers);
        BrowserDetailViewModel detail = (BrowserDetailViewModel) window.ActiveDetail!;
        DetectedBrowserRowViewModel row = (DetectedBrowserRowViewModel) detail.ExtraRows[0];

        row.AddToConfigCommand.Execute(null);

        Assert.Single(persister.Current.Browsers);
        Assert.Equal("chrome", persister.Current.Browsers[0].Id);
        Assert.Equal("/chromium", persister.Current.Browsers[0].Executable);
    }

    [Fact]
    public void CacheArea_RefreshCommand_RediscoverBrowsers()
    {
        JsonConfigPersister persister = new(_configPath, _logger);
        StubInventory inventory = new(
            MakeBrowser("firefox", "Firefox", "/ff"));
        BrowserDiscoveryAdapter discovery = new(inventory, null, _logger);
        discovery.Refresh();

        MgmtUiWindowViewModel window = BuildVm(persister, discovery);
        window.DrillTo(AreaId.Cache);
        CacheDetailViewModel cache = (CacheDetailViewModel) window.ActiveDetail!;
        Assert.Equal(1, cache.DiscoveredBrowserCount);

        inventory.Add(MakeBrowser("chrome", "Chrome", "/chrome"));
        cache.RefreshCommand.Execute(null);

        Assert.Equal(2, cache.DiscoveredBrowserCount);
    }

    private void SeedConfigWith(JsonConfigPersister persister, params BrowserSpec[] browsers)
    {
        AppConfig seed = new() { Browsers = [.. browsers] };
        ConfigLoader.WriteAtomic(seed, _configPath);
        persister.Reload();
    }

    private static Browser MakeBrowser(string id, string displayName, string executablePath)
    {
        return new Browser { Id = id, DisplayName = displayName, ExecutablePath = executablePath };
    }

    private static BrowserSpec MakeBrowserSpec(string id, string displayName, string executable)
    {
        return new BrowserSpec { Id = id, DisplayName = displayName, Executable = executable };
    }

    private static MgmtUiWindowViewModel BuildVm(IConfigPersister persister, IBrowserDiscovery discovery)
    {
        return new MgmtUiWindowViewModel(
            persister,
            new EmptyRecentPicksReader(),
            new RulePreviewService(),
            new StubConfigFileWatcher(),
            discovery,
            NullPlatformIntegration.Instance,
            new NullNotifier(),
            NoOpUrlLauncher.Instance);
    }

    private sealed class StubInventory : IBrowserInventory
    {
        private readonly List<Browser> _browsers;

        public StubInventory(params Browser[] browsers)
        {
            _browsers = [.. browsers];
        }

        public IReadOnlyList<Browser> Discover() => _browsers;

        public Browser? FindById(string id)
        {
            foreach (Browser browser in _browsers)
            {
                if (browser.Id == id)
                {
                    return browser;
                }
            }
            return null;
        }

        public IReadOnlyList<Browser> Refresh() => Discover();

        public void Add(Browser browser)
        {
            _browsers.Add(browser);
        }
    }

    private sealed class StubDetector : IBrowserProfileDetector
    {
        private readonly Dictionary<string, BrowserProfile> _profiles;

        public StubDetector(params (string BrowserId, BrowserProfile Profile)[] entries)
        {
            _profiles = entries.ToDictionary(e => e.BrowserId, e => e.Profile);
        }

        public IReadOnlyList<BrowserProfile> Detect(Browser browser)
        {
            return _profiles.TryGetValue(browser.Id, out BrowserProfile? p)
                ? [p]
                : [];
        }
    }
}
