namespace AskMeFirst.MgmtUi.Areas;

using System.Collections.Generic;

public static class AreaCatalog
{
    public static readonly IReadOnlyList<AreaDescriptor> All = new AreaDescriptor[]
    {
        new(AreaId.Browsers,    "Browsers",    "B", "B", "Discovered installs and profiles, pin what matters.", "Browsers"),
        new(AreaId.Profiles,    "Profiles",    "P", "P", "Browser profile listing and visibility.",            "Profiles"),
        new(AreaId.Rules,       "Rules",       "R", "R", "URL patterns that decide where links go.",          "Rules"),
        new(AreaId.Usage,       "Usage",       "U", "U", "Recent picks — the raw log the suggestion engine reads.", "Usage"),
        new(AreaId.Suggestions, "Suggestions", "S", "S", "Learnings from your recent picks.",                  "Suggestions"),
        new(AreaId.Unshortener, "Unshortener", "N", "N", "Resolve short links before the picker decides.",     "Unshortener"),
        new(AreaId.Tracking,    "Tracking",    "T", "T", "Strip utm_*, fbclid, etc. before launch.",            "Tracking"),
        new(AreaId.Cache,       "Cache",       "C", "C", "Persisted browser inventory; refresh after installs.","Cache"),
    };

    public static AreaDescriptor? Find(AreaId id)
    {
        foreach (AreaDescriptor area in All)
        {
            if (area.Id == id)
            {
                return area;
            }
        }
        return null;
    }
}
