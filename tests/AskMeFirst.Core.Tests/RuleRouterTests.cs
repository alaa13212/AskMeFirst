using AskMeFirst.Core;
using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Config;
using AskMeFirst.Core.Models;
using AskMeFirst.Core.Routing;
using Xunit;

namespace AskMeFirst.Core.Tests;

public class RuleRouterTests
{
    private static readonly DateTimeOffset Monday10amUtc = new(2026, 6, 1, 10, 0, 0, TimeSpan.Zero);

    private static RuleRouter BuildRouter(
        FakeInventory inv,
        FakeLauncher launcher,
        FakeProfileDetector profiles,
        FakeLogger logger,
        AppConfig appConfig,
        DateTimeOffset? now = null)
    {
        PredicateEvaluator evaluator = TestEvaluator.Default();
        IReadOnlyList<ITargetResolver> resolvers = TestResolvers.For(appConfig, evaluator);
        ProfileResolver profileResolver = new(profiles, appConfig.Profiles, logger);
        TrackingStripper stripper = new(appConfig);
        IRoutingExecutor executor = new RoutingExecutor(inv, profileResolver, stripper, appConfig);
        IPickerLauncher pickerLauncher = new RecordingPickerLauncher();
        return new RuleRouter(
            resolvers,
            executor,
            inv,
            pickerLauncher,
            usePickerAsCatchAll: false,
            appConfig.Profiles,
            profiles,
            launcher,
            logger,
            new NullNotifier(),
            new FixedTimeProvider(now ?? Monday10amUtc),
            new FakeUnshortenTaskBuilder());
    }

    private static AppConfig TenRuleConfig()
    {
        return new AppConfig
        {
            Settings = new Settings
            {
                StripTracking = true,
            },
            Profiles = new ProfileSpec[]
            {
                new() { Id = "firefox-work-profile", BrowserId = "firefox-work", Name = "Work" },
            },
            Rules = new Rule[]
            {
                new()
                {
                    Name = "GitHub PRs",
                    Priority = 250,
                    When = new()
                    {
                        UrlRegex = "^https://github\\.com/[^/]+/[^/]+/pull/\\d+",
                    },
                    Then = new() { Browser = "chrome-work", StripTracking = true },
                },
                new()
                {
                    Name = "Work domains to Firefox Work profile",
                    Priority = 200,
                    When = new()
                    {
                        UrlMatchesAny = ["*.atlassian.net", "*.github.com", "*.slack.com"],
                    },
                    Then = new() { Browser = "firefox-work", ProfileId = "firefox-work-profile", StripTracking = true },
                },
                new()
                {
                    Name = "YouTube always personal",
                    Priority = 180,
                    When = new() { UrlMatchesAny = ["youtube.com", "youtu.be"] },
                    Then = new() { Browser = "chrome-personal" },
                },
                new()
                {
                    Name = "All matches: amazon must include /dp/",
                    Priority = 60,
                    When = new()
                    {
                        UrlMatchesAll = ["*.amazon.com", "*.amazon.com/dp/*"],
                    },
                    Then = new() { Browser = "chrome-personal" },
                },
                new()
                {
                    Name = "Https-only",
                    Priority = 40,
                    When = new() { SchemeIn = ["https"] },
                    Then = new() { Browser = "chrome-work" },
                },
                new()
                {
                    Name = "HTTP only to Chrome personal",
                    Priority = 35,
                    When = new() { SchemeIn = ["http"] },
                    Then = new() { Browser = "chrome-personal" },
                },
                new()
                {
                    Name = "Untouched: catchall",
                    Priority = 0,
                    When = new(),
                    Then = new() { Browser = "chrome-work" },
                },
            },
        };
    }

    private static FakeInventory StandardInventory()
    {
        return new FakeInventory
        {
            Browsers =
            {
                TestBrowser.Make("chrome-personal", "Chrome (Personal)", @"C:\chrome.exe"),
                TestBrowser.Make("chrome-work", "Chrome (Work)", @"C:\chrome.exe"),
                TestBrowser.Make("firefox-work", "Firefox (Work)", @"C:\firefox.exe"),
            },
        };
    }

