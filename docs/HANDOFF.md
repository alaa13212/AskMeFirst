# Handoff — 2026-06-28

> **First thing next session: read this file.**

## TL;DR

Phases 0, 1, and 2 complete. This session was a **Phase 2 code-quality pass** — no new user-facing features, but two big structural changes:

- **Routing refactor**: PredicateEvaluator + RuleRouter decomposed via Strategy + Pipeline + Result-type patterns. RoutingOutcome is now a discriminated union (`Success` / `Failure`) with `RoutingExitCode` enum. RoutingExecutor extracted as a separate component. 9-dep RuleRouter → 6 deps.
- **Profiles first-class**: Top-level `profiles:` config section with `ProfileSpec { Id, BrowserId, Name?, Directory?, DisplayName? }`. Rules reference profiles by stable ID (`profileId`), not by name/directory string. `ProfileResolver` rewritten as ID-based lookup. `ConfigValidator` runs at load time (unique IDs, all `profileId` references resolve).

**161/161 tests passing.** AOT binary **4.19 MB** (was 4.15 MB; +40 KB for the extra types). Cold start ~46 ms.

---

## Where we are

| Phase | Status | Notes |
|---|---|---|
| 0 — Bootstrap | ✅ Done | Build, test, AOT publish all working |
| 1 — MVP router | ✅ Done + polished | ICommand architecture, profile detection, browser-family launch strategies |
| 2 — Rule engine | ✅ Done + refactored | Rules + predicates + actions + source-app detection + tracking strip + profiles-first-class |
| 3 — Picker UI | ⏭ Next | Avalonia, two-panel layout |
| 4 — OS integration | 📋 Planned | Win/Mac/Linux registration |
| 5 — Link processing | 📋 Planned | Async Unshortener (tracking strip already done) |
| 6 — Polish | 📋 Planned | Bench command, README, examples, config ↔ inventory mapping |

Full plan: [`docs/roadmap.md`](./roadmap.md).

---

## Project tree

