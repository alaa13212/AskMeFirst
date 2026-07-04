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
            ["opera"] = "opera",
            ["opera-gx"] = "opera-gx",
            ["vivaldi"] = "vivaldi",
        };

    public IReadOnlyList<BrowserProfile> Detect(Browser browser)
    {
        if (!OperatingSystem.IsLinux() && !OperatingSystem.IsFreeBSD())
        {
            return [];
        }

        string browserId = browser.Id;
        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        if (ProfileRoots.TryGetValue(browserId, out string? relativeRoot))
        {
            string? snapName = ResolveSnapName(browser.ExecutablePath);
            foreach (string root in EnumerateChromiumRoots(home, browser.FlatpakAppId, snapName, relativeRoot))
            {
                List<BrowserProfile> profiles = DetectChromiumProfiles(root);
                if (profiles.Count > 0)
                {
                    return profiles;
                }
            }
            return [];
        }

        if (string.Equals(browserId, "firefox", StringComparison.OrdinalIgnoreCase))
        {
            foreach (string firefoxDir in EnumerateFirefoxRoots(home))
            {
                string profilesIni = Path.Combine(firefoxDir, "profiles.ini");
                if (!File.Exists(profilesIni))
                {
                    continue;
                }
                string groupsRoot = Path.Combine(firefoxDir, "Profile Groups");
                List<BrowserProfile> profiles = [.. FirefoxProfilesParser.Parse(profilesIni, groupsRoot)];
                if (profiles.Count > 0)
                {
                    return profiles;
                }
            }
            return [];
        }

        return [];
    }

    private static IEnumerable<string> EnumerateChromiumRoots(string home, string? flatpakAppId, string? snapName, string relativeRoot)
    {
        if (flatpakAppId is not null)
        {
            string systemFlatpak = Path.Combine("/var/lib/flatpak/app", flatpakAppId);
            if (Directory.Exists(systemFlatpak))
            {
                yield return Path.Combine(home, ".var", "app", flatpakAppId, "config", relativeRoot);
            }

            string userFlatpak = Path.Combine(home, ".local", "share", "flatpak", "app", flatpakAppId);
            if (Directory.Exists(userFlatpak))
            {
                yield return Path.Combine(home, ".var", "app", flatpakAppId, "config", relativeRoot);
            }
        }

        if (snapName is not null)
        {
            yield return Path.Combine(home, "snap", snapName, "common", ".config", relativeRoot);
            yield return Path.Combine(home, "snap", snapName, "current", ".config", relativeRoot);
        }

        yield return Path.Combine(home, ".config", relativeRoot);
    }

    private static string? ResolveSnapName(string executablePath)
    {
        if (string.IsNullOrEmpty(executablePath))
        {
            return null;
        }

        string snapBinPrefix = "/snap/bin/";
        if (executablePath.StartsWith(snapBinPrefix, StringComparison.Ordinal)
            && executablePath.Length > snapBinPrefix.Length)
        {
            string remainder = executablePath[snapBinPrefix.Length..];
            int slash = remainder.IndexOf('/');
            return slash > 0 ? remainder[..slash] : remainder;
        }

        const string snapPrefix = "/snap/";
        if (executablePath.StartsWith(snapPrefix, StringComparison.Ordinal)
            && executablePath.Length > snapPrefix.Length)
        {
            string remainder = executablePath[snapPrefix.Length..];
            int slash = remainder.IndexOf('/');
            return slash > 0 ? remainder[..slash] : null;
        }

        return null;
    }

    private static IEnumerable<string> EnumerateFirefoxRoots(string home)
    {
        yield return Path.Combine(home, ".mozilla", "firefox");
        yield return Path.Combine(home, ".config", "mozilla", "firefox");
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
        return name == "Default" || name.StartsWith("Profile ", StringComparison.Ordinal);
    }
}