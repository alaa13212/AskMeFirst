using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Launch;
using AskMeFirst.Core.Models;
using AskMeFirst.Core.Routing;
using AskMeFirst.Picker.ViewModels;
using Xunit;

namespace AskMeFirst.Picker.Tests;

public class PickerWindowRenderTests
{
    [AvaloniaFact]
    public void Window_RendersAllBrowserOptions()
    {
        PickerRequest request = MakeRequest(
            "https://example.com",
            browsers:
            [
                MakeBrowser("chrome", "Chrome"),
                MakeBrowser("firefox", "Firefox"),
                MakeBrowser("edge", "Edge"),
            ]);

        using PickerWindowViewModel vm = new(request, new NullLogger());
        Window window = new PickerWindow(vm);

        window.Show();
        PumpLayout(window);

        ItemsControl? browserList = window.FindControl<ItemsControl>("BrowserList");
        Assert.NotNull(browserList);

        List<Button> rendered = browserList.GetVisualDescendants()
            .OfType<Button>()
            .Where(b => b.Name == "PART_BrowserButton")
            .ToList();
        Assert.Equal(3, rendered.Count);

        List<string> labels = rendered
            .Select(b => b.GetVisualDescendants().OfType<TextBlock>()
                .Where(t => !string.IsNullOrEmpty(t.Text) && t.Text != "1" && t.Text != "2" && t.Text != "3")
                .Select(t => t.Text).FirstOrDefault() ?? "")
            .ToList();
        Assert.Contains("Chrome", labels);
        Assert.Contains("Firefox", labels);
        Assert.Contains("Edge", labels);
    }

    [AvaloniaFact]
    public void Window_RendersAllRememberOptions()
    {
        PickerRequest request = MakeRequest("https://example.com", sourceApp: "slack");

        using PickerWindowViewModel vm = new(request, new NullLogger());
        Window window = new PickerWindow(vm);

        window.Show();
        PumpLayout(window);

        ItemsControl? rememberList = window.FindControl<ItemsControl>("RememberList");
        Assert.NotNull(rememberList);

        List<RadioButton> radios = rememberList.GetVisualDescendants()
            .OfType<RadioButton>()
            .ToList();
        Assert.Equal(5, radios.Count);

        Assert.Single(radios, r => r.IsChecked == true);
    }

    [AvaloniaFact]
    public void Window_HasNoOpenButton()
    {
        PickerRequest request = MakeRequest("https://example.com");
        using PickerWindowViewModel vm = new(request, new NullLogger());
        Window window = new PickerWindow(vm);
        window.Show();
        PumpLayout(window);

        Assert.Null(window.FindControl<Button>("OpenButton"));
    }

    [AvaloniaFact]
    public void Window_HasNoCancelButton()
    {
        PickerRequest request = MakeRequest("https://example.com");
        using PickerWindowViewModel vm = new(request, new NullLogger());
        Window window = new PickerWindow(vm);
        window.Show();
        PumpLayout(window);

        Assert.Null(window.FindControl<Button>("CancelButton"));
    }

    [AvaloniaFact]
    public void Window_ClickingBrowserButton_Commits()
    {
        PickerRequest request = MakeRequest(
            "https://example.com",
            browsers:
            [
                MakeBrowser("chrome", "Chrome"),
                MakeBrowser("firefox", "Firefox"),
            ]);
        using PickerWindowViewModel vm = new(request, new NullLogger());
        Window window = new PickerWindow(vm);
        window.Show();
        PumpLayout(window);

        bool closed = false;
        window.Closed += (_, _) => closed = true;

        Button firefoxButton = FindBrowserButtons(window).Skip(1).First();
        firefoxButton.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        PumpLayout(window);

        Launched launched = Assert.IsType<Launched>(vm.Result);
        Assert.Equal("firefox", launched.Browser.Id);
        Assert.True(closed, "Window should close after click commits.");
    }

