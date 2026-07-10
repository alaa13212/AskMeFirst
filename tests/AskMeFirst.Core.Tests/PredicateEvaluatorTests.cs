using AskMeFirst.Core.Config;
using AskMeFirst.Core.Routing;
using Xunit;

namespace AskMeFirst.Core.Tests;

public class PredicateEvaluatorTests
{
    private static readonly DateTimeOffset MondayNoon = new(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly PredicateEvaluator Evaluator = TestEvaluator.Default();

    private static RoutingContext Ctx(Uri url)
    {
        return RoutingContext.Create(url, MondayNoon);
    }

    [Fact]
    public void EmptyWhen_MatchesAnything()
    {
        RuleWhen ruleWhen = new();
        Assert.True(Evaluator.Matches(ruleWhen, Ctx(new Uri("https://example.com"))));
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
    public void CombinedPredicates_AndSemantics()
    {
        RuleWhen ruleWhen = new()
        {
            UrlMatchesAny = ["*.atlassian.net"],
            SchemeIn = ["https"],
        };

        Assert.True(Evaluator.Matches(
            ruleWhen,
            Ctx(new Uri("https://company.atlassian.net/browse/X"))));

        Assert.False(Evaluator.Matches(
            ruleWhen,
            Ctx(new Uri("http://company.atlassian.net/browse/X"))));

        Assert.False(Evaluator.Matches(
            ruleWhen,
            Ctx(new Uri("https://github.com/foo"))));
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
