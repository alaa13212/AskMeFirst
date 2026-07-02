using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Models;

namespace AskMeFirst.Core.Launch;

public sealed class ChromiumLaunchStrategy : IBrowserLaunchStrategy
{
    public static readonly ChromiumLaunchStrategy Instance = new();

    public string[] BuildArguments(Uri url, BrowserProfile? profile, bool newWindow = false)
    {
        List<string> args = [];
        if (newWindow)
        {
            args.Add("--new-window");
        }
        args.Add(url.ToString());
        if (profile is not null)
        {
            args.Add($"--profile-directory={profile.DirectoryName}");
        }
        return [.. args];
    }
}