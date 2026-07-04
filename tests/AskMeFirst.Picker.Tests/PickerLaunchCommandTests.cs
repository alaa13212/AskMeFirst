using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Launch;
using AskMeFirst.Core.Models;
using AskMeFirst.Core.Routing;
using AskMeFirst.Picker.ViewModels;
using Xunit;

namespace AskMeFirst.Picker.Tests;

public class PickerLaunchCommandTests
{
    [AvaloniaFact]
    public void Commit_FirefoxProfile_BuildsCorrectLaunchCommand()
    {
        Browser firefox = new()
        {
            Id = "firefox",
            DisplayName = "Firefox",
            ExecutablePath = "/usr/bin/firefox",
            LaunchStrategy = FirefoxLaunchStrategy.Instance,
        };

        BrowserProfile workProfile = new("Work", "Work", IsDefault: false);
        PickerRequest request = new(
            OriginalUrl: new Uri("https://google.com/"),
            SourceApp: "slack",
            UnshortenTask: null,
            AvailableBrowsers:
            [
                new PickerBrowserOption(firefox, workProfile),
            ]);

        using PickerWindowViewModel vm = new(request, new NullLogger());
        Window window = new PickerWindow(vm);
        window.Show();
        PumpLayout(window);

        Button button = window.GetVisualDescendants().OfType<Button>()
            .First(b => b.Name == "PART_BrowserButton");
        button.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        PumpLayout(window);

        Launched launched = Assert.IsType<Launched>(vm.Result);
        Assert.Equal("firefox", launched.Browser.Id);
        Assert.Equal("Work", launched.Browser.Profile!.Name);
        Assert.Equal("Work", launched.Browser.Profile!.DirectoryName);

        string[] args = launched.Browser.LaunchStrategy.BuildArguments(
            launched.Url, launched.Browser.Profile, launched.Browser.NewWindow);
        string fullCommand = $"/usr/bin/firefox {string.Join(" ", args)}";

        Assert.Equal(3, args.Length);
        Assert.Equal("-profile", args[0]);
        Assert.EndsWith("/Work", args[1]);
        Assert.Equal("https://google.com/", args[2]);

        string profilePath = args[1];
        Assert.True(
            profilePath.EndsWith(".mozilla/firefox/Work", StringComparison.Ordinal)
            || profilePath.EndsWith(".config/mozilla/firefox/Work", StringComparison.Ordinal),
            $"Firefox profile path should be under .mozilla/firefox or .config/mozilla/firefox, got: {profilePath}");
    }

    [AvaloniaFact]
    public void Commit_ChromeFlatpakProfile_BuildsCorrectLaunchCommand()
    {
        Browser chrome = new()
        {
            Id = "chrome",
            DisplayName = "Google Chrome",
            ExecutablePath = "/usr/bin/flatpak",
            LaunchStrategy = new FlatpakLaunchStrategy("com.google.Chrome", ChromiumLaunchStrategy.Instance),
            FlatpakAppId = "com.google.Chrome",
        };

        BrowserProfile workProfile = new("Work", "Profile 1", IsDefault: false);
        PickerRequest request = new(
            OriginalUrl: new Uri("https://google.com/"),
            SourceApp: "slack",
            UnshortenTask: null,
            AvailableBrowsers:
            [
                new PickerBrowserOption(chrome, workProfile),
            ]);

        using PickerWindowViewModel vm = new(request, new NullLogger());
        Window window = new PickerWindow(vm);
        window.Show();
        PumpLayout(window);

        Button button = window.GetVisualDescendants().OfType<Button>()
            .First(b => b.Name == "PART_BrowserButton");
        button.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        PumpLayout(window);

        Launched launched = Assert.IsType<Launched>(vm.Result);
        Assert.Equal("chrome", launched.Browser.Id);
        Assert.Equal("Work", launched.Browser.Profile!.Name);
        Assert.Equal("Profile 1", launched.Browser.Profile!.DirectoryName);

        string[] args = launched.Browser.LaunchStrategy.BuildArguments(
            launched.Url, launched.Browser.Profile, launched.Browser.NewWindow);
        string fullCommand = $"/usr/bin/flatpak {string.Join(" ", args)}";

        Assert.Equal(4, args.Length);
        Assert.Equal("run", args[0]);
        Assert.Equal("com.google.Chrome", args[1]);
        Assert.Equal("--profile-directory=Profile 1", args[2]);
        Assert.Equal("https://google.com/", args[3]);

        Assert.Equal(
            "/usr/bin/flatpak run com.google.Chrome --profile-directory=Profile 1 https://google.com/",
            fullCommand);
    }

