using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Models;

namespace AskMeFirst.Core.Launch;

public sealed class ChromiumLaunchStrategy : IBrowserLaunchStrategy
{
    public static readonly ChromiumLaunchStrategy Instance = new();

    public string[] BuildArguments(Uri url, BrowserProfile? profile)
    {
        List<string> args = [];
        if (profile is not null)
        {
            args.Add($"--profile-directory={profile.DirectoryName}");
        }
        args.Add(url.ToString());
        return [.. args];
    }
}