    [AvaloniaFact]
    public void Window_PressingOneHotkey_CommitsFirstBrowser()
    {
        PickerRequest request = MakeRequest("https://example.com");
        using PickerWindowViewModel vm = new(request, new NullLogger());
        Window window = new PickerWindow(vm);
        window.Show();
        PumpLayout(window);

        bool closed = false;
        window.Closed += (_, _) => closed = true;

        window.RaiseEvent(new KeyEventArgs
        {
            Key = Key.D1,
            RoutedEvent = InputElement.KeyDownEvent,
        });
        PumpLayout(window);

        Launched launched = Assert.IsType<Launched>(vm.Result);
        Assert.Equal("chrome", launched.Browser.Id);
        Assert.True(closed, "Window should close after 1-hotkey commit.");
    }

    [AvaloniaFact]
    public void Window_PressingOneHotkey_WhenAlreadySelected_StillCommits()
    {
        PickerRequest request = MakeRequest("https://example.com");
        using PickerWindowViewModel vm = new(request, new NullLogger());
        Window window = new PickerWindow(vm);
        window.Show();
        PumpLayout(window);

        bool closed = false;
        window.Closed += (_, _) => closed = true;

        vm.SelectedBrowserIndex = 0;

        window.RaiseEvent(new KeyEventArgs
        {
            Key = Key.D1,
            RoutedEvent = InputElement.KeyDownEvent,
        });
        PumpLayout(window);

        Assert.IsType<Launched>(vm.Result);
        Assert.True(closed, "Window should close after hotkey commit.");
    }

    [AvaloniaFact]
    public void Window_PressingEscape_Cancels()
    {
        PickerRequest request = MakeRequest("https://example.com");
        using PickerWindowViewModel vm = new(request, new NullLogger());
        Window window = new PickerWindow(vm);
        window.Show();
        PumpLayout(window);

        bool closed = false;
        window.Closed += (_, _) => closed = true;

        window.RaiseEvent(new KeyEventArgs
        {
            Key = Key.Escape,
            RoutedEvent = InputElement.KeyDownEvent,
        });
        PumpLayout(window);

        Assert.IsType<Cancelled>(vm.Result);
        Assert.True(closed, "Window should close after Escape cancels.");
    }

    [AvaloniaFact]
    public void Window_PressingEnter_CommitsSelected()
    {
        PickerRequest request = MakeRequest(
            "https://example.com",
            browsers:
            [
                MakeBrowser("chrome", "Chrome"),
                MakeBrowser("firefox", "Firefox"),
            ]);
        using PickerWindowViewModel vm = new(request, new NullLogger());
        Window window = new PickerWindow(vm);
        window.Show();
        PumpLayout(window);

        bool closed = false;
        window.Closed += (_, _) => closed = true;

        vm.SelectedBrowserIndex = 1;

        window.RaiseEvent(new KeyEventArgs
        {
            Key = Key.Enter,
            RoutedEvent = InputElement.KeyDownEvent,
        });
        PumpLayout(window);

        Launched launched = Assert.IsType<Launched>(vm.Result);
        Assert.Equal("firefox", launched.Browser.Id);
        Assert.True(closed, "Window should close after Enter commits.");
    }

    [AvaloniaFact]
    public void Window_PressingArrowDown_MovesFocusToNextBrowserButton()
    {
        PickerRequest request = MakeRequest(
            "https://example.com",
            browsers:
            [
                MakeBrowser("chrome", "Chrome"),
                MakeBrowser("firefox", "Firefox"),
                MakeBrowser("edge", "Edge"),
            ]);
        using PickerWindowViewModel vm = new(request, new NullLogger());
        Window window = new PickerWindow(vm);
        window.Show();
        PumpLayout(window);

        List<Button> buttons = FindBrowserButtons(window);
        Assert.Equal(3, buttons.Count);
        Assert.True(buttons[0].IsFocused, "First button should be focused on open.");

        window.RaiseEvent(new KeyEventArgs
        {
            Key = Key.Down,
            RoutedEvent = InputElement.KeyDownEvent,
        });
        PumpLayout(window);

        Assert.True(buttons[1].IsFocused, "Down arrow should focus second button.");
        Assert.False(buttons[0].IsFocused);
    }

