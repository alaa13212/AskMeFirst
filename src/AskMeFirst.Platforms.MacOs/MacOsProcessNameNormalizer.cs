using AskMeFirst.Core.Routing;

namespace AskMeFirst.Platforms.MacOs;

public sealed class MacOsProcessNameNormalizer : IProcessNameNormalizer
{
    private static readonly Dictionary<string, string> BundleIdMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["com.tinyspeck.chatlyo"] = "slack",
        ["com.microsoft.teams"] = "teams",
        ["com.microsoft.VSCode"] = "code",
        ["com.microsoft.Outlook"] = "outlook",
        ["com.google.Chrome"] = "chrome",
        ["com.google.Chrome.canary"] = "chrome",
        ["org.mozilla.firefox"] = "firefox",
        ["com.brave.Browser"] = "brave",
        ["com.brave.Browser.beta"] = "brave",
        ["com.operasoftware.Opera"] = "opera",
        ["com.microsoft.edgemac"] = "edge",
        ["com.apple.mail"] = "mail",
        ["com.apple.Safari"] = "safari",
        ["org.thunderbird.Thunderbird"] = "thunderbird",
        ["com.hnc.Discord"] = "discord",
        ["com.tdesktop.Telegram"] = "telegram",
        ["org.whispersystems.signal-desktop"] = "signal",
        ["com.apple.Terminal"] = "terminal",
        ["com.googlecode.iterm2"] = "terminal",
        ["io.alacritty"] = "terminal",
        ["net.kovidgoyal.kitty"] = "terminal",
        ["com.sublimetext.4"] = "sublime",
        ["com.jetbrains.intellij"] = "intellij",
    };

    private static readonly Dictionary<string, string> ProcessNameMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Slack"] = "slack",
        ["Microsoft Teams"] = "teams",
        ["Code"] = "code",
        ["Microsoft Outlook"] = "outlook",
        ["Google Chrome"] = "chrome",
        ["Firefox"] = "firefox",
        ["Brave Browser"] = "brave",
        ["Opera"] = "opera",
        ["Microsoft Edge"] = "edge",
        ["Mail"] = "mail",
        ["Safari"] = "safari",
        ["Thunderbird"] = "thunderbird",
        ["Discord"] = "discord",
        ["Telegram"] = "telegram",
        ["Signal"] = "signal",
        ["Terminal"] = "terminal",
        ["iTerm"] = "terminal",
        ["Alacritty"] = "terminal",
        ["kitty"] = "terminal",
    };

    public string Normalize(string rawName, string? bundleId = null, string? executablePath = null)
    {
        if (bundleId is not null && BundleIdMap.TryGetValue(bundleId, out string? fromBundle))
        {
            return fromBundle;
        }
        if (ProcessNameMap.TryGetValue(rawName, out string? fromProcess))
        {
            return fromProcess;
        }
        return rawName.ToLowerInvariant();
    }
}