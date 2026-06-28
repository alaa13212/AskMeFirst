using AskMeFirst.Core.Models;

namespace AskMeFirst.Core.Routing;

public abstract record RoutingOutcome;

public sealed record Success(Browser Browser, Uri FinalUrl, Uri OriginalUrl) : RoutingOutcome;

public sealed record Failure(RoutingExitCode Code, string Message) : RoutingOutcome;