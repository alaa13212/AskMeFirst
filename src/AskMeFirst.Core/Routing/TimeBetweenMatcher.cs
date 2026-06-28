using System.Globalization;
using AskMeFirst.Core.Config;

namespace AskMeFirst.Core.Routing;

public sealed class TimeBetweenMatcher : IPredicateMatcher
{
    public bool Matches(RuleWhen ruleWhen, RoutingContext ctx)
    {
        if (ruleWhen.TimeBetween is not { Length: > 0 } range)
        {
            return true;
        }
        int dash = range.IndexOf('-');
        if (dash <= 0 || dash >= range.Length - 1)
        {
            return false;
        }
        if (!TimeOnly.TryParseExact(range[..dash], "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out TimeOnly start))
        {
            return false;
        }
        if (!TimeOnly.TryParseExact(range[(dash + 1)..], "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out TimeOnly end))
        {
            return false;
        }
        TimeOnly nowTime = TimeOnly.FromDateTime(ctx.Now.LocalDateTime);
        if (start <= end)
        {
            return nowTime >= start && nowTime <= end;
        }
        return nowTime >= start || nowTime <= end;
    }
}