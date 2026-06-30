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
    ];

    private static readonly string[] CandidateNames =
    [
        "{0}",
        "{0}-browser",
        "{0}-desktop",
    ];

    public byte[]? GetBrowserIconPng(string browserId, string executablePath)
    {
        if (string.IsNullOrEmpty(browserId))
        {
            return null;
        }

        foreach (string root in IconRoots)
        {
            if (!Directory.Exists(root))
            {
                continue;
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
