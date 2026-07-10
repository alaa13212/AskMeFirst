using AskMeFirst.Core.Config;

namespace AskMeFirst.Core.Routing;

public sealed class ConfigShortenerDomainList : IShortenerDomainList
{
    private readonly IReadOnlySet<string> hosts;

    public ConfigShortenerDomainList(AppConfig config)
    {
        hosts = Build(config);
    }

    public bool IsKnown(string host) => hosts.Contains(host);

    public static IReadOnlySet<string> Build(AppConfig config)
    {
        IReadOnlyList<string> extras = config.UnshortenDomainsExtra ?? [];
        if (config.UnshortenDomainsOverride)
        {
            return new HashSet<string>(extras, StringComparer.OrdinalIgnoreCase);
        }
        HashSet<string> set = new(BuiltInShortenerDomains.Hosts, StringComparer.OrdinalIgnoreCase);
        foreach (string extra in extras)
        {
            set.Add(extra);
        }
        return set;
    }
}