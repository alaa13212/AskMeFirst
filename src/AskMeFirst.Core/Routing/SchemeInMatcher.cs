using AskMeFirst.Core.Config;

namespace AskMeFirst.Core.Routing;

public sealed class SchemeInMatcher : IPredicateMatcher
{
    public bool Matches(RuleWhen ruleWhen, RoutingContext ctx)
    {
        if (ruleWhen.SchemeIn is not { Count: > 0 })
        {
            return true;
        }
        foreach (string s in ruleWhen.SchemeIn)
        {
            if (string.Equals(s, ctx.Url.Scheme, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }
}