using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Data;
using AskMeFirst.Core.Models;
using AskMeFirst.Core.Profiles;

namespace AskMeFirst.Platforms.Linux;

public sealed class LinuxIconProvider : IIconProvider
{
    private static readonly string[] IconRoots =
    [
        "/usr/share/pixmaps",
        "/usr/share/icons/hicolor/48x48/apps",
        "/usr/share/icons/hicolor/64x64/apps",
        "/usr/share/icons/hicolor/128x128/apps",
        "/usr/share/icons/hicolor/256x256/apps",
        "/var/lib/flatpak/exports/share/icons/hicolor/48x48/apps",
        "/var/lib/flatpak/exports/share/icons/hicolor/64x64/apps",
        "/var/lib/flatpak/exports/share/icons/hicolor/128x128/apps",
        "/var/lib/flatpak/exports/share/icons/hicolor/256x256/apps",
    ];

    private static readonly string[] CandidateNames =
    [
        "{0}",
        "{0}-browser",
        "{0}-desktop",
    ];

    private static readonly string[] FlatpakIconSizes = ["48x48", "64x64", "128x128"];

    public byte[]? GetBrowserIconPng(string browserId, string executablePath, string? iconName = null)
    {
        if (string.IsNullOrEmpty(browserId))
        {
            return null;
        }

        if (!string.IsNullOrEmpty(iconName) && Path.IsPathRooted(iconName))
        {
            if (TryReadPng(iconName, out byte[] directBytes))
            {
                return directBytes;
            }
        }

        foreach (string root in IconRoots)
        {
            if (!Directory.Exists(root))
            {
                continue;
            }
            if (!string.IsNullOrEmpty(iconName) && !Path.IsPathRooted(iconName))
            {
                string candidate = Path.Combine(root, iconName + ".png");
                if (TryReadPng(candidate, out byte[] bytes))
                {
                    return bytes;
                }
            }
            foreach (string name in CandidateNames)
            {
                string candidate = Path.Combine(root, string.Format(System.Globalization.CultureInfo.InvariantCulture, name, browserId) + ".png");
                if (TryReadPng(candidate, out byte[] bytes))
                {
                    return bytes;
                }
            }
        }

        if (!string.IsNullOrEmpty(iconName) && !Path.IsPathRooted(iconName))
        {
            byte[]? result = TryFlatpakAppInfoIcon(iconName);
            if (result is not null)
            {
                return result;
            }
        }

        return null;
    }

    private static byte[]? TryFlatpakAppInfoIcon(string iconName)
    {
        string? home = Environment.GetEnvironmentVariable("HOME");
        string[] appRoots = ["/var/lib/flatpak/app", $"{home}/.local/share/flatpak/app"];

        foreach (string appRoot in appRoots)
        {
            if (string.IsNullOrEmpty(appRoot) || !Directory.Exists(appRoot))
            {
                continue;
            }

            string appDir = Path.Combine(appRoot, iconName, "x86_64", "stable", "active", "files", "share", "app-info", "icons", "flatpak");
            if (!Directory.Exists(appDir))
            {
                continue;
            }

            foreach (string size in FlatpakIconSizes)
            {
                string candidate = Path.Combine(appDir, size, iconName + ".png");
                if (TryReadPng(candidate, out byte[] bytes))
                {
                    return bytes;
                }
            }
        }

        return null;
    }

    public byte[]? GetProfileIconPng(string browserId, BrowserProfile profile)
    {
        if (string.IsNullOrEmpty(profile.DirectoryName))
        {
            return null;
        }

        BrowserProfileKind kind = ClassifyProfile(browserId);

        if (kind == BrowserProfileKind.Firefox)
        {
            string? home = Environment.GetEnvironmentVariable("HOME");
            if (!string.IsNullOrEmpty(home))
            {
                string tail = ProfileDirTail(profile);
                foreach (string groupsRoot in new[] {
                    Path.Combine(home, ".mozilla", "firefox", "Profile Groups"),
                    Path.Combine(home, ".config", "mozilla", "firefox", "Profile Groups"),
                })
                {
                    byte[]? avatar = FirefoxProfileAvatarReader.ReadAvatarPng(groupsRoot, tail);
                    if (avatar is not null)
                    {
                        return avatar;
                    }
                }
            }
        }

        string[] candidates = kind switch
        {
            BrowserProfileKind.Chromium => ["Google Profile Picture.png", "Profile Picture.png", "avatar.png"],
            BrowserProfileKind.Firefox => ["avatar.png", "Profile.png", "Profile Picture.png"],
            _ => [],
        };
        if (candidates.Length == 0)
        {
            return null;
        }

        foreach (string profileDir in ResolveProfileDirs(browserId, profile))
        {
            if (!Directory.Exists(profileDir))
            {
                continue;
            }
            foreach (string name in candidates)
            {
                string full = Path.Combine(profileDir, name);
                if (TryReadPng(full, out byte[] bytes))
                {
                    return bytes;
                }
            }
        }
        return null;
    }

