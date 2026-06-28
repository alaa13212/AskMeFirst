using AskMeFirst.Core.Config;

namespace AskMeFirst.Core.Routing;

public sealed class ProcessInMatcher : IPredicateMatcher
{
    public bool Matches(RuleWhen ruleWhen, RoutingContext ctx)
    {
        if (ruleWhen.ProcessIn is not { Count: > 0 })
        {
            return true;
        }
        if (ctx.SourceProcess is null)
        {
            return false;
        }
        foreach (string s in ruleWhen.ProcessIn)
        {
            if (string.Equals(s, ctx.SourceProcess, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }
}