using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Models;
using AskMeFirst.Core.Profiles;

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

        return ["-profile", ResolveProfilePath(profile.DirectoryName), url.ToString()];
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
