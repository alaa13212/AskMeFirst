using AskMeFirst.Core.Config;
using AskMeFirst.Core.Routing;
using Xunit;

namespace AskMeFirst.Core.Tests;

public class PredicateEvaluatorTests
{
    private static readonly PredicateEvaluator Evaluator = TestEvaluator.Default();

    private static RoutingContext Ctx(Uri url, string? sourceProcess = null, DateTimeOffset? now = null)
    {
        return RoutingContext.Create(url, sourceProcess, now ?? new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void EmptyWhen_MatchesAnything()
    {
        RuleWhen ruleWhen = new();
        Assert.True(Evaluator.Matches(ruleWhen, Ctx(new Uri("https://example.com"))));
    }

    [Fact]
    public void ProcessIn_Matches()
    {
        RuleWhen ruleWhen = new() { ProcessIn = ["slack", "outlook"] };
        Assert.True(Evaluator.Matches(ruleWhen, Ctx(new Uri("https://example.com"), "slack")));
        Assert.True(Evaluator.Matches(ruleWhen, Ctx(new Uri("https://example.com"), "OUTLOOK")));
        Assert.False(Evaluator.Matches(ruleWhen, Ctx(new Uri("https://example.com"), "code")));
    }

    [Fact]
    public void ProcessIn_NoSource_DoesNotMatch()
    {
        RuleWhen ruleWhen = new() { ProcessIn = ["slack"] };
        Assert.False(Evaluator.Matches(ruleWhen, Ctx(new Uri("https://example.com"))));
    }

    [Fact]
    public void UrlMatchesAny_GlobSemantics()
    {
        RuleWhen ruleWhen = new() { UrlMatchesAny = ["*.github.com", "github.com"] };
        Assert.True(Evaluator.Matches(ruleWhen, Ctx(new Uri("https://github.com/foo"))));
        Assert.True(Evaluator.Matches(ruleWhen, Ctx(new Uri("https://api.github.com/foo"))));
        Assert.True(Evaluator.Matches(ruleWhen, Ctx(new Uri("https://www.github.com/foo"))));
        Assert.False(Evaluator.Matches(ruleWhen, Ctx(new Uri("https://example.com"))));
    }

    [Fact]
    public void UrlMatchesAny_PathMatching()
    {
        RuleWhen ruleWhen = new() { UrlMatchesAny = ["github.com/*/issues/*"] };
        Assert.True(Evaluator.Matches(ruleWhen, Ctx(new Uri("https://github.com/foo/issues/123"))));
        Assert.False(Evaluator.Matches(ruleWhen, Ctx(new Uri("https://github.com/foo/repo/issues/123"))));
        Assert.False(Evaluator.Matches(ruleWhen, Ctx(new Uri("https://github.com/foo/repo/pull/1"))));
    }

    [Fact]
    public void UrlMatchesAny_PathMatching_DoubleStar_CrossesSegments()
    {
        RuleWhen ruleWhen = new() { UrlMatchesAny = ["github.com/**/issues/**"] };
        Assert.True(Evaluator.Matches(ruleWhen, Ctx(new Uri("https://github.com/foo/repo/issues/123"))));
        Assert.True(Evaluator.Matches(ruleWhen, Ctx(new Uri("https://github.com/foo/issues/123"))));
    }

    [Fact]
    public void UrlMatchesAll_RequiresEveryPattern()
    {
        RuleWhen ruleWhen = new() { UrlMatchesAll = ["*.amazon.com", "*.amazon.com/dp/*"] };
        Assert.True(Evaluator.Matches(ruleWhen, Ctx(new Uri("https://www.amazon.com/dp/123"))));
        Assert.False(Evaluator.Matches(ruleWhen, Ctx(new Uri("https://www.example.com/dp/123"))));
    }

    [Fact]
    public void UrlRegex_MatchesAgainstAbsoluteUri()
    {
        RuleWhen ruleWhen = new() { UrlRegex = "^https://github\\.com/[^/]+/[^/]+/pull/\\d+" };
        Assert.True(Evaluator.Matches(ruleWhen, Ctx(new Uri("https://github.com/foo/repo/pull/123"))));
        Assert.False(Evaluator.Matches(ruleWhen, Ctx(new Uri("https://github.com/foo/repo"))));
        Assert.False(Evaluator.Matches(ruleWhen, Ctx(new Uri("https://example.com/pull/123"))));
    }

    [Fact]
    public void SchemeIn_Matches()
    {
        RuleWhen ruleWhen = new() { SchemeIn = ["https"] };
        Assert.True(Evaluator.Matches(ruleWhen, Ctx(new Uri("https://example.com"))));
        Assert.False(Evaluator.Matches(ruleWhen, Ctx(new Uri("http://example.com"))));
    }

    [Fact]
    public void TimeBetween_WithinRange()
    {
        DateTimeOffset at10am = new(2026, 6, 1, 10, 0, 0, TimeSpan.Zero);
        DateTimeOffset at8pm = new(2026, 6, 1, 20, 0, 0, TimeSpan.Zero);
        RuleWhen ruleWhen = new() { TimeBetween = "09:00-18:00" };
        Assert.True(Evaluator.Matches(ruleWhen, Ctx(new Uri("https://example.com"), now: at10am)));
        Assert.False(Evaluator.Matches(ruleWhen, Ctx(new Uri("https://example.com"), now: at8pm)));
    }

    [Fact]
    public void TimeBetween_SpansMidnight()
    {
        DateTimeOffset at11pm = new(2026, 6, 1, 23, 0, 0, TimeSpan.Zero);
        DateTimeOffset at3am = new(2026, 6, 1, 3, 0, 0, TimeSpan.Zero);
        DateTimeOffset atNoon = new(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);
        RuleWhen ruleWhen = new() { TimeBetween = "22:00-06:00" };
        Assert.True(Evaluator.Matches(ruleWhen, Ctx(new Uri("https://example.com"), now: at11pm)));
        Assert.True(Evaluator.Matches(ruleWhen, Ctx(new Uri("https://example.com"), now: at3am)));
        Assert.False(Evaluator.Matches(ruleWhen, Ctx(new Uri("https://example.com"), now: atNoon)));
    }

    [Fact]
    public void WeekdayIn_Matches()
    {
        DateTimeOffset monday = new(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);
        DateTimeOffset saturday = new(2026, 6, 6, 12, 0, 0, TimeSpan.Zero);
        RuleWhen ruleWhen = new() { WeekdayIn = ["Mon", "Tue", "Wed", "Thu", "Fri"] };
        Assert.True(Evaluator.Matches(ruleWhen, Ctx(new Uri("https://example.com"), now: monday)));
        Assert.False(Evaluator.Matches(ruleWhen, Ctx(new Uri("https://example.com"), now: saturday)));
    }

    [Fact]
    public void BrowserRunning_RequiresMatchingState()
    {
        RuleWhen ruleWhen = new() { BrowserRunning = true };
        RoutingContext running = RoutingContext.Create(new Uri("https://example.com"), null, DateTimeOffset.UtcNow, isRunning: true);
        RoutingContext stopped = RoutingContext.Create(new Uri("https://example.com"), null, DateTimeOffset.UtcNow, isRunning: false);
        Assert.True(Evaluator.Matches(ruleWhen, running));
        Assert.False(Evaluator.Matches(ruleWhen, stopped));
    }

    [Fact]
    public void CombinedPredicates_AndSemantics()
    {
        DateTimeOffset monday10am = new(2026, 6, 1, 10, 0, 0, TimeSpan.Zero);
        RuleWhen ruleWhen = new()
        {
            ProcessIn = ["slack"],
            UrlMatchesAny = ["*.atlassian.net"],
            SchemeIn = ["https"],
            WeekdayIn = ["Mon"],
        };

        Assert.True(Evaluator.Matches(
            ruleWhen,
            RoutingContext.Create(new Uri("https://company.atlassian.net/browse/X"), "slack", monday10am)));

        Assert.False(Evaluator.Matches(
            ruleWhen,
            RoutingContext.Create(new Uri("https://company.atlassian.net/browse/X"), "code", monday10am)));

        Assert.False(Evaluator.Matches(
            ruleWhen,
            RoutingContext.Create(new Uri("https://github.com/foo"), "slack", monday10am)));
    }

    [Theory]
    [InlineData("*.example.com", "example.com", true)]
    [InlineData("*.example.com", "www.example.com", true)]
    [InlineData("*.example.com", "deep.sub.example.com", false)]
    [InlineData("**.example.com", "deep.sub.example.com", true)]
    [InlineData("example.com/**", "example.com/foo/bar", true)]
    [InlineData("example.com/*", "example.com/foo/bar", false)]
    [InlineData("example.com/*", "example.com/foo", true)]
    [InlineData("example.com", "example.com", true)]
    [InlineData("example.com", "www.example.com", false)]
    public void GlobToRegex_Cases(string pattern, string text, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.Matches(pattern, text, text));
    }
}