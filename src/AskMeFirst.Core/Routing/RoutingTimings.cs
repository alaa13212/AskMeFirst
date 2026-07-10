namespace AskMeFirst.Core.Routing;

public sealed record RoutingTimings(
    TimeSpan RuleEval,
    TimeSpan Executor,
    TimeSpan Total);
