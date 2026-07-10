using System.Diagnostics;
using AskMeFirst.Core;
using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Commands;
using AskMeFirst.Core.Config;
using AskMeFirst.Core.Inventory;
using AskMeFirst.Core.Launch;
using AskMeFirst.Core.Models;
using AskMeFirst.Core.Profiles;
using AskMeFirst.Core.Routing;

namespace AskMeFirst.Commands;

public sealed class BenchCommand : ICommand
{
    private const int Iterations = 1000;

    private static readonly BenchBudget ConfigLoadBudget =
        new("cold_config_load", TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(25));
    private static readonly BenchBudget RuleEvalBudget =
        new("cold_rule_eval", TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(25));
    private static readonly BenchBudget InventoryBudget =
        new("cold_inventory", TimeSpan.FromMilliseconds(15), TimeSpan.FromMilliseconds(50));
    private static readonly BenchBudget TotalBudget =
        new("warm_total", TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(100));

    public string Name => "--bench";
    public string Usage => "--bench";
    public string Description => "Run a routing workload and print per-phase timings.";

    public Task<int> Execute(string[] args, CommandContext ctx)
    {
        ILogger logger = ctx.Resolve<ILogger>();
        Stopwatch configLoadSw = Stopwatch.StartNew();
        RuleRouter router = BuildBenchRouter(logger);
        configLoadSw.Stop();
        Uri url = new("https://example.com/abc");

        List<TimeSpan> ruleEval = new(Iterations);
        List<TimeSpan> inventory = new(Iterations);
        List<TimeSpan> total = new(Iterations);
        for (int i = 0; i < Iterations; i++)
        {
            RouteResult result = router.Route(url, null, null);
            ruleEval.Add(result.Timings.RuleEval);
            inventory.Add(result.Timings.InventoryLoad);
            total.Add(result.Timings.Total);
        }

        Console.WriteLine($"{ProgramInfo.ExecutableName} --bench");
        Console.WriteLine($"  iterations: {Iterations}");
        bool ok = true;
        ok &= Report("cold_config_load", [configLoadSw.Elapsed], ConfigLoadBudget);
        ok &= Report("cold_rule_eval", ruleEval, RuleEvalBudget);
        ok &= Report("cold_inventory", inventory, InventoryBudget);
        ok &= Report("warm_total", total, TotalBudget);
        return Task.FromResult(ok ? 0 : 1);
    }

    private static bool Report(string phaseName, List<TimeSpan> samples, BenchBudget budget)
    {
        bool single = samples.Count == 1;
        TimeSpan headline = single ? samples[0] : Percentile(samples, 0.95);
        double headlineMs = headline.TotalMilliseconds;
        string headlineLabel = single ? "sample" : "p95";
        TimeSpan p50 = Percentile(samples, 0.50);
        TimeSpan max = samples.Max();
        Console.WriteLine(
            single
                ? $"  {phaseName,-15} sample={headlineMs,7:F2}ms          budget={budget.Target.TotalMilliseconds,3:F0}ms  hard={budget.HardLimit.TotalMilliseconds,3:F0}ms"
                : $"  {phaseName,-15} p50={p50.TotalMilliseconds,6:F2}ms  p95={headlineMs,6:F2}ms  max={max.TotalMilliseconds,7:F2}ms  budget={budget.Target.TotalMilliseconds,3:F0}ms  hard={budget.HardLimit.TotalMilliseconds,3:F0}ms");
        if (single)
        {
            if (samples[0] > budget.HardLimit)
            {
                Console.WriteLine(
                    $"  BENCH FAIL: {phaseName} sample={samples[0].TotalMilliseconds:F2}ms exceeds hard limit {budget.HardLimit.TotalMilliseconds:F0}ms");
                return false;
            }
        }
        else if (headline > budget.HardLimit)
        {
            Console.WriteLine(
                $"  BENCH FAIL: {phaseName} p95={headlineMs:F2}ms exceeds hard limit {budget.HardLimit.TotalMilliseconds:F0}ms");
            return false;
        }
        return true;
    }

    private static TimeSpan Percentile(List<TimeSpan> samples, double percentile)
    {
        List<TimeSpan> sorted = [.. samples];
        sorted.Sort();
        int idx = (int)Math.Round(percentile * (sorted.Count - 1));
        return sorted[Math.Clamp(idx, 0, sorted.Count - 1)];
    }

    private sealed record BenchBudget(string Name, TimeSpan Target, TimeSpan HardLimit);

    private static RuleRouter BuildBenchRouter(ILogger logger)
    {
        BenchInventory inventory = new();
        inventory.Browsers.Add(new Browser
        {
            Id = "bench",
            DisplayName = "Bench",
            ExecutablePath = "/dev/null",
            LaunchStrategy = DefaultLaunchStrategy.Instance,
        });

        AppConfig config = new()
        {
            Settings = new Settings(),
            Browsers = [new BrowserSpec { Id = "bench", DisplayName = "Bench", Executable = "/dev/null" }],
            Profiles = [],
            Rules =
            [
                new Rule
                {
                    Name = "bench-match",
                    Priority = 1,
                    When = new RuleWhen { UrlMatchesAny = ["example.com"] },
                    Then = new RuleThen { Browser = "bench", StripTracking = false },
                },
            ],
        };

        PredicateEvaluator predicateEvaluator = new(RoutingDefaults.Matchers());
        IReadOnlyList<ITargetResolver> resolvers = RoutingDefaults.Resolvers(config, predicateEvaluator);
        TrackingStripper stripper = new(config);
        ProfileResolver profileResolver = new(new NoProfilesDetector(), config.Profiles, logger);
        IRoutingExecutor executor = new RoutingExecutor(inventory, profileResolver, stripper, config);

        IUnshortenTaskBuilder unshorten = new UnshortenTaskBuilder(
            new NoOpUnshortener(),
            new StaticShortenerDomainList([]),
            stripper,
            logger);

        return new RuleRouter(
            resolvers,
            executor,
            inventory,
            new NoOpPickerLauncher(logger),
            usePickerAsCatchAll: false,
            profileSpecs: config.Profiles,
            new NoProfilesDetector(),
            new BenchLauncher(),
            logger,
            new NullNotifier(),
            TimeProvider.System,
            unshorten);
    }

    private sealed class BenchInventory : IBrowserInventory
    {
        public List<Browser> Browsers { get; } = [];

        public IReadOnlyList<Browser> Discover() => Browsers;

        public Browser? FindById(string id) =>
            Browsers.FirstOrDefault(b => string.Equals(b.Id, id, StringComparison.OrdinalIgnoreCase));
    }

    private sealed class BenchLauncher : IUrlLauncher
    {
        public void Launch(Browser browser, Uri url)
        {
        }
    }

    private sealed class NoProfilesDetector : IBrowserProfileDetector
    {
        public IReadOnlyList<BrowserProfile> Detect(Browser browser) => [];
    }

    private sealed class StaticShortenerDomainList : IShortenerDomainList
    {
        private readonly HashSet<string> hosts;

        public StaticShortenerDomainList(IEnumerable<string> hosts)
        {
            this.hosts = new HashSet<string>(hosts, StringComparer.OrdinalIgnoreCase);
        }

        public bool IsKnown(string host) => hosts.Contains(host);
    }
}
