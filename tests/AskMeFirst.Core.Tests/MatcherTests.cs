using AskMeFirst.Core.Config;
using AskMeFirst.Core.Routing;
using Xunit;

namespace AskMeFirst.Core.Tests;

public class MatcherTests
{
    private static readonly DateTimeOffset Monday10am = new(2026, 6, 1, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void UrlMatchesAnyMatcher_GlobMatch()
    {
        RuleWhen ruleWhen = new() { UrlMatchesAny = ["*.github.com"] };
        RoutingContext ctx = RoutingContext.Create(new Uri("https://api.github.com/foo"), Monday10am);
        Assert.True(new UrlMatchesAnyMatcher().Matches(ruleWhen, ctx));
    }

    [Fact]
    public void UrlMatchesAllMatcher_AllRequired()
    {
        RuleWhen ruleWhen = new() { UrlMatchesAll = ["*.amazon.com", "*.amazon.com/dp/*"] };
        RoutingContext ctx1 = RoutingContext.Create(new Uri("https://www.amazon.com/dp/1"), Monday10am);
        RoutingContext ctx2 = RoutingContext.Create(new Uri("https://www.example.com/dp/1"), Monday10am);
        Assert.True(new UrlMatchesAllMatcher().Matches(ruleWhen, ctx1));
        Assert.False(new UrlMatchesAllMatcher().Matches(ruleWhen, ctx2));
    }

    [Fact]
    public void UrlRegexMatcher_MatchesAgainstAbsoluteUri()
    {
        RuleWhen ruleWhen = new() { UrlRegex = "^https://github\\.com/.+/pull/\\d+$" };
        RoutingContext ctx = RoutingContext.Create(new Uri("https://github.com/foo/repo/pull/123"), Monday10am);
        Assert.True(new UrlRegexMatcher().Matches(ruleWhen, ctx));
    }

    [Fact]
    public void SchemeInMatcher_CaseInsensitive()
    {
        RuleWhen ruleWhen = new() { SchemeIn = ["HTTPS"] };
        RoutingContext ctx = RoutingContext.Create(new Uri("https://example.com"), Monday10am);
        Assert.True(new SchemeInMatcher().Matches(ruleWhen, ctx));
    }

    [Fact]
    public void PredicateEvaluator_IteratesMatchers_FailFast()
    {
        PredicateEvaluator evaluator = new(new IPredicateMatcher[] { new UrlMatchesAnyMatcher(), new SchemeInMatcher() });
        RuleWhen ruleWhen = new() { UrlMatchesAny = ["example.com"], SchemeIn = ["ftp"] };
        RoutingContext ctx = RoutingContext.Create(new Uri("https://example.com"), Monday10am);
        Assert.False(evaluator.Matches(ruleWhen, ctx));
    }

    [Fact]
    public void PredicateEvaluator_NoMatchers_AcceptsEverything()
    {
        PredicateEvaluator evaluator = new(Array.Empty<IPredicateMatcher>());
        RuleWhen ruleWhen = new() { UrlMatchesAny = ["example.com"] };
        RoutingContext ctx = RoutingContext.Create(new Uri("https://github.com"), Monday10am);
        Assert.True(evaluator.Matches(ruleWhen, ctx));
    }
}
