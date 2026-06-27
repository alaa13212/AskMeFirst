using AskMeFirst.Core.Abstractions;

namespace AskMeFirst.Core.Logging;

public sealed class ConsoleLogger : ILogger
{
    private readonly bool _verbose;

    public ConsoleLogger(bool verbose = false)
    {
        _verbose = verbose;
    }

    public void LogInfo(string message)
    {
        if (_verbose)
        {
            Console.Error.WriteLine($"[info] {message}");
        }
    }

    public void LogWarn(string message)
    {
        Console.Error.WriteLine($"[warn] {message}");
    }

    public void LogError(string message)
    {
        Console.Error.WriteLine($"[error] {message}");
    }
}
