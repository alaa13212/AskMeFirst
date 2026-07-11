using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Config;
using AskMeFirst.Core.Models;
using AskMeFirst.MgmtUi.Services;
using AskMeFirst.MgmtUi.ViewModels;
using Xunit;

namespace AskMeFirst.MgmtUi.Tests;

public class UrlTesterLaunchTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _configPath;
    private readonly FakeLogger _logger = new();

    public UrlTesterLaunchTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"askmefirst-urltester-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _configPath = Path.Combine(_tempDir, "config.json");
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    [Fact]
    public void OpenCommand_NoMatchingRule_DoesNotLaunch()
    {
        UrlTesterFixtures fixtures = UrlTesterFixtures.Build(_tempDir, _configPath, _logger);
        fixtures.ViewModel.UrlInput = "https://no-match.example/x";
        Assert.False(fixtures.ViewModel.CanLaunch);

        fixtures.ViewModel.OpenCommand.Execute(null);

        Assert.Empty(fixtures.Launcher.Launches);
        Assert.Contains("No rule", fixtures.ViewModel.LaunchStatus);
    }

    [Fact]
    public void OpenCommand_MatchingRule_LaunchesViaUrlLauncher()
    {
        Browser firefox = MakeBrowser("firefox", "Firefox", "/ff");
        UrlTesterFixtures fixtures = UrlTesterFixtures.Build(
            _tempDir, _configPath, _logger,
            discoveredBrowsers: [firefox],
            rule: MakeRule("github", priority: 50, urlRegex: ".*github\\.com.*"),
            launcherResult: "Launched in Firefox");

        fixtures.ViewModel.UrlInput = "https://github.com/foo/bar";
        Assert.True(fixtures.ViewModel.CanLaunch);
        fixtures.ViewModel.OpenCommand.Execute(null);

        Assert.Single(fixtures.Launcher.Launches);
        Browser launched = fixtures.Launcher.Launches[0].Browser;
        Assert.Equal("firefox", launched.Id);
        Assert.Equal("https://github.com/foo/bar", fixtures.Launcher.Launches[0].Url.ToString());
        Assert.StartsWith("Launched in", fixtures.ViewModel.LaunchStatus, StringComparison.Ordinal);
    }

    [Fact]
    public void OpenCommand_MatchedBrowserMissing_NotifiesAndSetsStatus()
    {
        UrlTesterFixtures fixtures = UrlTesterFixtures.Build(
            _tempDir, _configPath, _logger,
            rule: MakeRule("bad", priority: 50, browserId: "ghost", urlRegex: ".*example\\.com.*"));

        fixtures.ViewModel.UrlInput = "https://example.com/";
        fixtures.ViewModel.OpenCommand.Execute(null);

        Assert.Empty(fixtures.Launcher.Launches);
        Assert.Contains("not installed", fixtures.ViewModel.LaunchStatus);
        Assert.Single(fixtures.Notifier.Calls);
    }

    private static Browser MakeBrowser(string id, string displayName, string executablePath)
    {
        return new Browser { Id = id, DisplayName = displayName, ExecutablePath = executablePath };
    }

    private static Rule MakeRule(string name, int priority, string urlRegex, string browserId = "firefox")
    {
        return new Rule
        {
            Name = name,
            Priority = priority,
            When = new RuleWhen { UrlRegex = urlRegex },
            Then = new RuleThen { Browser = browserId },
            Origin = "user",
        };
    }

    private sealed class UrlTesterFixtures
    {
        public UrlTesterViewModel ViewModel { get; }
        public TestNotifier Notifier { get; }
        public RecordingUrlLauncher Launcher { get; }

        private UrlTesterFixtures(UrlTesterViewModel vm, TestNotifier notifier, RecordingUrlLauncher launcher)
        {
            ViewModel = vm;
            Notifier = notifier;
            Launcher = launcher;
        }

        public static UrlTesterFixtures Build(
            string tempDir,
            string configPath,
            FakeLogger logger,
            IReadOnlyList<BrowserSpec>? browserSpecs = null,
            IReadOnlyList<Browser>? discoveredBrowsers = null,
            Rule? rule = null,
            string launcherResult = "")
        {
            AppConfig seed = new();
            if (browserSpecs is not null)
            {
                seed = seed with { Browsers = browserSpecs.ToArray() };
            }
            if (rule is not null)
            {
                seed = seed with { Rules = [rule] };
            }
            ConfigLoader.WriteAtomic(seed, configPath);

            JsonConfigPersister persister = new(configPath, logger);
            StubInventory inventory = new(discoveredBrowsers ?? []);
            BrowserDiscoveryAdapter discovery = new(inventory, null, logger);
            discovery.Refresh();
            TestNotifier notifier = new();
            RecordingUrlLauncher launcher = new(launcherResult);
            UrlTesterViewModel vm = new(persister, new RulePreviewService(), discovery, launcher, notifier);
            return new UrlTesterFixtures(vm, notifier, launcher);
        }
    }

    private sealed class RecordingUrlLauncher : IUrlLauncher
    {
        public List<(Browser Browser, Uri Url)> Launches { get; } = [];
        private readonly string _result;

        public RecordingUrlLauncher(string result)
        {
            _result = result;
        }

        public void Launch(Browser browser, Uri url)
        {
            Launches.Add((browser, url));
            if (_result.StartsWith('!'))
            {
                throw new InvalidOperationException(_result.Substring(1));
            }
        }
    }

    private sealed class StubInventory : IBrowserInventory
    {
        private readonly List<Browser> _browsers;
        public StubInventory(IEnumerable<Browser> browsers)
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
    }
}
