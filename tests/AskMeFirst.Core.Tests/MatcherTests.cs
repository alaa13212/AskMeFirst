using AskMeFirst.Core.Config;
using AskMeFirst.Core.Routing;
using Xunit;

namespace AskMeFirst.Core.Tests;

public class MatcherTests
{
    private static readonly DateTimeOffset Monday10am = new(2026, 6, 1, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Saturday10am = new(2026, 6, 6, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ProcessInMatcher_EmptyField_MatchesAnything()
    {
        RuleWhen ruleWhen = new();
        RoutingContext ctx = RoutingContext.Create(new Uri("https://example.com"), null, Monday10am);
        Assert.True(new ProcessInMatcher().Matches(ruleWhen, ctx));
    }

    [Fact]
    public void ProcessInMatcher_NullSource_DoesNotMatch()
    {
        RuleWhen ruleWhen = new() { ProcessIn = ["slack"] };
        RoutingContext ctx = RoutingContext.Create(new Uri("https://example.com"), null, Monday10am);
        Assert.False(new ProcessInMatcher().Matches(ruleWhen, ctx));
    }

    [Fact]
    public void UrlMatchesAnyMatcher_GlobMatch()
    {
        RuleWhen ruleWhen = new() { UrlMatchesAny = ["*.github.com"] };
        RoutingContext ctx = RoutingContext.Create(new Uri("https://api.github.com/foo"), null, Monday10am);
        Assert.True(new UrlMatchesAnyMatcher().Matches(ruleWhen, ctx));
    }

    [Fact]
    public void UrlMatchesAllMatcher_AllRequired()
    {
        RuleWhen ruleWhen = new() { UrlMatchesAll = ["*.amazon.com", "*.amazon.com/dp/*"] };
        RoutingContext ctx1 = RoutingContext.Create(new Uri("https://www.amazon.com/dp/1"), null, Monday10am);
        RoutingContext ctx2 = RoutingContext.Create(new Uri("https://www.example.com/dp/1"), null, Monday10am);
        Assert.True(new UrlMatchesAllMatcher().Matches(ruleWhen, ctx1));
        Assert.False(new UrlMatchesAllMatcher().Matches(ruleWhen, ctx2));
    }

    [Fact]
    public void UrlRegexMatcher_MatchesAgainstAbsoluteUri()
    {
        RuleWhen ruleWhen = new() { UrlRegex = "^https://github\\.com/.+/pull/\\d+$" };
        RoutingContext ctx = RoutingContext.Create(new Uri("https://github.com/foo/repo/pull/123"), null, Monday10am);
        Assert.True(new UrlRegexMatcher().Matches(ruleWhen, ctx));
    }

    [Fact]
    public void SchemeInMatcher_CaseInsensitive()
    {
        RuleWhen ruleWhen = new() { SchemeIn = ["HTTPS"] };
        RoutingContext ctx = RoutingContext.Create(new Uri("https://example.com"), null, Monday10am);
        Assert.True(new SchemeInMatcher().Matches(ruleWhen, ctx));
    }

    [Fact]
    public void TimeBetweenMatcher_WithinRange()
    {
        RuleWhen ruleWhen = new() { TimeBetween = "09:00-18:00" };
        RoutingContext inRange = RoutingContext.Create(new Uri("https://example.com"), null, Monday10am);
        RoutingContext outOfRange = RoutingContext.Create(new Uri("https://example.com"), null,
            new DateTimeOffset(2026, 6, 1, 20, 0, 0, TimeSpan.Zero));
        Assert.True(new TimeBetweenMatcher().Matches(ruleWhen, inRange));
        Assert.False(new TimeBetweenMatcher().Matches(ruleWhen, outOfRange));
    }

    [Fact]
    public void WeekdayInMatcher_RequiresExactDay()
    {
        RuleWhen ruleWhen = new() { WeekdayIn = ["Sat", "Sun"] };
        RoutingContext monday = RoutingContext.Create(new Uri("https://example.com"), null, Monday10am);
        RoutingContext saturday = RoutingContext.Create(new Uri("https://example.com"), null, Saturday10am);
        Assert.False(new WeekdayInMatcher().Matches(ruleWhen, monday));
        Assert.True(new WeekdayInMatcher().Matches(ruleWhen, saturday));
    }

    [Fact]
    public void BrowserRunningMatcher_RespectsIsRunningFlag()
    {
        RuleWhen ruleWhen = new() { BrowserRunning = true };
        RoutingContext running = RoutingContext.Create(new Uri("https://example.com"), null, Monday10am, isRunning: true);
        RoutingContext stopped = RoutingContext.Create(new Uri("https://example.com"), null, Monday10am, isRunning: false);
        Assert.True(new BrowserRunningMatcher().Matches(ruleWhen, running));
        Assert.False(new BrowserRunningMatcher().Matches(ruleWhen, stopped));
    }

    [Fact]
    public void PredicateEvaluator_IteratesMatchers_FailFast()
    {
        PredicateEvaluator evaluator = new(new IPredicateMatcher[] { new ProcessInMatcher(), new SchemeInMatcher() });
        RuleWhen ruleWhen = new() { ProcessIn = ["slack"], SchemeIn = ["ftp"] };
        RoutingContext ctx = RoutingContext.Create(new Uri("https://example.com"), "slack", Monday10am);
        Assert.False(evaluator.Matches(ruleWhen, ctx));
    }

    [Fact]
    public void PredicateEvaluator_NoMatchers_AcceptsEverything()
    {
        PredicateEvaluator evaluator = new(Array.Empty<IPredicateMatcher>());
        RuleWhen ruleWhen = new() { ProcessIn = ["slack"] };
        RoutingContext ctx = RoutingContext.Create(new Uri("https://example.com"), null, Monday10am);
        Assert.True(evaluator.Matches(ruleWhen, ctx));
    }
}