    [AvaloniaFact]
    public void Commit_OperaGxProfile_BuildsCorrectLaunchCommand()
    {
        Browser operaGx = new()
        {
            Id = "opera-gx",
            DisplayName = "Opera GX",
            ExecutablePath = "/usr/bin/flatpak",
            LaunchStrategy = new FlatpakLaunchStrategy("com.opera.opera-gx", ChromiumLaunchStrategy.Instance),
            FlatpakAppId = "com.opera.opera-gx",
        };

        BrowserProfile defaultProfile = new("Default", "Default", IsDefault: true);
        PickerRequest request = new(
            OriginalUrl: new Uri("https://google.com/"),
            SourceApp: "slack",
            UnshortenTask: null,
            AvailableBrowsers:
            [
                new PickerBrowserOption(operaGx, defaultProfile),
            ]);

        using PickerWindowViewModel vm = new(request, new NullLogger());
        Window window = new PickerWindow(vm);
        window.Show();
        PumpLayout(window);

        Button button = window.GetVisualDescendants().OfType<Button>()
            .First(b => b.Name == "PART_BrowserButton");
        button.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        PumpLayout(window);

        Launched launched = Assert.IsType<Launched>(vm.Result);

        string[] args = launched.Browser.LaunchStrategy.BuildArguments(
            launched.Url, launched.Browser.Profile, launched.Browser.NewWindow);

        Assert.Equal(4, args.Length);
        Assert.Equal("run", args[0]);
        Assert.Equal("com.opera.opera-gx", args[1]);
        Assert.Equal("--profile-directory=Default", args[2]);
        Assert.Equal("https://google.com/", args[3]);
    }

    [AvaloniaFact]
    public void Commit_PicksSecondOfThreeBrowsers_CorrectBrowserSelected()
    {
        Browser chrome = new()
        {
            Id = "chrome",
            DisplayName = "Google Chrome",
            ExecutablePath = "/usr/bin/flatpak",
            LaunchStrategy = new FlatpakLaunchStrategy("com.google.Chrome", ChromiumLaunchStrategy.Instance),
        };
        Browser firefox = new()
        {
            Id = "firefox",
            DisplayName = "Firefox",
            ExecutablePath = "/usr/bin/firefox",
            LaunchStrategy = FirefoxLaunchStrategy.Instance,
        };
        Browser operaGx = new()
        {
            Id = "opera-gx",
            DisplayName = "Opera GX",
            ExecutablePath = "/usr/bin/flatpak",
            LaunchStrategy = new FlatpakLaunchStrategy("com.opera.opera-gx", ChromiumLaunchStrategy.Instance),
        };

        PickerRequest request = new(
            OriginalUrl: new Uri("https://google.com/"),
            SourceApp: null,
            UnshortenTask: null,
            AvailableBrowsers:
            [
                new PickerBrowserOption(chrome, new BrowserProfile("Default", "Default", IsDefault: true)),
                new PickerBrowserOption(firefox, new BrowserProfile("Work", "Work", IsDefault: false)),
                new PickerBrowserOption(operaGx, new BrowserProfile("Default", "Default", IsDefault: true)),
            ]);

        using PickerWindowViewModel vm = new(request, new NullLogger());
        Window window = new PickerWindow(vm);
        window.Show();
        PumpLayout(window);

        List<Button> buttons = window.GetVisualDescendants().OfType<Button>()
            .Where(b => b.Name == "PART_BrowserButton")
            .ToList();
        Assert.Equal(3, buttons.Count);

        buttons[1].RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        PumpLayout(window);

        Launched launched = Assert.IsType<Launched>(vm.Result);
        Assert.Equal("firefox", launched.Browser.Id);
        Assert.Equal("Work", launched.Browser.Profile!.Name);
    }

    [AvaloniaFact]
    public void Commit_PicksChromeWithWorkProfile_ArgOrderIsFlagThenUrl()
    {
        Browser chrome = new()
        {
            Id = "chrome",
            DisplayName = "Google Chrome",
            ExecutablePath = "/usr/bin/flatpak",
            LaunchStrategy = new FlatpakLaunchStrategy("com.google.Chrome", ChromiumLaunchStrategy.Instance),
            FlatpakAppId = "com.google.Chrome",
        };
        BrowserProfile profile = new("Work", "Profile 1", IsDefault: false);

        string[] args = chrome.LaunchStrategy.BuildArguments(new Uri("https://google.com/"), profile, newWindow: false);

        int profileFlagIndex = Array.FindIndex(args, a => a.StartsWith("--profile-directory=", StringComparison.Ordinal));
        int urlIndex = Array.FindIndex(args, a => a == "https://google.com/");

        Assert.True(profileFlagIndex >= 0, $"--profile-directory flag not found in: {string.Join(' ', args)}");
        Assert.True(urlIndex >= 0, $"URL not found in: {string.Join(' ', args)}");
        Assert.True(profileFlagIndex < urlIndex,
            $"Profile flag must come BEFORE URL, got: {string.Join(' ', args)}");
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

    private sealed class NullLogger : ILogger
    {
        public void LogInfo(string message) { }
        public void LogWarn(string message) { }
        public void LogError(string message) { }
    }
}