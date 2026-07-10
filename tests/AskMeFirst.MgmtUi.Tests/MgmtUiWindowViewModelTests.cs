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
        MgmtUiWindowViewModel window = new(persister, new EmptyRecentPicksReader());

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
        MgmtUiWindowViewModel window = new(persister, new EmptyRecentPicksReader());

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
        MgmtUiWindowViewModel window = new(persister, new EmptyRecentPicksReader());

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
        MgmtUiWindowViewModel window = new(persister, new EmptyRecentPicksReader());

        window.DrillToEntity(AreaId.Profiles, "p1");

        ProfileEntityDetailViewModel entity = Assert.IsType<ProfileEntityDetailViewModel>(window.ActiveDetail);
        Assert.Single(entity.Rows);
    }

    [Fact]
    public void BackToArea_RestoresAreaDetail()
    {
        SeedBrowsers(BrowserSpecFor("firefox", "Firefox"));
        JsonConfigPersister persister = new(_configPath, _logger);
        MgmtUiWindowViewModel window = new(persister, new EmptyRecentPicksReader());
        window.DrillToEntity(AreaId.Browsers, "firefox");
        window.BackToArea(AreaId.Browsers);

        Assert.IsType<BrowserDetailViewModel>(window.ActiveDetail);
    }

    [Fact]
    public void Tree_PopulatesBrowserLeaves_PerConfigEntry()
    {
        SeedBrowsers(BrowserSpecFor("firefox", "Firefox"), BrowserSpecFor("chrome", "Chrome"));
        JsonConfigPersister persister = new(_configPath, _logger);
        MgmtUiWindowViewModel window = new(persister, new EmptyRecentPicksReader());

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
        MgmtUiWindowViewModel window = new(persister, new EmptyRecentPicksReader());

        Assert.Single(window.Tree[0].Children[0].Children);
        SeedBrowsers(BrowserSpecFor("firefox", "Firefox"), BrowserSpecFor("chrome", "Chrome"));
        persister.Reload();

        Assert.Equal(2, window.Tree[0].Children[0].Children.Count);
    }

    [Fact]
    public void PersisterChanged_WhileDrilled_RebuildsRows()
    {
        SeedConfig(MakeRule("original", priority: 10));
        JsonConfigPersister persister = new(_configPath, _logger);
        MgmtUiWindowViewModel window = new(persister, new EmptyRecentPicksReader());
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
        MgmtUiWindowViewModel window = new(persister, new EmptyRecentPicksReader());
        window.DrillTo(AreaId.Rules);

        Assert.False(window.HasDrafts);
        RuleRowViewModel firstRow = (RuleRowViewModel) ((AreaDetailViewModel) window.ActiveDetail!).Rows[0];
        firstRow.Name = "edited";
        Assert.True(window.HasDrafts);
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

    private void SeedConfig(params Rule[] rules)
    {
        AppConfig seed = new() { Rules = [.. rules] };
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
}