    [Fact]
    public void ExplicitBrowser_OverridesRules()
    {
        FakeInventory inv = StandardInventory();
        FakeLauncher launcher = new();
        FakeProfileDetector profiles = new();
        FakeLogger logger = new();
        RuleRouter router = BuildRouter(inv, launcher, profiles, logger, TenRuleConfig());

        int code = router.Route(new Uri("https://company.atlassian.net/x"), "chrome-personal", null).ExitCode;

        Assert.Equal(0, code);
        Assert.Equal("chrome-personal", launcher.Launches[0].Browser.Id);
    }

    [Fact]
    public void ExplicitUnknownBrowser_ReturnsError()
    {
        FakeInventory inv = StandardInventory();
        FakeLauncher launcher = new();
        FakeProfileDetector profiles = new();
        FakeLogger logger = new();
        RuleRouter router = BuildRouter(inv, launcher, profiles, logger, TenRuleConfig());

        int code = router.Route(new Uri("https://example.com"), "lynx", null).ExitCode;

        Assert.Equal(3, code);
        Assert.Empty(launcher.Launches);
        Assert.Contains(logger.Errors, e => e.Contains("lynx"));
    }

    [Fact]
    public void WorkDomain_RoutesToFirefoxWork()
    {
        FakeInventory inv = StandardInventory();
        FakeLauncher launcher = new();
        FakeProfileDetector profiles = new()
        {
            Profiles =
            {
                ["firefox-work"] = [new BrowserProfile("Work", "work", IsDefault: true)],
            },
        };
        FakeLogger logger = new();
        RuleRouter router = BuildRouter(inv, launcher, profiles, logger, TenRuleConfig());

        int code = router.Route(new Uri("https://company.atlassian.net/browse/X-1"), null, null).ExitCode;

        Assert.Equal(0, code);
        Assert.Equal("firefox-work", launcher.Launches[0].Browser.Id);
        Assert.Equal("Work", launcher.Launches[0].Browser.Profile!.Name);
    }

    [Fact]
    public void GitHubPr_RoutesToChromeWork()
    {
        FakeInventory inv = StandardInventory();
        FakeLauncher launcher = new();
        FakeProfileDetector profiles = new();
        FakeLogger logger = new();
        RuleRouter router = BuildRouter(inv, launcher, profiles, logger, TenRuleConfig());

        int code = router.Route(new Uri("https://github.com/foo/repo/pull/42"), null, null).ExitCode;

        Assert.Equal(0, code);
        Assert.Equal("chrome-work", launcher.Launches[0].Browser.Id);
    }

    [Fact]
    public void YouTube_AlwaysChromePersonal()
    {
        FakeInventory inv = StandardInventory();
        FakeLauncher launcher = new();
        FakeProfileDetector profiles = new();
        FakeLogger logger = new();
        RuleRouter router = BuildRouter(inv, launcher, profiles, logger, TenRuleConfig());

        int code = router.Route(new Uri("https://youtube.com/watch?v=abc"), null, null).ExitCode;

        Assert.Equal(0, code);
        Assert.Equal("chrome-personal", launcher.Launches[0].Browser.Id);
    }

    [Fact]
    public void HttpsFallback_RoutesToChromeWork()
    {
        FakeInventory inv = StandardInventory();
        FakeLauncher launcher = new();
        FakeProfileDetector profiles = new();
        FakeLogger logger = new();
        RuleRouter router = BuildRouter(inv, launcher, profiles, logger, TenRuleConfig());

        int code = router.Route(new Uri("https://random-site.example/x"), null, null).ExitCode;

        Assert.Equal(0, code);
        Assert.Equal("chrome-work", launcher.Launches[0].Browser.Id);
    }

    [Fact]
    public void AmazonProduct_RoutesToChromePersonal()
    {
        FakeInventory inv = StandardInventory();
        FakeLauncher launcher = new();
        FakeProfileDetector profiles = new();
        FakeLogger logger = new();
        RuleRouter router = BuildRouter(inv, launcher, profiles, logger, TenRuleConfig());

        int code = router.Route(new Uri("https://www.amazon.com/dp/123"), null, null).ExitCode;

        Assert.Equal(0, code);
        Assert.Equal("chrome-personal", launcher.Launches[0].Browser.Id);
    }

