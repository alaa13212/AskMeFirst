namespace AskMeFirst.Core.Routing;

public sealed record RoutingIntent(
    string BrowserId,
    string? ProfileId,
    bool? StripTrackingOverride,
    bool NewWindow,
    RoutingExitCode NotFoundExitCode,
    string NotFoundMessagePrefix);