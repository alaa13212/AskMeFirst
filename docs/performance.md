# Performance — hitting the <3 s budget

The user-perceived experience is: *click link → browser opens with that link*. The budget for everything between click and browser-visible is **3 seconds**; we target **1.5 seconds** internally.

## Where time goes

| Phase | Typical | Our budget |
|---|---|---|
| OS spawns our process | ~5–15 ms | n/a (OS) |
| Process startup (Native AOT) | 30–80 ms | **< 100 ms** |
| CLI arg parsing | 1–5 ms | < 5 ms |
| Config load (JSON, cache hit) | 1–3 ms | < 10 ms |
| Source-app detection (L1) | 1–20 ms | < 25 ms |
| URL parsing + tracking strip | 1–5 ms | < 5 ms |
| Rule evaluation | 1–10 ms | < 15 ms |
| Browser inventory check (cache hit) | 1–10 ms | < 15 ms |
| Running browser detection | 5–50 ms | < 50 ms |
| Browser launch + paint | 200–800 ms | n/a (browser) |
| **Total (our code)** | **~50–200 ms** | **< 250 ms** |
| **Total wall-clock** | **~250–1000 ms** | **< 1500 ms** |

We control everything except the browser's own startup. The browser is the slowest part and we can't fix that — we can only avoid making it slower.

## Strategies, ranked by impact

### 1. Native AOT compilation (biggest win)

.NET 10 Native AOT compiles C# directly to native machine code. No JIT, no runtime initialization, no GC warmup. Result:

- **Cold start: 30–80 ms** for a small CLI (vs 300–800 ms with regular .NET, vs 500 ms – 2 s for JVM)
- Single-file executable — no runtime DLL extraction
- Trimmed — unused BCL code is removed

Build command:
```bash
dotnet publish -c Release -r win-x64 --self-contained -p:PublishAot=true
```

Risks:
- Some reflection-based libraries don't work with trimming. We vet dependencies.
- `System.Text.Json` is AOT-safe (uses source generators for native AOT in .NET 10).
- Avalonia (Phase 7+ picker UI) has AOT caveats; if they bite, the picker build drops AOT (no cold-start concern for picker — short-lived, on demand).

### 2. Lazy everything

Most code paths run only when needed:
- Browser inventory: loaded from cache on cold start. Full scan only on first install or manual refresh.
- Tracking param list: compiled once at config load.
- Regex patterns: compiled at config load (`RegexOptions.Compiled`).
- Config file: parsed only if mtime changed.

### 3. Embedded defaults

A built-in default config is embedded as a resource. If the user's config is missing or corrupt, we fall back to the embedded version without I/O.

```csharp
var configPath = Path.Combine(UserConfigDir, "config.json");
var config = File.Exists(configPath) && IsValid(File.ReadAllText(configPath))
    ? ParseJson(File.ReadAllText(configPath))
    : LoadEmbeddedDefault();
```

### 4. Single-file publish

`dotnet publish -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true` (or `false` — `false` is faster).

### 5. Avoid reflection at startup

Reflection is slow and AOT-unfriendly. Use:
- Source generators where possible (`System.Text.Json` has one)
- Compile-time DI / factory methods (we use a hand-rolled `Composition.cs`)
- Static dispatch tables

### 6. Async unshortening — picker-only, non-blocking

Unshortening requires a network round-trip. Doing it synchronously adds 100 ms – 2 s of latency.

**v1 behavior**: unshortener runs **only when the picker is about to show** AND URL is from a known shortener domain. It fires in a background task; the picker is fully interactive while it runs. Picker UI updates live when unshortening completes. User choice stands regardless of unshortening state.

The browser itself follows redirects natively after we hand it the URL — no browser extension or remote-debugging required.

### 7. Skip the picker when rules suffice

Picker UI costs ~150 ms to show + variable user wait. If a high-priority rule resolves the URL, never show it. The rule engine always tries rules first; picker is the implicit catchall.

### 8. Process spawn optimization

We don't fork — we exit. Each invocation is a new process. Native AOT minimizes the cost.

A daemon (deferred to Phase 8+) could eliminate process spawn via IPC, but Native AOT makes the cost negligible. Stateless CLI is simpler.

## What we will not do

- **No lazy-loaded modules / plugins at startup.** Every byte in the binary is paid for on every cold start.
- **No HTTP client pre-warming.** `HttpClient` is only created on demand (for unshortening, which is async and picker-only).
- **No startup telemetry.** Don't call home on every URL launch — that would be unacceptable latency *and* a privacy violation. (Per [decisions-log.md](./decisions-log.md#23-telemetry-none).)

## Benchmarking

We ship a `--bench` CLI command that runs a representative workload (synthetic URL, rules, cold and warm) and prints timings. This is for development, not shipped by default.

```bash
askmefirst --bench
# Cold start: 47 ms
# Warm start: 8 ms
# Rule eval:   2 ms
# Browser inv: 11 ms (cache hit)
```

CI runs `--bench` and fails if cold start exceeds 100 ms or warm exceeds 25 ms. We will not regress.

## Performance budget table (CI-enforced)

| Metric | Budget | Hard limit |
|---|---|---|
| Binary cold start | < 80 ms | 150 ms |
| Binary warm start | < 15 ms | 30 ms |
| Config load (cached) | < 10 ms | 25 ms |
| Rule evaluation | < 10 ms | 25 ms |
| Browser inventory (cached) | < 15 ms | 50 ms |
| **Total our code** | **< 150 ms** | **300 ms** |

If any metric regresses past its hard limit, CI fails.

## Future optimizations (deferred)

- Pre-warmed daemon holding the rule engine + browser inventory in memory. Router mode becomes a  5 ms IPC call. Worth it only if user volume is high or per-process startup cost is observed to dominate.
- mmap'd config file for truly zero-copy load.
- Native AOT with profile-guided optimization (PGO) in .NET 11+.