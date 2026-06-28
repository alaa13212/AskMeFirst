namespace AskMeFirst.Core.Routing;

public interface ITargetResolver
{
    RoutingIntent? Resolve(RoutingContext ctx);
}