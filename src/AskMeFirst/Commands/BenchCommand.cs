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
    private const int WarmupIterations = 5;

    private static readonly BenchBudget RuleEvalBudget =
        new("rule_eval", TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(25));
    private static readonly BenchBudget ExecuteBudget =
        new("execute", TimeSpan.FromMilliseconds(15), TimeSpan.FromMilliseconds(50));
    private static readonly BenchBudget InventoryBudget =
        new("inventory", TimeSpan.FromMilliseconds(15), TimeSpan.FromMilliseconds(50));
    private static readonly BenchBudget TotalBudget =
        new("total_warm", TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(100));

    public string Name => "--bench";
    public string Usage => "--bench";
    public string Description => "Run a routing workload and print per-phase timings.";

    public Task<int> Execute(string[] args, CommandContext ctx)
    {
        ILogger logger = ctx.Resolve<ILogger>();
        BenchHarness harness = BenchHarness.Build(logger);

        for (int i = 0; i < WarmupIterations; i++)
        {
            harness.Router.Route(harness.Url, null, null);
        }

        List<TimeSpan> ruleEval = new(Iterations);
        List<TimeSpan> execute = new(Iterations);
        List<TimeSpan> total = new(Iterations);
        List<TimeSpan> inventory = new(Iterations);
        for (int i = 0; i < Iterations; i++)
        {
            RouteResult result = harness.Router.RouteTimed(harness.Url, null, null);
            ruleEval.Add(result.Timings.RuleEval);
            execute.Add(result.Timings.Executor);
            total.Add(result.Timings.Total);

            Stopwatch sw = Stopwatch.StartNew();
            harness.Inventory.Discover();
            sw.Stop();
            inventory.Add(sw.Elapsed);
        }

        Console.WriteLine($"{ProgramInfo.ExecutableName} --bench");
        Console.WriteLine($"  iterations: {Iterations}");
        bool ok = true;
        ok &= Report("rule_eval", ruleEval, RuleEvalBudget);
        ok &= Report("execute", execute, ExecuteBudget);
        ok &= Report("inventory", inventory, InventoryBudget);
        ok &= Report("total_warm", total, TotalBudget);
        return Task.FromResult(ok ? 0 : 1);
    }

    private static bool Report(string phaseName, List<TimeSpan> samples, BenchBudget budget)
    {
        TimeSpan p50 = Percentile(samples, 0.50);
        TimeSpan p95 = Percentile(samples, 0.95);
        TimeSpan max = TimeSpan.Zero;
        foreach (TimeSpan s in samples)
        {
            if (s > max)
            {
                max = s;
            }
        }
        Console.WriteLine(
            $"  {phaseName,-11} p50={p50.TotalMilliseconds,6:F2}ms  p95={p95.TotalMilliseconds,6:F2}ms  max={max.TotalMilliseconds,7:F2}ms  budget={budget.Target.TotalMilliseconds,3:F0}ms  hard={budget.HardLimit.TotalMilliseconds,3:F0}ms");
        if (p95 > budget.HardLimit)
        {
            Console.WriteLine(
                $"  BENCH FAIL: {phaseName} p95={p95.TotalMilliseconds:F2}ms exceeds hard limit {budget.HardLimit.TotalMilliseconds:F0}ms");
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

    private sealed class BenchHarness
    {
        public required RuleRouter Router { get; init; }

        public required BenchInventory Inventory { get; init; }

        public required Uri Url { get; init; }

        public static BenchHarness Build(ILogger logger)
        {
            Browser benchBrowser = new()
            {
                Id = "bench",
                DisplayName = "Bench",
                ExecutablePath = OperatingSystem.IsWindows() ? @"C:\bench\bench.exe" : "/dev/null",
                LaunchStrategy = DefaultLaunchStrategy.Instance,
            };

            BenchInventory inventory = new();
            inventory.Browsers.Add(benchBrowser);

            AppConfig config = new()
            {
                Settings = new Settings(),
                Browsers = [new BrowserSpec { Id = "bench", DisplayName = "Bench", Executable = benchBrowser.ExecutablePath }],
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

            BenchLauncher launcher = new();
            RuleRouter router = new(
                resolvers,
                executor,
                inventory,
                new NoOpPickerLauncher(logger),
                usePickerAsCatchAll: false,
                profileSpecs: config.Profiles,
                new NoProfilesDetector(),
                launcher,
                logger,
                new NullNotifier(),
                TimeProvider.System,
                unshorten);

            return new BenchHarness
            {
                Router = router,
                Inventory = inventory,
                Url = new Uri("https://example.com/abc"),
            };
        }
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
