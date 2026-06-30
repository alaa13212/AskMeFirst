using System.Diagnostics;
using AskMeFirst.Core.Commands;

namespace AskMeFirst.Commands;

public sealed class BenchCommand : ICommand
{
    public string Name => "--bench";
    public string Usage => "--bench";
    public string Description => "Run a placeholder self-timing loop.";

    public int Execute(string[] args, CommandContext ctx)
    {
        Stopwatch sw = Stopwatch.StartNew();
        int iterations = 100_000;
        long sum = 0;
        for (int i = 0; i < iterations; i++)
        {
            sum += i;
        }
        sw.Stop();
        Console.WriteLine($"{ProgramInfo.ExecutableName} --bench (placeholder)");
        Console.WriteLine($"  iterations: {iterations:N0}");
        Console.WriteLine($"  elapsed:    {sw.Elapsed.TotalMilliseconds:F2} ms");
        Console.WriteLine($"  per-iter:   {sw.Elapsed.TotalMilliseconds / iterations * 1000:F3} µs");
        Console.WriteLine($"  sum:        {sum} (consumed)");
        Console.WriteLine();
        Console.WriteLine("Sanity check, not a real benchmark.");
        return 0;
    }
}
