using AskMeFirst.Core;
using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Config;
using AskMeFirst.Core.Models;
using AskMeFirst.Core.Routing;
using Xunit;

namespace AskMeFirst.Core.Tests;

public class RuleRouterShortenerTests
{
    private static readonly DateTimeOffset Monday10amUtc = new(2026, 6, 1, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Catchall_ShortenerUrl_PassesUnshortenTask()
    {
        FakeInventory inv = new()
        {
            Browsers = { TestBrowser.Make("chrome-personal", "Chrome", "/chrome") },
        };
        FakeLauncher launcher = new();
        FakeProfileDetector profiles = new();
        FakeLogger logger = new();
        FakeUnshortener unshortener = new();
        FakeShortenerDomainList shorteners = new() { Hosts = { "t.co" } };
        RecordingPickerLauncher picker = new();
        RuleRouter router = BuildRouter(inv, launcher, profiles, logger, new AppConfig(), unshortener, shorteners, picker);

        int code = router.Route(new Uri("https://t.co/abc"), null, null);

        Assert.Equal((int)RoutingExitCode.Success, code);
        Assert.Single(picker.Requests);
        Assert.NotNull(picker.Requests[0].UnshortenTask);
        Assert.Single(unshortener.ResolveCalls);
        Assert.Equal(new Uri("https://t.co/abc"), unshortener.ResolveCalls[0]);
    }

    [Fact]
    public void Catchall_NonShortenerUrl_PassesNullUnshortenTask()
    {
        FakeInventory inv = new()
        {
            Browsers = { TestBrowser.Make("chrome-personal", "Chrome", "/chrome") },
        };
        FakeLauncher launcher = new();
        FakeProfileDetector profiles = new();
        FakeLogger logger = new();
        FakeUnshortener unshortener = new();
        FakeShortenerDomainList shorteners = new() { Hosts = { "t.co" } };
        RecordingPickerLauncher picker = new();
        RuleRouter router = BuildRouter(inv, launcher, profiles, logger, new AppConfig(), unshortener, shorteners, picker);

        router.Route(new Uri("https://example.com/article"), null, null);

        Assert.Single(picker.Requests);
        Assert.Null(picker.Requests[0].UnshortenTask);
        Assert.Empty(unshortener.ResolveCalls);
    }

    [Fact]
    public async Task Catchall_ResolvedUrlIsStripped_BeforeReachingPicker()
    {
        FakeInventory inv = new()
        {
            Browsers = { TestBrowser.Make("chrome-personal", "Chrome", "/chrome") },
        };
        FakeLauncher launcher = new();
        FakeProfileDetector profiles = new();
        FakeLogger logger = new();
        FakeUnshortener unshortener = new()
        {
            ResolveResult = _ => "https://example.com/article?utm_source=tracker&page=2",
        };
        FakeShortenerDomainList shorteners = new() { Hosts = { "t.co" } };
        RecordingPickerLauncher picker = new();
        RuleRouter router = BuildRouter(inv, launcher, profiles, logger, new AppConfig(), unshortener, shorteners, picker);

        router.Route(new Uri("https://t.co/abc"), null, null);

        Task<string?>? task = picker.Requests[0].UnshortenTask;
        Assert.NotNull(task);
        string? resolved = await task!;
        Assert.Equal("https://example.com/article?page=2", resolved);
    }

    [Fact]
    public async Task Catchall_UnshortenReturnsNull_TaskCompletesWithNull()
    {
        FakeInventory inv = new()
        {
            Browsers = { TestBrowser.Make("chrome-personal", "Chrome", "/chrome") },
        };
        FakeLauncher launcher = new();
        FakeProfileDetector profiles = new();
        FakeLogger logger = new();
        FakeUnshortener unshortener = new() { ResolveResult = _ => null };
        FakeShortenerDomainList shorteners = new() { Hosts = { "t.co" } };
        RecordingPickerLauncher picker = new();
        RuleRouter router = BuildRouter(inv, launcher, profiles, logger, new AppConfig(), unshortener, shorteners, picker);

        router.Route(new Uri("https://t.co/abc"), null, null);

        Task<string?>? task = picker.Requests[0].UnshortenTask;
        Assert.NotNull(task);
        Assert.Null(await task!);
    }

    private static RuleRouter BuildRouter(
        FakeInventory inv,
        FakeLauncher launcher,
        FakeProfileDetector profiles,
        FakeLogger logger,
        AppConfig appConfig,
        FakeUnshortener unshortener,
        FakeShortenerDomainList shorteners,
        IPickerLauncher picker)
    {
        PredicateEvaluator evaluator = TestEvaluator.Default();
        IReadOnlyList<ITargetResolver> resolvers = TestResolvers.For(appConfig, evaluator);
        ProfileResolver profileResolver = new(profiles, appConfig.Profiles, logger);
        TrackingStripper stripper = new(appConfig);
        IRoutingExecutor executor = new RoutingExecutor(inv, profileResolver, stripper, appConfig);
        return new RuleRouter(
            resolvers,
            executor,
            inv,
            picker,
            usePickerAsCatchAll: true,
            appConfig.Profiles,
            profiles,
            launcher,
            logger,
            new NullNotifier(),
            new FixedTimeProvider(Monday10amUtc),
            unshortener,
            shorteners,
            stripper);
    }

    private sealed class RecordingPickerLauncher : IPickerLauncher
    {
        public List<PickerRequest> Requests { get; } = [];

        public PickerResult Show(PickerRequest request)
        {
            Requests.Add(request);
            return new Cancelled();
        }
    }
}