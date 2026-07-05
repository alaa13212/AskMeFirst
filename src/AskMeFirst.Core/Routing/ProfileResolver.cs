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
        IReadOnlyList<BrowserProfile> detected = detector.Detect(browser);

        if (profileId is null)
        {
            return DefaultProfile(browser, detected);
        }

        ProfileSpec? spec = FindSpec(profileId);
        if (spec is not null)
        {
            if (!string.Equals(spec.BrowserId, browser.Id, StringComparison.OrdinalIgnoreCase))
            {
                logger.LogError(
                    $"Profile '{profileId}' is declared for browser '{spec.BrowserId}' " +
                    $"but rule selected browser '{browser.Id}'.");
                return DefaultProfile(browser, detected);
            }

            BrowserProfile? declaredMatch = detected.FirstOrDefault(p =>
                (spec.Directory is not null
                    && string.Equals(p.DirectoryName, spec.Directory, StringComparison.OrdinalIgnoreCase))
                || (spec.Name is not null
                    && string.Equals(p.Name, spec.Name, StringComparison.OrdinalIgnoreCase)));

            if (declaredMatch is null)
            {
                logger.LogWarn(
                    $"Profile '{profileId}' declared but not detected on disk for {browser.DisplayName}. " +
                    $"Falling back to default.");
                return DefaultProfile(browser, detected);
            }

            return browser with { Profile = declaredMatch };
        }

        BrowserProfile? detectedMatch = detected.FirstOrDefault(p =>
            string.Equals(p.DirectoryName, profileId, StringComparison.OrdinalIgnoreCase)
            || string.Equals(p.Name, profileId, StringComparison.OrdinalIgnoreCase));

        if (detectedMatch is not null)
        {
            return browser with { Profile = detectedMatch };
        }

        if (detected.Count > 0)
        {
            logger.LogWarn(
                $"Profile '{profileId}' not found for {browser.DisplayName}. " +
                $"Available: {string.Join(", ", detected.Select(p => p.Name))}. " +
                $"Falling back to default.");
        }
        else
        {
            logger.LogWarn(
                $"Profile '{profileId}' requested but no profiles are detected for {browser.DisplayName}.");
        }
        return DefaultProfile(browser, detected);
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

    private static Browser DefaultProfile(Browser browser, IReadOnlyList<BrowserProfile> detected)
    {
        if (detected.Count == 0)
        {
            return browser;
        }
        BrowserProfile defaultProfile = detected.FirstOrDefault(p => p.IsDefault) ?? detected[0];
        return browser with { Profile = defaultProfile };
    }
}