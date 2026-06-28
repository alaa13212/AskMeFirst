using AskMeFirst.Core.Commands;

namespace AskMeFirst.Commands;

public sealed class RouteCommand : ICommand
{
    public string Name => "open";
    public string Usage => "<url> [--browser <id>] [--profile <profileId>] [--verbose]";
    public string Description => "Route a URL to the chosen browser.";

    public int Execute(string[] args, CommandContext ctx)
    {
        RouteArgs parsed = ParseArgs(args);
        Console.Error.WriteLine($"[info] platform: {ctx.PlatformName}");
        return ctx.Router.Route(parsed.Url, parsed.BrowserId, parsed.ProfileId);
    }

    public static RouteArgs ParseArgs(string[] args)
    {
        string? url = null;
        string? browserId = null;
        string? profileId = null;
        bool verbose = false;

        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];
            switch (arg)
            {
                case "--browser" or "-b":
                    if (i + 1 >= args.Length)
                    {
                        throw new CliArgsException("--browser requires a value.");
                    }
                    browserId = args[++i];
                    break;
                case "--profile" or "-p":
                    if (i + 1 >= args.Length)
                    {
                        throw new CliArgsException("--profile requires a value.");
                    }
                    profileId = args[++i];
                    break;
                case "--verbose" or "-v":
                    verbose = true;
                    break;
                default:
                    if (arg.StartsWith('-'))
                    {
                        throw new CliArgsException($"Unknown flag: {arg}");
                    }
                    url ??= arg;
                    break;
            }
        }

        if (url is null)
        {
            throw new CliArgsException("No URL provided.");
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? parsed)
            || (parsed.Scheme != Uri.UriSchemeHttp && parsed.Scheme != Uri.UriSchemeHttps))
        {
            throw new CliArgsException($"Not a valid http(s) URL: {url}");
        }

        return new RouteArgs(parsed, browserId, profileId, verbose);
    }
}