    private static string ProfileDirTail(BrowserProfile profile)
    {
        return profile.DirectoryName.Contains('/')
            ? profile.DirectoryName[(profile.DirectoryName.LastIndexOf('/') + 1)..]
            : profile.DirectoryName;
    }

    private enum BrowserProfileKind { None, Chromium, Firefox }

    private static BrowserProfileKind ClassifyProfile(string? browserId)
    {
        if (string.IsNullOrEmpty(browserId))
        {
            return BrowserProfileKind.None;
        }
        if (string.Equals(browserId, "firefox", StringComparison.OrdinalIgnoreCase))
        {
            return BrowserProfileKind.Firefox;
        }
        return browserId.ToLowerInvariant() switch
        {
            "chrome" or "chromium" or "edge" or "brave" or "opera" or "opera-gx" or "vivaldi"
                => BrowserProfileKind.Chromium,
            _ => BrowserProfileKind.None,
        };
    }

    private static IEnumerable<string> ResolveProfileDirs(string? browserId, BrowserProfile profile)
    {
        if (Path.IsPathRooted(profile.DirectoryName) && Directory.Exists(profile.DirectoryName))
        {
            yield return profile.DirectoryName;
        }

        string? home = Environment.GetEnvironmentVariable("HOME");
        if (string.IsNullOrEmpty(home))
        {
            yield break;
        }

        string tail = profile.DirectoryName.Contains('/')
            ? profile.DirectoryName[(profile.DirectoryName.LastIndexOf('/') + 1)..]
            : profile.DirectoryName;

        if (string.Equals(browserId, "firefox", StringComparison.OrdinalIgnoreCase))
        {
            foreach (string root in FirefoxProfileRoots(home))
            {
                yield return Path.Combine(root, tail);
                yield return Path.Combine(root, "Profiles", tail);
            }
            yield break;
        }

        string? relativeRoot = ChromiumRelativeRoot(browserId);
        if (relativeRoot is null)
        {
            yield break;
        }

        foreach (string root in ChromiumRoots(home, relativeRoot))
        {
            yield return Path.Combine(root, tail);
        }
    }

    private static IEnumerable<string> FirefoxProfileRoots(string home)
    {
        yield return Path.Combine(home, ".mozilla", "firefox");
        yield return Path.Combine(home, ".config", "mozilla", "firefox");
    }

    private static string? ChromiumRelativeRoot(string? browserId)
    {
        if (string.IsNullOrEmpty(browserId))
        {
            return null;
        }
        return browserId.ToLowerInvariant() switch
        {
            "chrome" => "google-chrome",
            "chromium" => "chromium",
            "edge" => "microsoft-edge",
            "brave" => "BraveSoftware/Brave-Browser",
            "opera" => "opera",
            "opera-gx" => "opera-gx",
            "vivaldi" => "vivaldi",
            _ => null,
        };
    }

    private static IEnumerable<string> ChromiumRoots(string home, string relativeRoot)
    {
        yield return Path.Combine("/var/lib/flatpak/app/.");
        string? flatpakAppId = TryDetectFlatpakAppId(home, relativeRoot);
        if (flatpakAppId is not null)
        {
            yield return Path.Combine(home, ".var", "app", flatpakAppId, "config", relativeRoot);
        }
        yield return Path.Combine(home, ".config", relativeRoot);
        string? snapName = TryDetectSnapName(home, relativeRoot);
        if (snapName is not null)
        {
            yield return Path.Combine(home, "snap", snapName, "common", ".config", relativeRoot);
            yield return Path.Combine(home, "snap", snapName, "current", ".config", relativeRoot);
        }
    }

    private static string? TryDetectFlatpakAppId(string home, string relativeRoot)
    {
        foreach (string appDir in new[] { "/var/lib/flatpak/app", Path.Combine(home, ".local", "share", "flatpak", "app") })
        {
            if (!Directory.Exists(appDir))
            {
                continue;
            }
            foreach (string sub in Directory.EnumerateDirectories(appDir))
            {
                string configDir = Path.Combine(home, ".var", "app", Path.GetFileName(sub), "config", relativeRoot);
                if (Directory.Exists(configDir))
                {
                    return Path.GetFileName(sub);
                }
            }
        }
        return null;
    }

    private static string? TryDetectSnapName(string home, string relativeRoot)
    {
        string snapRoot = Path.Combine(home, "snap");
        if (!Directory.Exists(snapRoot))
        {
            return null;
        }
        foreach (string sub in Directory.EnumerateDirectories(snapRoot))
        {
            string configDir = Path.Combine(snapRoot, Path.GetFileName(sub), "common", ".config", relativeRoot);
            if (Directory.Exists(configDir))
            {
                return Path.GetFileName(sub);
            }
        }
        return null;
    }

    private static bool TryReadPng(string path, out byte[] bytes)
    {
        bytes = [];
        try
        {
            if (!File.Exists(path))
            {
                return false;
            }
            byte[] raw = File.ReadAllBytes(path);
            if (PngSignature.Matches(raw))
            {
                bytes = raw;
                return true;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }
}
