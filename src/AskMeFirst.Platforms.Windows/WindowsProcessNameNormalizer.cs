using System.Diagnostics;
using AskMeFirst.Core.Routing;

namespace AskMeFirst.Platforms.Windows;

public sealed class WindowsProcessNameNormalizer : IProcessNameNormalizer
{
    private static readonly Dictionary<string, string> Known = new(StringComparer.OrdinalIgnoreCase)
    {
        ["slack"] = "slack",
        ["teams"] = "teams",
        ["code"] = "code",
        ["devenv"] = "visualstudio",
        ["outlook"] = "outlook",
        ["chrome"] = "chrome",
        ["msedge"] = "edge",
        ["firefox"] = "firefox",
        ["brave"] = "brave",
        ["discord"] = "discord",
        ["telegram"] = "telegram",
        ["signal"] = "signal",
        ["thunderbird"] = "thunderbird",
        ["notepad"] = "notepad",
        ["notepad++"] = "notepadpp",
        ["explorer"] = "explorer",
        ["powershell"] = "powershell",
        ["pwsh"] = "powershell",
        ["cmd"] = "cmd",
        ["windowsterminal"] = "terminal",
        ["wt"] = "terminal",
    };

    public string Normalize(string rawName, string? bundleId = null, string? executablePath = null)
    {
        string stripped = rawName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
            ? rawName[..^4]
            : rawName;
        string lower = stripped.ToLowerInvariant();
        if (Known.TryGetValue(lower, out string? canonical))
        {
            return canonical;
        }
        return lower;
    }
}