using AskMeFirst.Core.Config;

namespace AskMeFirst.Core.Routing;

public sealed class WeekdayInMatcher : IPredicateMatcher
{
    public bool Matches(RuleWhen ruleWhen, RoutingContext ctx)
    {
        if (ruleWhen.WeekdayIn is not { Count: > 0 })
        {
            return true;
        }
        string today = ctx.Now.LocalDateTime.DayOfWeek switch
        {
            DayOfWeek.Monday => "Mon",
            DayOfWeek.Tuesday => "Tue",
            DayOfWeek.Wednesday => "Wed",
            DayOfWeek.Thursday => "Thu",
            DayOfWeek.Friday => "Fri",
            DayOfWeek.Saturday => "Sat",
            DayOfWeek.Sunday => "Sun",
            _ => "",
        };
        foreach (string d in ruleWhen.WeekdayIn)
        {
            if (string.Equals(d, today, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }
}