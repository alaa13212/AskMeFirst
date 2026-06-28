using System.Text.RegularExpressions;
using AskMeFirst.Core.Config;

namespace AskMeFirst.Core.Routing;

public sealed class UrlRegexMatcher : IPredicateMatcher
{
    private static readonly Dictionary<string, Regex> Cache = new(StringComparer.Ordinal);

    public bool Matches(RuleWhen ruleWhen, RoutingContext ctx)
    {
        if (ruleWhen.UrlRegex is not { Length: > 0 } pattern)
        {
            return true;
        }
        if (!Cache.TryGetValue(pattern, out Regex? regex))
        {
            regex = new Regex(pattern, RegexOptions.Compiled | RegexOptions.CultureInvariant);
            Cache[pattern] = regex;
        }
        return regex.IsMatch(ctx.Url.AbsoluteUri);
    }
}