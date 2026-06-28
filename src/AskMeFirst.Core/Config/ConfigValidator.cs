using AskMeFirst.Core.Abstractions;

namespace AskMeFirst.Core.Config;

public static class ConfigValidator
{
    public static bool Validate(AppConfig config, ILogger logger)
    {
        List<string> errors = [];
        ValidateUniqueProfileIds(config, errors);
        ValidateProfileIdReferences(config, errors);
        ValidateBrowserProfileIdReferences(config, errors);

        foreach (string error in errors)
        {
            logger.LogError($"config: {error}");
        }
        return errors.Count == 0;
    }

    private static void ValidateUniqueProfileIds(AppConfig config, List<string> errors)
    {
        HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);
        HashSet<string> dupes = new(StringComparer.OrdinalIgnoreCase);
        foreach (ProfileSpec profile in config.Profiles ?? [])
        {
            if (!seen.Add(profile.Id))
            {
                dupes.Add(profile.Id);
            }
        }
        foreach (string dupe in dupes)
        {
            errors.Add($"profile id '{dupe}' is declared more than once");
        }
    }

    private static void ValidateProfileIdReferences(AppConfig config, List<string> errors)
    {
        HashSet<string> declared = new(
            (config.Profiles ?? []).Select(p => p.Id),
            StringComparer.OrdinalIgnoreCase);
        foreach (Rule rule in config.Rules ?? [])
        {
            string? profileId = rule.Then?.ProfileId;
            if (profileId is not null && !declared.Contains(profileId))
            {
                string ruleName = rule.Name ?? "(unnamed)";
                errors.Add(
                    $"rule '{ruleName}' references undeclared profileId '{profileId}' " +
                    $"(add it to the profiles section)");
            }
        }
    }

    private static void ValidateBrowserProfileIdReferences(AppConfig config, List<string> errors)
    {
        HashSet<string> declared = new(
            (config.Profiles ?? []).Select(p => p.Id),
            StringComparer.OrdinalIgnoreCase);
        foreach (BrowserSpec browser in config.Browsers ?? [])
        {
            string? profileId = browser.ProfileId;
            if (profileId is not null && !declared.Contains(profileId))
            {
                errors.Add(
                    $"browser '{browser.Id}' references undeclared profileId '{profileId}' " +
                    $"(add it to the profiles section)");
            }
        }
    }
}