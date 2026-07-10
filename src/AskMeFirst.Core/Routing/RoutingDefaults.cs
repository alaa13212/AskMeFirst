using AskMeFirst.Core.Config;

namespace AskMeFirst.Core.Routing;

public static class RoutingDefaults
{
    public static IReadOnlyList<IPredicateMatcher> Matchers() =>
    [
        new UrlMatchesAnyMatcher(),
        new UrlMatchesAllMatcher(),
        new UrlRegexMatcher(),
        new SchemeInMatcher(),
    ];

    public static IReadOnlyList<ITargetResolver> Resolvers(AppConfig appConfig, PredicateEvaluator evaluator) =>
    [
        new ExplicitOverrideResolver(),
        new RuleMatchingResolver(appConfig.Rules, evaluator),
    ];
}