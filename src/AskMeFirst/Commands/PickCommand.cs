using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Commands;
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
        IUnshortenTaskBuilder unshortenTasks = ctx.Resolve<IUnshortenTaskBuilder>();

        IReadOnlyList<PickerBrowserOption> options = PickerOptions.Build(inventory.Discover(), profiles);
        PickerRequest request = new(
            OriginalUrl: url,
            UnshortenTask: unshortenTasks.Build(url),
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
