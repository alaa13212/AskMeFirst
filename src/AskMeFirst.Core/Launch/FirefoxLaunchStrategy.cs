using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Models;
using AskMeFirst.Core.Profiles;

namespace AskMeFirst.Core.Launch;

public sealed class FirefoxLaunchStrategy : IBrowserLaunchStrategy
{
    public static readonly FirefoxLaunchStrategy Instance = new();

    public string[] BuildArguments(Uri url, BrowserProfile? profile)
    {
        List<string> args = [];
        if (profile is not null)
        {
            args.Add("-profile");
            args.Add(ResolveProfilePath(profile.DirectoryName));
        }
        args.Add(url.ToString());
        return [.. args];
    }

    private static string ResolveProfilePath(string directoryName)
    {
        if (Path.IsPathRooted(directoryName))
        {
            return directoryName;
        }

        string normalized = directoryName.StartsWith("Profiles/", StringComparison.OrdinalIgnoreCase)
            || directoryName.StartsWith("Profiles\\", StringComparison.OrdinalIgnoreCase)
            ? directoryName["Profiles/".Length..]
            : directoryName;

        return Path.Combine(FirefoxProfilesRoot.Get(), normalized);
    }
}