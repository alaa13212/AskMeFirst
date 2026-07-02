using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Data;
using AskMeFirst.Core.Models;

namespace AskMeFirst.Platforms.MacOs;

public sealed class MacIconProvider : IIconProvider
{
    private static readonly string[] AppRoots =
    [
        "/Applications",
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Applications"),
    ];

    public byte[]? GetBrowserIconPng(string browserId, string executablePath, string? iconName = null)
    {
        if (string.IsNullOrEmpty(browserId))
        {
            return null;
        }

        string? appPath = LocateAppFor(browserId);
        if (appPath is null)
        {
            return null;
        }

        string resources = Path.Combine(appPath, "Contents", "Resources");
        if (!Directory.Exists(resources))
        {
            return null;
        }

        foreach (string name in new[] { browserId, browserId.ToLowerInvariant(), "icon" })
        {
            foreach (string ext in new[] { ".png", ".icns" })
            {
                string candidate = Path.Combine(resources, name + ext);
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

        string? home = Environment.GetEnvironmentVariable("HOME");
        if (string.IsNullOrEmpty(home))
        {
            return null;
        }

        string root = string.Equals(browserId, "firefox", StringComparison.OrdinalIgnoreCase)
            ? Path.Combine(home, "Library", "Application Support", "Firefox", "Profiles")
            : Path.Combine(home, "Library", "Application Support", browserId ?? "", "Profiles");

        if (!Directory.Exists(root))
        {
            return null;
        }

        foreach (string name in candidates)
        {
            string full = Path.Combine(root, profile.DirectoryName, name);
            if (TryReadPng(full, out byte[] bytes))
            {
                return bytes;
            }
        }
        return null;
    }

    private static string? LocateAppFor(string browserId)
    {
        foreach (string root in AppRoots)
        {
            if (!Directory.Exists(root))
            {
                continue;
            }
            foreach (string dir in Directory.EnumerateDirectories(root))
            {
                if (Path.GetFileName(dir).Contains(browserId, StringComparison.OrdinalIgnoreCase))
                {
                    return dir;
                }
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
