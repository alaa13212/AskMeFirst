using AskMeFirst.Core.Config;

namespace AskMeFirst.Core.Routing;

public sealed class TrackingStripper
{
    public static readonly IReadOnlyList<string> BuiltInParams =
    [
        "utm_source", "utm_medium", "utm_campaign", "utm_term", "utm_content",
        "utm_id", "utm_name", "utm_brand", "utm_social", "utm_creative_format",
        "fbclid", "gclid", "gbraid", "wbraid", "msclkid", "dclid",
        "_ga", "_gl", "mc_eid", "mc_cid", "yclid", "ref", "ref_src",
    ];

    private readonly IReadOnlySet<string> trackers;

    public TrackingStripper(AppConfig config)
    {
        trackers = BuildTrackerSet(config);
    }

    public Uri Strip(Uri url) => Strip(url, trackers);

    public static IReadOnlySet<string> BuildTrackerSet(AppConfig config)
    {
        IReadOnlyList<string> extras = config.TrackingParamsExtra ?? [];
        if (config.TrackingParamsOverride)
        {
            return new HashSet<string>(extras, StringComparer.Ordinal);
        }
        HashSet<string> set = new(BuiltInParams, StringComparer.Ordinal);
        foreach (string extra in extras)
        {
            set.Add(extra);
        }
        return set;
    }

    public static Uri Strip(Uri url, IReadOnlySet<string> trackers)
    {
        if (trackers.Count == 0)
        {
            return url;
        }
        if (string.IsNullOrEmpty(url.Query))
        {
            return url;
        }

        string raw = url.Query.StartsWith('?') ? url.Query[1..] : url.Query;
        List<string> kept = [];
        foreach (string pair in raw.Split('&'))
        {
            if (pair.Length == 0)
            {
                continue;
            }
            int eq = pair.IndexOf('=');
            string key = eq < 0 ? pair : pair[..eq];
            if (!trackers.Contains(key))
            {
                kept.Add(pair);
            }
        }

        UriBuilder builder = new(url);
        builder.Query = kept.Count == 0 ? null : string.Join("&", kept);
        return builder.Uri;
    }
}