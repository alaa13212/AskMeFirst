using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Config;
using AskMeFirst.Core.Models;

namespace AskMeFirst.Core;

public sealed class UrlRouter(
    IBrowserInventory inventory,
    IUrlLauncher launcher,
    IBrowserProfileDetector profiles,
    ILogger logger,
    AppConfig appConfig)
{
    public int Route(Uri url, string? browserId, string? profileName)
    {
        IReadOnlyList<Browser> browsers = inventory.Discover();
        if (browsers.Count == 0)
        {
            logger.LogError("No browsers discovered on this system.");
            return 2;
        }

        Browser? chosen = ResolveBrowser(browserId, browsers);
        if (chosen is null)
        {
            logger.LogError(
                $"Browser '{browserId}' not found. " +
                $"Discovered: {string.Join(", ", browsers.Select(b => b.Id))}");
            return 3;
        }

        Browser resolved = ResolveProfile(chosen, profileName);
        logger.LogInfo($"Routing {url} → {resolved.DisplayName} ({resolved.ExecutablePath})"
            + (resolved.Profile is null ? "" : $" [profile: {resolved.Profile.Name}]"));
        launcher.Launch(resolved, url);
        return 0;
    }

    private Browser? ResolveBrowser(string? browserId, IReadOnlyList<Browser> browsers)
    {
        if (browserId is null or "system")
        {
            string? configured = appConfig.Settings.DefaultBrowserId;
            if (!string.IsNullOrWhiteSpace(configured) && configured != "system")
            {
                Browser? match = inventory.FindById(configured);
                if (match is not null)
                {
                    logger.LogInfo($"No --browser specified; using configured default '{configured}'.");
                    return match;
                }
                logger.LogWarn(
                    $"Configured default browser '{configured}' not found; falling back to first discovered.");
            }
            else
            {
                logger.LogInfo("No --browser specified; using first discovered browser.");
            }
            return browsers[0];
        }
        return inventory.FindById(browserId);
    }

    private Browser ResolveProfile(Browser browser, string? profileName)
    {
        IReadOnlyList<BrowserProfile> detected = profiles.Detect(browser.Id);
        if (detected.Count == 0)
        {
            if (profileName is null)
            {
                return browser;
            }
            BrowserProfile synthetic = new(profileName, profileName, IsDefault: false);
            return browser with { Profile = synthetic };
        }

        if (profileName is null)
        {
            BrowserProfile defaultProfile = detected.FirstOrDefault(p => p.IsDefault) ?? detected[0];
            return browser with { Profile = defaultProfile };
        }

        BrowserProfile? match = detected.FirstOrDefault(p =>
            string.Equals(p.Name, profileName, StringComparison.OrdinalIgnoreCase)
            || string.Equals(p.DirectoryName, profileName, StringComparison.OrdinalIgnoreCase));
        if (match is null)
        {
            logger.LogWarn(
                $"Profile '{profileName}' not found for {browser.DisplayName}. " +
                $"Available: {string.Join(", ", detected.Select(p => p.Name))}");
            BrowserProfile fallback = detected.FirstOrDefault(p => p.IsDefault) ?? detected[0];
            return browser with { Profile = fallback };
        }
        return browser with { Profile = match };
    }
}