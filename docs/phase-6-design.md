# Phase 6 — Polish design

**Status**: 📋 Planning locked
**Source**: grill session 2026-07-10
**Phase 6 (roadmap line)**: `--bench`, README, samples, polish. Phase 5 (unshortener) already shipped at commit `6841dd3`.

## Scope (with cuts applied)

Shipped in Phase 6:

- ✅ **Real `--bench` command** — per-phase timings, 1000 iterations, self-enforces budgets, CI gate replaces cold-start-via-`time` step
- ✅ **Inventory cache** — full discovered-browsers + profiles snapshot, ExecutablePath-canonicalized identity, manual refresh only
- ✅ **`askmefirst init` command** — writes the polished sample to OS config path; idempotent on existing config
- ✅ **Polished sample** (`samples/askmefirst.example.json`) — minimal schema tour
- ✅ **README rewrite** — friend-first two-section, replaces stale "Phase 0" stub

Deferred:

- ❌ **Browser inventory cache TTL knob** — `settings.InventoryCacheTtl` is in the sample schema but ignored; manual refresh is the only invalidation trigger
- ❌ **Inventory cache per-platform dir split** — cache lives beside `config.json`, not in OS cache dir
- ❌ **`install` / `uninstall` ↔ cache refresh coupling** — neither command touches the cache; user runs `askmefirst refresh` manually
- ❌ **Embedded browser icons in picker** — slides to Phase 7 alongside the Management UI
- ❌ **Test coverage > 80 % gate** — defer to 6.1; existing 296-test suite has no coverage number, gate would be a release-prep task

## Exit criteria

1. `askmefirst --bench` prints per-phase timings (`cold_config_load`, `cold_rule_eval`, `cold_inventory`, `warm_total`, …) and exits non-zero if any per-phase hard-limit budget from `docs/performance.md` is breached.
2. `askmefirst --bench` runs in CI on all three platforms; the build fails if budget breached. Existing `time` cold-start step at `.github/workflows/ci.yml:82-85` is replaced.
3. First CLI invocation of any URL-family command runs a full inventory scan, writes `discovery-cache.json` beside `config.json` in the OS-standard config dir.
4. Subsequent warm invocations read `discovery-cache.json`, skip the scan entirely.
5. `askmefirst refresh` runs the inventory scan, rewrites the cache file, prints a summary line, exits 0.
6. `askmefirst init` writes `samples/askmefirst.example.json` (polished) to the OS config path if absent; if present, prints a "config already exists at <path>" line and exits 0.
7. README describes install + init + route + pick + list + uninstall + refresh + bench end-to-end on all three OSes.

## Decisions (from the grill)

| # | Decision | Pick | Rationale |
|---|---|---|---|
| 1 | Phase 6 scope | bench + cache + init + README | Picker icons slide to Phase 7 (which is already the management UI). Inventory cache stays. |
| 2 | Cache join key | `ExecutablePath` (canonicalized) | Reinstalls → re-scan, acceptable. No family detection in hot path. |
| 3 | Cache scope | Full inventory snapshot | Warm `Discover()` is a read+parse. `auto` resolution is a separate small match step. |
| 4 | Invalidation | Manual refresh only | No mtime check, no TTL. `askmefirst refresh` is the user contract. |
| 5 | Cache OS location | Beside `config.json` | Single dir to inspect; user-edit + program-managed cohabit. |
| 6 | `install` ↔ cache | No coupling | `install`/`uninstall` leave cache untouched; user runs `refresh`. |
| 7 | Bench shape | Per-phase breakdown | Budgets are per-phase; gate with non-phased signal is half the value. |
| 8 | Bench iteration count | 1000, self-enforcing | Tighter stats; ~10 sec CI cost. Bench checks itself, exits non-zero. |
| 9 | Init conflict behavior | Idempotent | No `--force`. Existing config: print path + "run `askmefirst refresh` to reset the discovery cache, or edit the config directly". |
| 10 | Sample pedagogy | Minimal schema tour | One worked rule + comments per section. README links to `docs/rule-engine.md` for vocabulary. |
| 11 | README shape | Friend-first, two-section | Single README; install/init/quickstart at top, dev reference at bottom. |

## Architecture

### Inventory cache

```
OS spawn
  ├─ Read IConfig (config.json)
  ├─ Read IDiscoveryCache (discovery-cache.json)  ← returns IReadOnlyList<Browser> + Profiles
  ├─ If absent / corrupt / unreadable:
  │     → RealBrowserInventory.Discover() → IReadOnlyList<Browser>
  │     → WriteDiscoveryCache(result)
  └─ If present and parses: use cached result
```

The cache wraps `IBrowserInventory`. Two interfaces:

