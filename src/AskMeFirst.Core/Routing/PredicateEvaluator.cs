using AskMeFirst.Core.Config;

namespace AskMeFirst.Core.Routing;

public sealed class PredicateEvaluator
{
    private readonly IReadOnlyList<IPredicateMatcher> matchers;

    public PredicateEvaluator(IReadOnlyList<IPredicateMatcher> matchers)
    {
        this.matchers = matchers;
    }

    public bool Matches(RuleWhen ruleWhen, RoutingContext ctx)
    {
        foreach (IPredicateMatcher matcher in matchers)
        {
            if (!matcher.Matches(ruleWhen, ctx))
            {
                return false;
            }
        }
        return true;
    }
}