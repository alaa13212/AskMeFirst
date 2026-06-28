using AskMeFirst.Core.Config;

namespace AskMeFirst.Core.Routing;

public sealed class SettingsFallbackResolver : ITargetResolver
{
    private readonly AppConfig appConfig;

    public SettingsFallbackResolver(AppConfig appConfig)
    {
        this.appConfig = appConfig;
    }

    public RoutingIntent? Resolve(RoutingContext ctx)
    {
        string? fallbackId = appConfig.Settings.DefaultBrowserId;
        if (string.IsNullOrWhiteSpace(fallbackId))
        {
            return null;
        }
        return new RoutingIntent(
            fallbackId,
            ProfileId: null,
            StripTrackingOverride: null,
            NotFoundExitCode: RoutingExitCode.BrowserNotFound,
            NotFoundMessagePrefix: "Browser");
    }
}