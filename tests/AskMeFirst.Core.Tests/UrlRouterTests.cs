using AskMeFirst.Core.Config;
using AskMeFirst.Core.Models;
using Xunit;

namespace AskMeFirst.Core.Tests;

public class UrlRouterTests
{
    private static UrlRouter BuildRouter(
        FakeInventory inv,
        FakeLauncher launcher,
        FakeProfileDetector profiles,
        FakeLogger logger)
    {
        return new UrlRouter(
            inv,
            launcher,
            profiles,
            logger);
    }

    [Fact]
    public void Route_NoBrowserId_UsesFirstDiscovered()
    {
        Browser chrome = TestBrowser.Make("chrome", "Chrome", @"C:\Program Files\Chrome\chrome.exe");
        Browser firefox = TestBrowser.Make("firefox", "Firefox", @"C:\Program Files\Firefox\firefox.exe");
        FakeInventory inv = new() { Browsers = [chrome, firefox] };
        FakeLauncher launcher = new();
        FakeProfileDetector profiles = new();
        FakeLogger logger = new();
        UrlRouter router = BuildRouter(inv, launcher, profiles, logger);

        int code = router.Route(new Uri("https://example.com"), browserId: null, profileName: null);

        Assert.Equal(0, code);
        Assert.Single(launcher.Launches);
        Assert.Equal(chrome.Id, launcher.Launches[0].Browser.Id);
        Assert.Equal(new Uri("https://example.com"), launcher.Launches[0].Url);
    }

    [Fact]
    public void Route_BrowserId_FindsAndLaunchesThatBrowser()
    {
        Browser chrome = TestBrowser.Make("chrome", "Chrome", @"C:\chrome.exe");
        Browser firefox = TestBrowser.Make("firefox", "Firefox", @"C:\firefox.exe");
        FakeInventory inv = new() { Browsers = [chrome, firefox] };
        FakeLauncher launcher = new();
        FakeProfileDetector profiles = new();
        FakeLogger logger = new();
        UrlRouter router = BuildRouter(inv, launcher, profiles, logger);

        int code = router.Route(new Uri("https://example.com"), "firefox", profileName: null);

        Assert.Equal(0, code);
        Assert.Equal(firefox.Id, launcher.Launches[0].Browser.Id);
    }

    [Fact]
    public void Route_BrowserIdCaseInsensitive()
    {
        Browser chrome = TestBrowser.Make("chrome", "Chrome", @"C:\chrome.exe");
        FakeInventory inv = new() { Browsers = [chrome] };
        FakeLauncher launcher = new();
        FakeProfileDetector profiles = new();
        FakeLogger logger = new();
        UrlRouter router = BuildRouter(inv, launcher, profiles, logger);

        int code = router.Route(new Uri("https://example.com"), "CHROME", profileName: null);

        Assert.Equal(0, code);
        Assert.Equal(chrome.Id, launcher.Launches[0].Browser.Id);
    }

    [Fact]
    public void Route_NoBrowsers_ReturnsError2()
    {
        FakeInventory inv = new() { Browsers = [] };
        FakeLauncher launcher = new();
        FakeProfileDetector profiles = new();
        FakeLogger logger = new();
        UrlRouter router = BuildRouter(inv, launcher, profiles, logger);

        int code = router.Route(new Uri("https://example.com"), null, null);

        Assert.Equal(2, code);
        Assert.Empty(launcher.Launches);
        Assert.Contains(logger.Errors, e => e.Contains("No browsers discovered"));
    }

    [Fact]
    public void Route_UnknownBrowserId_ReturnsError3()
    {
        FakeInventory inv = new() { Browsers = [TestBrowser.Make("chrome", "Chrome", "x")] };
        FakeLauncher launcher = new();
        FakeProfileDetector profiles = new();
        FakeLogger logger = new();
        UrlRouter router = BuildRouter(inv, launcher, profiles, logger);

        int code = router.Route(new Uri("https://example.com"), "lynx", null);

        Assert.Equal(3, code);
        Assert.Empty(launcher.Launches);
        Assert.Contains(logger.Errors, e => e.Contains("'lynx'"));
    }

    [Fact]
    public void Route_ProfileName_MatchesAndAttachesToBrowser()
    {
        Browser chrome = TestBrowser.Make("chrome", "Chrome", @"C:\chrome.exe");
        FakeInventory inv = new() { Browsers = [chrome] };
        FakeLauncher launcher = new();
        FakeProfileDetector profiles = new()
        {
            Profiles =
            {
                ["chrome"] =
                [
                    new BrowserProfile("Default", "Default", IsDefault: true),
                    new BrowserProfile("Work", "Profile 1", IsDefault: false),
                ],
            },
        };
        FakeLogger logger = new();
        UrlRouter router = BuildRouter(inv, launcher, profiles, logger);

        int code = router.Route(new Uri("https://example.com"), "chrome", "Work");

        Assert.Equal(0, code);
        Assert.Equal("Profile 1", launcher.Launches[0].Browser.Profile!.DirectoryName);
    }

    [Fact]
    public void Route_NoProfileName_FallsBackToDefaultProfile()
    {
        Browser chrome = TestBrowser.Make("chrome", "Chrome", @"C:\chrome.exe");
        FakeInventory inv = new() { Browsers = [chrome] };
        FakeLauncher launcher = new();
        FakeProfileDetector profiles = new()
        {
            Profiles =
            {
                ["chrome"] =
                [
                    new BrowserProfile("Default", "Default", IsDefault: true),
                    new BrowserProfile("Work", "Profile 1", IsDefault: false),
                ],
            },
        };
        FakeLogger logger = new();
        UrlRouter router = BuildRouter(inv, launcher, profiles, logger);

        int code = router.Route(new Uri("https://example.com"), "chrome", null);

        Assert.Equal(0, code);
        Assert.Equal("Default", launcher.Launches[0].Browser.Profile!.DirectoryName);
    }

    [Fact]
    public void Route_ProfileDetectorReturnsEmpty_BrowserLaunchesUnchanged()
    {
        Browser chrome = TestBrowser.Make("chrome", "Chrome", @"C:\chrome.exe");
        FakeInventory inv = new() { Browsers = [chrome] };
        FakeLauncher launcher = new();
        FakeProfileDetector profiles = new();
        FakeLogger logger = new();
        UrlRouter router = BuildRouter(inv, launcher, profiles, logger);

        int code = router.Route(new Uri("https://example.com"), "chrome", null);

        Assert.Equal(0, code);
        Assert.Null(launcher.Launches[0].Browser.Profile);
    }

    [Fact]
    public void Route_UnknownProfileName_WarnsAndLaunchesDefault()
    {
        Browser chrome = TestBrowser.Make("chrome", "Chrome", @"C:\chrome.exe");
        FakeInventory inv = new() { Browsers = [chrome] };
        FakeLauncher launcher = new();
        FakeProfileDetector profiles = new()
        {
            Profiles =
            {
                ["chrome"] = [new BrowserProfile("Default", "Default", IsDefault: true)],
            },
        };
        FakeLogger logger = new();
        UrlRouter router = BuildRouter(inv, launcher, profiles, logger);

        int code = router.Route(new Uri("https://example.com"), "chrome", "nope");

        Assert.Equal(0, code);
        Assert.Equal("Default", launcher.Launches[0].Browser.Profile!.DirectoryName);
        Assert.Contains(logger.Warns, w => w.Contains("'nope'"));
    }

}