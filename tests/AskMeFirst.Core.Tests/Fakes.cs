using AskMeFirst.Core.Models;

namespace AskMeFirst.Core.Tests;

internal sealed class FakeInventory : Core.Abstractions.IBrowserInventory
{
    public List<Browser> Browsers { get; init; } = [];

    public IReadOnlyList<Browser> Discover() => Browsers;

    public Browser? FindById(string id) =>
        Browsers.FirstOrDefault(b => string.Equals(b.Id, id, StringComparison.OrdinalIgnoreCase));
}

internal sealed class FakeLauncher : Core.Abstractions.IUrlLauncher
{
    public List<(Browser Browser, Uri Url)> Launches { get; } = [];

    public void Launch(Browser browser, Uri url)
    {
        Launches.Add((browser, url));
    }
}

internal sealed class FakeLogger : Core.Abstractions.ILogger
{
    public List<string> Infos { get; } = [];
    public List<string> Warns { get; } = [];
    public List<string> Errors { get; } = [];

    public void LogInfo(string message) => Infos.Add(message);
    public void LogWarn(string message) => Warns.Add(message);
    public void LogError(string message) => Errors.Add(message);
}