    [AvaloniaFact]
    public void Window_PressingArrowUp_FromFirst_WrapsToLast()
    {
        PickerRequest request = MakeRequest(
            "https://example.com",
            browsers:
            [
                MakeBrowser("chrome", "Chrome"),
                MakeBrowser("firefox", "Firefox"),
                MakeBrowser("edge", "Edge"),
            ]);
        using PickerWindowViewModel vm = new(request, new NullLogger());
        Window window = new PickerWindow(vm);
        window.Show();
        PumpLayout(window);

        List<Button> buttons = FindBrowserButtons(window);
        Assert.True(buttons[0].IsFocused);

        window.RaiseEvent(new KeyEventArgs
        {
            Key = Key.Up,
            RoutedEvent = InputElement.KeyDownEvent,
        });
        PumpLayout(window);

        Assert.True(buttons[^1].IsFocused, "Up arrow from first should wrap to last.");
    }

    [AvaloniaFact]
    public void Window_PressingArrowDown_FromLast_WrapsToFirst()
    {
        PickerRequest request = MakeRequest(
            "https://example.com",
            browsers:
            [
                MakeBrowser("chrome", "Chrome"),
                MakeBrowser("firefox", "Firefox"),
                MakeBrowser("edge", "Edge"),
            ]);
        using PickerWindowViewModel vm = new(request, new NullLogger());
        Window window = new PickerWindow(vm);
        window.Show();
        PumpLayout(window);

        List<Button> buttons = FindBrowserButtons(window);
        buttons[^1].Focus();
        PumpLayout(window);

        window.RaiseEvent(new KeyEventArgs
        {
            Key = Key.Down,
            RoutedEvent = InputElement.KeyDownEvent,
        });
        PumpLayout(window);

        Assert.True(buttons[0].IsFocused, "Down arrow from last should wrap to first.");
    }

    [AvaloniaFact]
    public void Window_SectionHeaders_ShowNoCount()
    {
        PickerRequest request = MakeRequest("https://example.com");
        using PickerWindowViewModel vm = new(request, new NullLogger());
        Window window = new PickerWindow(vm);
        window.Show();
        PumpLayout(window);

        List<string> headerTexts = window.GetVisualDescendants()
            .OfType<TextBlock>()
            .Where(t => t.FontWeight == Avalonia.Media.FontWeight.Bold)
            .Select(t => t.Text ?? "")
            .ToList();

        Assert.Contains("Open with", headerTexts);
        Assert.Contains("Remember", headerTexts);
        Assert.DoesNotContain(headerTexts, t => t.Contains('(') && t.Contains(')'));
    }

    [AvaloniaFact]
    public void Window_ProfileFirst_BrowserButtonShowsProfileNameAsPrimary()
    {
        BrowserProfile work = new(Name: "Work", DirectoryName: "work", IsDefault: false);
        Browser chrome = new()
        {
            Id = "chrome",
            DisplayName = "Chrome",
            ExecutablePath = "/chrome",
            LaunchStrategy = BrowserLaunchStrategies.For("chrome"),
            Profile = work,
        };
        PickerRequest request = MakeRequest("https://example.com", browsers: [chrome]);
        using PickerWindowViewModel vm = new(request, new NullLogger());
        Window window = new PickerWindow(vm);
        window.Show();
        PumpLayout(window);

        Button button = FindBrowserButtons(window).Single();
        List<string> textBlocks = button.GetVisualDescendants()
            .OfType<TextBlock>()
            .Select(t => t.Text ?? "")
            .ToList();

        Assert.Contains("Work", textBlocks);
        Assert.Contains("Chrome", textBlocks);
    }

    [AvaloniaFact]
    public void Window_OpeningFocusesFirstBrowserButton()
    {
        PickerRequest request = MakeRequest("https://example.com");
        using PickerWindowViewModel vm = new(request, new NullLogger());
        Window window = new PickerWindow(vm);
        window.Show();
        PumpLayout(window);

        Button? firstButton = FindBrowserButtons(window).FirstOrDefault();
        Assert.NotNull(firstButton);
        Assert.True(firstButton!.IsFocused);
    }

