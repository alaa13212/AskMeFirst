using AskMeFirst.Core;
using AskMeFirst.Core.Config;
using AskMeFirst.Core.Models;
using Xunit;

namespace AskMeFirst.Core.Tests;

public class UrlRouterTests
{
    private static UrlRouter BuildRouter(FakeInventory inv, FakeLauncher launcher, FakeLogger logger)
    {
        return new UrlRouter(inv, launcher, logger, ConfigLoader.LoadDefault());
    }

    [Fact]
    public void Route_NoBrowserId_UsesFirstDiscovered()
    {
        Browser chrome = new("chrome", "Chrome", @"C:\Program Files\Chrome\chrome.exe");
        Browser firefox = new("firefox", "Firefox", @"C:\Program Files\Firefox\firefox.exe");
        FakeInventory inv = new() { Browsers = [chrome, firefox] };
        FakeLauncher launcher = new();
        FakeLogger logger = new();
        UrlRouter router = BuildRouter(inv, launcher, logger);

        int code = router.Route(new Uri("https://example.com"), browserId: null);

        Assert.Equal(0, code);
        Assert.Single(launcher.Launches);
        Assert.Equal(chrome, launcher.Launches[0].Browser);
        Assert.Equal(new Uri("https://example.com"), launcher.Launches[0].Url);
    }

    [Fact]
    public void Route_BrowserId_FindsAndLaunchesThatBrowser()
    {
        Browser chrome = new("chrome", "Chrome", @"C:\chrome.exe");
        Browser firefox = new("firefox", "Firefox", @"C:\firefox.exe");
        FakeInventory inv = new() { Browsers = [chrome, firefox] };
        FakeLauncher launcher = new();
        FakeLogger logger = new();
        UrlRouter router = BuildRouter(inv, launcher, logger);

        int code = router.Route(new Uri("https://example.com"), "firefox");

        Assert.Equal(0, code);
        Assert.Equal(firefox, launcher.Launches[0].Browser);
    }

    [Fact]
    public void Route_BrowserIdCaseInsensitive()
    {
        Browser chrome = new("chrome", "Chrome", @"C:\chrome.exe");
        FakeInventory inv = new() { Browsers = [chrome] };
        FakeLauncher launcher = new();
        FakeLogger logger = new();
        UrlRouter router = BuildRouter(inv, launcher, logger);

        int code = router.Route(new Uri("https://example.com"), "CHROME");

        Assert.Equal(0, code);
        Assert.Equal(chrome, launcher.Launches[0].Browser);
    }

    [Fact]
    public void Route_SystemKeyword_FallsBackToFirstDiscovered()
    {
        Browser chrome = new("chrome", "Chrome", @"C:\chrome.exe");
        FakeInventory inv = new() { Browsers = [chrome] };
        FakeLauncher launcher = new();
        FakeLogger logger = new();
        UrlRouter router = BuildRouter(inv, launcher, logger);

        int code = router.Route(new Uri("https://example.com"), "system");

        Assert.Equal(0, code);
        Assert.Equal(chrome, launcher.Launches[0].Browser);
    }

    [Fact]
    public void Route_NoBrowsers_ReturnsError2()
    {
        FakeInventory inv = new() { Browsers = [] };
        FakeLauncher launcher = new();
        FakeLogger logger = new();
        UrlRouter router = BuildRouter(inv, launcher, logger);

        int code = router.Route(new Uri("https://example.com"), null);

        Assert.Equal(2, code);
        Assert.Empty(launcher.Launches);
        Assert.Contains(logger.Errors, e => e.Contains("No browsers discovered"));
    }

    [Fact]
    public void Route_UnknownBrowserId_ReturnsError3()
    {
        FakeInventory inv = new() { Browsers = [new("chrome", "Chrome", "x")] };
        FakeLauncher launcher = new();
        FakeLogger logger = new();
        UrlRouter router = BuildRouter(inv, launcher, logger);

        int code = router.Route(new Uri("https://example.com"), "lynx");

        Assert.Equal(3, code);
        Assert.Empty(launcher.Launches);
        Assert.Contains(logger.Errors, e => e.Contains("'lynx'"));
    }
}
