using System.Text.RegularExpressions;
using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Models;

namespace AskMeFirst.Platforms.Linux;

public sealed class LinuxBrowserInventory : IBrowserInventory
{
    private static readonly string[] DesktopDirs =
    [
        "/usr/share/applications",
        "/usr/local/share/applications",
        "/var/lib/flatpak/exports/share/applications",
    ];

    private static readonly Regex CategoryBrowser = new(
        @"^Categories=.*\b(?:WebBrowser|WebBrowser\.)\b",
        RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly Regex NameLine = new(
        @"^Name=(.+)$",
        RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly Regex ExecLine = new(
        @"^Exec=(.+)$",
        RegexOptions.Compiled | RegexOptions.Multiline);

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
                if (browser is not null)
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

        if (!CategoryBrowser.IsMatch(content))
        {
            return null;
        }

        Match nameMatch = NameLine.Match(content);
        Match execMatch = ExecLine.Match(content);
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
        return new Browser(id, displayName, executable);
    }

    private static string? ResolveExecutable(string exec)
    {
        string stripped = Regex.Replace(exec, @"%[a-zA-Z]", "");
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

    private static string NormalizeId(string displayName, string path)
    {
        string lowered = displayName.ToLowerInvariant();
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
        return Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
    }
}
