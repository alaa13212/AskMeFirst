using AskMeFirst.Core.Config;
using AskMeFirst.Core.Routing;
using Xunit;

namespace AskMeFirst.Core.Tests;

public class ResolverTests
{
    private static readonly DateTimeOffset Monday10am = new(2026, 6, 1, 10, 0, 0, TimeSpan.Zero);
    private static readonly PredicateEvaluator Evaluator = TestEvaluator.Default();

    [Fact]
    public void ExplicitOverrideResolver_NoExplicitArgs_ReturnsNull()
    {
        RoutingContext ctx = RoutingContext.Create(new Uri("https://example.com"), Monday10am);
        Assert.Null(new ExplicitOverrideResolver().Resolve(ctx));
    }

    [Fact]
    public void ExplicitOverrideResolver_WithExplicitArgs_ReturnsIntent()
    {
        RoutingContext ctx = RoutingContext.Create(
            new Uri("https://example.com"), Monday10am,
            explicitBrowserId: "chrome-personal", explicitProfileId: "chrome-personal-profile");
        RoutingIntent? intent = new ExplicitOverrideResolver().Resolve(ctx);
        Assert.NotNull(intent);
        Assert.Equal("chrome-personal", intent!.BrowserId);
        Assert.Equal("chrome-personal-profile", intent.ProfileId);
        Assert.Equal(RoutingExitCode.BrowserNotFound, intent.NotFoundExitCode);
        Assert.Equal("Browser", intent.NotFoundMessagePrefix);
    }

    [Fact]
    public void RuleMatchingResolver_NoMatchingRules_ReturnsNull()
    {
        AppConfig config = new() { Rules = [] };
        RuleMatchingResolver resolver = new(config.Rules, Evaluator);
        RoutingContext ctx = RoutingContext.Create(new Uri("https://example.com"), Monday10am);
        Assert.Null(resolver.Resolve(ctx));
    }

    [Fact]
    public void RuleMatchingResolver_MatchingRule_ReturnsIntentWithRuleFailureSemantics()
    {
        AppConfig config = new()
        {
            Rules = new Rule[]
            {
                new() { Priority = 100, When = new() { UrlMatchesAny = ["github.com"] }, Then = new() { Browser = "chrome-work" } },
            },
        };
        RuleMatchingResolver resolver = new(config.Rules, Evaluator);
        RoutingContext ctx = RoutingContext.Create(new Uri("https://github.com/foo"), Monday10am);
        RoutingIntent? intent = resolver.Resolve(ctx);
        Assert.NotNull(intent);
        Assert.Equal("chrome-work", intent!.BrowserId);
        Assert.Equal(RoutingExitCode.RuleBrowserNotFound, intent.NotFoundExitCode);
        Assert.Equal("Rule matched browser", intent.NotFoundMessagePrefix);
    }

    [Fact]
    public void ResolverChain_ExplicitBeatsRule()
    {
        AppConfig config = new()
        {
            Rules = new Rule[]
            {
                new() { Priority = 100, When = new(), Then = new() { Browser = "rule-browser" } },
            },
        };
        IReadOnlyList<ITargetResolver> resolvers = TestResolvers.For(config, Evaluator);

        RoutingContext noArgs = RoutingContext.Create(new Uri("https://example.com"), Monday10am);
        RoutingIntent? first = null;
        foreach (ITargetResolver r in resolvers)
        {
            first = r.Resolve(noArgs);
            if (first is not null)
            {
                break;
            }
        }
        Assert.NotNull(first);
        Assert.Equal("rule-browser", first!.BrowserId);
        Assert.Equal(RoutingExitCode.RuleBrowserNotFound, first.NotFoundExitCode);

        RoutingContext withExplicit = RoutingContext.Create(
            new Uri("https://example.com"), Monday10am,
            explicitBrowserId: "explicit-browser");
        RoutingIntent? firstExplicit = null;
        foreach (ITargetResolver r in resolvers)
        {
            firstExplicit = r.Resolve(withExplicit);
            if (firstExplicit is not null)
            {
                break;
            }
        }
        Assert.NotNull(firstExplicit);
        Assert.Equal("explicit-browser", firstExplicit!.BrowserId);
        Assert.Equal(RoutingExitCode.BrowserNotFound, firstExplicit.NotFoundExitCode);
    }

    [Fact]
    public void ResolverChain_NoResolverMatches_ReturnsNull()
    {
        AppConfig config = new()
        {
            Rules = [],
        };
        IReadOnlyList<ITargetResolver> resolvers = TestResolvers.For(config, Evaluator);
        RoutingContext ctx = RoutingContext.Create(new Uri("https://example.com"), Monday10am);

        RoutingIntent? intent = null;
        foreach (ITargetResolver r in resolvers)
        {
            intent = r.Resolve(ctx);
            if (intent is not null)
            {
                break;
            }
        }
        Assert.Null(intent);
    }
}