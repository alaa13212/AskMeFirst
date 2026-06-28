using AskMeFirst.Core.Config;

namespace AskMeFirst.Core.Routing;

public sealed class BrowserRunningMatcher : IPredicateMatcher
{
    public bool Matches(RuleWhen ruleWhen, RoutingContext ctx)
    {
        if (ruleWhen.BrowserRunning is not bool running)
        {
            return true;
        }
        return ctx.IsTargetBrowserRunning == running;
    }
}