using AskMeFirst.Core.Routing;

namespace AskMeFirst.Platforms.Linux;

public sealed class LinuxProcessNameNormalizer : IProcessNameNormalizer
{
    private static readonly Dictionary<string, string> Known = new(StringComparer.OrdinalIgnoreCase)
    {
        ["slack"] = "slack",
        ["teams"] = "teams",
        ["code"] = "code",
        ["thunderbird"] = "thunderbird",
        ["chrome"] = "chrome",
        ["google-chrome"] = "chrome",
        ["chromium"] = "chromium",
        ["firefox"] = "firefox",
        ["brave-browser"] = "brave",
        ["discord"] = "discord",
        ["telegram-desktop"] = "telegram",
        ["signal-desktop"] = "signal",
        ["nautilus"] = "files",
        ["dolphin"] = "files",
        ["thunar"] = "files",
        ["gedit"] = "editor",
        ["kate"] = "editor",
        ["vim"] = "vim",
        ["nvim"] = "neovim",
        ["wezterm"] = "terminal",
        ["alacritty"] = "terminal",
        ["kitty"] = "terminal",
        ["gnome-terminal"] = "terminal",
        ["konsole"] = "terminal",
    };

    public string Normalize(string rawName, string? bundleId = null, string? executablePath = null)
    {
        string baseName = Path.GetFileName(rawName);
        string lower = baseName.ToLowerInvariant();
        if (Known.TryGetValue(lower, out string? canonical))
        {
            return canonical;
        }
        return lower;
    }
}