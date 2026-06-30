using AskMeFirst.Core.Models;

namespace AskMeFirst.Core.Routing;

public interface IRoutingExecutor
{
    RoutingOutcome Execute(RoutingIntent intent, Uri url);
}