using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Models;

namespace AskMeFirst.Core.Launch;

public sealed class ChromiumLaunchStrategy : IBrowserLaunchStrategy
{
    public static readonly ChromiumLaunchStrategy Instance = new();

    public string[] BuildArguments(Uri url, BrowserProfile? profile)
    {
        if (profile is null)
        {
            return [url.ToString()];
        }

        return [url.ToString(), $"--profile-directory={profile.DirectoryName}"];
    }
}