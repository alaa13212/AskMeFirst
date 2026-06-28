using AskMeFirst.Core.Config;

namespace AskMeFirst.Core.Routing;

public sealed class UrlMatchesAnyMatcher : IPredicateMatcher
{
    public bool Matches(RuleWhen ruleWhen, RoutingContext ctx)
    {
        if (ruleWhen.UrlMatchesAny is not { Count: > 0 })
        {
            return true;
        }
        foreach (string pattern in ruleWhen.UrlMatchesAny)
        {
            if (GlobMatcher.Matches(pattern, ctx.Host, ctx.HostPath))
            {
                return true;
            }
        }
        return false;
    }
}