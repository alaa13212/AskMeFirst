namespace AskMeFirst;

public sealed record CliArgs(Uri Url, string? BrowserId, bool Verbose);

public static class CliArgsParser
{
    public const string HelpText =
        """
        askmefirst — smart browser router

        Usage:
          askmefirst <url> [--browser <id>] [--verbose]
          askmefirst --version
          askmefirst --help
          askmefirst --bench
          askmefirst --list

        Options:
          <url>             The http(s) URL to route.
          -b, --browser <id>  Force a specific browser by id (chrome, firefox, edge, ...).
                              Default: first discovered browser.
          -v, --verbose     Print routing decisions to stderr.
          --list            List discovered browsers and exit.
        """;

    public static CliArgs Parse(string[] args)
    {
        string? url = null;
        string? browserId = null;
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
            throw new CliArgsException("No URL provided. Run 'askmefirst --help' for usage.");
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? parsed)
            || (parsed.Scheme != Uri.UriSchemeHttp && parsed.Scheme != Uri.UriSchemeHttps))
        {
            throw new CliArgsException($"Not a valid http(s) URL: {url}");
        }

        return new CliArgs(parsed, browserId, verbose);
    }
}

public sealed class CliArgsException : Exception
{
    public CliArgsException(string message) : base(message) { }
}