```
src/
├── AskMeFirst.Core/                          ← pure BCL, no platform deps
│   ├── Models/
│   │   ├── Browser.cs                        ← Id, DisplayName, ExecutablePath, LaunchStrategy, Profile?
│   │   └── BrowserProfile.cs                 ← Name, DirectoryName, IsDefault (UNCHANGED)
│   ├── Abstractions/                         ← IBrowserInventory, IBrowserProfileDetector,
│   │                                          IBrowserLaunchStrategy, IUrlLauncher, ILogger
│   ├── Logging/ConsoleLogger.cs
│   ├── Launch/                               ← browser-family arg strategies
│   │   ├── ChromiumLaunchStrategy.cs
│   │   ├── FirefoxLaunchStrategy.cs
│   │   ├── DefaultLaunchStrategy.cs
│   │   └── BrowserLaunchStrategies.cs
│   ├── Profiles/FirefoxProfilesParser.cs
│   ├── Config/
│   │   ├── AppConfig.cs                      ← + Profiles (NEW), Rules, TrackingParamsExtra
│   │   ├── Settings.cs
│   │   ├── BrowserSpec.cs                    ← Profile → ProfileId
│   │   ├── ProfileSpec.cs                    ← NEW: Id, BrowserId, Name?, Directory?, DisplayName?
│   │   ├── Rule.cs
│   │   ├── RuleWhen.cs
│   │   ├── RuleThen.cs                       ← Profile → ProfileId
│   │   ├── IConfigPathResolver.cs
│   │   ├── ConfigPath.cs
│   │   ├── ConfigJsonContext.cs              ← + ProfileSpec
│   │   ├── ConfigLoader.cs
│   │   └── ConfigValidator.cs                ← NEW: unique IDs, profileId references resolve
│   ├── Commands/
│   │   ├── ICommand.cs
│   │   ├── CommandContext.cs                 ← + Router
│   │   ├── CommandRegistry.cs
│   │   ├── CommandNotFoundException.cs
│   │   └── HelpFormatter.cs
│   ├── Composition/BootstrapContext.cs
│   ├── Routing/                              ← REFACTORED
│   │   ├── RoutingContext.cs                 ← + ExplicitBrowserId, ExplicitProfileId
│   │   ├── RoutingDecision.cs                ← ProfileName → ProfileId
│   │   ├── RoutingIntent.cs                  ← NEW: BrowserId, ProfileId, StripTrackingOverride,
│   │   │                                      NotFoundExitCode, NotFoundMessagePrefix
│   │   ├── RoutingOutcome.cs                 ← NEW: abstract record + Success + Failure
│   │   ├── RoutingExitCode.cs                ← NEW: enum (Success=0, NoBrowsersDiscovered=2,
│   │   │                                      BrowserNotFound=3, RuleBrowserNotFound=4, NoRouteFound=5)
│   │   ├── IRoutingExecutor.cs               ← NEW: RoutingOutcome Execute(intent, url)
│   │   ├── RoutingExecutor.cs                ← NEW: lookup → profile → strip pipeline
│   │   ├── ITargetResolver.cs                ← NEW: RoutingIntent? Resolve(ctx)
│   │   ├── RoutingDefaults.cs                ← NEW: Matchers() + Resolvers(appConfig, evaluator)
│   │   ├── Resolvers/                        ← NEW (3 classes):
│   │   │   ├── ExplicitOverrideResolver.cs   ← picks up --browser/--profile CLI flags
│   │   │   ├── RuleMatchingResolver.cs       ← evaluates rules via RuleEngine
│   │   │   └── SettingsFallbackResolver.cs   ← falls back to Settings.DefaultBrowserId
│   │   ├── IPredicateMatcher.cs              ← NEW: bool Matches(RuleWhen, RoutingContext)
│   │   ├── PredicateEvaluator.cs             ← was 237 lines (8 ifs), now ~25 (dispatches matchers)
│   │   ├── GlobMatcher.cs                    ← NEW: static helper for glob→regex (shared cache)
│   │   ├── Matchers/                         ← NEW (8 classes, one per predicate field):
│   │   │   ├── ProcessInMatcher.cs
│   │   │   ├── UrlMatchesAnyMatcher.cs
│   │   │   ├── UrlMatchesAllMatcher.cs
│   │   │   ├── UrlRegexMatcher.cs
│   │   │   ├── SchemeInMatcher.cs
│   │   │   ├── TimeBetweenMatcher.cs
│   │   │   ├── WeekdayInMatcher.cs
│   │   │   └── BrowserRunningMatcher.cs
│   │   ├── RuleEngine.cs                     ← now takes PredicateEvaluator as parameter
│   │   ├── ProfileResolver.cs                ← REWRITTEN: takes ProfileSpec list,
│   │   │                                      resolves by profileId (BrowserId match check)
│   │   ├── TrackingStripper.cs               ← now instance class (DI); static helpers preserved
│   │   ├── ISourceAppDetector.cs
│   │   ├── IProcessNameNormalizer.cs
│   │   └── SourceApp.cs
│   ├── UrlRouter.cs                          ← legacy; still used by RuleRouter for explicit routes
│   ├── RuleRouter.cs                         ← REWRITTEN: 9 deps → 6 deps
│   │                                          (resolvers, executor, sourceApp, launcher, logger, time)
│   │                                          Owns: detect source → iterate resolvers → call executor
│   │                                          → switch on Success/Failure outcome
│   └── Resources/DefaultConfig.jsonc         ← + "Profiles": []

├── AskMeFirst.Platforms.Windows/
├── AskMeFirst.Platforms.MacOs/
├── AskMeFirst.Platforms.Linux/               ← platform bootstraps + inventories + detectors

└── AskMeFirst/                               ← thin CLI host
    ├── ProgramInfo.cs
    ├── Program.cs
    ├── Composition.cs                        ← wires routing chain + ConfigValidator
    ├── CliArgsException.cs
    └── Commands/
        ├── VersionCommand.cs
        ├── HelpCommand.cs
        ├── BenchCommand.cs
        ├── ListCommand.cs
        ├── RouteCommand.cs                   ← uses ctx.Router.Route(...)
        └── RouteArgs.cs                      ← ProfileName → ProfileId

tests/
└── AskMeFirst.Core.Tests/                    ← 161 tests total
    ├── Fakes.cs                              ← + TestEvaluator, TestResolvers
    ├── UrlRouterTests.cs
    ├── ConfigLoaderTests.cs
    ├── RouteCommandTests.cs
    ├── HelpFormatterTests.cs
    ├── LaunchStrategyTests.cs
    ├── CliTests.cs
    ├── PredicateEvaluatorTests.cs            ← uses instance PredicateEvaluator + GlobMatcher
    ├── RuleEngineTests.cs                    ← passes evaluator explicitly
    ├── TrackingStripperTests.cs
    ├── RuleRouterTests.cs                    ← BuildRouter constructs full routing chain
    ├── MatcherTests.cs                       ← NEW: 11 tests, one per matcher in isolation
    ├── ResolverTests.cs                      ← NEW: 7 tests, one per resolver + chain precedence
    ├── ProfileResolverTests.cs               ← REWRITTEN for profileId API: 9 tests
    ├── RoutingExecutorTests.cs               ← NEW: 9 tests (no-browsers, browser-missing,
    │                                          strip on/off/override, profile resolution)
    └── ConfigValidatorTests.cs               ← NEW: 10 tests (unique IDs, references resolve,
                                               browser profileId refs)
```

