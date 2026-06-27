# Handoff — 2026-06-28

> **First thing next session: read this file.**

## TL;DR

Phases 0 and 1 complete. Phase 1 went through several rounds of polish and one structural refactor on top of the original MVP router:

- `ICommand` abstraction: each CLI verb is its own class implementing `ICommand`. `CommandRegistry` dispatches by name/alias, with explicit `RegisterDefault` for the dispatch fallback.
- Per-command arg parsing: `CliArgsParser` and `CliArgs` are gone. Each command parses its own args. The dispatcher passes the full `userArgs` to every command.
- Dynamic help: `HelpFormatter` reads from `CommandRegistry.All()` and renders column-aligned help with no hardcoded text.
- Browser entity carries a launch strategy: `IBrowserLaunchStrategy` decides which CLI args to pass. Chromium uses `--profile-directory=<dir>`, Firefox uses `-P <name>`, default is url-only. The launcher is now ~10 lines — just spawn the process.
- Profile detection: `IBrowserProfileDetector` + `BrowserProfile`. Windows/Mac/Linux all detect Chrome, Edge, Firefox, Brave, Chromium where applicable. `--profile <name>` flows end-to-end.
- Cleanup: `Config` class renamed to `AppConfig` (namespace shadow). Settings use `TimeSpan`. JSON dropped snake_case → PascalCase. All static `Regex` are `[GeneratedRegex]`. Windows/Linux `NormalizeId` are dictionary lookups. Firefox registry hash suffix (`Firefox-308046B0AF4A39CB`) is stripped via regex.
- `ICommand.IsDefault` is gone (bad practice — see "Decisions" below). The registry holds the default, not the command.

**62/62 tests passing.** AOT binary **3.30 MB**, cold start **19–26 ms**.

## Where we are

| Phase | Status | Notes |
|---|---|---|
| 0 — Bootstrap | ✅ Done | Build, test, AOT publish all working |
| 1 — MVP router | ✅ Done + polished | ICommand architecture, profile detection, browser-family launch strategies |
| 2 — Rule engine | ⏭ Next | JSON config rules + predicates + actions + source-app detection |
| 3 — Picker UI | 📋 Planned | Avalonia, two-panel layout |
| 4 — OS integration | 📋 Planned | Win/Mac/Linux registration |
| 5 — Link processing | 📋 Planned | Async Unshortener + tracking strip |
| 6 — Polish | 📋 Planned | Bench command, README, examples |

Full plan: [`docs/roadmap.md`](./roadmap.md).

## Project tree

