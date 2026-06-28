namespace AskMeFirst.Core.Routing;

public sealed class ExplicitOverrideResolver : ITargetResolver
{
    public RoutingIntent? Resolve(RoutingContext ctx)
    {
        if (ctx.ExplicitBrowserId is null)
        {
            return null;
        }
        return new RoutingIntent(
            ctx.ExplicitBrowserId,
            ctx.ExplicitProfileId,
            StripTrackingOverride: null,
            NotFoundExitCode: RoutingExitCode.BrowserNotFound,
            NotFoundMessagePrefix: "Browser");
    }
}