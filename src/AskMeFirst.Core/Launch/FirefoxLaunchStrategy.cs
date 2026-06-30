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

        string absolutePath = Path.IsPathRooted(profile.DirectoryName)
            ? profile.DirectoryName
            : Path.Combine(FirefoxProfilesRoot.Get(), profile.DirectoryName);

        return ["-profile", absolutePath, url.ToString()];
    }
}