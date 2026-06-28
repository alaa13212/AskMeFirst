using AskMeFirst.Core.Config;

namespace AskMeFirst.Core.Routing;

public interface IPredicateMatcher
{
    bool Matches(RuleWhen ruleWhen, RoutingContext ctx);
}