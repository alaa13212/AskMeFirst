using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Models;

namespace AskMeFirst.Core.Launch;

public sealed class FlatpakLaunchStrategy : IBrowserLaunchStrategy
{
    private readonly string _appId;
    private readonly IBrowserLaunchStrategy _inner;

    public FlatpakLaunchStrategy(string appId, IBrowserLaunchStrategy inner)
    {
        _appId = appId;
        _inner = inner;
    }

    public string[] BuildArguments(Uri url, BrowserProfile? profile, bool newWindow = false)
    {
        return ["run", _appId, .. _inner.BuildArguments(url, profile, newWindow)];
    }
}