```csharp
public interface IDiscoveryCache
{
    IReadOnlyList<Browser>? TryRead();
    void Write(IReadOnlyList<Browser> browsers);
    DateTimeOffset? LastGenerated { get; }
}

public sealed class FileDiscoveryCache(
    string cacheFilePath,
    ILogger logger) : IDiscoveryCache
{
    // Implementation: read/write <configDir>/discovery-cache.json
}
```

`IBrowserInventory` interface stays; a new `CachingBrowserInventory` decorates the real one. `CachingBrowserInventory.Discover()` reads cache first; on miss, calls the inner `Discover()` and writes.

### `RefreshCommand`

```csharp
public sealed class RefreshCommand : ICommand
{
    public string Name => "refresh";
    public string Usage => "refresh";

    public Task<int> Execute(string[] args, CommandContext ctx)
    {
        IBrowserInventory inventory = ctx.Resolve<IBrowserInventory>();
        IReadOnlyList<Browser> browsers = inventory.Refresh();
        Console.WriteLine($"Cache refreshed: {browsers.Count} browser(s).");
        return Task.FromResult(0);
    }
}
```

`RefreshCommand` runs the inventory scan, writes the cache, prints summary, exits 0. No flags. Errors → log + non-zero.

### `InitCommand`

```csharp
public sealed class InitCommand : ICommand
{
    public string Name => "init";
    public string Usage => "init";

    public Task<int> Execute(string[] args, CommandContext ctx)
    {
        IConfigPathResolver paths = ctx.Resolve<IConfigPathResolver>();
        string configPath = paths.DefaultConfigPath;
        if (File.Exists(configPath))
        {
            Console.WriteLine($"Config already exists at {configPath}.");
            Console.WriteLine($"Run '{ProgramInfo.ExecutableName} refresh' to reset the discovery cache, or edit the config directly.");
            return Task.FromResult(0);
        }
        string? dir = Path.GetDirectoryName(configPath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        string sample = LoadEmbeddedSample();   // assembly resource
        File.WriteAllText(configPath, sample);
        Console.WriteLine($"Wrote {configPath}");
        return Task.FromResult(0);
    }
}
```

Sample is embedded as a `Resources/askmefirst.example.json` resource (project `<EmbeddedResource>`). `InitCommand` writes it as-is to the OS path.

### Bench — per-phase timings

`RuleRouter.Route` returns a `RouteResult(int ExitCode, RoutingTimings Timings)` record. Production callers (e.g. `RouteCommand`) read `.ExitCode`; bench reads the timings. No `RouteTimed()` shim.

```csharp
public sealed record RoutingTimings(
    TimeSpan RuleEval,
    TimeSpan InventoryLoad,
    TimeSpan Total);

public sealed record RouteResult(int ExitCode, RoutingTimings Timings);

public sealed class RuleRouter
{
    public RouteResult Route(Uri url, string? explicitBrowserId, string? explicitProfileId);
}
```

The bench command:

1. Times `BenchHarness.Build(logger)` once as `cold_config_load` (one-time setup; config load is a startup cost, not per-route).
2. Runs `Route(syntheticUrl)` 1000 times with the harness warmed up, aggregating per-phase timings as `cold_rule_eval`, `cold_inventory`, `warm_total`.
3. Prints `cold_config_load` as a single `sample`, the other three as `p50 / p95 / max`.
4. Checks against budgets from `docs/performance.md`:

| Phase | Budget (ms) | Hard limit (ms) |
|---|---|---|
| cold_config_load | 10 | 25 |
| cold_rule_eval | 10 | 25 |
| cold_inventory | 15 | 50 |
| warm_total | 50 | 100 |

If any phase's hard limit breached → exit 1 with `BENCH FAIL: <phase> = <ms>ms > <hard_limit>ms` line.

Test: `Bench_PrintsPerPhaseTimingsAndExitsZero` smoke test verifies exit code + that the four phase names appear in stdout.

## Files added

```
src/AskMeFirst.Core/Inventory/
├── IDiscoveryCache.cs                       ← new
├── FileDiscoveryCache.cs                    ← new
├── CachingBrowserInventory.cs               ← new decorator
├── CachedBrowserDto.cs                      ← one record per file
└── CachedInventory.cs                       ← envelope

src/AskMeFirst.Core/Routing/
├── RoutingTimings.cs                        ← (RuleEval, InventoryLoad, Total)
└── RouteResult.cs                           ← one record per file

src/AskMeFirst/Commands/
├── InitCommand.cs                           ← new
└── RefreshCommand.cs                        ← new

src/AskMeFirst/Resources/
└── askmefirst.example.json                  ← polished sample (embedded)

tests/AskMeFirst.Core.Tests/Inventory/
├── FileDiscoveryCacheTests.cs
└── CachingBrowserInventoryTests.cs

tests/AskMeFirst.Core.Tests/
├── InitCommandTests.cs
└── SampleConfigTests.cs                     ← drift guard: samples/ == Resources/
```

