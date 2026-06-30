using System.Text.RegularExpressions;
using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Models;
using AskMeFirst.Core.Profiles;

namespace AskMeFirst.Platforms.Windows;

public sealed partial class WindowsBrowserProfileDetector : IBrowserProfileDetector
{
    private static readonly Dictionary<string, string> ProfileRoots =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["chrome"] = @"Google\Chrome\User Data",
            ["edge"] = @"Microsoft\Edge\User Data",
        };

    public IReadOnlyList<BrowserProfile> Detect(string browserId)
    {
        if (!OperatingSystem.IsWindows())
        {
            return [];
        }

        if (ProfileRoots.TryGetValue(browserId, out string? relativeRoot))
        {
            string? localAppData = Environment.GetEnvironmentVariable("LOCALAPPDATA");
            if (string.IsNullOrWhiteSpace(localAppData))
            {
                return [];
            }
            return DetectChromiumProfiles(Path.Combine(localAppData, relativeRoot));
        }

        if (string.Equals(browserId, "firefox", StringComparison.OrdinalIgnoreCase))
        {
            string? appData = Environment.GetEnvironmentVariable("APPDATA");
            if (string.IsNullOrWhiteSpace(appData))
            {
                return [];
            }
            string profilesIni = Path.Combine(appData, @"Mozilla\Firefox\profiles.ini");
            string groupsRoot = Path.Combine(appData, @"Mozilla\Firefox\Profile Groups");
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

        IReadOnlyDictionary<string, string> nameByDir = ChromiumProfileNames.Read(root);

        List<BrowserProfile> profiles = [];
        foreach (string dir in Directory.EnumerateDirectories(root))
        {
            string dirName = Path.GetFileName(dir);
            if (IsChromiumProfileDir(dirName))
            {
                string name = nameByDir.TryGetValue(dirName, out string? display) ? display : dirName;
                profiles.Add(new BrowserProfile(
                    Name: name,
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
        return name == "Default" || ChromiumProfileIndexRegex().IsMatch(name);
    }

    [GeneratedRegex(@"^Profile\s+\d+$")]
    private static partial Regex ChromiumProfileIndexRegex();
}