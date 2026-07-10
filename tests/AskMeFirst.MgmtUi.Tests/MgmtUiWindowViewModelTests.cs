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
        Assert.False(window.ActiveDetail!.HasRows);
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
        RuleRowViewModel firstRow = (RuleRowViewModel) window.ActiveDetail!.Rows[0];
        firstRow.Name = "edited";
        Assert.True(window.HasDrafts);
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
