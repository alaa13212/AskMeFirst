using AskMeFirst.Core.Config;
using AskMeFirst.Core.Routing;
using Xunit;

namespace AskMeFirst.Core.Tests;

public class RuleEngineTests
{
    private static readonly DateTimeOffset Monday10am = new(2026, 6, 1, 10, 0, 0, TimeSpan.Zero);
    private static readonly PredicateEvaluator Evaluator = TestEvaluator.Default();

    private static RoutingContext Ctx(Uri url, string? sourceProcess = null)
    {
        return RoutingContext.Create(url, sourceProcess, Monday10am);
    }

    [Fact]
    public void EmptyRules_ReturnsNull()
    {
        RoutingDecision? decision = RuleEngine.Evaluate([], Ctx(new Uri("https://example.com")), Evaluator);
        Assert.Null(decision);
    }

    [Fact]
    public void NoRuleMatches_ReturnsNull()
    {
        IReadOnlyList<Rule> rules = new Rule[]
        {
            new() { Name = "Work only", Priority = 100, When = new() { ProcessIn = ["slack"] }, Then = new() { Browser = "firefox" } },
        };
        RoutingDecision? decision = RuleEngine.Evaluate(rules, Ctx(new Uri("https://example.com"), sourceProcess: "code"), Evaluator);
        Assert.Null(decision);
    }

    [Fact]
    public void SingleMatchingRule_ReturnsDecision()
    {
        IReadOnlyList<Rule> rules = new Rule[]
        {
            new() { Priority = 100, When = new() { UrlMatchesAny = ["github.com"] }, Then = new() { Browser = "chrome-work", ProfileId = "firefox-work" } },
        };
        RoutingDecision? decision = RuleEngine.Evaluate(rules, Ctx(new Uri("https://github.com/foo")), Evaluator);
        Assert.NotNull(decision);
        Assert.Equal("chrome-work", decision!.BrowserId);
        Assert.Equal("firefox-work", decision.ProfileId);
    }

    [Fact]
    public void HigherPriorityWins()
    {
        IReadOnlyList<Rule> rules = new Rule[]
        {
            new() { Priority = 10, When = new(), Then = new() { Browser = "low-priority" } },
            new() { Priority = 100, When = new(), Then = new() { Browser = "high-priority" } },
            new() { Priority = 50, When = new(), Then = new() { Browser = "mid-priority" } },
        };
        RoutingDecision? decision = RuleEngine.Evaluate(rules, Ctx(new Uri("https://example.com")), Evaluator);
        Assert.Equal("high-priority", decision!.BrowserId);
    }

    [Fact]
    public void SamePriority_TieGoesToEarlierArrayIndex()
    {
        IReadOnlyList<Rule> rules = new Rule[]
        {
            new() { Priority = 50, When = new(), Then = new() { Browser = "first" } },
            new() { Priority = 50, When = new(), Then = new() { Browser = "second" } },
        };
        RoutingDecision? decision = RuleEngine.Evaluate(rules, Ctx(new Uri("https://example.com")), Evaluator);
        Assert.Equal("first", decision!.BrowserId);
    }

    [Fact]
    public void LowerPriorityNeverBeatsHigher()
    {
        IReadOnlyList<Rule> rules = new Rule[]
        {
            new() { Priority = 100, When = new(), Then = new() { Browser = "high" } },
            new() { Priority = 1, When = new(), Then = new() { Browser = "low" } },
        };
        RoutingDecision? decision = RuleEngine.Evaluate(rules, Ctx(new Uri("https://example.com")), Evaluator);
        Assert.Equal("high", decision!.BrowserId);
    }

    [Fact]
    public void PredicateMustAllMatch()
    {
        IReadOnlyList<Rule> rules = new Rule[]
        {
            new()
            {
                Priority = 100,
                When = new() { ProcessIn = ["slack"], UrlMatchesAny = ["*.atlassian.net"] },
                Then = new() { Browser = "firefox-work" },
            },
        };

        RoutingDecision? match = RuleEngine.Evaluate(rules, Ctx(new Uri("https://company.atlassian.net/x"), sourceProcess: "slack"), Evaluator);
        Assert.NotNull(match);

        RoutingDecision? noSource = RuleEngine.Evaluate(rules, Ctx(new Uri("https://company.atlassian.net/x")), Evaluator);
        Assert.Null(noSource);

        RoutingDecision? wrongUrl = RuleEngine.Evaluate(rules, Ctx(new Uri("https://github.com"), sourceProcess: "slack"), Evaluator);
        Assert.Null(wrongUrl);
    }

    [Fact]
    public void AllActionFields_PropagateToDecision()
    {
        IReadOnlyList<Rule> rules = new Rule[]
        {
            new()
            {
                Priority = 100,
                When = new() { UrlMatchesAny = ["**"] },
                Then = new()
                {
                    Browser = "chrome-work",
                    ProfileId = "firefox-work",
                    FocusExisting = false,
                    NewWindow = true,
                    Private = true,
                    StripTracking = false,
                    Unshorten = true,
                },
            },
        };
        RoutingDecision? decision = RuleEngine.Evaluate(rules, Ctx(new Uri("https://example.com")), Evaluator);
        Assert.NotNull(decision);
        Assert.Equal("chrome-work", decision!.BrowserId);
        Assert.Equal("firefox-work", decision.ProfileId);
        Assert.False(decision.FocusExisting);
        Assert.True(decision.NewWindow);
        Assert.True(decision.Private);
        Assert.False(decision.StripTracking);
        Assert.True(decision.Unshorten);
    }
}