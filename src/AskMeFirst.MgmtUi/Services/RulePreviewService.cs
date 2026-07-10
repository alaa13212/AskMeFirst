using System.Text.RegularExpressions;
using AskMeFirst.Core.Config;
using AskMeFirst.Core.Routing;

namespace AskMeFirst.MgmtUi.Services;

public sealed record RulePreviewResult(bool HasMatch, Rule? MatchedRule);

public interface IRulePreviewService
{
    RulePreviewResult Preview(Uri url, AppConfig config);
}

public sealed class RulePreviewService : IRulePreviewService
{
    public RulePreviewResult Preview(Uri url, AppConfig config)
    {
        if (config.Rules.Count == 0)
        {
            return new RulePreviewResult(false, null);
        }
        string host = url.Host;
        string hostPath = url.AbsolutePath == "/" ? host : host + url.AbsolutePath;

        IOrderedEnumerable<Rule> ordered = config.Rules.OrderByDescending(r => r.Priority);
        foreach (Rule rule in ordered)
        {
            if (MatchesRule(rule, url, host, hostPath))
            {
                return new RulePreviewResult(true, rule);
            }
        }
        return new RulePreviewResult(false, null);
    }

    private static bool MatchesRule(Rule rule, Uri url, string host, string hostPath)
    {
        RuleWhen when = rule.When;
        if (when is null)
        {
            return true;
        }
        if (when.UrlMatchesAny is { Count: > 0 })
        {
            bool any = false;
            foreach (string pattern in when.UrlMatchesAny)
            {
                if (GlobMatcher.Matches(pattern, host, hostPath))
                {
                    any = true;
                    break;
                }
            }
            if (!any)
            {
                return false;
            }
        }
        if (when.UrlMatchesAll is { Count: > 0 })
        {
            foreach (string pattern in when.UrlMatchesAll)
            {
                if (!GlobMatcher.Matches(pattern, host, hostPath))
                {
                    return false;
                }
            }
        }
        if (when.UrlRegex is not null)
        {
            if (!Regex.IsMatch(url.ToString(), when.UrlRegex))
            {
                return false;
            }
        }
        if (when.SchemeIn is { Count: > 0 })
        {
            bool schemeMatch = false;
            foreach (string s in when.SchemeIn)
            {
                if (string.Equals(s, url.Scheme, StringComparison.OrdinalIgnoreCase))
                {
                    schemeMatch = true;
                    break;
                }
            }
            if (!schemeMatch)
            {
                return false;
            }
        }
        return true;
    }
}
