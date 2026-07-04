using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Audit;
using AskMeFirst.Core.Commands;
using AskMeFirst.Core.Config;
using AskMeFirst.Core.Launch;
using AskMeFirst.Core.Models;
using AskMeFirst.Core.Routing;

namespace AskMeFirst.Core.Tests;

internal static class TestBrowser
{
    public static Browser Make(string id, string displayName, string executablePath)
    {
        return new Browser
        {
            Id = id,
            DisplayName = displayName,
            ExecutablePath = executablePath,
            LaunchStrategy = BrowserLaunchStrategies.For(id),
        };
    }
}

internal sealed class FakeInventory : IBrowserInventory
{
    public List<Browser> Browsers { get; init; } = [];

    public IReadOnlyList<Browser> Discover() => Browsers;

    public Browser? FindById(string id) =>
        Browsers.FirstOrDefault(b => string.Equals(b.Id, id, StringComparison.OrdinalIgnoreCase));
}

internal sealed class FakeLauncher : IUrlLauncher
{
    public List<(Browser Browser, Uri Url)> Launches { get; } = [];

    public void Launch(Browser browser, Uri url)
    {
        Launches.Add((browser, url));
    }
}

internal sealed class FakeLogger : ILogger
{
    public List<string> Infos { get; } = [];
    public List<string> Warns { get; } = [];
    public List<string> Errors { get; } = [];

    public void LogInfo(string message) => Infos.Add(message);
    public void LogWarn(string message) => Warns.Add(message);
    public void LogError(string message) => Errors.Add(message);
}

internal sealed class FakeProfileDetector : IBrowserProfileDetector
{
    public Dictionary<string, List<BrowserProfile>> Profiles { get; } = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<BrowserProfile> Detect(Browser browser)
    {
        return Profiles.TryGetValue(browser.Id, out List<BrowserProfile>? list)
            ? list
            : [];
    }
}

internal sealed class FakeSourceAppDetector : ISourceAppDetector
{
    public SourceApp? Value { get; set; }

    public SourceApp? Detect() => Value;
}

internal sealed class FixedTimeProvider : TimeProvider
{
    private readonly DateTimeOffset now;

    public FixedTimeProvider(DateTimeOffset now)
    {
        this.now = now;
    }

    public override DateTimeOffset GetUtcNow() => now;
}

internal static class TestConfig
{
    public static AppConfig WithRules(params Rule[] rules)
    {
        return new AppConfig
        {
            Rules = rules,
        };
    }
}

internal static class TestEvaluator
{
    public static PredicateEvaluator Default() => new(RoutingDefaults.Matchers());
}

internal static class TestResolvers
{
    public static IReadOnlyList<ITargetResolver> For(AppConfig appConfig, PredicateEvaluator evaluator) =>
        RoutingDefaults.Resolvers(appConfig, evaluator);
}

internal sealed class FakeConfigPathResolver : IConfigPathResolver
{
    public string DefaultConfigPath { get; set; } = Path.Combine(Path.GetTempPath(), "askmefirst-test-config.json");
}

internal sealed class FakeProcessNameNormalizer : IProcessNameNormalizer
{
    public string Normalize(string rawName, string? bundleId = null, string? executablePath = null)
    {
        return rawName.ToLowerInvariant();
    }
}

internal static class TestCommandContext
{
    public static CommandContext Build(IDefaultBrowserRegistrar registrar, FakeLogger logger)
    {
        FakeInventory inventory = new();
        FakeLauncher launcher = new();
        FakeProfileDetector profiles = new();
        FakeSourceAppDetector sourceApp = new();
        FakeNotifier notifier = new();
        AppConfig appConfig = new() { Rules = [] };
        CommandRegistry registry = new();
        PredicateEvaluator evaluator = TestEvaluator.Default();
        IReadOnlyList<ITargetResolver> resolvers = TestResolvers.For(appConfig, evaluator);
        ProfileResolver profileResolver = new(profiles, appConfig.Profiles, logger);
        TrackingStripper stripper = new(appConfig);
        IRoutingExecutor executor = new RoutingExecutor(inventory, profileResolver, stripper, appConfig);
        RuleRouter router = new(
            resolvers,
            executor,
            inventory,
            sourceApp,
            new RecordingPickerLauncher(),
            usePickerAsCatchAll: false,
            appConfig.Profiles,
            profiles,
            launcher,
            logger,
            notifier,
            new FixedTimeProvider(new DateTimeOffset(2026, 6, 1, 10, 0, 0, TimeSpan.Zero)));
        return new CommandContext(
            logger,
            inventory,
            launcher,
            profiles,
            sourceApp,
            new FakeProcessNameNormalizer(),
            new FakeConfigPathResolver(),
            appConfig,
            TimeProvider.System,
            "test",
            registry,
            router,
            new RecordingPickerLauncher(),
            new NoOpRecentPicksLog(),
            notifier,
            registrar);
    }
}

internal sealed class RecordingPickerLauncher : IPickerLauncher
{
    public List<PickerRequest> Requests { get; } = [];

    public PickerResult Show(PickerRequest request)
    {
        Requests.Add(request);
        return new Cancelled();
    }
}

internal sealed class FakeNotifier : INotifier
{
    public List<(string Title, string Message)> Calls { get; } = [];

    public void Show(string title, string message)
    {
        Calls.Add((title, message));
    }
}

internal sealed class ThrowingLauncher : IUrlLauncher
{
    public Exception ToThrow { get; set; } = new InvalidOperationException("launch failed");

    public void Launch(Browser browser, Uri url)
    {
        throw ToThrow;
    }
}

internal sealed class FakeRegistrar : IDefaultBrowserRegistrar
{
    public RegistrationResult RegisterResult { get; set; } = new(Success: true, Message: "Registered.");
    public RegistrationResult UnregisterResult { get; set; } = new(Success: true, Message: "Unregistered.");
    public bool OpenOsSettingsResult { get; set; } = true;

    public int RegisterCalls { get; private set; }
    public int UnregisterCalls { get; private set; }
    public int OpenOsSettingsCalls { get; private set; }

    public Task<RegistrationResult> RegisterAsync(CancellationToken ct = default)
    {
        RegisterCalls++;
        return Task.FromResult(RegisterResult);
    }

    public Task<RegistrationResult> UnregisterAsync(CancellationToken ct = default)
    {
        UnregisterCalls++;
        return Task.FromResult(UnregisterResult);
    }

    public bool TryOpenOsSettings()
    {
        OpenOsSettingsCalls++;
        return OpenOsSettingsResult;
    }
}