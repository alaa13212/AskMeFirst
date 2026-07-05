using System.Text.RegularExpressions;
using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Launch;
using AskMeFirst.Core.Models;
using AskMeFirst.Core.Paths;

namespace AskMeFirst.Platforms.Linux;

public sealed partial class LinuxBrowserInventory : IBrowserInventory
{
    private static readonly string[] DesktopDirs =
    [
        "/usr/share/applications",
        "/usr/local/share/applications",
        "/var/lib/flatpak/exports/share/applications",
        "/var/lib/snapd/desktop/applications",
    ];

    private static readonly Dictionary<string, string> KnownIds =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["chrome"] = "chrome",
            ["chromium"] = "chromium",
            ["google-chrome"] = "chrome",
            ["google-chrome-stable"] = "chrome",
            ["firefox"] = "firefox",
            ["firefox-esr"] = "firefox",
            ["microsoft-edge"] = "edge",
            ["msedge"] = "edge",
            ["brave-browser"] = "brave",
            ["opera"] = "opera",
            ["opera-gx"] = "opera-gx",
            ["com.opera.opera-gx"] = "opera-gx",
            ["vivaldi"] = "vivaldi",
            ["vivaldi-stable"] = "vivaldi",
        };

    public IReadOnlyList<Browser> Discover()
    {
        HashSet<string> userDirs = LoadUserDesktopDirs();
        HashSet<string> allDirs = [.. DesktopDirs, .. userDirs];

        Dictionary<string, Browser> byId = new(StringComparer.OrdinalIgnoreCase);
        foreach (string dir in allDirs)
        {
            if (!Directory.Exists(dir))
            {
                continue;
            }

            foreach (string file in Directory.EnumerateFiles(dir, "*.desktop"))
            {
                Browser? browser = ParseDesktopFile(file);
                if (browser is not null && !SelfExecutable.IsSelf(browser.ExecutablePath))
                {
                    byId[browser.Id] = browser;
                }
            }
        }
        return byId.Values.ToList();
    }

    public Browser? FindById(string id)
    {
        return Discover().FirstOrDefault(b =>
            string.Equals(b.Id, id, StringComparison.OrdinalIgnoreCase));
    }

    private static HashSet<string> LoadUserDesktopDirs()
    {
        HashSet<string> dirs = [];
        string? xdgDataHome = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
        if (!string.IsNullOrWhiteSpace(xdgDataHome))
        {
            string userApps = Path.Combine(xdgDataHome, "applications");
            if (Directory.Exists(userApps))
            {
                dirs.Add(userApps);
            }
        }
        else
        {
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string userApps = Path.Combine(home, ".local", "share", "applications");
            if (Directory.Exists(userApps))
            {
                dirs.Add(userApps);
            }
        }
        return dirs;
    }

    private static Browser? ParseDesktopFile(string path)
    {
        string content;
        try
        {
            content = File.ReadAllText(path);
        }
        catch
        {
            return null;
        }

        if (!CategoryBrowserRegex().IsMatch(content))
        {
            return null;
        }

        if (NoDisplayRegex().IsMatch(content))
        {
            return null;
        }

        Match nameMatch = NameLineRegex().Match(content);
        Match execMatch = ExecLineRegex().Match(content);
        if (!nameMatch.Success || !execMatch.Success)
        {
            return null;
        }

        string displayName = nameMatch.Groups[1].Value.Trim();
        string exec = execMatch.Groups[1].Value.Trim();
        string? executable = ResolveExecutable(exec);
        if (executable is null)
        {
            return null;
        }

        string id = NormalizeId(displayName, path);

        string? iconName = null;
        Match iconMatch = IconLineRegex().Match(content);
        if (iconMatch.Success)
        {
            iconName = iconMatch.Groups[1].Value.Trim();
        }

        IBrowserLaunchStrategy launchStrategy = BrowserLaunchStrategies.For(id);
        string? flatpakAppId = null;
        if (IsFlatpakExecLine(exec, out string? appId) && appId is not null)
        {
            launchStrategy = new FlatpakLaunchStrategy(appId, launchStrategy);
            flatpakAppId = appId;
        }

        return new Browser
        {
            Id = id,
            DisplayName = displayName,
            ExecutablePath = executable,
            IconName = iconName,
            LaunchStrategy = launchStrategy,
            FlatpakAppId = flatpakAppId,
        };
    }

    private static string? ResolveExecutable(string exec)
    {
        string stripped = FieldCodeRegex().Replace(exec, "");
        string[] parts = stripped.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return null;
        }

        string candidate = parts[0];
        if (Path.IsPathRooted(candidate) && File.Exists(candidate))
        {
            return candidate;
        }

        string? pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (pathEnv is null)
        {
            return null;
        }

        foreach (string dir in pathEnv.Split(Path.PathSeparator))
        {
            string fullPath = Path.Combine(dir, candidate);
            if (File.Exists(fullPath))
            {
                return fullPath;
            }
        }
        return null;
    }

    private static bool IsFlatpakExecLine(string exec, out string? appId)
    {
        appId = null;
        string stripped = FieldCodeRegex().Replace(exec, "");
        string[] parts = stripped.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 3)
        {
            return false;
        }

        string binary = Path.GetFileName(parts[0]);
        if (!string.Equals(binary, "flatpak", StringComparison.OrdinalIgnoreCase)
            || !string.Equals(parts[1], "run", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        for (int i = 2; i < parts.Length; i++)
        {
            if (parts[i].StartsWith("--", StringComparison.Ordinal))
            {
                continue;
            }
            if (parts[i] is "@@u" or "@@")
            {
                continue;
            }
            appId = parts[i];
            return true;
        }

        return false;
    }

    private static string NormalizeId(string displayName, string path)
    {
        string desktopId = Path.GetFileNameWithoutExtension(path);
        if (KnownIds.TryGetValue(desktopId, out string? mapped))
        {
            return mapped;
        }

        string lowered = displayName.ToLowerInvariant();
        if (lowered.Contains("opera gx") || lowered.Contains("opera-gx"))
        {
            return "opera-gx";
        }
        if (lowered.Contains("chrome"))
        {
            return "chrome";
        }
        if (lowered.Contains("firefox"))
        {
            return "firefox";
        }
        if (lowered.Contains("edge") || lowered.Contains("msedge"))
        {
            return "edge";
        }
        if (lowered.Contains("brave"))
        {
            return "brave";
        }
        if (lowered.Contains("opera"))
        {
            return "opera";
        }
        if (lowered.Contains("vivaldi"))
        {
            return "vivaldi";
        }
        if (lowered.Contains("chromium"))
        {
            return "chromium";
        }
        return desktopId.ToLowerInvariant();
    }

    [GeneratedRegex(@"^Categories=.*\b(?:WebBrowser|WebBrowser\.)\b", RegexOptions.Multiline)]
    private static partial Regex CategoryBrowserRegex();

    [GeneratedRegex(@"^Name=(.+)$", RegexOptions.Multiline)]
    private static partial Regex NameLineRegex();

    [GeneratedRegex(@"^Exec=(.+)$", RegexOptions.Multiline)]
    private static partial Regex ExecLineRegex();

    [GeneratedRegex(@"^Icon=(.+)$", RegexOptions.Multiline)]
    private static partial Regex IconLineRegex();

    [GeneratedRegex(@"^NoDisplay=true$", RegexOptions.Multiline | RegexOptions.IgnoreCase)]
    private static partial Regex NoDisplayRegex();

    [GeneratedRegex(@"%[a-zA-Z]")]
    private static partial Regex FieldCodeRegex();
}