```
src/
├── AskMeFirst.Core/                          ← pure BCL, no platform deps
│   ├── AskMeFirst.Core.csproj
│   ├── Models/
│   │   ├── Browser.cs                        ← Id, DisplayName, ExecutablePath, LaunchStrategy, Profile?
│   │   └── BrowserProfile.cs                 ← Name, DirectoryName, IsDefault
│   ├── Abstractions/
│   │   ├── IBrowserInventory.cs
│   │   ├── IBrowserProfileDetector.cs
│   │   ├── IBrowserLaunchStrategy.cs         ← BuildArguments(Uri, BrowserProfile?)
│   │   ├── IUrlLauncher.cs
│   │   └── ILogger.cs
│   ├── Logging/ConsoleLogger.cs
│   ├── Launch/                               ← browser-family arg strategies
│   │   ├── ChromiumLaunchStrategy.cs         ← --profile-directory=<dir>
│   │   ├── FirefoxLaunchStrategy.cs          ← -P <name>
│   │   ├── DefaultLaunchStrategy.cs
│   │   └── BrowserLaunchStrategies.cs        ← factory by browser id
│   ├── Profiles/FirefoxProfilesParser.cs     ← shared profiles.ini parser
│   ├── Config/
│   │   ├── AppConfig.cs                      ← (was Config.cs)
│   │   ├── Settings.cs                       ← TimeSpan fields
│   │   ├── BrowserSpec.cs
│   │   ├── ConfigJsonContext.cs              ← PascalCase (no naming policy)
│   │   └── ConfigLoader.cs
│   ├── Commands/                             ← command infrastructure
│   │   ├── ICommand.cs                       ← Name / Aliases / Usage / Description / Execute
│   │   ├── CommandRegistry.cs                ← Register / RegisterDefault / Resolve / All
│   │   ├── CommandNotFoundException.cs
│   │   └── HelpFormatter.cs                  ← dynamic help from registry
│   ├── Composition/BootstrapContext.cs
│   ├── UrlRouter.cs                          ← honors Settings.DefaultBrowserId + profile resolution
│   └── Resources/DefaultConfig.jsonc         ← PascalCase keys, ISO-8601 TimeSpan
│
├── AskMeFirst.Platforms.Windows/
│   ├── WindowsBrowserInventory.cs            ← partial, dictionary-driven, Firefox hash strip
│   ├── WindowsBrowserProfileDetector.cs
│   ├── WindowsUrlLauncher.cs                 ← ~10 lines; calls LaunchStrategy.BuildArguments
│   └── WindowsBootstrap.cs
│
├── AskMeFirst.Platforms.MacOs/
│   ├── MacOsBrowserInventory.cs
│   ├── MacOsBrowserProfileDetector.cs
│   ├── MacOsUrlLauncher.cs                   ← /usr/bin/open -a ... --args <strategyArgs>
│   └── MacOsBootstrap.cs
│
├── AskMeFirst.Platforms.Linux/
│   ├── LinuxBrowserInventory.cs              ← partial, dictionary + [GeneratedRegex]
│   ├── LinuxBrowserProfileDetector.cs
│   ├── LinuxUrlLauncher.cs                   ← ~10 lines; calls LaunchStrategy.BuildArguments
│   └── LinuxBootstrap.cs
│
└── AskMeFirst/                               ← thin CLI host
    ├── ProgramInfo.cs                        ← Version + ExecutableName (one place)
    ├── Program.cs                            ← tiny dispatcher (~70 lines)
    ├── Composition.cs                        ← Bootstrap(verbose, registry) builds CommandContext
    ├── CliArgsException.cs                   ← thrown by command parsers
    └── Commands/
        ├── VersionCommand.cs
        ├── HelpCommand.cs                    ← HelpFormatter.Render(ctx.Registry)
        ├── BenchCommand.cs
        ├── ListCommand.cs                    ← shows profiles per browser
        ├── RouteCommand.cs                   ← default; RouteArgs, ParseArgs
        └── RouteArgs.cs                      ← (Url, BrowserId?, ProfileName?, Verbose)

tests/
└── AskMeFirst.Core.Tests/
    ├── Fakes.cs                              ← TestBrowser.Make + FakeInventory/Launcher/Logger/ProfileDetector
    ├── UrlRouterTests.cs                     ← 12 tests
    ├── ConfigLoaderTests.cs                  ← 2 tests
    ├── RouteCommandTests.cs                  ← 9 tests (replaces CliArgsParserTests)
    ├── HelpFormatterTests.cs                 ← 8 tests (incl. RegisterDefault_Twice_Throws)
    ├── LaunchStrategyTests.cs                ← 17 tests (per-strategy + factory)
    └── CliTests.cs                           ← 8 tests (--version, --help, --bench, --list, errors)
```

## Architecture highlights

### Command pattern — `ICommand` + `CommandRegistry`

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
    public CommandRegistry RegisterDefault(ICommand command);  // throws on double-default
    public ICommand Resolve(string? firstArg);
    public IReadOnlyList<ICommand> All();
}
```

Each command parses its own args. The dispatcher (`Program.Main`) probes only `--verbose`/`-v` globally (needed for logger setup), then passes the full `userArgs` to the matched command.

### Per-browser launch strategy

```csharp
public interface IBrowserLaunchStrategy
{
    string[] BuildArguments(Uri url, BrowserProfile? profile);
}