## Files modified

- `src/AskMeFirst.Core/RuleRouter.cs` — `Route()` returns `RouteResult` directly; `Stopwatch`-based timings inline
- `src/AskMeFirst.Core/Abstractions/IBrowserInventory.cs` — added `Refresh()` default method
- `src/AskMeFirst/Program.cs` — register `InitCommand` + `RefreshCommand` in `CommandRegistry`
- `src/AskMeFirst/Composition.cs` (+ Win/Mac/Linux partials) — register `IDiscoveryCache → FileDiscoveryCache`, `IBrowserInventory → CachingBrowserInventory(real, cache)`
- `src/AskMeFirst/AskMeFirst.csproj` — embed `askmefirst.example.json` as resource
- `samples/askmefirst.example.json` — polished schema-tour version (kept as sibling file; `SampleConfigTests` enforces byte equality with the embedded copy)
- `src/AskMeFirst/Commands/BenchCommand.cs` — replaced placeholder with real benchmark reading `RoutingTimings`, comparing budgets; reports `cold_config_load / cold_rule_eval / cold_inventory / warm_total`
- `.github/workflows/ci.yml` — replaced the `time`-of-`--version` step with `--bench` and `::error::` annotation on non-zero exit
- `tests/AskMeFirst.Core.Tests/CliTests.cs` — `Bench_PrintsPerPhaseTimingsAndExitsZero` smoke checks phase labels
- `README.md` — full rewrite to friend-first two-section shape
- `docs/HANDOFF.md` — refresh after Phase 6 ships
- `docs/roadmap.md` — tick Phase 6 exit-criteria lines

## Cache file format

`discovery-cache.json` schema:

```json
{
  "version": 1,
  "generatedAt": "2026-07-10T15:30:00+00:00",
  "browsers": [
    {
      "id": "chromium-x7q1",
      "displayName": "Chromium",
      "executablePath": "/usr/bin/chromium-browser",
      "iconName": "chromium",
      "flatpakAppId": null
    }
  ]
}
```

JSON, no comments (program-managed; user inspects but doesn't edit). Strict UTF-8, no whitespace. `version` field enables future schema migration. No `platform` field — cache files are local to one machine (per Q12 note).

Per-browser `profiles` is **deferred to Phase 6.1** (Chromium/Firefox profile enumeration belongs with the management UI work). `RefreshCommand` reports `N browsers` only.

## Style rules

(No change from `~/.mavis/memory/user.md` — confirmed by reading user preference file.)

- No `var`
- Braces on single-line `if`
- One type per file
- Comments describe WHAT, not WHY/HOW
- No dead code
- No `IsDefault`-style flags on interfaces

## Test strategy

- **`FileDiscoveryCacheTests`**: roundtrip, corrupt-JSON recovery, missing-file, empty-list roundtrip, `LastGenerated` populated. 5 tests.
- **`CachingBrowserInventoryTests`**: cache hit, cache miss + write, cached across calls, `FindById` after cache warm. 4 tests.
- **`InitCommandTests`**: writes file when absent, creates missing parent directory, doesn't overwrite existing file. 3 tests.
- **`BenchCommandTests`** (`CliTests`): smoke test — exit 0, all four phase names in stdout (`cold_config_load`, `cold_rule_eval`, `cold_inventory`, `warm_total`).
- **`SampleConfigTests`**: byte-equal assert that `samples/askmefirst.example.json` matches the embedded `Resources/askmefirst.example.json` (drift guard).

Total Phase 6 tests: ~14 new (Cache + Init + Bench + Sample). Cumulative: ~256 (from ~238 before Phase 6).

## Notes applied post-grill (Q12 user notes)

The user clarified two constraints that override earlier over-spec'd bits:

- **No backward compatibility is required** — the app is not yet released. Where the sketch added a `RouteTimed()` shim to keep `Route()` returning `int`, the implementation changes `Route()` to return `RouteResult(int ExitCode, RoutingTimings Timings)` directly. All call sites update. Likewise `IDiscoveryCache.Invalidate()` (no caller) is dropped from the interface entirely.
- **Config is not shareable or platform-portable** — the cache file is local to one machine. The `Platform` field and the cross-platform rejection in `FileDiscoveryCache.TryRead` are dropped; the cache holds what the engine happens to write.

## Verification (Phase 6 done = true)

```bash
# Local
dotnet build
dotnet test
# Manual on each OS:
./publish/<RID>/askmefirst --bench       # exits 0, prints phases
askmefirst refresh                       # writes cache
askmefirst init                          # writes config
cat $(askmefirst init --print-path 2>/dev/null || echo $ASKMEFIRST_CONFIG)  # confirm

# CI (after push)
gh run watch                              # bench step green on all 3 platforms
```