    [AvaloniaFact]
    public void Window_RendersScreenshot_WithAllItems()
    {
        string screenshotPath = @"C:\Users\Ali\.mavis\cache\askmefirst-picker.png";
        Directory.CreateDirectory(Path.GetDirectoryName(screenshotPath)!);

        PickerRequest request = MakeRequest(
            "https://test.example.com/path?q=1",
            sourceApp: "slack",
            browsers:
            [
                MakeBrowser("chrome", "Google Chrome"),
                MakeBrowser("firefox", "Mozilla Firefox"),
                MakeBrowser("edge", "Microsoft Edge"),
            ]);

        using PickerWindowViewModel vm = new(request, new NullLogger());
        Window window = new PickerWindow(vm)
        {
            Width = 720,
            Height = 440,
        };

        window.Show();
        PumpLayout(window);

        WriteableBitmap? frame = window.CaptureRenderedFrame();
        Assert.NotNull(frame);
        frame!.Save(screenshotPath);
        Assert.True(File.Exists(screenshotPath), $"File not found at {screenshotPath}");
        long size = new FileInfo(screenshotPath).Length;
        Assert.True(size > 1000, $"File too small: {size} bytes");

        _lastScreenshot = screenshotPath;
    }

    [AvaloniaFact]
    public void Window_RendersScreenshot_WithProfilesPerBrowser()
    {
        string screenshotPath = @"C:\Users\Ali\.mavis\cache\askmefirst-picker-profiles.png";
        Directory.CreateDirectory(Path.GetDirectoryName(screenshotPath)!);

        Browser chrome = MakeBrowser("chrome", "Chrome");
        chrome = chrome with { Profile = new BrowserProfile(Name: "Work", DirectoryName: "work", IsDefault: false) };
        Browser chromePersonal = MakeBrowser("chrome-personal", "Chrome (Personal)");
        chromePersonal = chromePersonal with { Profile = new BrowserProfile(Name: "Personal", DirectoryName: "personal", IsDefault: false) };
        Browser firefox = MakeBrowser("firefox", "Firefox");
        firefox = firefox with { Profile = new BrowserProfile(Name: "default-release", DirectoryName: "default-release", IsDefault: true) };

        List<PickerBrowserOption> options =
        [
            new(chrome, chrome.Profile),
            new(chromePersonal, chromePersonal.Profile),
            new(firefox, firefox.Profile),
        ];

        PickerRequest request = new(
            OriginalUrl: new Uri("https://company.atlassian.net/wiki/spaces/ENG"),
            SourceApp: "slack",
            UnshortenTask: null,
            AvailableBrowsers: options);

        using PickerWindowViewModel vm = new(request, new NullLogger());
        Window window = new PickerWindow(vm)
        {
            Width = 720,
            Height = 440,
        };

        window.Show();
        PumpLayout(window);

        WriteableBitmap? frame = window.CaptureRenderedFrame();
        Assert.NotNull(frame);
        frame!.Save(screenshotPath);
        Assert.True(File.Exists(screenshotPath), $"File not found at {screenshotPath}");
        long size = new FileInfo(screenshotPath).Length;
        Assert.True(size > 1000, $"File too small: {size} bytes");
    }

