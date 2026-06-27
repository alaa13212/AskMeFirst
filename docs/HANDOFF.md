# Handoff — 2026-06-27

> **First thing next session: read this file.**

## TL;DR

Phases 0 and 1 are complete. Phase 0 bootstrapped the .NET 10 + Native AOT toolchain. Phase 1 split the codebase into Core + 3 Platforms projects, shipped a working CLI router (`askmefirst <url> --browser <id>`), discovered real browsers on Windows (registry), macOS (.app scan), and Linux (.desktop parse). **All 25 design decisions still locked** in [`docs/decisions-log.md`](./decisions-log.md). 26 tests passing. AOT binary 2.79 MB, cold start 16-39 ms (well under 80 ms target / 150 ms hard limit).

## Where we are

| Phase | Status | Notes |
|---|---|---|
| 0 — Bootstrap | ✅ Done | Build, test, AOT publish all working |
| 1 — MVP router | ✅ Done | Core + 3 Platforms, real browser discovery, hardcoded pick |
| 2 — Rule engine | ⏭ Next | JSON config rules + predicates + actions |
| 3 — Picker UI | 📋 Planned | Avalonia, two-panel layout |
| 4 — OS integration | 📋 Planned | Win/Mac/Linux registration |
| 5 — Link processing | 📋 Planned | Async unshortener + tracking strip |
| 6 — Polish | 📋 Planned | Bench command, README, examples |

Full plan: [`docs/roadmap.md`](./roadmap.md).

## What was built

### Phase 0 — bootstrap

```
src/AskMeFirst/                          ← single-project Phase 0 CLI
├── AskMeFirst.csproj
├── Program.cs                           ← --version, --help, --bench
└── Properties/PublishProfiles/Aot.pubxml

tests/AskMeFirst.Tests/AskMeFirst.Tests.csproj + CliTests.cs   ← 7 tests
```

### Phase 1 — split into Core + Platforms

```
src/
├── AskMeFirst.Core/                     ← pure BCL, no platform deps
│   ├── AskMeFirst.Core.csproj
│   ├── Models/Browser.cs                ← record(Id, DisplayName, ExecutablePath, Profile?)
│   ├── Abstractions/
│   │   ├── IBrowserInventory.cs
│   │   ├── IUrlLauncher.cs
│   │   └── ILogger.cs                   ← LogInfo/LogWarn/LogError (CA1716-safe names)
│   ├── Logging/ConsoleLogger.cs
│   ├── Config/
│   │   ├── Config.cs                    ← Settings, BrowserSpec, Config records + JsonSourceGenerationOptions
│   │   └── ConfigLoader.cs              ← LoadDefault (embedded) + LoadFromFile
│   ├── Composition/BootstrapContext.cs  ← record(Inventory, Launcher, PlatformName)
│   ├── UrlRouter.cs                     ← hardcoded pick, no rule engine
│   └── Resources/DefaultConfig.jsonc    ← embedded default
│
├── AskMeFirst.Platforms.Windows/        ← real registry-based inventory
│   ├── WindowsBrowserInventory.cs       ← reads HKLM\..\Clients\StartMenuInternet
│   ├── WindowsUrlLauncher.cs            ← Process.Start with UseShellExecute
│   └── WindowsBootstrap.cs
│
├── AskMeFirst.Platforms.MacOs/          ← real .app bundle scan
│   ├── MacOsBrowserInventory.cs         ← checks /Applications/*.app
│   ├── MacOsUrlLauncher.cs              ← /usr/bin/open -a <app> <url>
│   └── MacOsBootstrap.cs
│
├── AskMeFirst.Platforms.Linux/          ← real .desktop file parse
│   ├── LinuxBrowserInventory.cs         ← /usr/share/applications + ~/.local
│   ├── LinuxUrlLauncher.cs              ← exec <path> <url>
│   └── LinuxBootstrap.cs
│
└── AskMeFirst/                          ← thin CLI host
    ├── AskMeFirst.csproj                ← refs all 3 Platforms (cross-compile)
    ├── Program.cs                       ← dispatches --version/--help/--bench/--list/<url>
    ├── CliArgsParser.cs                 ← <url> + --browser + --verbose
    └── Composition.cs                   ← hand-rolled DI: OS check → Bootstrap.Create()

tests/
└── AskMeFirst.Core.Tests/               ← consolidated all tests
    ├── AskMeFirst.Core.Tests.csproj
    ├── Fakes.cs                         ← FakeInventory, FakeLauncher, FakeLogger
    ├── UrlRouterTests.cs                ← 6 tests (hardcoded pick paths + errors)
    ├── ConfigLoaderTests.cs             ← 2 tests (default config shape)
    ├── CliArgsParserTests.cs            ← 10 tests (URL validation, flag parsing)
    └── CliTests.cs                      ← 8 tests (--version, --help, --bench, --list)
```

