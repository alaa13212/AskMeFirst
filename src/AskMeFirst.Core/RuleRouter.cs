using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Routing;

namespace AskMeFirst.Core;

public sealed class RuleRouter(
    IReadOnlyList<ITargetResolver> resolvers,
    IRoutingExecutor executor,
    ISourceAppDetector sourceAppDetector,
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
            logger.LogError(
                "No rule matched and no default browser configured. " +
                "Set Settings.DefaultBrowserId or add a rule.");
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
}