namespace AskMeFirst.Core.Routing;

public static class BuiltInShortenerDomains
{
    public static readonly IReadOnlySet<string> Hosts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "t.co",
        "bit.ly",
        "tinyurl.com",
        "goo.gl",
        "ow.ly",
        "is.gd",
        "buff.ly",
        "lnkd.in",
        "rebrand.ly",
        "shorturl.at",
        "cutt.ly",
        "rb.gy",
        "ift.tt",
        "fb.me",
        "t.me",
        "dlvr.it",
        "snip.ly",
        "po.st",
        "mcaf.ee",
        "tr.im",
        "v.gd",
        "adf.ly",
        "sh.st",
        "tcrn.ch",
        "bl.ink",
        "clck.ru",
        "short.io",
    };
}