## What's verified locally

- ✅ `dotnet build` — clean, 0 warnings, 0 errors (5 source projects + 1 test project)
- ✅ `dotnet test` — **26/26** passing in ~0.4 s
- ✅ `dotnet publish -p:PublishProfile=Aot -r win-x64` — produces `askmefirst.exe` **2.79 MB**
- ✅ Cold start: **16.4–39.2 ms** across 10 runs (Phase 0 was 17–25 ms; Phase 1 widened the range but mean is similar)
- ✅ `--list` discovers 3 real browsers on this box (Chrome, Edge, Firefox)
- ✅ `--browser lynx` returns exit code 3 with clear "not found" message
- ✅ No-args prints help to stderr and exits 1
- ✅ Other RIDs (`osx-arm64`, `linux-x64`, `linux-arm64`) only exercised in CI

## Toolchain setup (this machine)

For Native AOT we installed **VS Build Tools 2022 + C++ workload + Windows 11 SDK**:

```
C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\
```

Bootstrapper used: `https://aka.ms/vs/17/release/vs_BuildTools.exe` with args:
```
--quiet --wait --norestart --nocache
--add Microsoft.VisualStudio.Workload.VCTools
--add Microsoft.VisualStudio.Component.VC.Tools.x86.x64
--add Microsoft.VisualStudio.Component.Windows11SDK.22621
```

3.3 minutes, exit 0. **This install is permanent** — no need to redo.

## Style rules (READ THESE — non-obvious)

These are user-level preferences, saved in `~/.mavis/memory/user.md`. The summary:

> **Comments describe WHAT, never WHY/HOW.** No plan-phase references (`// for Phase N`), no requirement-doc references (`// per REQUIREMENTS.md`), no decision-history (`// chose X over Y because Y`), no trivial info (`// calls AddWindowsService()`). Comment on symbols (classes / methods), not on instructions inside methods. Default to no comment.

Also enforced in `.editorconfig`:

> **NEVER use `var`** — always use explicit types (`csharp_style_var_elsewhere = false:error`).
> **Prefer braces** even on single-line `if` statements (`csharp_prefer_braces = true:warning`).

When you edit any file, prune comments that violate these. Phase 0 and Phase 1 were swept clean before this handoff.

## Decisions recap (point to docs/decisions-log.md for full detail)

| # | Pick |
|---|---|
| 1 | Project name: AskMeFirst |
| 2-3 | C# + .NET 10 LTS + Native AOT |
| 4 | No daemon for v1 |
| 5 | Config: JSON (comment-tolerant parser, System.Text.Json source generator) |
| 6 | No app tags |
| 7 | No cross-OS config sync |
| 8 | Rich rule schema (per docs, no `tag_in`) |
| 9 | L1 source-app detection |
| 10 | P2 browser profiles (auto-discovered) |
| 11 | Picker philosophy A — rules-first fallback |
| 12 | DI: hand-rolled composition root |
| 13-15 | OS registration: StartMenuInternet / .app / xdg-mime (all with one-time user prompt) |
| 16 | Picker UI: centered modal, single screen, browser-buttons + remember-radios |
| 17 | Unshortener: picker-only + known shortener + async, no browser extension |
| 18-19 | Built-in default lists (shorteners + tracking params) + user extensions |
| 20-21 | "Just this once" = forever; Esc = nothing opens |
| 22-25 | MIT, no telemetry, unsigned macOS, manual download (Phase 9: package managers) |

