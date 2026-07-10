namespace AskMeFirst.Core.Routing;

public sealed record RoutingTimings(
    TimeSpan RuleEval,
    TimeSpan InventoryLoad,
    TimeSpan Total);