---

## Architecture highlights

### Command pattern — `ICommand` + `CommandRegistry` (UNCHANGED)

```csharp
public interface ICommand
{
    string Name { get; }
    IReadOnlyList<string> Aliases => [];
    string Usage => Name;
    string Description => "";
    int Execute(string[] args, CommandContext ctx);
}

public sealed class CommandRegistry
{
    public CommandRegistry Register(ICommand command);
    public CommandRegistry RegisterDefault(ICommand command);
    public ICommand Resolve(string? firstArg);
    public IReadOnlyList<ICommand> All();
}
```

### Predicate evaluation — Strategy pattern

```csharp
public interface IPredicateMatcher
{
    bool Matches(RuleWhen ruleWhen, RoutingContext ctx);
}

public sealed class PredicateEvaluator(IReadOnlyList<IPredicateMatcher> matchers)
{
    public bool Matches(RuleWhen ruleWhen, RoutingContext ctx)
    {
        foreach (IPredicateMatcher m in matchers)
            if (!m.Matches(ruleWhen, ctx)) return false;
        return true;
    }
}
```

Each matcher owns one predicate field. Returns `true` if field is null/empty (predicate inactive) OR predicate evaluates true. Adding a new predicate = one new class + register in `RoutingDefaults.Matchers()`. No edit to `PredicateEvaluator`.

### Rule routing — Strategy + Pipeline + Result type

```csharp
public interface ITargetResolver { RoutingIntent? Resolve(RoutingContext ctx); }

public sealed record RoutingIntent(
    string BrowserId, string? ProfileId, bool? StripTrackingOverride,
    RoutingExitCode NotFoundExitCode,     // what exit code if executor can't find browser
    string NotFoundMessagePrefix);        // what message prefix ("Browser" / "Rule matched browser")

public abstract record RoutingOutcome;
public sealed record Success(Browser Browser, Uri FinalUrl, Uri OriginalUrl) : RoutingOutcome;
public sealed record Failure(RoutingExitCode Code, string Message) : RoutingOutcome;

public interface IRoutingExecutor { RoutingOutcome Execute(RoutingIntent intent, Uri url); }

public sealed class RuleRouter(
    IReadOnlyList<ITargetResolver> resolvers,
    IRoutingExecutor executor,
    ISourceAppDetector sourceAppDetector,
    IUrlLauncher launcher,
    ILogger logger,
    TimeProvider timeProvider)
{
    public int Route(Uri url, string? explicitBrowserId, string? explicitProfileId)
    {
        // 1. detect source app
        // 2. build RoutingContext
        // 3. iterate resolvers — first non-null intent wins
        // 4. if no intent → log + return NoRouteFound
        // 5. executor.Execute(intent, url) → outcome
        // 6. switch outcome: Success → log + launch, Failure → log + return exit code
    }
}
```

Key design point: **`NotFoundExitCode` + `NotFoundMessagePrefix` live on the intent itself, set by each resolver**. The executor reads them opaquely — no chain knowledge leaks into the executor. Resolvers self-describe "if my target browser is missing, here's the exit code + message prefix to use."

### Profile resolution — ID-based lookup

```csharp
public sealed class ProfileResolver(
    IBrowserProfileDetector detector,
    IReadOnlyList<ProfileSpec> profileSpecs,
    ILogger logger)
{
    public Browser Resolve(Browser browser, string? profileId)
    {
        if (profileId is null) return DefaultProfile(browser);

        ProfileSpec? spec = FindSpec(profileId);
        if (spec is null) { log error + return DefaultProfile(browser); }
        if (spec.BrowserId != browser.Id) { log error + return DefaultProfile(browser); }

        // find detected profile matching spec.Name OR spec.Directory
        BrowserProfile? match = ...;
        if (match is null) { log warn + return DefaultProfile(browser); }
        return browser with { Profile = match };
    }
}
```

Profile mismatch is **soft-fail** (warn + fall back to default). Browser mismatch is **hard-fail** (exit 4).

### Config validation — at load time

