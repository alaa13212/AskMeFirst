using System.Diagnostics;
using AskMeFirst.Core;
using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Models;

namespace AskMeFirst;

internal static class Program
{
    private const string VERSION = "0.2.0";
    private const string EXECUTABLE_NAME = "askmefirst";

    private static int Main()
    {
        string[] cli = Environment.GetCommandLineArgs();
        string[] userArgs = cli.Length > 1 ? cli[1..] : [];

        if (userArgs.Length == 0)
        {
            Console.Error.WriteLine(CliArgsParser.HelpText);
            return 1;
        }

        string first = userArgs[0];
        if (first is "--version" or "-V")
        {
            return PrintVersion();
        }
        if (first is "--help" or "-h" or "-?")
        {
            return PrintHelp();
        }
        if (first is "--bench")
        {
            return PrintBench();
        }
        if (first is "--list")
        {
            return PrintList();
        }

        try
        {
            CliArgs args = CliArgsParser.Parse(userArgs);
            UrlRouter router = Composition.BuildRouter(args.Verbose, out string platform);
            Console.Error.WriteLine($"[info] platform: {platform}");
            return router.Route(args.Url, args.BrowserId);
        }
        catch (CliArgsException ex)
        {
            Console.Error.WriteLine($"error: {ex.Message}");
            Console.Error.WriteLine($"Run '{EXECUTABLE_NAME} --help' for usage.");
            return 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"fatal: {ex.Message}");
            return 99;
        }
    }

    private static int PrintVersion()
    {
        Console.WriteLine($"{EXECUTABLE_NAME} {VERSION}");
        return 0;
    }

    private static int PrintHelp()
    {
        Console.WriteLine(CliArgsParser.HelpText);
        return 0;
    }

    private static int PrintBench()
    {
        Stopwatch sw = Stopwatch.StartNew();
        int iterations = 100_000;
        long sum = 0;
        for (int i = 0; i < iterations; i++)
        {
            sum += i;
        }
        sw.Stop();
        Console.WriteLine($"askmefirst --bench (placeholder)");
        Console.WriteLine($"  iterations: {iterations:N0}");
        Console.WriteLine($"  elapsed:    {sw.Elapsed.TotalMilliseconds:F2} ms");
        Console.WriteLine($"  per-iter:   {sw.Elapsed.TotalMilliseconds / iterations * 1000:F3} µs");
        Console.WriteLine($"  sum:        {sum} (consumed)");
        Console.WriteLine();
        Console.WriteLine("Sanity check, not a real benchmark.");
        return 0;
    }

    private static int PrintList()
    {
        IBrowserInventory inventory = Composition.BuildInventory();
        IReadOnlyList<Browser> browsers = inventory.Discover();
        if (browsers.Count == 0)
        {
            Console.WriteLine("No browsers discovered on this system.");
            return 0;
        }
        Console.WriteLine($"Discovered {browsers.Count} browser(s):");
        foreach (Browser b in browsers)
        {
            Console.WriteLine($"  {b.Id,-12} {b.DisplayName,-24} {b.ExecutablePath}");
        }
        return 0;
    }
}
