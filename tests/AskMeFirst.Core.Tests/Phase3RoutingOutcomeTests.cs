using AskMeFirst.Core;
using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Config;
using AskMeFirst.Core.Models;
using AskMeFirst.Core.Routing;
using Xunit;

namespace AskMeFirst.Core.Tests;

public class Phase3RoutingOutcomeTests
{
    [Fact]
    public void ShowPicker_IsValidRoutingOutcome()
    {
        PickerRequest req = new(
            OriginalUrl: new Uri("https://example.com"),
            SourceApp: null,
            UnshortenTask: null,
            AvailableBrowsers: []);
        RoutingOutcome outcome = new ShowPicker(req);

        ShowPicker sp = Assert.IsType<ShowPicker>(outcome);
        Assert.Same(req, sp.Request);
    }

    [Fact]
    public void Success_IsStillValidRoutingOutcome()
    {
        Browser browser = TestBrowser.Make("test", "Test", "/test");
        RoutingOutcome outcome = new Success(browser, new Uri("https://example.com"), new Uri("https://example.com"));
        Assert.IsType<Success>(outcome);
    }

    [Fact]
    public void Failure_IsStillValidRoutingOutcome()
    {
        RoutingOutcome outcome = new Failure(RoutingExitCode.BrowserNotFound, "missing");
        Failure f = Assert.IsType<Failure>(outcome);
        Assert.Equal(RoutingExitCode.BrowserNotFound, f.Code);
    }

    [Fact]
    public void UsePickerAsCatchAll_False_ReturnsNoRouteFound()
    {
        FakeInventory inv = new();
        FakeLauncher launcher = new();
        FakeProfileDetector profiles = new();
        FakeSourceAppDetector sourceApp = new();
        FakeLogger logger = new();
        RecordingPickerLauncher picker = new();
        RuleRouter router = BuildRouter(inv, launcher, profiles, sourceApp, picker, logger, usePickerAsCatchAll: false);

        int code = router.Route(new Uri("https://example.com"), null, null);

        Assert.Equal((int)RoutingExitCode.NoRouteFound, code);
        Assert.Empty(picker.Requests);
    }

    [Fact]
    public void UsePickerAsCatchAll_True_InvokesPicker()
    {
        FakeInventory inv = new()
        {
            Browsers = { TestBrowser.Make("chrome-personal", "Chrome", "/chrome") },
        };
        FakeLauncher launcher = new();
        FakeProfileDetector profiles = new();
        FakeSourceAppDetector sourceApp = new() { Value = new SourceApp("slack", null, "") };
        FakeLogger logger = new();
        RecordingPickerLauncher picker = new();
        RuleRouter router = BuildRouter(inv, launcher, profiles, sourceApp, picker, logger, usePickerAsCatchAll: true);

        int code = router.Route(new Uri("https://example.com"), null, null);

        Assert.Equal((int)RoutingExitCode.Success, code);
        Assert.Single(picker.Requests);
        Assert.Equal("example.com", picker.Requests[0].OriginalUrl.Host);
        Assert.Equal("slack", picker.Requests[0].SourceApp);
        Assert.Single(picker.Requests[0].AvailableBrowsers);
    }

    [Fact]
    public void UsePickerAsCatchAll_True_PickerReturnsLaunched_LaunchesBrowser()
    {
        Browser browser = TestBrowser.Make("chrome-personal", "Chrome", "/chrome");
        FakeInventory inv = new() { Browsers = { browser } };
        FakeLauncher launcher = new();
        FakeProfileDetector profiles = new();
        FakeSourceAppDetector sourceApp = new();
        FakeLogger logger = new();
        BrowserLaunchingPickerLauncher picker = new(browser);
        RuleRouter router = BuildRouter(inv, launcher, profiles, sourceApp, picker, logger, usePickerAsCatchAll: true);

        int code = router.Route(new Uri("https://example.com"), null, null);

        Assert.Equal((int)RoutingExitCode.Success, code);
        Assert.Single(launcher.Launches);
        Assert.Equal("chrome-personal", launcher.Launches[0].Browser.Id);
    }

    [Fact]
    public void UsePickerAsCatchAll_True_PickerCancelled_ReturnsSuccess()
    {
        FakeInventory inv = new()
        {
            Browsers = { TestBrowser.Make("chrome-personal", "Chrome", "/chrome") },
        };
        FakeLauncher launcher = new();
        FakeProfileDetector profiles = new();
        FakeSourceAppDetector sourceApp = new();
        FakeLogger logger = new();
        RecordingPickerLauncher picker = new();   // returns Cancelled
        RuleRouter router = BuildRouter(inv, launcher, profiles, sourceApp, picker, logger, usePickerAsCatchAll: true);

        int code = router.Route(new Uri("https://example.com"), null, null);

        Assert.Equal((int)RoutingExitCode.Success, code);
        Assert.Empty(launcher.Launches);
        Assert.Contains(logger.Infos, m => m.Contains("cancelled"));
    }

    private static RuleRouter BuildRouter(
        FakeInventory inv,
        FakeLauncher launcher,
        FakeProfileDetector profiles,
        FakeSourceAppDetector sourceApp,
        IPickerLauncher picker,
        FakeLogger logger,
        bool usePickerAsCatchAll)
    {
        AppConfig empty = new() { Settings = new Settings { DefaultBrowserId = null }, Rules = [] };
        PredicateEvaluator evaluator = TestEvaluator.Default();
        IReadOnlyList<ITargetResolver> resolvers = TestResolvers.For(empty, evaluator);
        ProfileResolver profileResolver = new(profiles, empty.Profiles, logger);
        TrackingStripper stripper = new(empty);
        IRoutingExecutor executor = new RoutingExecutor(inv, profileResolver, stripper, empty);
        return new RuleRouter(
            resolvers,
            executor,
            sourceApp,
            picker,
            usePickerAsCatchAll,
            launcher,
            logger,
            new FixedTimeProvider(new DateTimeOffset(2026, 6, 1, 10, 0, 0, TimeSpan.Zero)));
    }

    private sealed class BrowserLaunchingPickerLauncher(Browser browser) : IPickerLauncher
    {
        public PickerResult Show(PickerRequest request) => new Launched(browser, request.OriginalUrl);
    }
}