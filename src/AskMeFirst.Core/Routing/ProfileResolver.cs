using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Config;
using AskMeFirst.Core.Models;

namespace AskMeFirst.Core.Routing;

public sealed class ProfileResolver
{
    private readonly IBrowserProfileDetector detector;
    private readonly IReadOnlyList<ProfileSpec> profileSpecs;
    private readonly ILogger logger;

    public ProfileResolver(
        IBrowserProfileDetector detector,
        IReadOnlyList<ProfileSpec> profileSpecs,
        ILogger logger)
    {
        this.detector = detector;
        this.profileSpecs = profileSpecs;
        this.logger = logger;
    }

    public Browser Resolve(Browser browser, string? profileId)
    {
        if (profileId is null)
        {
            return DefaultProfile(browser);
        }

        ProfileSpec? spec = FindSpec(profileId);
        if (spec is null)
        {
            logger.LogError(
                $"Profile '{profileId}' is not declared in the profiles section of config.");
            return DefaultProfile(browser);
        }

        if (!string.Equals(spec.BrowserId, browser.Id, StringComparison.OrdinalIgnoreCase))
        {
            logger.LogError(
                $"Profile '{profileId}' is declared for browser '{spec.BrowserId}' " +
                $"but rule selected browser '{browser.Id}'.");
            return DefaultProfile(browser);
        }

        IReadOnlyList<BrowserProfile> detected = detector.Detect(browser.Id);
        BrowserProfile? match = detected.FirstOrDefault(p =>
            (spec.Directory is not null
                && string.Equals(p.DirectoryName, spec.Directory, StringComparison.OrdinalIgnoreCase))
            || (spec.Name is not null
                && string.Equals(p.Name, spec.Name, StringComparison.OrdinalIgnoreCase)));

        if (match is null)
        {
            logger.LogWarn(
                $"Profile '{profileId}' declared but not detected on disk for {browser.DisplayName}. " +
                $"Falling back to default.");
            return DefaultProfile(browser);
        }

        return browser with { Profile = match };
    }

    private ProfileSpec? FindSpec(string id)
    {
        foreach (ProfileSpec spec in profileSpecs)
        {
            if (string.Equals(spec.Id, id, StringComparison.OrdinalIgnoreCase))
            {
                return spec;
            }
        }
        return null;
    }

    private Browser DefaultProfile(Browser browser)
    {
        IReadOnlyList<BrowserProfile> detected = detector.Detect(browser.Id);
        if (detected.Count == 0)
        {
            return browser;
        }
        BrowserProfile defaultProfile = detected.FirstOrDefault(p => p.IsDefault) ?? detected[0];
        return browser with { Profile = defaultProfile };
    }
}