```csharp
ConfigValidator.Validate(appConfig, logger);  // logs errors if any
// if invalid → fall back to embedded defaults (matches docs/rule-engine.md design)
```

Checks:
- `ProfileSpec.Id` is unique (case-insensitive)
- Every rule's `ProfileId` resolves to a declared spec
- Every `BrowserSpec.ProfileId` resolves to a declared spec

---

## Profiles-first-class config schema

```jsonc
{
  "profiles": [
    { "id": "chrome-personal-profile", "browserId": "chrome-personal", "directory": "Default", "displayName": "Personal" },
    { "id": "firefox-work-profile",   "browserId": "firefox-work",   "name": "work",         "displayName": "Work" }
  ],

  "rules": [
    { "when": { "processIn": ["slack"] },
      "then": { "browser": "firefox-work", "profileId": "firefox-work-profile" } }
  ]
}
```

`ProfileSpec` is the only place to declare a profile. Rule references it by stable ID.

---

## Decisions recap (point to `docs/decisions-log.md` for full detail)

| # | Pick |
|---|---|
| 1-44 | (Previous) |
| 45 | `IPredicateMatcher` per-predicate-field (Strategy pattern over the 8 predicate fields) |
| 46 | `ITargetResolver` per-selection-mode (Strategy: Explicit / Rule / Fallback) |
| 47 | `RoutingIntent` carries `NotFoundExitCode` + `NotFoundMessagePrefix` instead of `IntentSource` enum — keeps chain knowledge inside resolvers, executor reads opaquely |
| 48 | `RoutingOutcome` is a discriminated union (abstract record + `Success` / `Failure`) — eliminates scattered `return 4` / `return 5` etc. |
| 49 | `IRoutingExecutor` extracted as separate component — owns the lookup→profile→strip pipeline; RuleRouter doesn't know about inventory or stripper anymore |
| 50 | `RoutingExitCode` enum replaces magic ints 0/2/3/4/5 with named members |
| 51 | `ProfileSpec` is a config-side entity; `BrowserProfile` (unchanged) is runtime-side. Two distinct types. |
| 52 | Top-level `profiles:` section + rule `then.profileId` (stable ID) replaces rule `then.profile` (string match) |
| 53 | `ConfigValidator` runs at load time; on any error → fall back to embedded defaults + log all errors |
| 54 | Profile mismatch is soft-fail (warn + default); browser mismatch is hard-fail (distinct exit code) |

### Why this refactor — the user's review notes (verbatim)

> "We can abstract PredicateEvaluator.Matches(). The many if can be separate matchers implementing one interface"
> "How can we clean RuleRouter. Give me options" (chose Strategy + Pipeline + Result type mix)
> "The exit codes are still magic ints. 2, 5, 0, 4, 3 make them enum for readability"
> "Use discriminated union RoutingOutcome"
> "Make ResolveOutcome() a separate component IRoutingExecutor"
> "intent.Source == IntentSource.RuleMatch inside ResolveOutcome leaks chain knowledge back into the router" → replaced with intent.NotFoundExitCode + NotFoundMessagePrefix
> "I'd like profiles to be a first class citizen not an afterthought" → top-level profiles section + ConfigValidator + ProfileId throughout

---

## What's verified locally

- ✅ `dotnet build` — clean, 0 warnings, 0 errors
- ✅ `dotnet test` — **161/161** passing in ~0.8 s
- ✅ `dotnet publish -p:PublishProfile=Aot -r win-x64` — produces `askmefirst.exe` **4.19 MB**
- ✅ Cold start: ~46 ms
- ✅ `--list` discovers 3 real browsers with profiles
- ✅ CLI smoke: `--version` → exit 0, `--browser notreal` → exit 3 with `"Browser 'notreal' not found. Discovered: firefox, chrome, edge"`, `--browser chrome --profile <id>` → exit 0
- ✅ With sample config installed: rule→browser→profile chain works; undeclared browser IDs surface as exit 4 with `"Rule matched browser 'X' not found. Discovered: ..."`

---

## Style rules (READ THESE — non-obvious)

User-level preferences, also in `~/.mavis/memory/user.md`:

> **Comments describe WHAT, never WHY/HOW.** No plan-phase references, no requirement-doc references, no decision-history, no trivial info. Comment on symbols (classes / methods), not on instructions inside methods. Default to no comment.

> **NEVER use `var`** — always use explicit types (`csharp_style_var_elsewhere = false:error`).

> **Prefer braces** even on single-line `if` statements.

> **One type per file** — split classes/records into their own `.cs` when adding to existing files.

