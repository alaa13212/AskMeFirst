using AskMeFirst.Core.Commands;
using AskMeFirst.Core.Routing;

namespace AskMeFirst.Commands;

public sealed class PickCommand : ICommand
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

        SourceApp? source = ctx.SourceApp.Detect();
        IReadOnlyList<PickerBrowserOption> options = PickerOptions.Build(ctx.Inventory.Discover(), ctx.Profiles);

        PickerRequest request = new(
            OriginalUrl: url,
            SourceApp: source?.ProcessName,
            UnshortenTask: null,
            AvailableBrowsers: options);

        ctx.Logger.LogInfo($"Opening picker for {url} (source: {source?.ProcessName ?? "unknown"})");
        PickerResult result = ctx.PickerLauncher.Show(request);

        return result switch
        {
            Cancelled => 0,
            Launched l => LaunchAndReturn(l, ctx),
            _ => 99,
        };
    }

    private static int LaunchAndReturn(Launched launched, CommandContext ctx)
    {
        try
        {
            ctx.Launcher.Launch(launched.Browser, launched.Url);
            return 0;
        }
        catch (Exception ex)
        {
            ctx.Logger.LogError($"Browser launch failed: {ex.Message}");
            ctx.Notifier.Show(
                title: "Couldn't open browser",
                message: $"Couldn't open {launched.Browser.DisplayName} for {launched.Url}. The URL is in your recent picks; try again.");
            return (int)RoutingExitCode.BrowserNotFound;
        }
    }
}
