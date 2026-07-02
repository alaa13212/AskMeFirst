using AskMeFirst.Core.Config;
using AskMeFirst.Core.Models;
using AskMeFirst.Core.Routing;
using Xunit;

namespace AskMeFirst.Core.Tests;

public class RoutingExecutorTests
{
    private static RoutingIntent Intent(
        string browserId,
        string? profileId = null,
        bool? stripTrackingOverride = null,
        bool newWindow = false,
        RoutingExitCode notFoundExitCode = RoutingExitCode.BrowserNotFound,
        string notFoundPrefix = "Browser")
    {
        return new RoutingIntent(browserId, profileId, stripTrackingOverride, newWindow, notFoundExitCode, notFoundPrefix);
    }

    private static AppConfig AppConfigWithStripTracking(bool stripTracking)
    {
        return new AppConfig
        {
            Settings = new Settings { StripTracking = stripTracking },
        };
    }

    [Fact]
    public void EmptyInventory_ReturnsNoBrowsersFailure()
    {
        FakeInventory inv = new();
        FakeProfileDetector profiles = new();
        FakeLogger logger = new();
        AppConfig config = AppConfigWithStripTracking(true);

        RoutingExecutor executor = new(inv, new ProfileResolver(profiles, [], logger), new TrackingStripper(config), config);
        RoutingOutcome outcome = executor.Execute(Intent("chrome"), new Uri("https://example.com"));

        Failure failure = Assert.IsType<Failure>(outcome);
        Assert.Equal(RoutingExitCode.NoBrowsersDiscovered, failure.Code);
        Assert.Contains("No browsers discovered", failure.Message);
    }

    [Fact]
    public void BrowserNotInInventory_ReturnsFailureWithIntentExitCode()
    {
        FakeInventory inv = new()
        {
            Browsers = { TestBrowser.Make("firefox", "Firefox", "/ff") },
        };
        FakeProfileDetector profiles = new();
        FakeLogger logger = new();
        AppConfig config = AppConfigWithStripTracking(true);

        RoutingExecutor executor = new(inv, new ProfileResolver(profiles, [], logger), new TrackingStripper(config), config);
        RoutingIntent intent = Intent("does-not-exist",
            notFoundExitCode: RoutingExitCode.RuleBrowserNotFound,
            notFoundPrefix: "Rule matched browser");
        RoutingOutcome outcome = executor.Execute(intent, new Uri("https://example.com"));

        Failure failure = Assert.IsType<Failure>(outcome);
        Assert.Equal(RoutingExitCode.RuleBrowserNotFound, failure.Code);
        Assert.Contains("Rule matched browser", failure.Message);
        Assert.Contains("does-not-exist", failure.Message);
        Assert.Contains("firefox", failure.Message);
    }

    [Fact]
    public void BrowserNotInInventory_GenericPrefix_UsesIntentPrefix()
    {
        FakeInventory inv = new()
        {
            Browsers = { TestBrowser.Make("firefox", "Firefox", "/ff") },
        };
        FakeProfileDetector profiles = new();
        FakeLogger logger = new();
        AppConfig config = AppConfigWithStripTracking(true);

        RoutingExecutor executor = new(inv, new ProfileResolver(profiles, [], logger), new TrackingStripper(config), config);
        RoutingIntent intent = Intent("missing",
            notFoundExitCode: RoutingExitCode.BrowserNotFound,
            notFoundPrefix: "Browser");
        RoutingOutcome outcome = executor.Execute(intent, new Uri("https://example.com"));

        Failure failure = Assert.IsType<Failure>(outcome);
        Assert.Equal(RoutingExitCode.BrowserNotFound, failure.Code);
        Assert.Contains("Browser 'missing' not found", failure.Message);
    }

