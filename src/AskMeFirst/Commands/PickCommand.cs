using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Commands;
using AskMeFirst.Core.Routing;
using Microsoft.Extensions.DependencyInjection;

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

        IServiceProvider services = ctx.Services;
        ILogger logger = services.GetRequiredService<ILogger>();
        ISourceAppDetector sourceApp = services.GetRequiredService<ISourceAppDetector>();
        IBrowserInventory inventory = services.GetRequiredService<IBrowserInventory>();
        IBrowserProfileDetector profiles = services.GetRequiredService<IBrowserProfileDetector>();
        IPickerLauncher pickerLauncher = services.GetRequiredService<IPickerLauncher>();

        SourceApp? source = sourceApp.Detect();
        IReadOnlyList<PickerBrowserOption> options = PickerOptions.Build(inventory.Discover(), profiles);

        PickerRequest request = new(
            OriginalUrl: url,
            SourceApp: source?.ProcessName,
            UnshortenTask: null,
            AvailableBrowsers: options);

        logger.LogInfo($"Opening picker for {url} (source: {source?.ProcessName ?? "unknown"})");
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
        IServiceProvider services = ctx.Services;
        ILogger logger = services.GetRequiredService<ILogger>();
        IUrlLauncher launcher = services.GetRequiredService<IUrlLauncher>();
        INotifier notifier = services.GetRequiredService<INotifier>();
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
