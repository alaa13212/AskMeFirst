using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Models;
using AskMeFirst.Core.Profiles;

namespace AskMeFirst.Platforms.Linux;

public sealed class LinuxBrowserProfileDetector : IBrowserProfileDetector
{
    private static readonly Dictionary<string, string> ProfileRoots =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["chrome"] = "google-chrome",
            ["chromium"] = "chromium",
            ["edge"] = "microsoft-edge",
            ["brave"] = "BraveSoftware/Brave-Browser",
        };

    public IReadOnlyList<BrowserProfile> Detect(string browserId)
    {
        if (!OperatingSystem.IsLinux() && !OperatingSystem.IsFreeBSD())
        {
            return [];
        }

        if (ProfileRoots.TryGetValue(browserId, out string? relativeRoot))
        {
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return DetectChromiumProfiles(
                Path.Combine(home, ".config", relativeRoot));
        }

        if (string.Equals(browserId, "firefox", StringComparison.OrdinalIgnoreCase))
        {
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string firefoxDir = Path.Combine(home, ".mozilla", "firefox");
            string profilesIni = Path.Combine(firefoxDir, "profiles.ini");
            string groupsRoot = Path.Combine(firefoxDir, "Profile Groups");
            return FirefoxProfilesParser.Parse(profilesIni, groupsRoot);
        }

        return [];
    }

    private static List<BrowserProfile> DetectChromiumProfiles(string root)
    {
        if (!Directory.Exists(root))
        {
            return [];
        }

        List<BrowserProfile> profiles = [];
        foreach (string dir in Directory.EnumerateDirectories(root))
        {
            string dirName = Path.GetFileName(dir);
            if (IsChromiumProfileDir(dirName))
            {
                profiles.Add(new BrowserProfile(
                    Name: dirName,
                    DirectoryName: dirName,
                    IsDefault: dirName == "Default"));
            }
        }

        if (profiles.Count == 0)
        {
            return profiles;
        }

        return profiles
            .OrderBy(p => p.IsDefault ? 0 : 1)
            .ThenBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool IsChromiumProfileDir(string name)
    {
        return name == "Default" || name.StartsWith("Profile ", StringComparison.Ordinal);
    }
}