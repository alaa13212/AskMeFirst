using AskMeFirst.Core.Config;

namespace AskMeFirst.Core.Routing;

public static class RoutingDefaults
{
    public static IReadOnlyList<IPredicateMatcher> Matchers() =>
    [
        new ProcessInMatcher(),
        new UrlMatchesAnyMatcher(),
        new UrlMatchesAllMatcher(),
        new UrlRegexMatcher(),
        new SchemeInMatcher(),
        new TimeBetweenMatcher(),
        new WeekdayInMatcher(),
        new BrowserRunningMatcher(),
    ];

    public static IReadOnlyList<ITargetResolver> Resolvers(AppConfig appConfig, PredicateEvaluator evaluator) =>
    [
        new ExplicitOverrideResolver(),
        new RuleMatchingResolver(appConfig.Rules, evaluator),
        new SettingsFallbackResolver(appConfig),
    ];
}