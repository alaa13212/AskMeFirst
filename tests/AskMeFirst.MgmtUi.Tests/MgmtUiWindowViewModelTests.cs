using AskMeFirst.Core.Config;
using AskMeFirst.MgmtUi.Areas;
using AskMeFirst.MgmtUi.Services;
using AskMeFirst.MgmtUi.ViewModels;
using Xunit;

namespace AskMeFirst.MgmtUi.Tests;

public class MgmtUiWindowViewModelTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _configPath;
    private readonly FakeLogger _logger = new();

    public MgmtUiWindowViewModelTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"askmefirst-window-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _configPath = Path.Combine(_tempDir, "config.json");
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    [Fact]
    public void DrillTo_Rules_PopulatesRowsFromCurrentConfig()
    {
        SeedConfig(MakeRule("first", priority: 10), MakeRule("second", priority: 20));
        JsonConfigPersister persister = new(_configPath, _logger);
        MgmtUiWindowViewModel window = Build(persister);

        window.DrillTo(AreaId.Rules);

        Assert.NotNull(window.ActiveDetail);
        RuleDetailViewModel detail = Assert.IsType<RuleDetailViewModel>(window.ActiveDetail);
        Assert.Equal(2, detail.Rows.Count);
        Assert.True(detail.HasRows);
        Assert.Equal("first", detail.Rows[0].Label);
    }

    [Fact]
    public void DrillTo_NonFormArea_ShowsPlaceholder_NoRows()
    {
        JsonConfigPersister persister = new(_configPath, _logger);
        MgmtUiWindowViewModel window = Build(persister);

        window.DrillTo(AreaId.Browsers);

        Assert.NotNull(window.ActiveDetail);
        AreaDetailViewModel detail = Assert.IsType<BrowserDetailViewModel>(window.ActiveDetail);
        Assert.False(detail.HasRows);
    }

    [Fact]
    public void DrillToEntity_Browser_PopulatesEntityRow()
    {
        SeedBrowsers(BrowserSpecFor("firefox", "Firefox"), BrowserSpecFor("chrome", "Chrome"));
        JsonConfigPersister persister = new(_configPath, _logger);
        MgmtUiWindowViewModel window = Build(persister);

        window.DrillToEntity(AreaId.Browsers, "firefox");

        Assert.NotNull(window.ActiveDetail);
        BrowserEntityDetailViewModel entity = Assert.IsType<BrowserEntityDetailViewModel>(window.ActiveDetail);
        Assert.Single(entity.Rows);
        BrowserRowViewModel row = (BrowserRowViewModel) entity.Rows[0];
        Assert.Equal("Firefox", row.DisplayName);
    }

    [Fact]
    public void DrillToEntity_Profile_PopulatesEntityRow()
    {
        SeedProfiles(MakeProfile("p1", "firefox", displayName: "Work"));
        JsonConfigPersister persister = new(_configPath, _logger);
        MgmtUiWindowViewModel window = Build(persister);

        window.DrillToEntity(AreaId.Profiles, "p1");

        ProfileEntityDetailViewModel entity = Assert.IsType<ProfileEntityDetailViewModel>(window.ActiveDetail);
        Assert.Single(entity.Rows);
    }

    [Fact]
    public void BackToArea_RestoresAreaDetail()
    {
        SeedBrowsers(BrowserSpecFor("firefox", "Firefox"));
        JsonConfigPersister persister = new(_configPath, _logger);
        MgmtUiWindowViewModel window = Build(persister);
        window.DrillToEntity(AreaId.Browsers, "firefox");
        window.BackToArea(AreaId.Browsers);

        Assert.IsType<BrowserDetailViewModel>(window.ActiveDetail);
    }

    [Fact]
    public void Tree_PopulatesBrowserLeaves_PerConfigEntry()
    {
        SeedBrowsers(BrowserSpecFor("firefox", "Firefox"), BrowserSpecFor("chrome", "Chrome"));
        JsonConfigPersister persister = new(_configPath, _logger);
        MgmtUiWindowViewModel window = Build(persister);

        TreeNodeViewModel inventory = window.Tree[0];
        TreeNodeViewModel browsersNode = inventory.Children[0];
        Assert.Equal(2, browsersNode.Children.Count);
        Assert.Equal("Firefox", browsersNode.Children[0].Label);
        Assert.Equal("firefox", browsersNode.Children[0].TargetEntityId);
    }

    [Fact]
    public void Tree_RebuildsOnConfigChange()
    {
        SeedBrowsers(BrowserSpecFor("firefox", "Firefox"));
        JsonConfigPersister persister = new(_configPath, _logger);
        MgmtUiWindowViewModel window = Build(persister);

        Assert.Single(window.Tree[0].Children[0].Children);
        SeedBrowsers(BrowserSpecFor("firefox", "Firefox"), BrowserSpecFor("chrome", "Chrome"));
        persister.Reload();

        Assert.Equal(2, window.Tree[0].Children[0].Children.Count);
    }

    [Fact]
    public void UrlTester_ResolvesMatchingRule()
    {
        SeedConfig(MakeRule("slack", priority: 50, urlRegex: ".*slack\\.com.*"));
        JsonConfigPersister persister = new(_configPath, _logger);
        MgmtUiWindowViewModel window = Build(persister);

        window.UrlTester.UrlInput = "https://slack.com/x";

        Assert.Contains("slack", window.UrlTester.MatchSummary);
    }

    [Fact]
    public void UrlTester_NoMatch_ShowsPickerFallback()
    {
        SeedConfig(MakeRule("github", priority: 50, urlRegex: ".*github\\.com.*"));
        JsonConfigPersister persister = new(_configPath, _logger);
        MgmtUiWindowViewModel window = Build(persister);

        window.UrlTester.UrlInput = "https://example.com/x";

        Assert.Contains("picker would fire", window.UrlTester.MatchSummary);
    }

    [Fact]
    public void UrlTester_InvalidUrl_ShowsValidationMessage()
    {
        JsonConfigPersister persister = new(_configPath, _logger);
        MgmtUiWindowViewModel window = Build(persister);

        window.UrlTester.UrlInput = "not a url";

        Assert.Contains("valid URL", window.UrlTester.MatchSummary);
    }

    [Fact]
    public void PersisterChanged_WhileDrilled_RebuildsRows()
    {
        SeedConfig(MakeRule("original", priority: 10));
        JsonConfigPersister persister = new(_configPath, _logger);
        MgmtUiWindowViewModel window = Build(persister);
        window.DrillTo(AreaId.Rules);
        RuleDetailViewModel before = (RuleDetailViewModel) window.ActiveDetail!;
        Assert.Equal("original", before.Rows[0].Label);

        Rule added = MakeRule("added", priority: 99);
        persister.Save(c => c with { Rules = [.. c.Rules, added] });

        RuleDetailViewModel afterRebuild = (RuleDetailViewModel) window.ActiveDetail!;
        Assert.Equal(2, afterRebuild.Rows.Count);
        Assert.Equal("added", afterRebuild.Rows[1].Label);
    }

    [Fact]
    public void HasDrafts_ReflectsAnyRowInFlight()
    {
        SeedConfig(MakeRule("first", priority: 10));
        JsonConfigPersister persister = new(_configPath, _logger);
        MgmtUiWindowViewModel window = Build(persister);
        window.DrillTo(AreaId.Rules);

        Assert.False(window.HasDrafts);
        RuleRowViewModel firstRow = (RuleRowViewModel) ((AreaDetailViewModel) window.ActiveDetail!).Rows[0];
        firstRow.Name = "edited";
        Assert.True(window.HasDrafts);
    }

    [Fact]
    public void MnemonicDrill_OpensTargetArea()
    {
        JsonConfigPersister persister = new(_configPath, _logger);
        MgmtUiWindowViewModel window = Build(persister);

        window.DrillByMnemonicCommand.Execute("R");

        Assert.IsType<RuleDetailViewModel>(window.ActiveDetail);
    }

    [Fact]
    public void MnemonicDrill_UnknownMnemonic_NoOp()
    {
        JsonConfigPersister persister = new(_configPath, _logger);
        MgmtUiWindowViewModel window = Build(persister);

        window.DrillByMnemonicCommand.Execute("Z");

        Assert.Null(window.ActiveDetail);
    }

    [Fact]
    public void NextArea_StepsThroughCardOrder()
    {
        JsonConfigPersister persister = new(_configPath, _logger);
        MgmtUiWindowViewModel window = Build(persister);

        window.NextAreaCommand.Execute(null);
        Assert.IsType<BrowserDetailViewModel>(window.ActiveDetail);

        window.NextAreaCommand.Execute(null);
        Assert.IsType<ProfileDetailViewModel>(window.ActiveDetail);
    }

    [Fact]
    public void PrevArea_StepsBackThroughCardOrder()
    {
        JsonConfigPersister persister = new(_configPath, _logger);
        MgmtUiWindowViewModel window = Build(persister);

        window.PrevAreaCommand.Execute(null);
        Assert.IsType<CacheDetailViewModel>(window.ActiveDetail);
    }

    [Fact]
    public void PrevArea_FromDashboard_WrapsToLastArea()
    {
        JsonConfigPersister persister = new(_configPath, _logger);
        MgmtUiWindowViewModel window = Build(persister);

        Assert.Null(window.ActiveDetail);
        window.PrevAreaCommand.Execute(null);
        Assert.IsType<CacheDetailViewModel>(window.ActiveDetail);
    }

    [Fact]
    public void Escape_FromArea_ReturnsToDashboard()
    {
        SeedConfig(MakeRule("first"));
        JsonConfigPersister persister = new(_configPath, _logger);
        MgmtUiWindowViewModel window = Build(persister);
        window.DrillTo(AreaId.Rules);

        window.EscapeCommand.Execute(null);

        Assert.Null(window.ActiveDetail);
    }

    [Fact]
    public void Escape_FromEntity_ReturnsToParentArea()
    {
        SeedBrowsers(BrowserSpecFor("firefox", "Firefox"));
        JsonConfigPersister persister = new(_configPath, _logger);
        MgmtUiWindowViewModel window = Build(persister);
        window.DrillToEntity(AreaId.Browsers, "firefox");

        window.EscapeCommand.Execute(null);

        Assert.IsType<BrowserDetailViewModel>(window.ActiveDetail);
    }

    [Fact]
    public void FileWatcher_NoDrafts_AutoReloads()
    {
        SeedConfig(MakeRule("original"));
        JsonConfigPersister persister = new(_configPath, _logger);
        StubConfigFileWatcher watcher = new();
        MgmtUiWindowViewModel window = new(persister, new EmptyRecentPicksReader(), new RulePreviewService(), watcher);
        window.DrillTo(AreaId.Rules);

        SeedConfig(MakeRule("original"), MakeRule("added", priority: 99));
        persister.Reload();
        RuleDetailViewModel afterReload = (RuleDetailViewModel) window.ActiveDetail!;
        Assert.Equal(2, afterReload.Rows.Count);

        _ = window;
        Assert.True(watcher.Started);
    }

    [Fact]
    public void FileWatcher_WithDrafts_RaisesConflict()
    {
        SeedConfig(MakeRule("original"));
        JsonConfigPersister persister = new(_configPath, _logger);
        StubConfigFileWatcher watcher = new();
        MgmtUiWindowViewModel window = new(persister, new EmptyRecentPicksReader(), new RulePreviewService(), watcher);
        window.DrillTo(AreaId.Rules);
        ((RuleRowViewModel) ((AreaDetailViewModel) window.ActiveDetail!).Rows[0]).Name = "drafted";

        Assert.True(window.HasDrafts);
        watcher.RaiseChanged();

        Assert.NotNull(window.PendingConflict);
    }

    [Fact]
    public void ResolveConflict_DiscardDrafts_AndReload()
    {
        SeedConfig(MakeRule("original"));
        JsonConfigPersister persister = new(_configPath, _logger);
        StubConfigFileWatcher watcher = new();
        MgmtUiWindowViewModel window = new(persister, new EmptyRecentPicksReader(), new RulePreviewService(), watcher);
        window.DrillTo(AreaId.Rules);
        ((DraftableRowViewModel) ((AreaDetailViewModel) window.ActiveDetail!).Rows[0]).DiscardCommand.Execute(null);
        ((RuleRowViewModel) ((AreaDetailViewModel) window.ActiveDetail!).Rows[0]).Name = "drafted";
        watcher.RaiseChanged();
        window.PendingConflict!.DiscardCommand.Execute(null);

        SeedConfig(MakeRule("original"), MakeRule("from-disk", priority: 99));
        window.ResolveConflictAndReload();

        Assert.Null(window.PendingConflict);
        RuleDetailViewModel after = (RuleDetailViewModel) window.ActiveDetail!;
        RuleRowViewModel firstRow = (RuleRowViewModel) after.Rows[0];
        Assert.False(firstRow.HasDraft);
        Assert.Equal(2, after.Rows.Count);
    }

    [Fact]
    public void ResolveConflict_KeepDrafts_LeavesDraftsUntouched()
    {
        SeedConfig(MakeRule("original"));
        JsonConfigPersister persister = new(_configPath, _logger);
        StubConfigFileWatcher watcher = new();
        MgmtUiWindowViewModel window = new(persister, new EmptyRecentPicksReader(), new RulePreviewService(), watcher);
        window.DrillTo(AreaId.Rules);
        RuleRowViewModel row = (RuleRowViewModel) ((AreaDetailViewModel) window.ActiveDetail!).Rows[0];
        row.Name = "kept";
        watcher.RaiseChanged();
        window.PendingConflict!.KeepCommand.Execute(null);

        window.ResolveConflictAndReload();

        Assert.Null(window.PendingConflict);
        RuleDetailViewModel after = (RuleDetailViewModel) window.ActiveDetail!;
        RuleRowViewModel firstRow = (RuleRowViewModel) after.Rows[0];
        Assert.True(firstRow.HasDraft);
        Assert.Equal("kept", firstRow.Name);
    }

    [Fact]
    public void EvaluateClose_NoDrafts_ProceedsWithoutPrompt()
    {
        SeedConfig(MakeRule("only"));
        JsonConfigPersister persister = new(_configPath, _logger);
        MgmtUiWindowViewModel window = Build(persister);
        window.DrillTo(AreaId.Rules);

        Assert.Equal(MgmtUiWindowViewModel.CloseDecision.Proceed, window.EvaluateCloseRequest());
    }

    [Fact]
    public void EvaluateClose_WithDrafts_CancelsByDefault()
    {
        SeedConfig(MakeRule("only"));
        JsonConfigPersister persister = new(_configPath, _logger);
        MgmtUiWindowViewModel window = Build(persister);
        window.DrillTo(AreaId.Rules);
        RuleRowViewModel row = (RuleRowViewModel) ((AreaDetailViewModel) window.ActiveDetail!).Rows[0];
        row.Name = "edited";

        Assert.Equal(MgmtUiWindowViewModel.CloseDecision.Cancel, window.EvaluateCloseRequest());
    }

    [Fact]
    public void ApplyCloseDecision_SaveAndClose_PersistsAll()
    {
        SeedConfig(MakeRule("a"), MakeRule("b"));
        JsonConfigPersister persister = new(_configPath, _logger);
        MgmtUiWindowViewModel window = Build(persister);
        window.DrillTo(AreaId.Rules);
        RuleRowViewModel a = (RuleRowViewModel) ((AreaDetailViewModel) window.ActiveDetail!).Rows[0];
        RuleRowViewModel b = (RuleRowViewModel) ((AreaDetailViewModel) window.ActiveDetail!).Rows[1];
        a.Name = "A-";
        b.Priority = 99;

        Assert.Equal(MgmtUiWindowViewModel.CloseDecision.Proceed,
            window.ApplyCloseDecision(MgmtUiWindowViewModel.CloseDecision.SaveAndClose));

        Assert.False(window.HasDrafts);
        Assert.Equal("A-", persister.Current.Rules[0].Name);
        Assert.Equal(99, persister.Current.Rules[1].Priority);
    }

    [Fact]
    public void ApplyCloseDecision_DiscardAndClose_DropsDraftsWithoutSaving()
    {
        SeedConfig(MakeRule("a"));
        JsonConfigPersister persister = new(_configPath, _logger);
        MgmtUiWindowViewModel window = Build(persister);
        window.DrillTo(AreaId.Rules);
        RuleRowViewModel a = (RuleRowViewModel) ((AreaDetailViewModel) window.ActiveDetail!).Rows[0];
        a.Name = "A-new";

        Assert.Equal(MgmtUiWindowViewModel.CloseDecision.Proceed,
            window.ApplyCloseDecision(MgmtUiWindowViewModel.CloseDecision.DiscardAndClose));

        Assert.False(window.HasDrafts);
        Assert.Equal("a", persister.Current.Rules[0].Name);
    }

    [Fact]
    public void ApplyCloseDecision_Cancel_LeavesDraftsAndStays()
    {
        SeedConfig(MakeRule("a"));
        JsonConfigPersister persister = new(_configPath, _logger);
        MgmtUiWindowViewModel window = Build(persister);
        window.DrillTo(AreaId.Rules);
        RuleRowViewModel a = (RuleRowViewModel) ((AreaDetailViewModel) window.ActiveDetail!).Rows[0];
        a.Name = "still";

        Assert.Equal(MgmtUiWindowViewModel.CloseDecision.Cancel,
            window.ApplyCloseDecision(MgmtUiWindowViewModel.CloseDecision.Cancel));

        Assert.True(window.HasDrafts);
    }

    private void SeedConfig(params Rule[] rules)
    {
        AppConfig seed = new() { Rules = [.. rules] };
        ConfigLoader.WriteAtomic(seed, _configPath);
    }

    private void SeedBrowsers(params BrowserSpec[] browsers)
    {
        AppConfig seed = new() { Browsers = [.. browsers] };
        ConfigLoader.WriteAtomic(seed, _configPath);
    }

    private void SeedProfiles(params ProfileSpec[] profiles)
    {
        AppConfig seed = new() { Profiles = [.. profiles] };
        ConfigLoader.WriteAtomic(seed, _configPath);
    }

    private static Rule MakeRule(string name, int priority = 50, string urlRegex = "")
    {
        return new Rule
        {
            Name = name,
            Priority = priority,
            When = string.IsNullOrEmpty(urlRegex) ? new RuleWhen() : new RuleWhen { UrlRegex = urlRegex },
            Then = new RuleThen { Browser = "firefox" },
            Origin = "user",
        };
    }

    private static MgmtUiWindowViewModel Build(JsonConfigPersister persister)
    {
        return new MgmtUiWindowViewModel(
            persister,
            new EmptyRecentPicksReader(),
            new RulePreviewService(),
            new StubConfigFileWatcher());
    }

    private static BrowserSpec BrowserSpecFor(string id, string displayName)
    {
        return new BrowserSpec { Id = id, DisplayName = displayName, Executable = "auto" };
    }

    private static ProfileSpec MakeProfile(string id, string browserId, string displayName)
    {
        return new ProfileSpec
        {
            Id = id,
            BrowserId = browserId,
            Name = id,
            DisplayName = displayName,
            Pinned = false,
        };
    }
}
