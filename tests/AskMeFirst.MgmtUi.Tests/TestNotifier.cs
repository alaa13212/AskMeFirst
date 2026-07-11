using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Models;
using AskMeFirst.MgmtUi.Services;

namespace AskMeFirst.MgmtUi.Tests;

internal sealed class TestNotifier : INotifier
{
    public List<(string Title, string Message)> Calls { get; } = [];

    public void Show(string title, string message)
    {
        Calls.Add((title, message));
    }
}

internal sealed class NoOpUrlLauncher : IUrlLauncher
{
    public static readonly NoOpUrlLauncher Instance = new();

    public List<(Browser Browser, Uri Url)> Launches { get; } = [];

    private NoOpUrlLauncher()
    {
    }

    public void Launch(Browser browser, Uri url)
    {
        Launches.Add((browser, url));
    }
}
