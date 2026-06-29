using AskMeFirst.Core;
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
        FakeSourceAppDetector sourceApp,
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
            sourceApp,
            pickerLauncher,
            usePickerAsCatchAll: false,
            appConfig.Profiles,
            profiles,
            launcher,
            logger,
            new FixedTimeProvider(now ?? Monday10amUtc));
    }

    private static AppConfig TenRuleConfig()
    {
        return new AppConfig
        {
            Settings = new Settings
            {
                DefaultBrowserId = "system",
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
                    Name = "GitHub PRs from work apps",
                    Priority = 250,
                    When = new()
                    {
                        ProcessIn = ["slack", "outlook"],
                        UrlRegex = "^https://github\\.com/[^/]+/[^/]+/pull/\\d+",
                    },
                    Then = new() { Browser = "chrome-work", StripTracking = true },
                },
                new()
                {
                    Name = "Work apps to Firefox Work profile",
                    Priority = 200,
                    When = new()
                    {
                        ProcessIn = ["slack", "outlook", "teams"],
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
                    Name = "Weekend: always personal",
                    Priority = 100,
                    When = new() { WeekdayIn = ["Sat", "Sun"] },
                    Then = new() { Browser = "chrome-personal" },
                },
                new()
                {
                    Name = "Work hours weekday fallback",
                    Priority = 50,
                    When = new()
                    {
                        TimeBetween = "09:00-18:00",
                        WeekdayIn = ["Mon", "Tue", "Wed", "Thu", "Fri"],
                    },
                    Then = new() { Browser = "chrome-work" },
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
                    Name = "All matches: amazon must include /dp/",
                    Priority = 30,
                    When = new()
                    {
                        UrlMatchesAll = ["*.amazon.com", "*.amazon.com/dp/*"],
                    },
                    Then = new() { Browser = "chrome-personal" },
                },
                new()
                {
                    Name = "Workday evening: prefer personal",
                    Priority = 20,
                    When = new()
                    {
                        TimeBetween = "18:00-23:59",
                        WeekdayIn = ["Mon", "Tue", "Wed", "Thu", "Fri"],
                    },
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
        FakeSourceAppDetector sourceApp = new() { Value = new SourceApp("slack", null, "") };
        FakeLogger logger = new();
        RuleRouter router = BuildRouter(inv, launcher, profiles, sourceApp, logger, TenRuleConfig());

        int code = router.Route(new Uri("https://company.atlassian.net/x"), "chrome-personal", null);

        Assert.Equal(0, code);
        Assert.Equal("chrome-personal", launcher.Launches[0].Browser.Id);
    }

    [Fact]
    public void ExplicitUnknownBrowser_ReturnsError()
    {
        FakeInventory inv = StandardInventory();
        FakeLauncher launcher = new();
        FakeProfileDetector profiles = new();
        FakeSourceAppDetector sourceApp = new();
        FakeLogger logger = new();
        RuleRouter router = BuildRouter(inv, launcher, profiles, sourceApp, logger, TenRuleConfig());

        int code = router.Route(new Uri("https://example.com"), "lynx", null);

        Assert.Equal(3, code);
        Assert.Empty(launcher.Launches);
        Assert.Contains(logger.Errors, e => e.Contains("lynx"));
    }

    [Fact]
    public void HighPriorityProcessPlusUrl_RoutesToFirefoxWork()
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
        FakeSourceAppDetector sourceApp = new() { Value = new SourceApp("slack", null, "") };
        FakeLogger logger = new();
        RuleRouter router = BuildRouter(inv, launcher, profiles, sourceApp, logger, TenRuleConfig());

        int code = router.Route(new Uri("https://company.atlassian.net/browse/X-1"), null, null);

        Assert.Equal(0, code);
        Assert.Equal("firefox-work", launcher.Launches[0].Browser.Id);
        Assert.Equal("Work", launcher.Launches[0].Browser.Profile!.Name);
    }

    [Fact]
    public void GitHubPrFromOutlook_RoutesToChromeWork()
    {
        FakeInventory inv = StandardInventory();
        FakeLauncher launcher = new();
        FakeProfileDetector profiles = new();
        FakeSourceAppDetector sourceApp = new() { Value = new SourceApp("outlook", null, "") };
        FakeLogger logger = new();
        RuleRouter router = BuildRouter(inv, launcher, profiles, sourceApp, logger, TenRuleConfig());

        int code = router.Route(new Uri("https://github.com/foo/repo/pull/42"), null, null);

        Assert.Equal(0, code);
        Assert.Equal("chrome-work", launcher.Launches[0].Browser.Id);
    }

    [Fact]
    public void YouTube_AlwaysChromePersonal()
    {
        FakeInventory inv = StandardInventory();
        FakeLauncher launcher = new();
        FakeProfileDetector profiles = new();
        FakeSourceAppDetector sourceApp = new() { Value = new SourceApp("code", null, "") };
        FakeLogger logger = new();
        RuleRouter router = BuildRouter(inv, launcher, profiles, sourceApp, logger, TenRuleConfig());

        int code = router.Route(new Uri("https://youtube.com/watch?v=abc"), null, null);

        Assert.Equal(0, code);
        Assert.Equal("chrome-personal", launcher.Launches[0].Browser.Id);
    }

    [Fact]
    public void WorkHoursFallback_RoutesToChromeWork()
    {
        FakeInventory inv = StandardInventory();
        FakeLauncher launcher = new();
        FakeProfileDetector profiles = new();
        FakeSourceAppDetector sourceApp = new() { Value = new SourceApp("code", null, "") };
        FakeLogger logger = new();
        RuleRouter router = BuildRouter(inv, launcher, profiles, sourceApp, logger, TenRuleConfig());

        int code = router.Route(new Uri("https://random-site.example/x"), null, null);

        Assert.Equal(0, code);
        Assert.Equal("chrome-work", launcher.Launches[0].Browser.Id);
    }

    [Fact]
    public void Weekend_RoutesToChromePersonal()
    {
        FakeInventory inv = StandardInventory();
        FakeLauncher launcher = new();
        FakeProfileDetector profiles = new();
        FakeSourceAppDetector sourceApp = new() { Value = new SourceApp("code", null, "") };
        FakeLogger logger = new();
        DateTimeOffset saturday = new(2026, 6, 6, 10, 0, 0, TimeSpan.Zero);
        RuleRouter router = BuildRouter(inv, launcher, profiles, sourceApp, new FakeLogger(), TenRuleConfig(), now: saturday);

        int code = router.Route(new Uri("https://random-site.example/x"), null, null);

        Assert.Equal(0, code);
        Assert.Equal("chrome-personal", launcher.Launches[0].Browser.Id);
    }

    [Fact]
    public void NoRuleMatchAndNoDefault_ReturnsError()
    {
        AppConfig config = new()
        {
            Settings = new Settings { DefaultBrowserId = null, StripTracking = true },
            Rules = [],
        };
        FakeInventory inv = StandardInventory();
        FakeLauncher launcher = new();
        FakeProfileDetector profiles = new();
        FakeSourceAppDetector sourceApp = new();
        FakeLogger logger = new();
        RuleRouter router = BuildRouter(inv, launcher, profiles, sourceApp, logger, config);

        int code = router.Route(new Uri("https://example.com"), null, null);

        Assert.Equal(5, code);
        Assert.Contains(logger.Errors, e => e.Contains("No rule matched"));
    }

    [Fact]
    public void NoBrowsers_ReturnsError2()
    {
        FakeInventory inv = new();
        FakeLauncher launcher = new();
        FakeProfileDetector profiles = new();
        FakeSourceAppDetector sourceApp = new();
        FakeLogger logger = new();
        RuleRouter router = BuildRouter(inv, launcher, profiles, sourceApp, logger, TenRuleConfig());

        int code = router.Route(new Uri("https://example.com"), null, null);

        Assert.Equal(2, code);
        Assert.Empty(launcher.Launches);
    }

    [Fact]
    public void TrackingStripped_WhenDecisionSaysSo()
    {
        FakeInventory inv = StandardInventory();
        FakeLauncher launcher = new();
        FakeProfileDetector profiles = new();
        FakeSourceAppDetector sourceApp = new() { Value = new SourceApp("slack", null, "") };
        FakeLogger logger = new();
        RuleRouter router = BuildRouter(inv, launcher, profiles, sourceApp, logger, TenRuleConfig());

        int code = router.Route(new Uri("https://company.atlassian.net/x?utm_source=foo"), null, null);

        Assert.Equal(0, code);
        Assert.DoesNotContain("utm_source", launcher.Launches[0].Url.ToString());
    }

    [Fact]
    public void TrackingStripped_OnExplicitRoute()
    {
        FakeInventory inv = StandardInventory();
        FakeLauncher launcher = new();
        FakeProfileDetector profiles = new();
        FakeSourceAppDetector sourceApp = new();
        FakeLogger logger = new();
        RuleRouter router = BuildRouter(inv, launcher, profiles, sourceApp, logger, TenRuleConfig());

        int code = router.Route(new Uri("https://example.com/?utm_source=foo&q=keep"), "chrome-personal", null);

        Assert.Equal(0, code);
        Assert.DoesNotContain("utm_source", launcher.Launches[0].Url.ToString());
        Assert.Contains("q=keep", launcher.Launches[0].Url.ToString());
    }

    [Fact]
    public void RuleWithStripTrackingFalse_PreservesTracking()
    {
        AppConfig config = new()
        {
            Settings = new Settings { DefaultBrowserId = "system", StripTracking = true },
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
        FakeSourceAppDetector sourceApp = new();
        FakeLogger logger = new();
        RuleRouter router = BuildRouter(inv, launcher, profiles, sourceApp, logger, config);

        int code = router.Route(new Uri("https://example.com/?utm_source=foo"), null, null);

        Assert.Equal(0, code);
        Assert.Contains("utm_source", launcher.Launches[0].Url.ToString());
    }

    [Fact]
    public void RuleBrowserNotInInventory_ReturnsError4()
    {
        AppConfig config = new()
        {
            Settings = new Settings { DefaultBrowserId = "system" },
            Rules = new Rule[]
            {
                new() { Priority = 100, When = new(), Then = new() { Browser = "does-not-exist" } },
            },
        };
        FakeInventory inv = StandardInventory();
        FakeLauncher launcher = new();
        FakeProfileDetector profiles = new();
        FakeSourceAppDetector sourceApp = new();
        FakeLogger logger = new();
        RuleRouter router = BuildRouter(inv, launcher, profiles, sourceApp, logger, config);

        int code = router.Route(new Uri("https://example.com"), null, null);

        Assert.Equal(4, code);
        Assert.Contains(logger.Errors, e => e.Contains("does-not-exist"));
    }

    [Fact]
    public void DefaultFallback_UsesConfiguredBrowser()
    {
        AppConfig config = new()
        {
            Settings = new Settings { DefaultBrowserId = "firefox-work", StripTracking = true },
            Rules = [],
        };
        FakeInventory inv = StandardInventory();
        FakeLauncher launcher = new();
        FakeProfileDetector profiles = new();
        FakeSourceAppDetector sourceApp = new();
        FakeLogger logger = new();
        RuleRouter router = BuildRouter(inv, launcher, profiles, sourceApp, logger, config);

        int code = router.Route(new Uri("https://example.com"), null, null);

        Assert.Equal(0, code);
        Assert.Equal("firefox-work", launcher.Launches[0].Browser.Id);
    }
}