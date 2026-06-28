namespace AskMeFirst.Core.Routing;

public sealed record RoutingIntent(
    string BrowserId,
    string? ProfileId,
    bool? StripTrackingOverride,
    RoutingExitCode NotFoundExitCode,
    string NotFoundMessagePrefix);