    [Fact]
    public void NoRuleMatchAndNoDefault_ReturnsError()
    {
        AppConfig config = new()
        {
            Settings = new Settings { StripTracking = true },
            Rules = [],
        };
        FakeInventory inv = StandardInventory();
        FakeLauncher launcher = new();
        FakeProfileDetector profiles = new();
        FakeLogger logger = new();
        RuleRouter router = BuildRouter(inv, launcher, profiles, logger, config);

        int code = router.Route(new Uri("https://example.com"), null, null).ExitCode;

        Assert.Equal(5, code);
        Assert.Contains(logger.Errors, e => e.Contains("No rule matched"));
    }

    [Fact]
    public void NoBrowsers_ReturnsError2()
    {
        FakeInventory inv = new();
        FakeLauncher launcher = new();
        FakeProfileDetector profiles = new();
        FakeLogger logger = new();
        RuleRouter router = BuildRouter(inv, launcher, profiles, logger, TenRuleConfig());

        int code = router.Route(new Uri("https://example.com"), null, null).ExitCode;

        Assert.Equal(2, code);
        Assert.Empty(launcher.Launches);
    }

    [Fact]
    public void TrackingStripped_WhenDecisionSaysSo()
    {
        FakeInventory inv = StandardInventory();
        FakeLauncher launcher = new();
        FakeProfileDetector profiles = new();
        FakeLogger logger = new();
        RuleRouter router = BuildRouter(inv, launcher, profiles, logger, TenRuleConfig());

        int code = router.Route(new Uri("https://company.atlassian.net/x?utm_source=foo"), null, null).ExitCode;

        Assert.Equal(0, code);
        Assert.DoesNotContain("utm_source", launcher.Launches[0].Url.ToString());
    }

    [Fact]
    public void TrackingStripped_OnExplicitRoute()
    {
        FakeInventory inv = StandardInventory();
        FakeLauncher launcher = new();
        FakeProfileDetector profiles = new();
        FakeLogger logger = new();
        RuleRouter router = BuildRouter(inv, launcher, profiles, logger, TenRuleConfig());

        int code = router.Route(new Uri("https://example.com/?utm_source=foo&q=keep"), "chrome-personal", null).ExitCode;

        Assert.Equal(0, code);
        Assert.DoesNotContain("utm_source", launcher.Launches[0].Url.ToString());
        Assert.Contains("q=keep", launcher.Launches[0].Url.ToString());
    }

    [Fact]
    public void RuleWithStripTrackingFalse_PreservesTracking()
    {
        AppConfig config = new()
        {
            Settings = new Settings { StripTracking = true },
            Rules = new Rule[]
            {
                new()
                {
                    Priority = 100,
                    When = new() { UrlMatchesAny = ["example.com"] },
                    Then = new() { Browser = "chrome-personal", StripTracking = false },
                },
            },
        };
        FakeInventory inv = StandardInventory();
        FakeLauncher launcher = new();
        FakeProfileDetector profiles = new();
        FakeLogger logger = new();
        RuleRouter router = BuildRouter(inv, launcher, profiles, logger, config);

        int code = router.Route(new Uri("https://example.com/?utm_source=foo"), null, null).ExitCode;

        Assert.Equal(0, code);
        Assert.Contains("utm_source", launcher.Launches[0].Url.ToString());
    }

    [Fact]
    public void RuleBrowserNotInInventory_ReturnsError4()
    {
        AppConfig config = new()
        {
            Settings = new Settings { },
            Rules = new Rule[]
            {
                new() { Priority = 100, When = new(), Then = new() { Browser = "does-not-exist" } },
            },
        };
        FakeInventory inv = StandardInventory();
        FakeLauncher launcher = new();
        FakeProfileDetector profiles = new();
        FakeLogger logger = new();
        RuleRouter router = BuildRouter(inv, launcher, profiles, logger, config);

        int code = router.Route(new Uri("https://example.com"), null, null).ExitCode;

        Assert.Equal(4, code);
        Assert.Contains(logger.Errors, e => e.Contains("does-not-exist"));
    }
}
