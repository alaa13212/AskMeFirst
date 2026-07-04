using AskMeFirst.Core.Abstractions;

namespace AskMeFirst.Core.Launch;

public static class BrowserLaunchStrategies
{
    private static readonly HashSet<string> ChromiumIds =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "chrome", "chromium", "edge", "brave", "opera", "opera-gx", "vivaldi", "arc",
        };

    public static IBrowserLaunchStrategy For(string browserId)
    {
        if (string.Equals(browserId, "firefox", StringComparison.OrdinalIgnoreCase))
        {
            return FirefoxLaunchStrategy.Instance;
        }
        if (ChromiumIds.Contains(browserId))
        {
            return ChromiumLaunchStrategy.Instance;
        }
        return DefaultLaunchStrategy.Instance;
    }
}