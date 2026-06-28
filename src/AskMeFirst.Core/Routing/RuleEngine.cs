using AskMeFirst.Core.Config;

namespace AskMeFirst.Core.Routing;

public static class RuleEngine
{
    public static RoutingDecision? Evaluate(
        IReadOnlyList<Rule> rules,
        RoutingContext ctx,
        PredicateEvaluator evaluator)
    {
        RoutingDecision? best = null;
        int bestPriority = int.MinValue;
        for (int i = 0; i < rules.Count; i++)
        {
            Rule rule = rules[i];
            if (rule.Priority < bestPriority)
            {
                continue;
            }
            if (rule.Priority == bestPriority && best is not null)
            {
                continue;
            }
            if (!evaluator.Matches(rule.When, ctx))
            {
                continue;
            }
            best = ToDecision(rule.Then);
            bestPriority = rule.Priority;
        }
        return best;
    }

    private static RoutingDecision ToDecision(RuleThen then)
    {
        return new RoutingDecision
        {
            BrowserId = then.Browser,
            ProfileId = then.ProfileId,
            FocusExisting = then.FocusExisting,
            NewWindow = then.NewWindow,
            Private = then.Private,
            StripTracking = then.StripTracking,
            Unshorten = then.Unshorten,
        };
    }
}