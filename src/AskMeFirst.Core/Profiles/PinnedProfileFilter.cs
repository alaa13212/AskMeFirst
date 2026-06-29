using AskMeFirst.Core.Config;
using AskMeFirst.Core.Routing;

namespace AskMeFirst.Core.Profiles;

public static class PinnedProfileFilter
{
    public static IReadOnlyList<PickerBrowserOption> Filter(
        IReadOnlyList<PickerBrowserOption> all,
        IReadOnlyList<ProfileSpec> specs)
    {
        List<ProfileSpec> pinned = specs.Where(s => s.Pinned).ToList();
        if (pinned.Count == 0)
        {
            return all;
        }

        return all.Where(opt => MatchesAnyPinnedSpec(opt, pinned)).ToList();
    }

    private static bool MatchesAnyPinnedSpec(PickerBrowserOption opt, List<ProfileSpec> pinned)
    {
        if (opt.Profile is null)
        {
            return true;
        }

        foreach (ProfileSpec spec in pinned)
        {
            if (!string.Equals(spec.BrowserId, opt.Browser.Id, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            if (spec.Name is not null &&
                string.Equals(spec.Name, opt.Profile.Name, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            if (spec.Directory is not null &&
                string.Equals(spec.Directory, opt.Profile.DirectoryName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}