using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Config;
using AskMeFirst.Core.Models;
using AskMeFirst.Core.Profiles;
using AskMeFirst.Core.Routing;

namespace AskMeFirst.Core;

public sealed class RuleRouter(
    IReadOnlyList<ITargetResolver> resolvers,
    IRoutingExecutor executor,
    ISourceAppDetector sourceAppDetector,
    IPickerLauncher pickerLauncher,
    bool usePickerAsCatchAll,
    IReadOnlyList<ProfileSpec> profileSpecs,
    IBrowserProfileDetector profileDetector,
    IUrlLauncher launcher,
    ILogger logger,
    TimeProvider timeProvider)
{
    public int Route(Uri url, string? explicitBrowserId, string? explicitProfileId)
    {
        SourceApp? sourceApp = sourceAppDetector.Detect();
        if (sourceApp is not null)
        {
            logger.LogInfo($"source app: {sourceApp.ProcessName}");
        }

        RoutingContext ctx = RoutingContext.Create(
            url,
            sourceApp?.ProcessName,
            timeProvider.GetUtcNow(),
            explicitBrowserId: explicitBrowserId,
            explicitProfileId: explicitProfileId);

        RoutingIntent? intent = null;
        foreach (ITargetResolver resolver in resolvers)
        {
            intent = resolver.Resolve(ctx);
            if (intent is not null)
            {
                break;
            }
        }

        if (intent is null)
        {
            if (usePickerAsCatchAll)
            {
                PickerRequest request = BuildPickerRequest(ctx, url, sourceApp?.ProcessName);
                return HandlePicker(request, url);
            }
            logger.LogError(
                "No rule matched. " +
                "Add a rule.");
            return (int)RoutingExitCode.NoRouteFound;
        }

        RoutingOutcome outcome = executor.Execute(intent, url);
        return HandleOutcome(outcome, url);
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
        launcher.Launch(success.Browser, success.FinalUrl);
        return (int)RoutingExitCode.Success;
    }

    private int LogAndReturnFailure(Failure failure)
    {
        logger.LogError(failure.Message);
        return (int)failure.Code;
    }

    private PickerRequest BuildPickerRequest(RoutingContext ctx, Uri url, string? sourceApp)
    {
        IReadOnlyList<Browser> browsers = executor.ListAvailableBrowsers();
        IReadOnlyList<PickerBrowserOption> options = PickerOptions.Build(browsers, profileDetector);
        IReadOnlyList<PickerBrowserOption> filtered = PinnedProfileFilter.Filter(options, profileSpecs);
        return new PickerRequest(
            OriginalUrl: url,
            SourceApp: sourceApp,
            UnshortenTask: null,
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
        launcher.Launch(launched.Browser, launched.Url);
        return (int)RoutingExitCode.Success;
    }
}