public sealed record Browser
{
    public required string Id { get; init; }
    public required string DisplayName { get; init; }
    public required string ExecutablePath { get; init; }
    public IBrowserLaunchStrategy LaunchStrategy { get; init; } = DefaultLaunchStrategy.Instance;
    public BrowserProfile? Profile { get; init; }
}
```

- **Chromium** (Chrome/Edge/Brave/Opera/Vivaldi/Arc): `--profile-directory=<dir>`
- **Firefox**: `-P <name>` (uses profile **Name**, not directory path)
- **Default** (unknown / no profile support): url only

Inventories assign the right strategy when constructing the `Browser`. Launchers just spawn the process with the strategy's args.

### Profile detection

`IBrowserProfileDetector.Detect(browserId) -> IReadOnlyList<BrowserProfile>`:

| Browser  | Windows                                                  | Mac                                                  | Linux                                              |
|----------|----------------------------------------------------------|------------------------------------------------------|----------------------------------------------------|
| Chrome   | `%LOCALAPPDATA%\Google\Chrome\User Data\*`               | `~/Library/Application Support/Google/Chrome/*`      | `~/.config/google-chrome/*`                        |
| Edge     | `%LOCALAPPDATA%\Microsoft\Edge\User Data\*`              | `~/Library/Application Support/Microsoft Edge/*`     | `~/.config/microsoft-edge/*`                       |
| Brave    | (registry; same as Chrome path layout)                   | (Mac finds `.app`)                                   | `~/.config/BraveSoftware/Brave-Browser/*`          |
| Chromium | —                                                        | —                                                    | `~/.config/chromium/*`                             |
| Firefox  | `%APPDATA%\Mozilla\Firefox\profiles.ini`                 | `~/Library/Application Support/Firefox/profiles.ini` | `~/.mozilla/firefox/profiles.ini`                  |

Firefox INI parsing is shared via `FirefoxProfilesParser` in `Core/Profiles/`.

`UrlRouter.ResolveProfile`:
- no `--profile` → default profile (or first discovered if no default marker)
- `--profile X` → exact match on Name or DirectoryName
- profile not found → warn and fall back to default

## Decisions recap (point to `docs/decisions-log.md` for full detail)

| # | Pick |
|---|---|
| 1 | Project name: AskMeFirst |
| 2-3 | C# + .NET 10 LTS + Native AOT |
| 4 | No daemon for v1 |
| 5 | Config: JSON (comment-tolerant, PascalCase, ISO-8601 durations) |
| 6 | No app tags |
| 7 | No cross-OS config sync |
| 8 | Rich rule schema (no `tag_in`) |
| 9 | L1 source-app detection |
| 10 | P2 browser profiles (auto-discovered) |
| 11 | Picker philosophy A — rules-first fallback |
| 12 | DI: hand-rolled composition root |
| 13-15 | OS registration: StartMenuInternet / .app / xdg-mime |
| 16 | Picker UI: centered modal, single screen |
| 17 | Unshortener: picker-only + known shortener + async |
| 18-19 | Built-in default lists + user extensions |
| 20-21 | "Just this once" = forever; Esc = nothing opens |
| 22-25 | MIT, no telemetry, unsigned macOS, manual download |

### Decisions added this session

| # | Pick | Why |
|---|---|---|
| 26 | `ICommand` per-command parsing; no central parser | Single-responsibility — each command knows its own grammar. Removes the implicit coupling between RouteCommand and the rest of the CLI. |
| 27 | `RegisterDefault` separate from `Register` | A boolean `IsDefault` flag on the command type lets two commands both claim default → invalid state. `RegisterDefault` enforces single-default. |
| 28 | Dispatcher passes full `userArgs`; no slicing | Uniform command contract. Each command decides what to do with `args[0]`. |
| 29 | `IBrowserLaunchStrategy` per browser family | Different browsers use different profile flags (`--profile-directory=` vs `-P <name>`). The browser entity owns this; the platform launcher stays thin. |
| 30 | `Browser.Profile` typed as `BrowserProfile?` | Strategies need both `Name` and `DirectoryName`. A bare string loses the distinction. |
| 31 | `HelpFormatter` in Core (not CLI project) | Reusable from any future host (GUI, daemon). Tests can exercise it without the CLI project. |
| 32 | `Config` class renamed to `AppConfig` | The class lived in `AskMeFirst.Core.Config` namespace — every usage needed `using AskMeConfig = ...Config;` alias to avoid the shadow. Renaming the class removes the alias workaround. |
| 33 | JSON config drops snake_case → PascalCase | No naming policy on the source generator. JSON keys = C# property names verbatim. `PropertyNameCaseInsensitive` retained so both casings parse. |
| 34 | Static `Regex` → `[GeneratedRegex]` partials | AOT-compatible (no runtime allocation), faster first match. All 7 regex across 3 files converted. |
| 35 | `NormalizeId` uses dictionary lookup, not chained `switch` / `Contains` | Easier to extend, less code, no chain fall-through bugs. |

## What's verified locally

- ✅ `dotnet build` — clean, 0 warnings, 0 errors (6 source projects + 1 test project)
- ✅ `dotnet test` — **62/62** passing in ~0.7 s
- ✅ `dotnet publish -p:PublishProfile=Aot -r win-x64` — produces `askmefirst.exe` **3.30 MB**
- ✅ Cold start: **19–26 ms** across 10 runs (Phase 1 baseline was 16–39 ms)
- ✅ `--list` discovers 3 real browsers with profiles:
  ```
  firefox      Mozilla Firefox          C:\Program Files\Mozilla Firefox\firefox.exe
      * Profiles/vc4ak1jq.Barrak-1706255686136 Barrak
        Profiles/0m6kw70o.Work Work
  chrome       Google Chrome            C:\...\chrome.exe
        Profile 6 / Profile 7 / Profile 8
  edge         Microsoft Edge           C:\...\msedge.exe
      * Default / Default
  ```
- ✅ `askmefirst https://example.com --browser chrome --profile "Profile 7" --verbose` → `chrome.exe --profile-directory=Profile 7 <url>` + `[profile: Profile 7]` log
- ✅ `askmefirst https://example.com --browser firefox --profile "Work"` → `firefox.exe -P Work <url>`
- ✅ Unknown `--profile "nope"` → warn + fall back to default profile
- ✅ Unknown `--browser "lynx"` → exit 3 with discovered list
- ✅ `--not-a-real-flag` → exit 1 with "Unknown flag" error (caught by RouteCommand)
- ✅ No-args → prints help to stderr and exits 1
- ✅ `--help` is dynamically generated from the registry

## Style rules (READ THESE — non-obvious)

User-level preferences, also in `~/.mavis/memory/user.md`:

> **Comments describe WHAT, never WHY/HOW.** No plan-phase references, no requirement-doc references, no decision-history, no trivial info. Comment on symbols (classes / methods), not on instructions inside methods. Default to no comment.

> **NEVER use `var`** — always use explicit types (`csharp_style_var_elsewhere = false:error`).

> **Prefer braces** even on single-line `if` statements.

> **One type per file** — split classes/records into their own `.cs` when adding to existing files.

> **No `IsDefault` (or similar) flags on interface types.** Use explicit registry methods (e.g. `RegisterDefault`) to enforce single-default invariants at the call site, not by inspection.

When you edit any file, prune comments that violate these.

## Bugs caught during this session

1. **`UrlRouter` warning CS9113 (unread `config`)** — promoted to error. Fixed by making `Route` actually consult `appConfig.Settings.DefaultBrowserId` when no `--browser` arg is passed. Added test `Route_ConfiguredDefaultBrowser_UsedWhenNoBrowserArg`.
2. **Firefox `Mozilla-308046B0AF4A39CB` hash** — Mozilla writes a per-install hash to `HKLM\..\Clients\StartMenuInternet\Firefox-<hash>`. **Not just your setup** — universal. Fixed by a `[GeneratedRegex]` that strips `-HHHHHHHHHHHHHHHH` suffix before lookup.
3. **`Config` namespace shadow** — `AskMeFirst.Core.Config` (namespace) shadowed the `Config` class. Resolved by renaming the class to `AppConfig` (eliminating the `AskMeConfig = ...` alias).
4. **`WindowsBrowserInventory` missing `partial`** — adding `[GeneratedRegex]` requires the class to be partial. Fixed.
5. **CA1859 on profile detectors** — `IReadOnlyList<...>` return types on private helpers internally build `List<...>`. Changed to `List<...>` (public surface stays `IReadOnlyList<...>` via interface).
6. **Firefox hash regex anchored `^...$`** — initial regex matched the whole registry name and stripped it to empty. Loosened to `-[\dA-F]{16}$` so only the suffix is stripped.
7. **`[info] platform: ...` printing on every command** — `Composition.Bootstrap` was writing platform info unconditionally. Moved into `RouteCommand.Execute` so `--version` / `--help` / `--list` stay clean.
8. **`CommandRegistry.Register` lost auto-detect for `IsDefault`** — during refactor. `_default` stayed null → every unknown arg threw `CommandNotFoundException` instead of falling through to RouteCommand. Restored, then later removed entirely in favor of explicit `RegisterDefault`.

## Next session — Phase 2 (Rule engine + source detection)

**Goal**: JSON config with priority-sorted rules, predicates (`ProcessIn`, `UrlMatchesAny`, `UrlRegex`, `TimeBetween`, `WeekdayIn`), actions (`Browser`, `Profile`, `FocusExisting`, `StripTracking`); plus source-app detection per platform (parent process / NSWorkspace / /proc).

**Tasks**:
1. Promote embedded `DefaultConfig.jsonc` to user-overridable `~/.config/askmefirst/config.json` (XDG-aware lookup).
2. `RuleEngine`: parse rule list, evaluate top-priority-match, fall back to `Settings.DefaultBrowserId`.
3. `PredicateEvaluator`: pure-logic dispatch on predicate type → bool.
4. `ISourceAppDetector` per platform (Windows: WMI/parent PID via P/Invoke; macOS: NSWorkspace; Linux: `/proc/<pid>/comm` of parent).
5. Compose `RuleRouter` wrapping the existing `UrlRouter`. The URL command's `Execute` becomes: `RuleRouter.Evaluate(url, sourceApp) → Browser`.
6. CLI: `askmefirst <url>` (no `--browser`) now consults rules + source app.
7. Update `samples/askmefirst.example.json` to drive integration tests.
8. Consider `--install` / `--uninstall` commands (new `ICommand` files — trivial now).

**Exit criteria**: a config with 10 rules routes correctly via unit tests + manual checks on each OS. Predicates all composable.

## Things to know about the user

- C# or Kotlin background — comfortable with both
- Cares about "understanding all the code" — no magic, no heavy frameworks
- Working style: detailed Q&A interview per design decision, one question at a time
- Code-style rules (comment / var / braces / one-class-per-file / no-interface-flags) apply project-wide
- Session preference: don't commit until asked. Review-friendly single big commit was OK for this handoff, but multi-commit logical groups are also welcome.

## How to pick up

1. Read this file (`docs/HANDOFF.md`)
2. Skim `docs/decisions-log.md` for context on locked choices (now 35 decisions)
3. Glance at `docs/roadmap.md` for the phase plan
4. Look at the comment rules in memory (`mavis memory show`)
5. Continue with Phase 2 from "Tasks" above