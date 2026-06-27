using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Launch;
using AskMeFirst.Core.Models;

namespace AskMeFirst.Core.Tests;

internal static class TestBrowser
{
    public static Browser Make(string id, string displayName, string executablePath)
    {
        return new Browser
        {
            Id = id,
            DisplayName = displayName,
            ExecutablePath = executablePath,
            LaunchStrategy = BrowserLaunchStrategies.For(id),
        };
    }
}

internal sealed class FakeInventory : IBrowserInventory
{
    public List<Browser> Browsers { get; init; } = [];

    public IReadOnlyList<Browser> Discover() => Browsers;

    public Browser? FindById(string id) =>
        Browsers.FirstOrDefault(b => string.Equals(b.Id, id, StringComparison.OrdinalIgnoreCase));
}

internal sealed class FakeLauncher : IUrlLauncher
{
    public List<(Browser Browser, Uri Url)> Launches { get; } = [];

    public void Launch(Browser browser, Uri url)
    {
        Launches.Add((browser, url));
    }
}

internal sealed class FakeLogger : ILogger
{
    public List<string> Infos { get; } = [];
    public List<string> Warns { get; } = [];
    public List<string> Errors { get; } = [];

    public void LogInfo(string message) => Infos.Add(message);
    public void LogWarn(string message) => Warns.Add(message);
    public void LogError(string message) => Errors.Add(message);
}

internal sealed class FakeProfileDetector : IBrowserProfileDetector
{
    public Dictionary<string, List<BrowserProfile>> Profiles { get; } = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<BrowserProfile> Detect(string browserId)
    {
        return Profiles.TryGetValue(browserId, out List<BrowserProfile>? list)
            ? list
            : [];
    }
}