> **No `IsDefault` (or similar) flags on interface types.** Use explicit registry methods (e.g. `RegisterDefault`) to enforce single-default invariants at the call site, not by inspection.

> **Pattern recognition over hand-rolling.** When the user pushes back with "which design pattern would you use", think about Strategy / Pipeline / Result-type / Discriminated-Union combinations, not monolithic refactor shapes.

When editing any file, prune comments that violate these.

---

## Bugs caught during Phase 2 (cumulative)

### From earlier Phase 2 work:
1. **`StartsWith(string)` analyzer warning** (CA1310) — replaced with explicit `StringComparison.Ordinal`.
2. **`pid.ToString()` analyzer warning** (CA1305) on macOS detector — replaced with `pid.ToString(CultureInfo.InvariantCulture)`.
3. **`CA1859` on `ConsoleLogger logger`** in Composition — promoted to concrete type for perf hint.
4. **Glob semantics** — initial impl anchored `*` against `hostPath` with `[^.]*`. Failed for `*.example.com` (docs require matching bare apex too). Special-cased leading `*.` to `(prefix\.)?` so `*.example.com` matches `example.com`, `www.example.com`, `smile.example.com` (single-level). `**` matches across dots for deep subdomains. Also: `*` excludes `/` (single path segment); use `**` to cross path segments.
5. **`*` matching nothing useful** — `*` is `[^./]*`, which matches only single non-dot non-slash segments. Use `**` to mean "anything". Fixed one test that wrote `UrlMatchesAny = ["*"]` to use `["**"]`.
6. **Rule priority inversion in test config** — GitHub PR rule at priority 190 was lower than the generic work-apps-at-github rule at priority 200, so PRs always hit the generic rule. Bumped GitHub PR to 250 (above work-apps).
7. **JSON deserialization resets `IReadOnlyList<>` defaults to null** — source-gen with init properties overrode `= []` default when the JSON omitted the field. Made `BuildTrackerSet` defensive (`?? []`). Future-proof: `ConfigValidator` uses `?? []` defensively on `config.Profiles`, `config.Rules`, `config.Browsers`.
8. **Embedding `→` arrow in JSON string** — PowerShell mangled the unicode `→` when writing the sample config to $APPDATA. Stripped from the example config (use plain words).

### This session:
9. **CA1716 on `when` parameter name** — `when` is a contextual keyword in C# (since switch expressions). Renamed to `ruleWhen` in `IPredicateMatcher.Matches` and all 8 matcher implementations.
10. **CA1859 in `ProfileResolver`** — `IReadOnlyList<...>` returned by private helper; analyzer wanted concrete `List<...>`. Switched to `detected[0]` index access where possible.
11. **Chain knowledge leak** — `intent.Source == IntentSource.RuleMatch` inside `ResolveOutcome` was resolvers whispering into the executor. Replaced `IntentSource` enum with `NotFoundExitCode` + `NotFoundMessagePrefix` on the intent — set by resolvers, read opaquely by executor.
12. **`PredicateEvaluator.MatchesGlob` removed** — moved glob logic to `GlobMatcher` static class; `GlobToRegex_Cases` test updated to call `GlobMatcher.Matches` directly.
13. **CLI `--verbose --version` routes to RouteCommand** — `args[0]` is `--verbose`, no command registered, falls through to default. Behavior unchanged from Phase 1; not a regression.
14. **CLI tests initially failed in batch** — process spawning inconsistency; resolved by re-running.

---

## Things to know about the user

- C# or Kotlin background — comfortable with both
- Cares about "understanding all the code" — no magic, no heavy frameworks
- Working style: detailed Q&A interview per design decision, one question at a time
- Code-style rules (comment / var / braces / one-class-per-file / no-interface-flags) apply project-wide
- Pattern-recognition preference: when reviewing refactor shapes, the user will push back with "which design pattern(s) would you use" — answer with a mix (Strategy + Pipeline + Result type), not a monolithic shape
- Session preference: don't commit until asked. Single big commit is acceptable for handoff; multi-commit logical groups also welcome.

---

## How to pick up

1. Read this file (`docs/HANDOFF.md`)
2. Skim `docs/decisions-log.md` for context on locked choices (now 54 decisions)
3. Glance at `docs/roadmap.md` for the phase plan
4. Look at the comment rules in memory (`mavis memory show`)
5. Pick a phase: **Phase 3 (Picker UI)** or **Phase 6 (Polish: README, --bench, browser inventory ↔ config `Executable: auto` mapping)**