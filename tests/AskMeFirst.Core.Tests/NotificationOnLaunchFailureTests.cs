using AskMeFirst.Core;
using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Config;
using AskMeFirst.Core.Models;
using AskMeFirst.Core.Routing;
using Xunit;

namespace AskMeFirst.Core.Tests;

public class NotificationOnLaunchFailureTests
{
    [Fact]
    public void RoutingLauncher_Throws_NotifiesUser_ReturnsBrowserNotFound()
    {
        ThrowingLauncher launcher = new();
        FakeNotifier notifier = new();
        FakeLogger logger = new();
        RuleRouter router = BuildRouter(launcher, notifier, logger);

        int code = router.Route(new Uri("https://example.com"), "chrome-work", null);

        Assert.Equal((int)RoutingExitCode.BrowserNotFound, code);
        Assert.Single(notifier.Calls);
        Assert.Equal("Couldn't open browser", notifier.Calls[0].Title);
        Assert.Contains("Chrome Work", notifier.Calls[0].Message);
        Assert.Contains("https://example.com", notifier.Calls[0].Message);
    }

    [Fact]
    public void PickerLauncher_ReturnsLaunched_ButLauncherThrows_NotifiesAndReturnsFailure()
    {
        ThrowingLauncher launcher = new();
        FakeNotifier notifier = new();
        FakeLogger logger = new();
        Browser browser = TestBrowser.Make("firefox-work", "Firefox Work", "/ff");
        BrowserLaunchingPickerLauncher picker = new(browser);
        RuleRouter router = BuildRouter(launcher, notifier, logger, picker, usePickerAsCatchAll: true);

        int code = router.Route(new Uri("https://example.com"), null, null);

        Assert.Equal((int)RoutingExitCode.BrowserNotFound, code);
        Assert.Single(notifier.Calls);
    }

    [Fact]
    public void NullNotifier_DoesNothing()
    {
        NullNotifier notifier = new();
        notifier.Show("title", "message");
    }

    private static RuleRouter BuildRouter(
        IUrlLauncher launcher,
        INotifier notifier,
        FakeLogger logger,
        IPickerLauncher? picker = null,
        bool usePickerAsCatchAll = false)
    {
        FakeInventory inv = new()
        {
            Browsers = { TestBrowser.Make("chrome-work", "Chrome Work", "/chrome") },
        };
        AppConfig empty = new() { Rules = [] };
        PredicateEvaluator evaluator = TestEvaluator.Default();
        IReadOnlyList<ITargetResolver> resolvers = TestResolvers.For(empty, evaluator);
        ProfileResolver profileResolver = new(new FakeProfileDetector(), empty.Profiles, logger);
        TrackingStripper stripper = new(empty);
        IRoutingExecutor executor = new RoutingExecutor(inv, profileResolver, stripper, empty);
        return new RuleRouter(
            resolvers,
            executor,
            inv,
            new FakeSourceAppDetector(),
            picker ?? new RecordingPickerLauncher(),
            usePickerAsCatchAll,
            empty.Profiles,
            new FakeProfileDetector(),
            launcher,
            logger,
            notifier,
            new FixedTimeProvider(new DateTimeOffset(2026, 6, 1, 10, 0, 0, TimeSpan.Zero)));
    }

    private sealed class BrowserLaunchingPickerLauncher(Browser browser) : IPickerLauncher
    {
        public PickerResult Show(PickerRequest request) => new Launched(browser, request.OriginalUrl);
    }
}