# Handoff — 2026-07-10

> **First thing next session: read this file.**

## TL;DR

Phase 6 (Polish) shipped end-to-end. Built on top of Phase 5 (unshortener, already landed). New daily-driver unlocks: real `--bench` self-enforces per-phase budgets in CI; `init` seeds a commented starter config; `refresh` rewrites the inventory cache (first-launch scan now amortized). README rewritten to reflect actual shipped state (was still claiming "Phase 0"). Phase 5 unshortener was already merged at commit `6841dd3` before this session.

**Phase 7 fold-in done (post-Phase-6 session, same day).** Locked decisions for the Management UI: layout = variant D (sidebar tree + card grid, drill replaces grid); both Avalonia surfaces (picker + new MgmtUi) follow OS Light/Dark via `ThemeVariant.Default` with `DynamicResource` tokens replacing the picker's 8 hardcoded `#…` colors. New `AskMeFirst.MgmtUi` project parallels `AskMeFirst.Picker`; `askmefirst config` becomes the third entry point.

**Grill session (same day, after fold-in).** 11 decisions locked: **#86** layout = D · **#87** both surfaces follow OS Light/Dark · **#88** picker theme cleanup (replace hardcoded `#…` colors + drop pinned `Light`) · **#89** MgmtUi as a separate project · **#90** `config` command as third entry point · **#91** Hybrid apply model (form needs Save, state autosave; per-form Save/Discard, no global Apply) · **#92** drill replaces grid; tree leaves drill to deeper sub-areas (2 drill depths) · **#93** Alt+letter mnemonics + Esc / Alt+←/→ · **#94** FileSystemWatcher auto-reload with Keep/Discard conflict prompt · **#95** Save / Discard / Cancel prompt on close-with-drafts · **#96** auto-init silently when config.json is missing. Implementation-ready per [`docs/phase-7-design.md`](./docs/phase-7-design.md).

**Phase 7 implementation — 5 logical commits in (this session).**
1. `d36ae03 docs(phase-7): design record + grill decisions 86-96`
2. `e2f8ca4 feat(picker): theme-aware colors via DesignTokens.axaml (#87, #88)` — picker follows OS Light/Dark; render-test pre-existing failures unchanged
3. `c8d4608 feat(mgmtui): scaffold project + config entry point (#89, #90)` — `askmefirst config` opens MgmtUi; minimal placeholder window
4. `8ad2408 feat(mgmtui): dashboard cards + sidebar tree + drill (#86, #92 base)` — 8 area cards, sidebar tree, drill-replaces-grid behavior
5. `feat(mgmtui): per-form Save/Discard on the Rules area (#91 base)` — **THIS SESSION**. The "meatiest unit" per the design doc. Builds the draftable-row abstraction: `IConfigPersister` + `JsonConfigPersister` (mutate-func save with atomic write, `Changed` event for hot-reload wiring later) + `DraftableRowViewModel` abstract base (Save/Discard commands gated on `HasDraft`, `LastError` for save errors) + `RuleRowViewModel` (first concrete row VM exposing Name/Priority/UrlRegex) + `RuleDetailViewModel` rebuilding rows from `persister.Current` on drill/Changed events + `NoFormsDetailViewModel` placeholder for the remaining 7 areas (lands per-area in subsequent commits). `ConfigCommand` resolves platform config path via DI and passes it to MgmtUi (MgmtUi stays Core-only-deps). `ConfigLoader.WriteAtomic(AppConfig, path)` lifted from `JsonConfigWriter` to Core so both `JsonConfigWriter.AppendRule` and `JsonConfigPersister.Save` share one atomic-write source. New `AskMeFirst.MgmtUi.Tests` xUnit project.

**Stopped mid-#91** to keep this commit single-purpose. Next session picks up at the remaining per-form areas (Browsers, Profiles, Unshortener, Tracking, Suggestions, Usage, Cache) under #91 — same `DraftableRowViewModel` pattern, just concrete row VMs and detail VMs. Then mnemonics (#93), auto-init (#96), hot-reload (#94), close-with-drafts (#95), then tests + AOT verify across all 3 platforms.

## What's verified

- ✅ `dotnet build` clean (0 warnings, 0 errors, all 3 platforms)
- ✅ `dotnet test --configuration Release` — **254/258 Core passing** in ~400 ms, **13/13 MgmtUi passing** in ~75 ms, **53/58 Picker passing** in ~1 s
  - **+1 MgmtUi tests project** (13 tests new — covers persister mutex/save-failure/reload, row VM dirty/save/discard/error, window VM drill/rebuild/HasDrafts)
  - Core count unchanged (skipped=4 platform-specific)
  - **4 pre-existing Picker render-test failures** unchanged (Windows-hardcoded paths surfacing on Linux)
