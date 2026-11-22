using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Audit;
using AskMeFirst.Core.Config;
using AskMeFirst.MgmtUi.Areas;
using AskMeFirst.MgmtUi.Services;
using AskMeFirst.MgmtUi.ViewModels;
using Xunit;

namespace AskMeFirst.MgmtUi.Tests;

public class SuggestionEntryViewModelTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _configPath;
    private readonly FakeLogger _logger = new();

    public SuggestionEntryViewModelTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"askmefirst-suggestions-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _configPath = Path.Combine(_tempDir, "config.json");
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    [Fact]
    public void AddRule_AppendsRememberRule_WithHostMatchAndBrowser()
    {
        JsonConfigPersister persister = new(_configPath, _logger);
        SuggestionEntryViewModel entry = new("slack.com", 7, "firefox", persister);

        entry.AddRuleCommand.Execute(null);

        Assert.Single(persister.Current.Rules);
        Assert.Equal("Pick slack.com in firefox", persister.Current.Rules[0].Name);
        Assert.Equal("remember", persister.Current.Rules[0].Origin);
        Assert.Equal("firefox", persister.Current.Rules[0].Then.Browser);
        Assert.Equal("slack.com", persister.Current.Rules[0].When!.UrlMatchesAny![0]);
    }

    [Fact]
    public void SuggestionsDetail_TopThreeHostsByVisitCount()
    {
        IRecentPicksReader reader = StubRecentPicks([
            MakePick("https://slack.com/x", "firefox"),
            MakePick("https://slack.com/y", "firefox"),
            MakePick("https://slack.com/z", "chrome"),
            MakePick("https://github.com/x", "firefox"),
            MakePick("https://example.com/x", "chrome"),
        ]);
        MgmtUiWindowViewModel window = new(new JsonConfigPersister(_configPath, _logger), reader, new RulePreviewService(), new StubConfigFileWatcher(), NullBrowserDiscovery.Instance, NullPlatformIntegration.Instance, new NullNotifier(), NoOpUrlLauncher.Instance);
        window.DrillTo(AreaId.Suggestions);

        SuggestionsDetailViewModel detail = (SuggestionsDetailViewModel) window.ActiveDetail!;
        Assert.Equal(3, detail.Rows.Count);
        Assert.Equal("slack.com", (detail.Rows[0] as SuggestionEntryViewModel)!.Host);
        Assert.Equal("github.com", (detail.Rows[1] as SuggestionEntryViewModel)!.Host);
        Assert.Equal("example.com", (detail.Rows[2] as SuggestionEntryViewModel)!.Host);
    }

    private static RecentPickEntry MakePick(string url, string browser)
    {
        return new RecentPickEntry(
            DateTimeOffset.UtcNow,
            new Uri(url),
            browser,
            ProfileId: null,
            RuleWritten: false);
    }

    private static StubPicksReader StubRecentPicks(IReadOnlyList<RecentPickEntry> picks)
    {
        return new StubPicksReader(picks);
    }

    private sealed class StubPicksReader : IRecentPicksReader
    {
        private readonly IReadOnlyList<RecentPickEntry> _picks;
        public StubPicksReader(IReadOnlyList<RecentPickEntry> picks)
        {
            _picks = picks;
        }
        public IReadOnlyList<RecentPickEntry> ReadAll() => _picks;
    }
}
