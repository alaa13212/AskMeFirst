using System.Diagnostics;
using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Config;
using AskMeFirst.Core.Models;
using AskMeFirst.Core.Profiles;
using AskMeFirst.Core.Routing;

namespace AskMeFirst.Core;

public sealed class RuleRouter(
    IReadOnlyList<ITargetResolver> resolvers,
    IRoutingExecutor executor,
    IBrowserInventory browserInventory,
    IPickerLauncher pickerLauncher,
    bool usePickerAsCatchAll,
    IReadOnlyList<ProfileSpec> profileSpecs,
    IBrowserProfileDetector profileDetector,
    IUrlLauncher launcher,
    ILogger logger,
    INotifier notifier,
    TimeProvider timeProvider,
    IUnshortenTaskBuilder unshortenTasks)
{
    public RouteResult Route(Uri url, string? explicitBrowserId, string? explicitProfileId)
    {
        Stopwatch totalSw = Stopwatch.StartNew();
        RoutingContext ctx = RoutingContext.Create(
            url,
            timeProvider.GetUtcNow(),
            explicitBrowserId: explicitBrowserId,
            explicitProfileId: explicitProfileId);

        Stopwatch ruleSw = Stopwatch.StartNew();
        RoutingIntent? intent = null;
        foreach (ITargetResolver resolver in resolvers)
        {
            intent = resolver.Resolve(ctx);
            if (intent is not null)
            {
                break;
            }
        }
        ruleSw.Stop();

        int exitCode;
        if (intent is null)
        {
            if (usePickerAsCatchAll)
            {
                PickerRequest request = BuildPickerRequest(url);
                exitCode = HandlePicker(request, url);
            }
            else
            {
                logger.LogError(
                    "No rule matched. " +
                    "Add a rule.");
                exitCode = (int)RoutingExitCode.NoRouteFound;
            }
            totalSw.Stop();
            return new RouteResult(exitCode, new RoutingTimings(ruleSw.Elapsed, TimeSpan.Zero, totalSw.Elapsed));
        }

        Stopwatch execSw = Stopwatch.StartNew();
        RoutingOutcome outcome = executor.Execute(intent, url);
        execSw.Stop();
        exitCode = HandleOutcome(outcome, url);
        totalSw.Stop();
        return new RouteResult(exitCode, new RoutingTimings(ruleSw.Elapsed, execSw.Elapsed, totalSw.Elapsed));
    }

    private int HandleOutcome(RoutingOutcome outcome, Uri url) =>
        outcome switch
        {
            Success success => LogAndLaunch(success, url),
            Failure failure => LogAndReturnFailure(failure),
            ShowPicker showPicker => HandlePicker(showPicker.Request, url),
            _ => throw new InvalidOperationException($"Unknown outcome: {outcome.GetType().Name}"),
        };

private int LogAndLaunch(Success success, Uri url)
    {
        string profileSuffix = success.Browser.Profile is null ? "" : $" [profile: {success.Browser.Profile.Name}]";
        logger.LogInfo($"Routing {url} → {success.Browser.DisplayName} ({success.Browser.ExecutablePath}){profileSuffix}");
        if (url != success.FinalUrl)
        {
            logger.LogInfo($"stripped tracking params → {success.FinalUrl}");
        }
        return TryLaunch(success.Browser, success.FinalUrl, success.Browser.DisplayName, url);
    }

    private int LogAndReturnFailure(Failure failure)
    {
        logger.LogError(failure.Message);
        return (int)failure.Code;
    }

    private PickerRequest BuildPickerRequest(Uri url)
    {
        IReadOnlyList<Browser> browsers = browserInventory.Discover();
        IReadOnlyList<PickerBrowserOption> options = PickerOptions.Build(browsers, profileDetector);
        IReadOnlyList<PickerBrowserOption> filtered = PinnedProfileFilter.Filter(options, profileSpecs);
        return new PickerRequest(
            OriginalUrl: url,
            UnshortenTask: unshortenTasks.Build(url),
            AvailableBrowsers: filtered);
    }

    private int HandlePicker(PickerRequest request, Uri url)
    {
        PickerResult result = pickerLauncher.Show(request);
        return result switch
        {
            Cancelled => LogCancel(url),
            Launched launched => LogAndLaunchResult(launched, url),
            _ => throw new InvalidOperationException($"Unknown picker result: {result.GetType().Name}"),
        };
    }

    private int LogCancel(Uri url)
    {
        logger.LogInfo($"User cancelled picker for {url}. URL dropped.");
        return (int)RoutingExitCode.Success;
    }

    private int LogAndLaunchResult(Launched launched, Uri url)
    {
        string profileSuffix = launched.Browser.Profile is null ? "" : $" [profile: {launched.Browser.Profile.Name}]";
        logger.LogInfo($"Picker chose {launched.Browser.DisplayName} ({launched.Browser.ExecutablePath}){profileSuffix} for {url}");
        return TryLaunch(launched.Browser, launched.Url, launched.Browser.DisplayName, url);
    }

    private int TryLaunch(Browser browser, Uri target, string displayName, Uri originalUrl)
    {
        try
        {
            launcher.Launch(browser, target);
            return (int)RoutingExitCode.Success;
        }
        catch (Exception ex)
        {
            logger.LogError($"Browser launch failed: {ex.Message}");
            notifier.Show(
                title: "Couldn't open browser",
                message: $"Couldn't open {displayName} for {originalUrl}. The URL is in your recent picks; try again.");
            return (int)RoutingExitCode.BrowserNotFound;
        }
    }
}
