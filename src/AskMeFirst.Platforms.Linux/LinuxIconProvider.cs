using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Data;
using AskMeFirst.Core.Models;

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

        string[] candidates = browserId?.ToLowerInvariant() switch
        {
            "chrome" or "edge" or "brave" or "chromium" => ["Google Profile Picture.png", "Profile Picture.png", "avatar.png"],
            "firefox" => ["avatar.png", "Profile.png"],
            _ => [],
        };

        foreach (string root in ProfileRoots(browserId))
        {
            if (!Directory.Exists(root))
            {
                continue;
            }
            string? profileDir = ResolveProfileDir(root, browserId, profile);
            if (profileDir is null)
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

    private static IEnumerable<string> ProfileRoots(string? browserId)
    {
        string? home = Environment.GetEnvironmentVariable("HOME");
        if (string.IsNullOrEmpty(home))
        {
            yield break;
        }
        if (string.Equals(browserId, "firefox", StringComparison.OrdinalIgnoreCase))
        {
            yield return Path.Combine(home, ".mozilla", "firefox", "Profiles");
            yield return Path.Combine(home, ".mozilla", "firefox", "Profile Groups");
        }
        else
        {
            yield return Path.Combine(home, ".config", browserId ?? "", "Profiles");
        }
    }

    private static string? ResolveProfileDir(string root, string? browserId, BrowserProfile profile)
    {
        if (Path.IsPathRooted(profile.DirectoryName) && Directory.Exists(profile.DirectoryName))
        {
            return profile.DirectoryName;
        }
        string tail = profile.DirectoryName.Contains('/')
            ? profile.DirectoryName[(profile.DirectoryName.LastIndexOf('/') + 1)..]
            : profile.DirectoryName;
        string candidate = Path.Combine(root, tail);
        return Directory.Exists(candidate) ? candidate : null;
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
