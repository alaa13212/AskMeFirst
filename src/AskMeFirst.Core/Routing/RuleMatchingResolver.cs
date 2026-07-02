using AskMeFirst.Core.Config;

namespace AskMeFirst.Core.Routing;

public sealed class RuleMatchingResolver : ITargetResolver
{
    private readonly IReadOnlyList<Rule> rules;
    private readonly PredicateEvaluator evaluator;

    public RuleMatchingResolver(IReadOnlyList<Rule> rules, PredicateEvaluator evaluator)
    {
        this.rules = rules;
        this.evaluator = evaluator;
    }

    public RoutingIntent? Resolve(RoutingContext ctx)
    {
        RoutingDecision? decision = RuleEngine.Evaluate(rules, ctx, evaluator);
        if (decision is null)
        {
            return null;
        }
        return new RoutingIntent(
            decision.BrowserId,
            decision.ProfileId,
            decision.StripTracking,
            decision.NewWindow,
            NotFoundExitCode: RoutingExitCode.RuleBrowserNotFound,
            NotFoundMessagePrefix: "Rule matched browser");
    }
}