using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Models;
using AskMeFirst.Core.Profiles;

namespace AskMeFirst.Platforms.MacOs;

public sealed class MacOsBrowserProfileDetector : IBrowserProfileDetector
{
    private static readonly Dictionary<string, string> ProfileRoots =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["chrome"] = "Google/Chrome",
            ["chromium"] = "Chromium",
            ["edge"] = "Microsoft Edge",
            ["brave"] = "BraveSoftware/Brave-Browser",
            ["opera"] = "com.operasoftware.Opera",
            ["opera-gx"] = "com.operasoftware.OperaGX",
            ["vivaldi"] = "Vivaldi",
        };

    public IReadOnlyList<BrowserProfile> Detect(Browser browser)
    {
        if (!OperatingSystem.IsMacOS())
        {
            return [];
        }

        string browserId = browser.Id;

        if (ProfileRoots.TryGetValue(browserId, out string? relativeRoot))
        {
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return DetectChromiumProfiles(
                Path.Combine(home, "Library", "Application Support", relativeRoot));
        }

        if (string.Equals(browserId, "firefox", StringComparison.OrdinalIgnoreCase))
        {
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return FirefoxProfilesParser.Parse(
                Path.Combine(home, "Library", "Application Support", "Firefox", "profiles.ini"));
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