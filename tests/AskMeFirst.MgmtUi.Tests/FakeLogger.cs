using AskMeFirst.Core.Abstractions;

namespace AskMeFirst.MgmtUi.Tests;

internal sealed class FakeLogger : ILogger
{
    public List<string> Infos { get; } = [];
    public List<string> Warns { get; } = [];
    public List<string> Errors { get; } = [];

    public void LogInfo(string message) => Infos.Add(message);
    public void LogWarn(string message) => Warns.Add(message);
    public void LogError(string message) => Errors.Add(message);
}
