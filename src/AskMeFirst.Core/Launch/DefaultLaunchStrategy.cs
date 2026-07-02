using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Models;

namespace AskMeFirst.Core.Launch;

public sealed class DefaultLaunchStrategy : IBrowserLaunchStrategy
{
    public static readonly DefaultLaunchStrategy Instance = new();

    public string[] BuildArguments(Uri url, BrowserProfile? profile, bool newWindow = false)
    {
        return [url.ToString()];
    }
}