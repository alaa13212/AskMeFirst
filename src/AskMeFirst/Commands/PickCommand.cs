using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Commands;
using AskMeFirst.Core.Routing;

namespace AskMeFirst.Commands;

public sealed class PickCommand(
    IPickerLauncher pickerLauncher,
    ISourceAppDetector sourceApp,
    IBrowserInventory inventory,
    IBrowserProfileDetector profileDetector,
    ILogger logger) : ICommand
{
    public string Name => "pick";

    public string Usage => "pick <url>";

    public string Description => "Open the picker for a URL, bypassing routing rules.";

    public int Execute(string[] args, CommandContext ctx)
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

        SourceApp? source = sourceApp.Detect();
        IReadOnlyList<PickerBrowserOption> options = PickerOptions.Build(inventory.Discover(), profileDetector);

        PickerRequest request = new(
            OriginalUrl: url,
            SourceApp: source?.ProcessName,
            UnshortenTask: null,
            AvailableBrowsers: options);

        logger.LogInfo($"Opening picker for {url} (source: {source?.ProcessName ?? "unknown"})");
        PickerResult result = pickerLauncher.Show(request);

        return result switch
        {
            Cancelled => 0,
            Launched l => LaunchAndReturn(l, ctx),
            _ => 99,
        };
    }

    private static int LaunchAndReturn(Launched launched, CommandContext ctx)
    {
        ctx.Launcher.Launch(launched.Browser, launched.Url);
        return 0;
    }
}