    [AvaloniaFact]
    public void Window_RendersScreenshot_FocusedButtonVisible()
    {
        string screenshotPath = @"C:\Users\Ali\.mavis\cache\askmefirst-picker-focus.png";
        Directory.CreateDirectory(Path.GetDirectoryName(screenshotPath)!);

        Browser chrome = MakeBrowser("chrome", "Chrome");
        chrome = chrome with { Profile = new BrowserProfile(Name: "Work", DirectoryName: "work", IsDefault: false) };
        Browser firefox = MakeBrowser("firefox", "Firefox");
        firefox = firefox with { Profile = new BrowserProfile(Name: "Personal", DirectoryName: "personal", IsDefault: false) };
        Browser edge = MakeBrowser("edge", "Edge");

        List<PickerBrowserOption> options =
        [
            new(chrome, chrome.Profile),
            new(firefox, firefox.Profile),
            new(edge, null),
        ];

        PickerRequest request = new(
            OriginalUrl: new Uri("https://example.com"),
            SourceApp: "slack",
            UnshortenTask: null,
            AvailableBrowsers: options);

        using PickerWindowViewModel vm = new(request, new NullLogger());
        Window window = new PickerWindow(vm)
        {
            Width = 720,
            Height = 440,
        };

        window.Show();
        PumpLayout(window);

        List<Button> buttons = FindBrowserButtons(window);
        Assert.Equal(3, buttons.Count);
        buttons[1].Focus();
        PumpLayout(window);
        Assert.True(buttons[1].IsFocused, "Second button should be focused for visual clarity in the screenshot.");

        WriteableBitmap? frame = window.CaptureRenderedFrame();
        Assert.NotNull(frame);
        frame!.Save(screenshotPath);
        Assert.True(File.Exists(screenshotPath), $"File not found at {screenshotPath}");
        long size = new FileInfo(screenshotPath).Length;
        Assert.True(size > 1000, $"File too small: {size} bytes");
    }

#if WINDOWS
    [AvaloniaFact]
    public void Window_RendersScreenshot_WithRealBrowserIcons()
    {
        string screenshotPath = @"C:\Users\Ali\.mavis\cache\askmefirst-picker-icons.png";
        Directory.CreateDirectory(Path.GetDirectoryName(screenshotPath)!);

        string chromeRoot = Path.Combine(
            Environment.GetEnvironmentVariable("LOCALAPPDATA") ?? "",
            @"Google\Chrome\User Data");

        Browser chrome = MakeBrowser("Google Chrome", "Chrome");
        chrome = chrome with
        {
            ExecutablePath = @"C:\Program Files\Google\Chrome\Application\chrome.exe",
        };

        Browser edge = MakeBrowser("Microsoft Edge", "Microsoft Edge");
        edge = edge with
        {
            ExecutablePath = @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
        };

        List<BrowserProfile> chromeProfiles =
        [
            new(Name: "Profile 6", DirectoryName: "Profile 6", IsDefault: false),
            new(Name: "Profile 7", DirectoryName: "Profile 7", IsDefault: false),
        ];

        List<PickerBrowserOption> options =
        [
            new(chrome, chromeProfiles[0]),
            new(chrome, chromeProfiles[1]),
            new(edge, null),
        ];

        PickerRequest request = new(
            OriginalUrl: new Uri("https://example.com"),
            SourceApp: "slack",
            UnshortenTask: null,
            AvailableBrowsers: options);

        IIconProvider icons = new AskMeFirst.Platforms.Windows.WindowsIconProvider();
        using PickerWindowViewModel vm = new(request, new NullLogger(), icons: icons);
        Window window = new PickerWindow(vm)
        {
            Width = 720,
            Height = 440,
        };

        window.Show();
        PumpLayout(window);

        WriteableBitmap? frame = window.CaptureRenderedFrame();
        Assert.NotNull(frame);
        frame!.Save(screenshotPath);
        Assert.True(File.Exists(screenshotPath), $"File not found at {screenshotPath}");
        long size = new FileInfo(screenshotPath).Length;
        Assert.True(size > 1000, $"File too small: {size} bytes");
    }
#endif

    private static string? _lastScreenshot;
    public static string? LastScreenshot => _lastScreenshot;

    private static List<Button> FindBrowserButtons(Window window)
    {
        ItemsControl? browserList = window.FindControl<ItemsControl>("BrowserList");
        Assert.NotNull(browserList);
        return browserList!.GetVisualDescendants()
            .OfType<Button>()
            .Where(b => b.Name == "PART_BrowserButton")
            .ToList();
    }

    private static void PumpLayout(Window window)
    {
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        for (int i = 0; i < 5; i++)
        {
            Dispatcher.UIThread.RunJobs();
        }
    }

    private static PickerRequest MakeRequest(string url, string? sourceApp = null, IReadOnlyList<Browser>? browsers = null) =>
        new(
            OriginalUrl: new Uri(url),
            SourceApp: sourceApp,
            UnshortenTask: null,
            AvailableBrowsers: (browsers ?? [MakeBrowser("chrome", "Chrome")]).Select(b => new PickerBrowserOption(b, b.Profile)).ToList());

    private static Browser MakeBrowser(string id, string name) =>
        new()
        {
            Id = id,
            DisplayName = name,
            ExecutablePath = $"/{id}",
            LaunchStrategy = BrowserLaunchStrategies.For(id),
        };

    private sealed class NullLogger : ILogger
    {
        public void LogInfo(string message) { }
        public void LogWarn(string message) { }
        public void LogError(string message) { }
    }
}