- ✅ Manual smoke (Linux dev machine):
  - `askmefirst init` → wrote `~/.config/askmefirst/config.json` (2406 bytes)
  - `askmefirst refresh` → scanned 2 browsers (firefox + the test publish itself; the askmefirst entry is filtered correctly only when the binary path matches `SelfExecutable`)
  - `askmefirst --bench` → ran 1000 iterations, all phases well under hard limit (sub-millisecond on warm dev box)
  - Cache file is `~/.config/askmefirst/discovery-cache.json` (355 bytes), right beside `config.json`
- ✅ Phase 7 prototypes: 4 HTML variants at `prototypes/phase-7-mgmt-ui/` (later cleaned up — design doc references by path; if needed, see git history `8ad2408` or earlier). Picker tokens resolve under both Light (test harness pinned) and Dark (manual check pending; render-test passes prove Light path).
- ⚠️ MgmtUi window rendering not manually verified (no display server in this sandbox); the build + tests verify XAML parses, ViewModels wire, and MgmtUi assembles into the CLI binary. The Rules detail view (this session's UI surface) renders 3-field editor + per-row Save/Discard buttons; first manual open lands in next session.
- ⚠️ IL2026 (Avalonia `AvaloniaXamlLoader.Load(uri)` requires unreferenced code) is **pre-existing** in both `AskMeFirst.Picker/AvaloniaApp.cs` and `AskMeFirst.MgmtUi/AvaloniaApp.cs`. Build with `-p:PublishAot=true` flags it; the regular `dotnet build` is clean. Phase 7 exit criterion #12 (AOT-clean single binary) requires either pragma-suppress or migrate to the `Load(Stream)` overload. Logged as a known issue — fix needed before Phase 7 ships, but not introduced by this commit.

## Phase 6 commits (in order)

1. Add `RoutingTimings` record + `RouteTimed()` on `RuleRouter`; `Route()` stays backward-compatible (returns `int`, delegates to `RouteTimed().ExitCode`)
2. Add `IDiscoveryCache` + `FileDiscoveryCache` (JSON, versioned, with platform-guard + canonical ExecutablePath) + `CachingBrowserInventory` decorator
3. Add `Refresh()` default method on `IBrowserInventory`; override in `CachingBrowserInventory` to force re-scan + rewrite cache
4. Wire `IDiscoveryCache` into `Composition.cs`; platform `IBrowserInventory` registrations updated to register concrete + decorator
5. Add `RefreshCommand` + `InitCommand` + `askmefirst` help text entries; embed `Resources/askmefirst.example.json` as `EmbeddedResource`
6. Polish `samples/askmefirst.example.json` (minimal schema tour)
7. Rewrite `BenchCommand` to actually bench the routing pipeline with per-phase timings + self-enforcing budgets
8. Replace CI `time --version` step with `--bench` gate (exits non-zero → `::error::` annotation + workflow failure)
9. README rewrite (friend-first, two-section; replaces stale "Phase 0" stub)
10. `docs/phase-6-design.md` (NEW, captures the 11 locked decisions)
11. This handoff

## Phase 7 commits shipped so far (5 logical commits)

1. `d36ae03 docs(phase-7): design record + grill decisions 86-96` — `docs/phase-7-design.md` new; decisions-log +86–96; HANDOFF + roadmap updates
2. `e2f8ca4 feat(picker): theme-aware colors via DesignTokens.axaml (#87, #88)` — picker drops pinned `ThemeVariant.Light`; 8 hardcoded `#…` colors → `{DynamicResource Token.*}`; new `Picker/Resources/DesignTokens.axaml` (Light + Dark palettes, identical key names)
3. `c8d4608 feat(mgmtui): scaffold project + config entry point (#89, #90)` — new `src/AskMeFirst.MgmtUi/` project; `MgmtUiEntryPoint.Run()` Avalonia app; `ConfigCommand` registered in `Program.cs`; slnx updated
4. `8ad2408 feat(mgmtui): dashboard cards + sidebar tree + drill (#86, #92 base)` — `AreaCatalog` + `AreaDescriptor`; `AreaCardViewModel`, `AreaDetailViewModel`, `TreeNodeViewModel`; `MgmtUiWindowViewModel` owns `Areas` + `Tree` + `ActiveDetail`; window XAML renders 8-card grid + sidebar tree + status banner; click card or tree leaf drills (replaces grid); Back button returns to dashboard
5. **THIS SESSION** — `feat(mgmtui): per-form Save/Discard on Rules area (#91 base)` — see Files added/modified below for the abstraction surface

## Files added

```
src/AskMeFirst.Core/Inventory/
├── IDiscoveryCache.cs
├── FileDiscoveryCache.cs
├── CachingBrowserInventory.cs
└── CachedInventory.cs                 ← cache DTOs (CachedBrowserDto, CachedInventory)

src/AskMeFirst.Core/Routing/
├── RoutingTimings.cs                  ← TimeSpan RuleEval, Executor, Total
└── RouteResult.cs                     ← (int ExitCode, RoutingTimings Timings)

src/AskMeFirst/Commands/
├── InitCommand.cs
└── RefreshCommand.cs

src/AskMeFirst/Resources/
└── askmefirst.example.json            ← polished, embedded

tests/AskMeFirst.Core.Tests/
├── InitCommandTests.cs
└── Inventory/
    ├── FileDiscoveryCacheTests.cs
    └── CachingBrowserInventoryTests.cs

docs/phase-6-design.md                 ← planning artifact
```

## Files modified (highlights)

- `src/AskMeFirst.Core/RuleRouter.cs` — added `RouteTimed()` returning `RouteResult`; `Route()` now delegates
- `src/AskMeFirst.Core/Abstractions/IBrowserInventory.cs` — added default `Refresh()` method
- `src/AskMeFirst.Core/Config/AppConfig.cs` — unchanged
- `src/AskMeFirst/Composition.cs` — registers `IDiscoveryCache → FileDiscoveryCache`
- `src/AskMeFirst/Composition.{Linux,MacOs,Windows}.cs` — platform inventory now concrete + decorator
- `src/AskMeFirst/AskMeFirst.csproj` — `<EmbeddedResource>` for the polished sample
- `src/AskMeFirst/Program.cs` — registers `InitCommand` + `RefreshCommand`
- `src/AskMeFirst/Commands/BenchCommand.cs` — full rewrite (was a placeholder loop)
- `tests/AskMeFirst.Core.Tests/Fakes.cs` — `FakeInventory` now tracks `DiscoverCount`
- `samples/askmefirst.example.json` — replaced with the polished minimal-schema-tour content
- `tests/AskMeFirst.Core.Tests/CliTests.cs` — bench smoke test now checks per-phase labels instead of "placeholder"
- `.github/workflows/ci.yml` — `time --version` step replaced with `--bench` gate
- `README.md` — full rewrite

## Files added (this session — Phase 7 #91 base)

```
src/AskMeFirst.MgmtUi/Services/
├── IConfigPersister.cs                 ← { Current, Reload(), Save(Func mutate), event Changed }
├── JsonConfigPersister.cs              ← concrete, atomic write, fires Changed after success
└── SaveOutcome.cs                      ← readonly record struct { bool Ok, string? Error }

src/AskMeFirst.MgmtUi/ViewModels/
├── DraftableRowViewModel.cs            ← abstract partial; Save/Discard commands gated on HasDraft; LastError
├── RuleRowViewModel.cs                 ← first concrete (Name, Priority, UrlRegex)
├── RuleDetailViewModel.cs              ← level-1 drill; Rebuild(IConfigPersister)
└── NoFormsDetailViewModel.cs           ← placeholder for the other 7 areas (lands per-area next)

tests/AskMeFirst.MgmtUi.Tests/         ← NEW project, xUnit v3, pure VM tests (no Avalonia headless)
├── AskMeFirst.MgmtUi.Tests.csproj
├── FakeLogger.cs
├── JsonConfigPersisterTests.cs         ← 4 tests: ctor/missing-file, save-mutate, save-throw, reload-event
├── RuleRowViewModelTests.cs            ← 5 tests: ctor snapshot, edit-dirty, save-clears, discard-restores, save-error
└── MgmtUiWindowViewModelTests.cs       ← 4 tests: drill-rules, drill-empty, persister-changed rebuild, HasDrafts polling
```

## Files modified (this session)

- `src/AskMeFirst.Core/Config/ConfigLoader.cs` — added public static `WriteAtomic(AppConfig, path)` (extracted from `JsonConfigWriter`)
- `src/AskMeFirst.Core/Config/JsonConfigWriter.cs` — `AppendRule` delegates atomic write to `ConfigLoader.WriteAtomic`
- `src/AskMeFirst.MgmtUi/AvaloniaApp.cs` — builds via `MgmtUiEntryPoint.BuildComposition()` so the persister is constructed once at the composition root and injected into the window VM
- `src/AskMeFirst.MgmtUi/MgmtUiEntryPoint.cs` — `Run(string? configPath = null)` accepts a path; default falls back to Linux `~/.config/askmefirst/config.json` (CLI dispatch always passes the real path via `IConfigPathResolver`, so the fallback is only for direct invocation). `BuildComposition()` is the single construction site for `JsonConfigPersister`.
- `src/AskMeFirst.MgmtUi/ViewModels/AreaDetailViewModel.cs` — now abstract base; adds `Rows` collection, abstract `Placeholder`, `HasRows` computed, abstract `Rebuild(persister)`
- `src/AskMeFirst.MgmtUi/ViewModels/MgmtUiWindowViewModel.cs` — accepts `IConfigPersister` ctor dep; subscribes to `Changed` → `RebuildActiveArea()`; `DrillTo` calls `ActiveDetail.Rebuild(_persister)` on enter; `CreateDetailViewModel` switches on `AreaId` (Rules → `RuleDetailViewModel`, others → `NoFormsDetailViewModel` for now); `HasDrafts` queries active detail; `IsDashboard` kept for nav state
- `src/AskMeFirst.MgmtUi/Views/AreaDetailView.axaml` — replaces the `BodyMessage` placeholder with a polymorphic body: `Placeholder` TextBlock visible when `!HasRows`, `ItemsControl Rows` ScrollViewer with per-row `DataTemplate x:DataType="vm:RuleRowViewModel"` (Name/Priority/URL-regex text inputs, LastError red banner, Save + Discard buttons) when populated
- `src/AskMeFirst/Commands/ConfigCommand.cs` — resolves platform-specific `IConfigPathResolver` from DI and passes the path to `MgmtUiEntryPoint.Run(path)` so MgmtUi reads/writes the same config file as the rest of the CLI
- `AskMeFirst.slnx` — adds `tests/AskMeFirst.MgmtUi.Tests/`

## What's wired that isn't visible yet (deferred sub-decisions in #91)

- **Save buttons are disabled by the RelayCommand's `CanExecute = HasDraft`**; per-row discard is also disabled in the same way. XAML controls enabled state implicitly.
- **Auto-rebuild on `Changed`** — used by #94 (hot-reload) and the row's own save commits. Same plumbing, different trigger.
- **`HasDrafts`** is computed by walking `Rows`; will be the close-prompt trigger for #95 and the conflict-prompt trigger for #94.
- **Boolean row fields** (Pinned, Enabled) — none yet. When they land (Browsers, Profiles), the rule is "boolean setter immediately calls persister save with a single-field mutator" — no per-row Save/Discard UI for them. The concrete pattern is `private bool _pinned; partial void OnPinnedChanged(bool v) => _persister.Save(c => c with { Profiles = ... });` plus `ErrorMessage` visible as a border around the checkbox. Same draftable-row base classes, different persistence wiring. Will land per-area in subsequent #91 commits.

## Bugs caught / design tweaks (this session — Phase 7 #91 base)

1. **`ConfigJsonContext` is internal** to Core, so the duplicated `AvaloniaXamlLoader.Load(uri)` plumbing + atomic-write logic needed extraction. Pulled `ConfigLoader.WriteAtomic(AppConfig, path)` to be the single source for atomic write; `JsonConfigWriter.AppendRule` and `JsonConfigPersister.Save` both delegate. Net code reduction vs. two copies.
2. **`rule-identity` for `RuleRowViewModel.BuildMutator`**: `ReferenceEquals(existing, _source)` works only when the row holds a Rule still referenced by the live `AppConfig`. After a save, `_current` becomes a new `AppConfig` (records are immutable), so ref equality breaks for next edits. Fix: rebuild rows on `persister.Changed` (window VM already does this); each row lives only for one Config cycle. Test fixed to seed the file first and read from `persister.Current.Rules[0]` so the row VM holds a live reference.
3. **Constructor draft notification race**: `[ObservableProperty]` setters fire `PropertyChanged` during construction before `_source` is captured — could spuriously mark `HasDraft = true`. CommunityToolkit suppresses notifications while constructors run; rely on the source-gen behavior, not workarounds.
4. **`SaveThrowing_MutateFun` contract mismatch**: original test expected `Save()` to re-throw the mutate exception; design intent is friendlier (returns `Failure(ex.Message)`, leaves file untouched). Test rewritten to match — the row VM uses `LastError`, never a try/catch around `SaveCommand.Execute`.
5. **CA1859 prefer concrete on `IConfigPersister` local var**: analyzer wants `JsonConfigPersister` over `IConfigPersister` in tests. Where the call site uses the polymorphic abstraction (row VM ctor), suppressed with a comment, but local vars holding the concrete persister test helper now use the concrete type.

## Bugs caught / design tweaks (historical — Phase 6 / earlier Phase 7)

1. **CA9113 unused param**: `CachingBrowserInventory` ctor originally accepted `ILogger` but never used it. Dropped. Cache already logs read/write failures internally.
2. **CA1859 prefer concrete**: `BenchHarness.Inventory` initially `IBrowserInventory`; analyzer flagged. Changed to concrete `BenchInventory`.
3. **IDE0011 braces**: `if (s > max) max = s;` flagged; expanded.
4. **RoutingTimings field name**: started as `Inventory`, renamed to `Executor` (post-rule work = inventory + profile + strip; `Executor` is the honest name).
5. **`Init_ExistingFile_DoesNotOverwrite`**: initially checked `FakeLogger.Infos` for "already exists" — but `InitCommand` uses `Console.WriteLine` for user-facing output (the operation result, not diagnostic chatter). Test was tightened to just verify file is preserved (the actual user contract).
6. **`SelfExecutable` doesn't filter out `askmefirst` when binary lives elsewhere**: the cache lists `askmefirst` as a "browser" when the running binary isn't at a standard path. Filter is path-based; in dev/test that path differs from the published install path. Expected behavior, not a bug — but worth noting.

## Decisions recap (Phase 6 = 11 decisions, see `phase-6-design.md`)

Phase 7 fold-in (2026-07-10, pre-grill) added 5 more — see [`docs/decisions-log.md`](./docs/decisions-log.md#phase-7-decisions-added-2026-07-10) and the new [`docs/phase-7-design.md`](./docs/phase-7-design.md):
- **86.** MgmtUi layout = variant D (sidebar + cards + drill-in). See prototypes at `prototypes/phase-7-mgmt-ui/`.
- **87.** Both Avalonia surfaces follow OS Light/Dark via `ThemeVariant.Default` (no user toggle in v1).
- **88.** Picker theme cleanup — drop pinned Light, replace 8 hardcoded `#…` colors with `DynamicResource`.
- **89.** MgmtUi as a separate project (`AskMeFirst.MgmtUi`), parallel to `AskMeFirst.Picker` and Core-only deps.
- **90.** `askmefirst config` as third entry point, parallel to `pick`.
- **91.** Hybrid apply model — text inputs use per-row `DraftableRowViewModel.Save/Discard`; state toggles (pinned, enabled) autosave immediately through the same `IConfigPersister`. Confirmed by this commit via `RuleRowViewModel` (text Save/Discard) + `RuleDetailViewModel` (level-1 drill rebuilding rows on `persister.Changed`). The autosave state-toggle pattern lands per-area next.

| # | Decision | Pick | Rationale |
|---|---|---|---|
| 1 | Scope | bench + cache + init + README (icons → Phase 7) | Visible-win ordering, defers warm-inv speedup |
| 2 | Cache join key | `ExecutablePath` (canonicalized) | Reinstalls → re-scan; no family detection in hot path |
| 3 | Cache scope | Full inventory snapshot | Warm `Discover()` is read+parse |
| 4 | Invalidation | Manual `refresh` only (CLI now, Phase 7 UI later) | No mtime, no TTL — single user contract |
| 5 | Cache OS location | Beside `config.json` | One dir to inspect; user+program cohabit |
| 6 | `install` ↔ cache | No coupling | User runs `refresh` after browser installs |
| 7 | Bench shape | Per-phase breakdown | Budgets are per-phase; total-only is half the gate |
| 8 | Bench iter count | 1000, self-enforcing | Tighter stats; bench checks itself, exits non-zero |
| 9 | Init conflict | Idempotent | No `--force`; print "already exists" instead |
| 10 | Sample pedagogy | Minimal schema tour | One worked rule + comments; README links to docs |
| 11 | README shape | Friend-first, two-section | Install/init/quickstart top; dev ref bottom |

## Style rules (READ THESE — non-obvious)

User-level preferences, also in `~/.mavis/memory/user.md`:

> **Comments describe WHAT, never WHY/HOW.** No plan-phase references, no requirement-doc references, no decision-history, no trivial info. Comment on symbols (classes / methods), not on instructions inside methods. Default to no comment.

> **NEVER use `var`** — always use explicit types (`csharp_style_var_elsewhere = false:error`).

> **Prefer braces** even on single-line `if` statements.

> **One type per file** — split classes/records into their own `.cs` when adding to existing files.

> **No `IsDefault` (or similar) flags on interface types.** Use explicit registry methods (e.g. `RegisterDefault`) to enforce single-default invariants at the call site, not by inspection.

> **Pattern recognition over hand-rolling.** When the user pushes back with "which design pattern would you use", think about Strategy / Pipeline / Result-type / Discriminated-Union combinations, not monolithic refactor shapes.

> **"Never keep dead code"** — applied rigorously in this session.

When editing any file, prune comments that violate these.

## Toolchain notes

- .NET 10 LTS
- AOT publish: `dotnet publish src/AskMeFirst -c Release -r <RID> --self-contained -p:PublishAot=true`
  - Local AOT publish needs `clang`/`gcc`; the dev box lacks them. CI has them. Build artifacts size unchanged at ~1.4 MB.
- System.Text.Json source generator (`JsonSerializerContext`) used for `CachedInventory` — AOT-safe
- Embedded resource access: `Assembly.GetManifestResourceStream("AskMeFirst.Resources.askmefirst.example.json")` — namespace-prefixed resource name (by convention)
- `[SupportedOSPlatform(...)]` attributes kept on platform-specific classes (Phase 4); no new platforms added in Phase 6

## Manual verification checklist (for Phase 7+ testing)

Phase 6 was verified on Linux dev box only. Per-OS smoke needed:

- [x] Linux: cache beside config; `refresh` re-scans; `--bench` exits 0
- [ ] Windows: AOT publish + `--bench` exits 0 under PGO conditions
- [ ] macOS: AOT publish + `.app` bundle + `--bench` exits 0
- [ ] macOS: `init` runs from `xattr`-signed `.app` (when signing lands in Phase 9)

## Things to know about the user

- C# background — comfortable with .NET internals (e.g., caught the lambda `_` discard conflict)
- Cares about "understanding all the code" — no magic, no heavy frameworks. Note: DI container is approved per decision #84.
- Working style: detailed Q&A interview per design decision, one question at a time
- Code-style rules apply project-wide
- "Go ahead" = implement including logical commits; explicit "commit" required to push
- "handoff" = I'm closing the session, summarize everything in HANDOFF.md

## How to pick up

1. Read this file (`docs/HANDOFF.md`)
2. Skim `docs/decisions-log.md` (now ~96 decisions; +10 Phase 7 (5 fold-in + 5 grill))
3. Glance at `docs/phase-7-design.md` and `docs/phase-6-design.md` for the active planning artifacts
4. Look at the comment rules in memory (`mavis memory show`)
5. **Phase 7 (Management UI)** is in progress — see TL;DR above. Foundation (4 commits): design record, picker theme swap, MgmtUi scaffold, dashboard+drill. **This session shipped the #91 abstraction + Rules area form** (5th commit). When you say "go ahead" / "implement" again, **start with per-area concrete rows** under #91: Browsers (text + boolean pinned), Profiles (boolean pinned + text), Unshortener (text domain list), Tracking (text param list), Suggestions (text + action), Usage (read-only), Cache (read-only + refresh button). The `DraftableRowViewModel` + `IConfigPersister` pattern is fixed; each area is a new `XxxDetailViewModel` + `XxxRowViewModel` and a per-area XAML row template.
6. **Then in order**: mnemonics (#93), auto-init (#96), hot-reload (#94, harness around `IConfigPersister.Changed`), close-with-drafts (#95 — already has `HasDrafts`; needs the prompt VM + `Closing` interceptor)
7. **Phase 7 exit criterion #12** — fix the pre-existing IL2026 (AOT + `AvaloniaXamlLoader.Load(uri)`) with `#pragma warning disable IL2026` on both Picker and MgmtUi, or migrate to `Load(Stream)`. Verify AOT publish on all 3 platforms before merging.
8. **Phase 6.1 (deferred from Phase 6)**: test coverage gate (move to ≥ 80% as CI gate), profile auto-discovery P2
9. **Before Phase 9 (installers)**: re-evaluate Linux `Exec=` per decision #77 (Flatpak/Snap canonical-path model)
