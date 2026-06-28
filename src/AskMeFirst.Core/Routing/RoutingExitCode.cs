namespace AskMeFirst.Core.Routing;

public enum RoutingExitCode
{
    Success = 0,
    NoBrowsersDiscovered = 2,
    BrowserNotFound = 3,
    RuleBrowserNotFound = 4,
    NoRouteFound = 5,
}