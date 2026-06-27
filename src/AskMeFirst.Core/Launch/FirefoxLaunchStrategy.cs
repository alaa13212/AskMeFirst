using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Models;

namespace AskMeFirst.Core.Launch;

public sealed class FirefoxLaunchStrategy : IBrowserLaunchStrategy
{
    public static readonly FirefoxLaunchStrategy Instance = new();

    public string[] BuildArguments(Uri url, BrowserProfile? profile)
    {
        if (profile is null)
        {
            return [url.ToString()];
        }

        return ["-P", profile.Name, url.ToString()];
    }
}