# Handoff — 2026-06-27

> **First thing next session: read this file.**

## TL;DR

Phase 0 (bootstrap) of AskMeFirst is complete and verified end-to-end, including Native AOT publish on this Windows box. The project is a cross-platform browser router — intercepts OS-level link clicks, picks the right browser by rule (source app + URL pattern), optionally unshortens and strips tracking. **All 25 design decisions are locked** in [`docs/decisions-log.md`](./decisions-log.md). No code beyond CLI scaffolding yet.

## Where we are

| Phase | Status | Notes |
|---|---|---|
| 0 — Bootstrap | ✅ Done | Build, test, AOT publish all working |
| 1 — MVP router | ⏭ Next | Hardcoded decision, no rules yet |
| 2 — Rule engine | 📋 Planned | JSON config + predicates + actions |
| 3 — Picker UI | 📋 Planned | Avalonia, two-panel layout |
| 4 — OS integration | 📋 Planned | Win/Mac/Linux registration |
| 5 — Link processing | 📋 Planned | Async unshortener + tracking strip |
| 6 — Polish | 📋 Planned | Bench command, README, examples |

Full plan: [`docs/roadmap.md`](./roadmap.md).

## What was built (Phase 0)

### Source

```
src/AskMeFirst/
├── AskMeFirst.csproj                  ← framework-dependent by default
├── Program.cs                         ← --version, --help, --bench
└── Properties/PublishProfiles/
    └── Aot.pubxml                     ← AOT settings, applied via -p:PublishProfile=Aot
```

```
tests/AskMeFirst.Tests/
├── AskMeFirst.Tests.csproj
└── CliTests.cs                        ← 7 tests, all green
```

### Config

```
.editorconfig                          ← root style config
tests/.editorconfig                    ← test override (CA1707 silenced for xUnit)
.gitignore .gitattributes              ← standard ignores, LF endings
global.json                            ← .NET 10 SDK pin (10.0.100, rollForward latestFeature)
Directory.Build.props                  ← nullable, warnings-as-errors, lang version, license, etc.
AskMeFirst.slnx
.github/workflows/ci.yml               ← test on 3 OSes + AOT publish for 4 RIDs
README.md  LICENSE                      ← user-facing
samples/askmefirst.example.json        ← example config (JSONC, comments OK)
```

### Docs

10 files in `docs/`:

| File | Purpose |
|---|---|
| `README.md` | Project overview, .NET 10, decisions link |
| `architecture.md` | No daemon, JSON, hand-rolled DI, picker UI v2 |
| `decisions-log.md` | **The 25 locked decisions** — read first when in doubt |
| `language-decision.md` | C# .NET 10 LTS + Native AOT rationale |
| `link-processing.md` | Picker-only async unshortener + domain lists |
| `performance.md` | .NET 10 Native AOT budget breakdown |
| `platform-integration.md` | P2 profiles, L1 source-app, signing deferred |
| `project-structure.md` | Phase 0 layout + PublishProfile pattern |
| `roadmap.md` | 9-phase build plan |
| `rule-engine.md` | Rich schema, `tag_in` dropped, tracking defaults |

## What's verified locally

- ✅ `dotnet build` — clean, 0 warnings, 0 errors
- ✅ `dotnet test` — **7/7** passing in ~1.9s
- ✅ `dotnet publish -p:PublishProfile=Aot -r win-x64` — produces `askmefirst.exe` 1.4 MB
- ✅ Cold start: **17–25 ms** across 10 runs (target: <80 ms; hard limit: 150 ms — well under)
- ✅ `--version`, `--help`, `--bench` all functional

The other RIDs (`osx-arm64`, `linux-x64`, `linux-arm64`) are exercised in CI only — this box is Windows.

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

When you edit any file, prune comments that violate this. Phase 0 was swept clean before this handoff.

## Decisions recap (point to docs/decisions-log.md for full detail)

| # | Pick |
|---|---|
| 1 | Project name: AskMeFirst |
| 2-3 | C# + .NET 10 LTS + Native AOT |
| 4 | No daemon for v1 |
| 5 | Config: JSON (comment-tolerant parser) |
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

## Bugs caught & fixed during Phase 0

1. **Args inconsistency** — `dotnet <dll>` vs native exe put user args at different indices. Fixed by using `Environment.GetCommandLineArgs()`.
2. **CA1707** — silenced via `tests/.editorconfig` (xUnit underscore convention).
3. **`WaitForEndProcess` typo** — should be `WaitForExit()`.
4. **AOT settings leaked into build output** — moved all AOT MSBuild props from `AskMeFirst.csproj` to `Properties/PublishProfiles/Aot.pubxml`. `dotnet build` now produces framework-dependent binary; `dotnet publish -p:PublishProfile=Aot` does the AOT build.

## Next session — Phase 1 (MVP router)

**Goal**: working CLI that takes a URL and launches it in a hardcoded browser. No rules, no picker, no platform integration. Just the core data flow end-to-end.

**Tasks**:
1. Split project into `Core` + `Platforms.{Windows,MacOs,Linux}` (per `docs/project-structure.md` Phase 1+ layout).
2. Implement `IBrowserInventory` per platform (read installed browsers from registry / .app bundles / .desktop files).
3. Implement `IUrlLauncher` per platform (spawn browser with URL).
4. `UrlRouter` orchestrator with hardcoded decision (no rule engine yet).
5. Embedded default config (one browser, "Just this once" default).
6. Basic logging.
7. CLI argument: `askmefirst <url> --browser <id>`.

**Exit criteria**: `askmefirst https://example.com --browser chrome` opens Chrome with the URL on all 3 OSes in < 1 s.

## Things to know about the user

- C# or Kotlin background — comfortable with both
- Cares about "understanding all the code" — no magic, no heavy frameworks
- Working style: detailed Q&A interview per design decision, one question at a time
- The comment rules in memory apply project-wide

## How to pick up

1. Read this file (`docs/HANDOFF.md`)
2. Skim `docs/decisions-log.md` for context on locked choices
3. Glance at `docs/roadmap.md` for the phase plan
4. Look at the comment rules in memory (`mavis memory show`)
5. Continue with Phase 1 from "Tasks" above