    [Fact]
    public void BrowserFound_ReturnsSuccessWithResolvedBrowser()
    {
        FakeInventory inv = new()
        {
            Browsers = { TestBrowser.Make("firefox", "Firefox", "/ff") },
        };
        FakeProfileDetector profiles = new();
        FakeLogger logger = new();
        AppConfig config = AppConfigWithStripTracking(true);

        RoutingExecutor executor = new(inv, new ProfileResolver(profiles, [], logger), new TrackingStripper(config), config);
        Uri url = new("https://example.com");
        RoutingOutcome outcome = executor.Execute(Intent("firefox"), url);

        Success success = Assert.IsType<Success>(outcome);
        Assert.Equal("firefox", success.Browser.Id);
        Assert.Equal(url, success.OriginalUrl);
        Assert.Equal(url, success.FinalUrl);
    }

    [Fact]
    public void StripTrackingTrue_StripsFromFinalUrl()
    {
        FakeInventory inv = new()
        {
            Browsers = { TestBrowser.Make("firefox", "Firefox", "/ff") },
        };
        FakeProfileDetector profiles = new();
        FakeLogger logger = new();
        AppConfig config = AppConfigWithStripTracking(true);

        RoutingExecutor executor = new(inv, new ProfileResolver(profiles, [], logger), new TrackingStripper(config), config);
        Uri url = new("https://example.com/?utm_source=x&q=keep");
        RoutingOutcome outcome = executor.Execute(Intent("firefox"), url);

        Success success = Assert.IsType<Success>(outcome);
        Assert.DoesNotContain("utm_source", success.FinalUrl.ToString());
        Assert.Contains("q=keep", success.FinalUrl.ToString());
    }

    [Fact]
    public void StripTrackingFalse_PreservesQuery()
    {
        FakeInventory inv = new()
        {
            Browsers = { TestBrowser.Make("firefox", "Firefox", "/ff") },
        };
        FakeProfileDetector profiles = new();
        FakeLogger logger = new();
        AppConfig config = AppConfigWithStripTracking(false);

        RoutingExecutor executor = new(inv, new ProfileResolver(profiles, [], logger), new TrackingStripper(config), config);
        Uri url = new("https://example.com/?utm_source=x");
        RoutingOutcome outcome = executor.Execute(Intent("firefox"), url);

        Success success = Assert.IsType<Success>(outcome);
        Assert.Contains("utm_source", success.FinalUrl.ToString());
    }

    [Fact]
    public void StripTrackingOverride_PreservesQuery_EvenWhenGlobalIsStrip()
    {
        FakeInventory inv = new()
        {
            Browsers = { TestBrowser.Make("firefox", "Firefox", "/ff") },
        };
        FakeProfileDetector profiles = new();
        FakeLogger logger = new();
        AppConfig config = AppConfigWithStripTracking(true);

        RoutingExecutor executor = new(inv, new ProfileResolver(profiles, [], logger), new TrackingStripper(config), config);
        Uri url = new("https://example.com/?utm_source=x");
        RoutingOutcome outcome = executor.Execute(Intent("firefox", stripTrackingOverride: false), url);

        Success success = Assert.IsType<Success>(outcome);
        Assert.Contains("utm_source", success.FinalUrl.ToString());
    }

    [Fact]
    public void ProfileResolve_SetsProfileOnBrowser()
    {
        FakeInventory inv = new()
        {
            Browsers = { TestBrowser.Make("firefox", "Firefox", "/ff") },
        };
        FakeProfileDetector profiles = new()
        {
            Profiles =
            {
                ["firefox"] = [new BrowserProfile("Work", "work-dir", IsDefault: true)],
            },
        };
        FakeLogger logger = new();
        AppConfig config = AppConfigWithStripTracking(false);

        RoutingExecutor executor = new(inv, new ProfileResolver(profiles, [], logger), new TrackingStripper(config), config);
        Uri url = new("https://example.com");
        RoutingOutcome outcome = executor.Execute(Intent("firefox", profileId: "work"), url);

        Success success = Assert.IsType<Success>(outcome);
        Assert.NotNull(success.Browser.Profile);
        Assert.Equal("Work", success.Browser.Profile!.Name);
    }
}