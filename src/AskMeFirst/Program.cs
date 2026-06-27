using System.Diagnostics;

namespace AskMeFirst;

internal static class Program
{
    private const string VERSION = "0.1.0";
    private const string EXECUTABLE_NAME = "askmefirst";

    private static int Main()
    {
        string[] cli = Environment.GetCommandLineArgs();
        string[] userArgs = cli.Length > 1 ? cli[1..] : [];

        if (userArgs.Length == 0)
        {
            PrintUsage();
            return 1;
        }

        return userArgs[0] switch
        {
            "--version" or "-v" => PrintVersion(),
            "--help" or "-h" or "-?" => PrintHelp(),
            "--bench" => PrintBench(),
            _ => UnknownCommand(userArgs[0])
        };
    }

    private static int PrintVersion()
    {
        Console.WriteLine($"{EXECUTABLE_NAME} {VERSION}");
        return 0;
    }

    private static int PrintHelp()
    {
        Console.WriteLine($"{EXECUTABLE_NAME} — smart browser router");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine($"  {EXECUTABLE_NAME} --version       Print version and exit");
        Console.WriteLine($"  {EXECUTABLE_NAME} --help          Print this help and exit");
        Console.WriteLine($"  {EXECUTABLE_NAME} --bench         Print a placeholder benchmark");
        Console.WriteLine();
        Console.WriteLine("Bootstrap build. URL routing not yet implemented.");
        Console.WriteLine("See docs/roadmap.md for the build plan.");
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

    private static int UnknownCommand(string command)
    {
        Console.Error.WriteLine($"Unknown argument: {command}");
        Console.Error.WriteLine($"Run '{EXECUTABLE_NAME} --help' for usage.");
        return 1;
    }

    private static void PrintUsage()
    {
        Console.Error.WriteLine($"Usage: {EXECUTABLE_NAME} --version | --help | --bench");
        Console.Error.WriteLine("No URL routing yet.");
    }
}
