# Handoff — 2026-06-27 (Phase 1 cleanup + profile detection)

> **First thing next session: read this file.**

## TL;DR

Phases 0 and 1 complete. Phase 1 was extended with a round of polish from user feedback:

- `Program.cs` now dispatches via `ICommand` (one class per command, no central if/else).
- Settings use `TimeSpan` instead of `*Hours` / `*Ms`.
- JSON config dropped snake_case — PascalCase keys throughout (`DefaultBrowserId`, `StripTracking`, etc.).
- All static `Regex` moved to `[GeneratedRegex]` source generators.
- Windows + Linux `NormalizeId` use dictionary lookups, not chained `Contains` / `switch`.
- **Firefox hash bug fixed**: `Firefox-308046B0AF4A39CB` (Mozilla's per-install hash) now resolves to plain `firefox` / `Mozilla Firefox`.
- Profile detection shipped: `IBrowserProfileDetector` + `BrowserProfile`, Windows / Mac / Linux all implemented, `--profile <name>` flag wired end-to-end.
- AOT binary grew 2.79 → 3.27 MB; cold start still 18–36 ms.

**34/34 tests passing**, AOT builds for `win-x64` clean.

## Where we are

| Phase | Status | Notes |
|---|---|---|
| 0 — Bootstrap | ✅ Done | Build, test, AOT publish all working |
| 1 — MVP router | ✅ Done + polished | ICommand, TimeSpan, PascalCase, GeneratedRegex, profile detection |
| 2 — Rule engine | ⏭ Next | JSON config rules + predicates + actions |
| 3 — Picker UI | 📋 Planned | Avalonia, two-panel layout |
| 4 — OS integration | 📋 Planned | Win/Mac/Linux registration |
| 5 — Link processing | 📋 Planned | Async Unshortener + tracking strip |
| 6 — Polish | 📋 Planned | Bench command, README, examples |

Full plan: [`docs/roadmap.md`](./roadmap.md).

## What was built

### Project tree

```
src/
├── AskMeFirst.Core/                          ← pure BCL, no platform deps
│   ├── AskMeFirst.Core.csproj
│   ├── Models/
│   │   ├── Browser.cs                        ← record(Id, DisplayName, ExecutablePath, Profile?)
│   │   └── BrowserProfile.cs                 ← record(Name, DirectoryName, IsDefault)
│   ├── Abstractions/
│   │   ├── IBrowserInventory.cs
│   │   ├── IBrowserProfileDetector.cs        ← NEW
│   │   ├── IUrlLauncher.cs
│   │   └── ILogger.cs                        ← LogInfo/LogWarn/LogError
│   ├── Logging/ConsoleLogger.cs
│   ├── Profiles/FirefoxProfilesParser.cs     ← NEW shared INI parser
│   ├── Config/
│   │   ├── Config.cs                         ← Config record
│   │   ├── Settings.cs                       ← TimeSpan fields
│   │   ├── BrowserSpec.cs
│   │   ├── ConfigJsonContext.cs              ← PascalCase (no naming policy)
│   │   └── ConfigLoader.cs
│   ├── Commands/                             ← NEW
│   │   ├── ICommand.cs                       ← Name / Aliases / Usage / Description / IsDefault / Execute
│   │   ├── CommandRegistry.cs                ← name+alias → ICommand dispatch
│   │   └── CommandNotFoundException.cs
│   ├── Composition/BootstrapContext.cs       ← record(Inventory, Launcher, Profiles, PlatformName)
│   ├── UrlRouter.cs                          ← honors Settings.DefaultBrowserId + profile resolution
│   └── Resources/DefaultConfig.jsonc         ← PascalCase keys, TimeSpan strings
│
├── AskMeFirst.Platforms.Windows/
│   ├── WindowsBrowserInventory.cs            ← partial, dictionary-driven, Firefox hash strip
│   ├── WindowsBrowserProfileDetector.cs      ← NEW (Chrome/Edge/Firefox)
│   ├── WindowsUrlLauncher.cs                 ← --profile-directory= when Profile is set
│   └── WindowsBootstrap.cs
│
├── AskMeFirst.Platforms.MacOs/
│   ├── MacOsBrowserInventory.cs
│   ├── MacOsBrowserProfileDetector.cs        ← NEW (Chrome/Edge/Firefox)
│   ├── MacOsUrlLauncher.cs                   ← /usr/bin/open -a ... --args --profile-directory= <url>
│   └── MacOsBootstrap.cs
│
├── AskMeFirst.Platforms.Linux/
│   ├── LinuxBrowserInventory.cs              ← partial, dictionary + [GeneratedRegex]
│   ├── LinuxBrowserProfileDetector.cs        ← NEW (Chromium + Firefox)
│   ├── LinuxUrlLauncher.cs                   ← --profile-directory= when Profile is set
│   └── LinuxBootstrap.cs
│
└── AskMeFirst/                               ← thin CLI host
    ├── ProgramInfo.cs                        ← Version + ExecutableName (one place)
    ├── Program.cs                            ← tiny dispatcher over ICommand
    ├── Composition.cs                        ← Bootstrap() builds logger/inventory/launcher/profiles/config
    ├── CliArgs.cs                            ← record(Url, BrowserId, ProfileName, Verbose)
    ├── CliArgsException.cs
    ├── CliArgsParser.cs                      ← <url> + --browser + --profile + --verbose
    └── Commands/
        ├── VersionCommand.cs
        ├── HelpCommand.cs
        ├── BenchCommand.cs
        ├── ListCommand.cs                    ← shows profiles per browser
        └── RouteCommand.cs                   ← IsDefault = true, parses CliArgs, runs UrlRouter

tests/
└── AskMeFirst.Core.Tests/
    ├── Fakes.cs                              ← + FakeProfileDetector
    ├── UrlRouterTests.cs                     ← 12 tests (added profile matching, default-config)
    ├── ConfigLoaderTests.cs                  ← 2 tests
    ├── CliArgsParserTests.cs                 ← 12 tests (added --profile)
    └── CliTests.cs                           ← 8 tests
```

## What changed in this session

### 1. `ICommand` abstraction

`Program.Main` went from a 110-line if/else chain to a 30-line dispatcher. Each command is its own class implementing `ICommand`:

```csharp
public interface ICommand
{
    string Name { get; }
    IReadOnlyList<string> Aliases => [];
    bool IsDefault => false;
    string Usage => Name;
    string Description => "";
    int Execute(string[] args, CommandContext ctx);
}
```

`CommandRegistry` resolves by name or alias, falls back to the `IsDefault` command (the `RouteCommand`). Adding `--install`, `--uninstall`, `askmefirst config` later is now a single new file under `Commands/`.

### 2. `TimeSpan` for durations

`Settings.InventoryCacheHours` (int) and `Settings.UnshortenTimeoutMs` (int) are gone. Now:

```csharp
public TimeSpan InventoryCacheTtl { get; init; } = TimeSpan.FromHours(24);
public TimeSpan UnshortenTimeout { get; init; } = TimeSpan.FromMilliseconds(1000);
```

JSON serialization uses ISO-8601 duration format automatically (`"1.00:00:00"`, `"00:00:01"`).

### 3. PascalCase JSON

`ConfigJsonContext` no longer applies `JsonKnownNamingPolicy.SnakeCaseLower`. Default = no policy = JSON keys match C# property names. `PropertyNameCaseInsensitive = true` is kept so both casings work, but the canonical form is now PascalCase.

Updated files: `DefaultConfig.jsonc`, `samples/askmefirst.example.json`, and all doc examples in `rule-engine.md`, `link-processing.md`, `architecture.md`, `roadmap.md`.

### 4. `[GeneratedRegex]` everywhere

`LinuxBrowserInventory` and `FirefoxProfilesParser` no longer allocate `Regex` instances at runtime. Each static regex is a partial method with the source generator. Same for the new Windows Firefox hash-suffix regex.

### 5. Dictionary-based normalization

`LinuxBrowserInventory.NormalizeId` now checks a `Dictionary<string, string>` keyed on the `.desktop` file's stem (`google-chrome`, `firefox-esr`, `microsoft-edge`, `vivaldi-stable`, etc.). Falls back to display-name substring matching only if the dict misses.

`WindowsBrowserInventory.NormalizeId` and `NormalizeDisplayName` use two dictionaries. Both first run `StripFirefoxHash` to strip Mozilla's `-HHHHHHHHHHHHHHHH` suffix from the registry key name, so `Firefox-308046B0AF4A39CB` becomes plain `Firefox`.

### 6. Profile detection

`IBrowserProfileDetector.Detect(browserId) -> IReadOnlyList<BrowserProfile>`.

| Browser  | Windows                                                  | Mac                                                       | Linux                                              |
|----------|----------------------------------------------------------|-----------------------------------------------------------|----------------------------------------------------|
| Chrome   | `%LOCALAPPDATA%\Google\Chrome\User Data\*` (Default, Profile N dirs) | `~/Library/Application Support/Google/Chrome/*`           | `~/.config/google-chrome/*`                        |
| Edge     | `%LOCALAPPDATA%\Microsoft\Edge\User Data\*`              | `~/Library/Application Support/Microsoft Edge/*`          | `~/.config/microsoft-edge/*`                        |
| Brave    | (same as Chrome registry key, different root)            | (same as Chrome, different root)                          | `~/.config/BraveSoftware/Brave-Browser/*`          |
| Chromium | —                                                        | —                                                         | `~/.config/chromium/*`                             |
| Firefox  | `%APPDATA%\Mozilla\Firefox\profiles.ini`                 | `~/Library/Application Support/Firefox/profiles.ini`      | `~/.mozilla/firefox/profiles.ini`                  |

Firefox parsing is a shared `FirefoxProfilesParser` in `Core/Profiles/` so all three platforms use the same INI logic.

`UrlRouter.ResolveProfile` takes a browser + optional profile name and returns a `Browser` with the resolved `Profile` set:
- no `--profile` → default profile, falling back to first discovered
- `--profile X` → exact match on name or directory, warn-and-fallback if not found
- no profiles detected → pass through unchanged (allows non-profile browsers)

### 7. `--profile` end-to-end

`askmefirst <url> --browser chrome --profile "Profile 7"` works. The Windows launcher passes `--profile-directory=Profile 7` to chrome.exe. The Mac launcher uses `/usr/bin/open -a ... --args --profile-directory=Profile 7`. Linux passes the same `--profile-directory=` flag directly.

### 8. Firefox `Mozilla-308046B0AF4A39CB` — was it just your machine?

No, it's universal. Mozilla uses a per-install hash suffix on the `Clients\StartMenuInternet` registry key for Firefox to avoid collisions across reinstalls and per-user installs. Same behavior on retail machines. The fix is regex-driven and now covered by tests.

## What's verified locally

- ✅ `dotnet build` — clean, 0 warnings, 0 errors (6 source projects + 1 test project)
- ✅ `dotnet test` — **34/34** passing in ~0.7 s
- ✅ `dotnet publish -p:PublishProfile=Aot -r win-x64` — produces `askmefirst.exe` **3.27 MB**
- ✅ Cold start: **18.4–36.4 ms** across 10 runs (Phase 1 baseline was 16–39 ms)
- ✅ `--list` discovers 3 real browsers, all with profiles:
  ```
  firefox      Mozilla Firefox          C:\Program Files\Mozilla Firefox\firefox.exe
      * Profiles/vc4ak1jq.Barrak-... Barrak
        Profiles/0m6kw70o.Work        Work
  chrome       Google Chrome            C:\...\chrome.exe
        Profile 6 / Profile 7 / Profile 8
  edge         Microsoft Edge           C:\...\msedge.exe
      * Default / Default
  ```
- ✅ `askmefirst https://example.com --browser chrome --profile "Profile 7" --verbose` logs `[profile: Profile 7]` and opens Chrome with the right profile
- ✅ Unknown `--profile "nope"` logs a warn, falls back to default profile (Profile 6 in user's setup)
- ✅ Unknown `--browser` returns exit code 3 with the discovered list
- ✅ `--no-such-flag` returns exit code 1 with usage hint

## Style rules (READ THESE — non-obvious)

User-level preferences, also in `~/.mavis/memory/user.md`:

> **Comments describe WHAT, never WHY/HOW.** No plan-phase references, no requirement-doc references, no decision-history, no trivial info. Comment on symbols (classes / methods), not on instructions inside methods. Default to no comment.

Also enforced in `.editorconfig`:

> **NEVER use `var`** — always use explicit types.
> **Prefer braces** even on single-line `if` statements.

**One type per file** — split classes/records into their own `.cs` when adding to existing files.

## Decisions recap (point to `docs/decisions-log.md` for full detail)

| # | Pick |
|---|---|
| 1 | Project name: AskMeFirst |
| 2-3 | C# + .NET 10 LTS + Native AOT |
| 4 | No daemon for v1 |
| 5 | Config: JSON (comment-tolerant parser, PascalCase) |
| 6 | No app tags |
| 7 | No cross-OS config sync |
| 8 | Rich rule schema (no `tag_in`) |
| 9 | L1 source-app detection |
| 10 | P2 browser profiles (auto-discovered) |
| 11 | Picker philosophy A — rules-first fallback |
| 12 | DI: hand-rolled composition root |
| 13-15 | OS registration: StartMenuInternet / .app / xdg-mime (all with one-time user prompt) |
| 16 | Picker UI: centered modal, single screen |
| 17 | Unshortener: picker-only + known shortener + async |
| 18-19 | Built-in default lists + user extensions |
| 20-21 | "Just this once" = forever; Esc = nothing opens |
| 22-25 | MIT, no telemetry, unsigned macOS, manual download |

## Bugs caught & fixed during this session

1. **`UrlRouter` warning CS9113 (unread `config`)** — promoted to error by the build config. Fixed by making `Route` actually consult `config.Settings.DefaultBrowserId` when no `--browser` arg is passed. Added a test (`Route_ConfiguredDefaultBrowser_UsedWhenNoBrowserArg`).
2. **Firefox `Mozilla-308046B0AF4A39CB` hash** — root-cause: Mozilla writes a per-install hash to the StartMenuInternet registry key. Fixed by a `[GeneratedRegex]` that strips `-HHHHHHHHHHHHHHHH` suffix before lookup. Result: `firefox` / `Mozilla Firefox`.
3. **`Config` namespace shadow** — `AskMeFirst.Core.Config` (namespace) clashed with the `Config` property in `CommandContext`. Same shadow showed up in `UrlRouter` and tests. Fixed with `using AskMeConfig = AskMeFirst.Core.Config.Config;` alias.
4. **`WindowsBrowserInventory` missing `partial`** — adding `[GeneratedRegex]` requires the class to be partial. Fixed.
5. **CA1859 on profile detectors** — the build flagged `IReadOnlyList<BrowserProfile>` return types on private helpers that internally build a `List<...>`. Changed to `List<BrowserProfile>` (the public surface stays `IReadOnlyList<...>` via the interface).
6. **Regex anchored `^...$`** — initial Firefox-hash regex matched the whole registry name and stripped it to empty. Loosened to `-[\dA-F]{16}$` so only the suffix is stripped.
7. **`[info] platform: ...` printing on every command** — `Composition.Bootstrap` was writing platform info unconditionally. Moved the log into `RouteCommand.Execute` so `--version` / `--help` / `--list` stay clean.

## Next session — Phase 2 (Rule engine + source detection)

**Goal**: JSON config with priority-sorted rules, predicates (`ProcessIn`, `UrlMatchesAny`, `UrlRegex`, `TimeBetween`, `WeekdayIn`), actions (`browser`, `FocusExisting`, `StripTracking`); plus source-app detection per platform (parent process / NSWorkspace / /proc).

**Tasks**:
1. Promote embedded `DefaultConfig.jsonc` to user-overridable `~/.config/askmefirst/config.json` (XDG-aware lookup).
2. `RuleEngine`: parse rule list, evaluate top-priority-match, fall back to `Settings.DefaultBrowserId`.
3. `PredicateEvaluator`: pure-logic dispatch on predicate type → bool.
4. `ISourceAppDetector` per platform (Windows: WMI/parent PID via P/Invoke; macOS: NSWorkspace; Linux: /proc/<pid>/comm of parent).
5. Compose `RuleRouter` wrapping the existing `UrlRouter`.
6. CLI: `askmefirst <url>` (no `--browser`) now consults rules + source app.
7. Update `samples/askmefirst.example.json` to drive integration tests.
8. Consider adding `--install` / `--uninstall` commands (new `ICommand` files — trivial now).

**Exit criteria**: a config with 10 rules routes correctly via unit tests + manual checks on each OS. Predicates all composable.

## Things to know about the user

- C# or Kotlin background — comfortable with both
- Cares about "understanding all the code" — no magic, no heavy frameworks
- Working style: detailed Q&A interview per design decision, one question at a time
- Code-style rules (comment / var / braces / one-class-per-file) apply project-wide

## How to pick up

1. Read this file (`docs/HANDOFF.md`)
2. Skim `docs/decisions-log.md` for context on locked choices
3. Glance at `docs/roadmap.md` for the phase plan
4. Look at the comment rules in memory (`mavis memory show`)
5. Continue with Phase 2 from "Tasks" above