## Bugs caught & fixed during Phases 0 + 1

**Phase 0:**
1. Args inconsistency — `dotnet <dll>` vs native exe put user args at different indices. Fixed with `Environment.GetCommandLineArgs()`.
2. CA1707 — silenced via `tests/.editorconfig` (xUnit underscore convention).
3. `WaitForEndProcess` typo — should be `WaitForExit()`.
4. AOT settings leaked into build output — moved to `Properties/PublishProfiles/Aot.pubxml`.

**Phase 1:**
5. `.gitignore` line 15: stray `docs/` entry would have nuked the docs folder from history. Removed.
6. `.gitignore` line 195: `*.pubxml` would have ignored `Aot.pubxml` itself, silently breaking AOT publish. Added explicit exception `!**/Properties/PublishProfiles/*.pubxml`.
7. TFM mismatch: `net10.0-windows` is not referenceable from `net10.0`. Switched all Platforms projects to `net10.0` + `<SupportedOSPlatform>` attribute + runtime `OperatingSystem.IsWindows()` guards. Slight AOT-binary size cost (all 3 Platforms linked in one binary) but enables cross-build AOT from any CI host.
8. `JsonCommentHandling` is in `System.Text.Json`, not `System.Text.Json.Serialization`. Added the missing `using`.
9. CA1716: `ILogger.Error` collides with reserved keyword. Renamed to `LogInfo`/`LogWarn`/`LogError` (matches `Microsoft.Extensions.Logging`).
10. Namespace shadow: `Config` (class) shadowed by `AskMeFirst.Core.Config` (namespace) inside `AskMeFirst.Core.UrlRouter`. Used `using AskMeConfig = AskMeFirst.Core.Config.Config;` alias.
11. JSON snake_case → C# PascalCase: needed `PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower` on the source generator.

## Next session — Phase 2 (Rule engine + source detection)

**Goal**: JSON config with priority-sorted rules, predicates (`process_in`, `url_matches_any`, `url_regex`, `time_between`, `weekday_in`), actions (`browser`, `focus_existing`, `strip_tracking`); plus source-app detection per platform (parent process / NSWorkspace / /proc).

**Tasks**:
1. Promote embedded `DefaultConfig.jsonc` to user-overridable `~/.config/askmefirst/config.json` (XDG-aware lookup).
2. `RuleEngine`: parse rule list, evaluate top-priority-match, fall back to `default_browser_id`.
3. `PredicateEvaluator`: pure-logic dispatch on predicate type → bool.
4. `ISourceAppDetector` per platform (Windows: WMI/parent PID via P/Invoke; macOS: NSWorkspace; Linux: /proc/<pid>/comm of parent).
5. Compose `RuleRouter` wrapping the existing hardcoded `UrlRouter`.
6. CLI: `askmefirst <url>` (no `--browser`) now consults rules + source app.
7. Update `samples/askmefirst.example.json` to drive integration tests.

**Exit criteria**: a config with 10 rules routes correctly via unit tests + manual checks on each OS. Predicates all composable.

## Things to know about the user

- C# or Kotlin background — comfortable with both
- Cares about "understanding all the code" — no magic, no heavy frameworks
- Working style: detailed Q&A interview per design decision, one question at a time
- The comment + var + braces rules in memory apply project-wide

## How to pick up

1. Read this file (`docs/HANDOFF.md`)
2. Skim `docs/decisions-log.md` for context on locked choices
3. Glance at `docs/roadmap.md` for the phase plan
4. Look at the comment rules in memory (`mavis memory show`)
5. Continue with Phase 2 from "Tasks" above
