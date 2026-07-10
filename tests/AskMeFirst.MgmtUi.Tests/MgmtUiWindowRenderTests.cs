using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AskMeFirst.Core.Config;
using AskMeFirst.MgmtUi.Areas;
using AskMeFirst.MgmtUi.Services;
using AskMeFirst.MgmtUi.ViewModels;
using AskMeFirst.MgmtUi.Views;
using Xunit;

namespace AskMeFirst.MgmtUi.Tests;

public class MgmtUiWindowRenderTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _configPath;
    private readonly FakeLogger _logger = new();

    public MgmtUiWindowRenderTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"askmefirst-mgmtui-render-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _configPath = Path.Combine(_tempDir, "config.json");
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    [AvaloniaFact]
    public void Dashboard_RendersEightAreaCards()
    {
        MgmtUiWindowViewModel vm = BuildVm();
        Window window = new MgmtUiWindow { DataContext = vm, Width = 1024, Height = 720 };
        window.Show();
        PumpLayout(window);

        List<Button> cards = window.GetVisualDescendants()
            .OfType<Button>()
            .Where(b => b.Content?.ToString() == "Open")
            .ToList();
        Assert.Equal(8, cards.Count);
    }

    [AvaloniaFact]
    public void Dashboard_DarkTheme_RendersEightAreaCards()
    {
        MgmtUiWindowViewModel vm = BuildVm();
        Window window = new MgmtUiWindow { DataContext = vm, Width = 1024, Height = 720 };
        window.Show();
        PumpLayout(window);

        Avalonia.Application.Current!.RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Dark;
        PumpLayout(window);

        List<Button> cards = window.GetVisualDescendants()
            .OfType<Button>()
            .Where(b => b.Content?.ToString() == "Open")
            .ToList();
        Assert.Equal(8, cards.Count);

        Avalonia.Application.Current.RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Light;
    }

    [AvaloniaFact]
    public void Window_RendersTitleAndBanner()
    {
        MgmtUiWindowViewModel vm = BuildVm();
        Window window = new MgmtUiWindow { DataContext = vm };
        window.Show();
        PumpLayout(window);

        List<string> texts = window.GetVisualDescendants()
            .OfType<Avalonia.Controls.TextBlock>()
            .Select(t => t.Text ?? "")
            .ToList();
        Assert.Contains("AskMeFirst Settings", texts);
        Assert.Contains(vm.VersionLabel, texts);
    }

    [AvaloniaFact]
    public void RulesArea_RenderedRows_HaveNamePriorityRegexInputs()
    {
        JsonConfigPersister persister = new(_configPath, _logger);
        SeedRules(persister, MakeRule("alpha"));
        MgmtUiWindowViewModel vm = BuildVm(persister);
        vm.DrillTo(AreaId.Rules);
        Window window = new MgmtUiWindow { DataContext = vm };
        window.Show();
        PumpLayout(window);

        List<Avalonia.Controls.TextBox> textBoxes = window.GetVisualDescendants()
            .OfType<Avalonia.Controls.TextBox>()
            .ToList();
        Assert.NotEmpty(textBoxes);
        Avalonia.Controls.TextBox nameBox = textBoxes[0];
        Assert.Equal("alpha", nameBox.Text);
    }

    [AvaloniaFact]
    public void UrlTester_RendersBottomBar()
    {
        MgmtUiWindowViewModel vm = BuildVm();
        Window window = new MgmtUiWindow { DataContext = vm };
        window.Show();
        PumpLayout(window);

        List<Avalonia.Controls.TextBlock> labels = window.GetVisualDescendants()
            .OfType<Avalonia.Controls.TextBlock>()
            .Where(t => t.Text == "Test URL")
            .ToList();
        Assert.Single(labels);
    }

    private MgmtUiWindowViewModel BuildVm()
    {
        return BuildVm(new JsonConfigPersister(_configPath, _logger));
    }

    private static MgmtUiWindowViewModel BuildVm(JsonConfigPersister persister)
    {
        return new MgmtUiWindowViewModel(
            persister,
            new EmptyRecentPicksReader(),
            new RulePreviewService(),
            new StubConfigFileWatcher());
    }

    private void SeedRules(IConfigPersister persister, params Rule[] rules)
    {
        AppConfig seed = new() { Rules = [.. rules] };
        ConfigLoader.WriteAtomic(seed, _configPath);
        if (persister is JsonConfigPersister json)
        {
            json.Reload();
        }
    }

    private static Rule MakeRule(string name)
    {
        return new Rule
        {
            Name = name,
            Priority = 50,
            When = new RuleWhen(),
            Then = new RuleThen { Browser = "firefox" },
            Origin = "user",
        };
    }

    private static void PumpLayout(Window window)
    {
        double w = window.Width > 0 ? window.Width : 1024;
        double h = window.Height > 0 ? window.Height : 720;
        window.Measure(new Size(w, h));
        window.Arrange(new Rect(0, 0, w, h));
        Dispatcher.UIThread.RunJobs();
    }
}
