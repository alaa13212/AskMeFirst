using AskMeFirst.Core.Config;

namespace AskMeFirst.Core.Routing;

public sealed class UrlMatchesAllMatcher : IPredicateMatcher
{
    public bool Matches(RuleWhen ruleWhen, RoutingContext ctx)
    {
        if (ruleWhen.UrlMatchesAll is not { Count: > 0 })
        {
            return true;
        }
        foreach (string pattern in ruleWhen.UrlMatchesAll)
        {
            if (!GlobMatcher.Matches(pattern, ctx.Host, ctx.HostPath))
            {
                return false;
            }
        }
        return true;
    }
}