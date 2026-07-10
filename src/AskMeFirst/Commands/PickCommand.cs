using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Commands;
using AskMeFirst.Core.Config;
using AskMeFirst.Core.Routing;

namespace AskMeFirst.Commands;

public sealed class PickCommand : ICommand
{
    public string Name => "pick";

    public string Usage => "pick <url>";

    public string Description => "Open the picker for a URL, bypassing routing rules.";

    public Task<int> Execute(string[] args, CommandContext ctx)
    {
        if (args.Length < 2)
        {
            throw new CliArgsException("pick requires a URL argument.");
        }

        string urlArg = args[1];
        if (!Uri.TryCreate(urlArg, UriKind.Absolute, out Uri? url)
            || (url.Scheme != Uri.UriSchemeHttp && url.Scheme != Uri.UriSchemeHttps))
        {
            throw new CliArgsException($"Not a valid http(s) URL: {urlArg}");
        }

        ILogger logger = ctx.Resolve<ILogger>();
        IBrowserInventory inventory = ctx.Resolve<IBrowserInventory>();
        IBrowserProfileDetector profiles = ctx.Resolve<IBrowserProfileDetector>();
        IPickerLauncher pickerLauncher = ctx.Resolve<IPickerLauncher>();
        IUnshortener unshortener = ctx.Resolve<IUnshortener>();
        IShortenerDomainList shortenerDomains = ctx.Resolve<IShortenerDomainList>();
        TrackingStripper stripper = ctx.Resolve<TrackingStripper>();

        IReadOnlyList<PickerBrowserOption> options = PickerOptions.Build(inventory.Discover(), profiles);
        Task<string?>? unshortenTask = BuildUnshortenTask(url, unshortener, shortenerDomains, stripper, logger);

        PickerRequest request = new(
            OriginalUrl: url,
            UnshortenTask: unshortenTask,
            AvailableBrowsers: options);

        logger.LogInfo($"Opening picker for {url}");
        PickerResult result = pickerLauncher.Show(request);

        return result switch
        {
            Cancelled => Task.FromResult(0),
            Launched l => LaunchAndReturn(l, ctx),
            _ => Task.FromResult(99),
        };
    }

    private static Task<string?>? BuildUnshortenTask(
        Uri url,
        IUnshortener unshortener,
        IShortenerDomainList shortenerDomains,
        TrackingStripper stripper,
        ILogger logger)
    {
        if (string.IsNullOrEmpty(url.Host) || !shortenerDomains.IsKnown(url.Host))
        {
            return null;
        }
        return ResolveAndStripAsync(url, unshortener, stripper, logger, CancellationToken.None);
    }

    private static async Task<string?> ResolveAndStripAsync(
        Uri url,
        IUnshortener unshortener,
        TrackingStripper stripper,
        ILogger logger,
        CancellationToken ct)
    {
        try
        {
            string? resolved = await unshortener.ResolveAsync(url, ct).ConfigureAwait(false);
            if (resolved is null)
            {
                return null;
            }
            if (!Uri.TryCreate(resolved, UriKind.Absolute, out Uri? resolvedUri))
            {
                return null;
            }
            return stripper.Strip(resolvedUri).ToString();
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (Exception ex)
        {
            logger.LogWarn($"Unshorten failed for {url}: {ex.Message}");
            return null;
        }
    }

    private static Task<int> LaunchAndReturn(Launched launched, CommandContext ctx)
    {
        ILogger logger = ctx.Resolve<ILogger>();
        IUrlLauncher launcher = ctx.Resolve<IUrlLauncher>();
        INotifier notifier = ctx.Resolve<INotifier>();
        try
        {
            launcher.Launch(launched.Browser, launched.Url);
            return Task.FromResult(0);
        }
        catch (Exception ex)
        {
            logger.LogError($"Browser launch failed: {ex.Message}");
            notifier.Show(
                title: "Couldn't open browser",
                message: $"Couldn't open {launched.Browser.DisplayName} for {launched.Url}. The URL is in your recent picks; try again.");
            return Task.FromResult((int)RoutingExitCode.BrowserNotFound);
        }
    }
}
