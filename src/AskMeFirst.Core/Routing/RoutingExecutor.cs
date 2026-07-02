using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Config;
using AskMeFirst.Core.Models;

namespace AskMeFirst.Core.Routing;

public sealed class RoutingExecutor(
    IBrowserInventory inventory,
    ProfileResolver profileResolver,
    TrackingStripper stripper,
    AppConfig appConfig) : IRoutingExecutor
{
    public RoutingOutcome Execute(RoutingIntent intent, Uri url)
    {
        IReadOnlyList<Browser> browsers = inventory.Discover();
        if (browsers.Count == 0)
        {
            return new Failure(RoutingExitCode.NoBrowsersDiscovered, "No browsers discovered on this system.");
        }

        Browser? browser = inventory.FindById(intent.BrowserId);
        if (browser is null)
        {
            string discovered = string.Join(", ", browsers.Select(b => b.Id));
            string message = $"{intent.NotFoundMessagePrefix} '{intent.BrowserId}' not found. Discovered: {discovered}";
            return new Failure(intent.NotFoundExitCode, message);
        }

        Browser resolved = profileResolver.Resolve(browser, intent.ProfileId);
        resolved = resolved with { NewWindow = intent.NewWindow };
        bool strip = intent.StripTrackingOverride ?? appConfig.Settings.StripTracking;
        Uri finalUrl = strip ? stripper.Strip(url) : url;
        return new Success(resolved, finalUrl, url);
    }
}