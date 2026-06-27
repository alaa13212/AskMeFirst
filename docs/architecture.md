# Architecture

## High-level shape

AskMeFirst is a **single binary** with multiple modes. There is no daemon — every decision happens in a short-lived CLI invocation.

```
askmefirst <url>                              # router mode (OS spawns this on link click)
askmefirst --version
askmefirst --help
askmefirst install                            # one-time setup per OS
askmefirst uninstall
askmefirst config                             # post-MVP: opens management UI in webview
askmefirst suggest                            # post-MVP: reads usage log, proposes rules
```

The binary is the same in all modes — mode is selected by CLI flags.

## Components

### 1. CLI entry (`Cli/`)

- Parses argv (minimal hand-rolled parser — no library dep)
- Routes to the right handler based on flags
- **Fast path**: the URL-router branch does the absolute minimum work before delegating

### 2. URL pre-processor (`LinkProcessor/`)

- Parses the URL into `scheme`, `host`, `path`, `query`, `fragment`
- **Tracking param stripping** (synchronous, local): removes known trackers using a configurable list. ~µs per URL.
- **Unshortener** (asynchronous, picker-only): runs only when the picker is about to show AND the URL is from a known shortener domain. Fires in background, picker UI displays live state.

### 3. Rule Engine (`RuleEngine/`)

- Loads rules from the user's JSON config (default location per OS).
- Compiles all URL patterns once at load time. Re-evaluates only when config mtime changes.
- Evaluates rules in **priority order** (highest first), first match wins.
- Inputs: `{ source_process (normalized), url, parsed_url, running_browsers, installed_browsers }`.
- Outputs: `{ browser_id, profile?, focus_existing?, new_window?, private?, strip_tracking? }`.
- If no rule matches → show picker (implicit catchall).
- See [rule-engine.md](./rule-engine.md) for the full format.

### 4. Browser Manager (`BrowserManager/`)

- **Inventory**: discovers installed browsers + their profiles (P2 — auto-discovered from each browser's standard locations). Cached to disk with a TTL (default 24 h). Forced refresh on config change or via CLI flag.
- **Runtime**: detects which browsers are currently running.
- **Launcher**: launches the chosen browser with the chosen URL, optionally with a profile argument (Chrome `--profile-directory=Default`, Firefox `-P work`).
- **Focus**: if the chosen browser is already running, route the URL to the existing instance instead of spawning a new one.

### 5. Picker UI (`Picker/`, Phase 7+)

- Cross-platform Avalonia window — **not in v1**. Phases 1–6 are CLI-only with deterministic rule resolution.
- Centered modal, ~720×440 px. Single screen.
- **Browser pane**: list of (browser, profile) options as buttons. Click or Enter commits immediately.
- **Remember pane** (right side): radio buttons. Default = "Just this once." Click or Enter changes state only.
- Keyboard: arrows, Tab to switch panels, 1-N hotkeys, Enter commits, Esc cancels.
- Shows source app, original URL, and live-resolved URL (when unshortener is in flight).

### 6. Config (`Config/`)

- JSON file at the OS-standard config location:
  - Windows: `%APPDATA%\AskMeFirst\config.json`
  - macOS: `~/Library/Application Support/AskMeFirst/config.json`
  - Linux: `~/.config/AskMeFirst/config.json`
- Parsed with `System.Text.Json` + `JsonCommentHandling.Skip` (so the file accepts JSONC-style `//` comments).
- Embedded defaults compiled into the binary (so cold start never fails when config is missing).
- Schema-validated at load. Errors reported with file path.
- v1: re-read on every CLI invocation with mtime check. Phase 7+: hot-reload via filesystem watcher (in the management UI).

### 7. Platform layer (`Platforms/`)

- OS-specific implementations behind interfaces:
  - `IBrowserInventory` — discovers installed browsers and their profiles
  - `IRunningBrowserDetector` — finds running browsers
  - `IDefaultBrowserRegistrar` — registers AskMeFirst as default
  - `IUrlLauncher` — launches a URL in a browser
  - `ISourceAppDetector` — identifies the parent process
  - `IProcessNameNormalizer` — OS-specific process name → canonical form
- Three implementations: Windows, macOS, Linux. See [platform-integration.md](./platform-integration.md).

### 8. Composition (`Composition.cs`)

- Hand-rolled composition root. **No DI framework.**
- One `Composition.cs` per platform with explicit constructor wiring.
- ~50 LOC. Trivial to read. AOT-friendly.

## Data flow (router mode, typical case — rule hits)

```
1. OS spawns askmefirst.exe https://github.com/foo                (~5 ms OS spawn)
2. Process startup (Native AOT)                                    (~50 ms)
3. CLI parses args                                                (~2 ms)
4. Load config (cache hit, mtime unchanged)                       (~3 ms)
5. Identify source process via parent PID → normalize             (~5 ms)
6. Pre-process URL (strip tracking params, if enabled)            (~1 ms)
7. Evaluate rules (precompiled patterns)                          (~2 ms)
   - Result: { browser: firefox, profile: work, focus_existing: true }
8. Find running firefox (process scan)                            (~10 ms)
9. Launch URL in firefox work profile                             (~200–500 ms browser startup)
10. Exit
```

Total our-code time: **~80 ms**. Total wall-clock: dominated by browser startup (~250 ms+). Comfortably under 1 s on warm systems.

## Data flow (router mode, picker case — no rule hit)

```
Steps 1–7 as above.
7. No rule matched → implicit catchall = "show picker"
8. Detect URL is from known shortener domain?
   - If yes: kick off async unshortener, pass Task<string?> to picker
   - If no: pass null
9. Show picker (Avalonia window)                                  (~150 ms window show)
10. Picker displays:
    - Source app name
    - Original URL (short or not)
    - "Resolving..." indicator if unshortener in flight
    - Resolved URL when complete (live update)
    - Browser pane (click-or-Enter to commit)
    - Remember pane (default = "Just this once")
11. User interaction (variable)
12. User commits → launch chosen browser, optionally save rule
13. Exit
```

## No daemon — confirmed

A long-running background process was considered and **dropped for v1**. Every daemon-driven feature has a stateless equivalent:

| Original daemon need | Without a daemon |
|---|---|
| Hot-reload of config | Router mode reads config on every invocation. mtime check is <1 ms. |
| Browser inventory refresh | Cache file with TTL (24h). Router mode checks staleness, refreshes on demand. |
| Picker UI | Short-lived process on demand when rules don't conclusively resolve. |
| Rule suggestion engine | Each routing decision appends a row to `usage.log`. `askmefirst suggest` subcommand reads the log and produces suggestions. |
| IPC for picker | Picker is invoked synchronously by the router when needed. No IPC. |

A daemon may be reconsidered in Phase 7+ for a system tray icon. Until then: **stateless CLI**.

## Threading model

- Router mode: **single thread**. Each spawned instance does its work and exits. No thread pool needed.
- Unshortener (when triggered): background `Task<string?>` started before picker shows. Picker awaits via `Task.ContinueWith` to update the UI live.

## Dependency budget

Minimal — every dep is a startup-time risk and a "understand the code" cost.

**Core dependencies:** zero. Pure BCL.
- `System.Text.Json` — built-in JSON parsing (with comment skipping)
- `HttpClient` — built-in for unshortener
- `System.Diagnostics.Process` — built-in for process info
- `Avalonia` — added in Phase 7+ for picker UI

That's it. No NuGet packages in v1.

## What we are *not* building

- **No browser extension.** All routing happens before the browser sees the URL. The browser itself follows redirects naturally.
- **No daemon.** Stateless CLI per the design above.
- **No telemetry.** Pure local tool. (See [decisions-log.md](./decisions-log.md).)
- **No cloud config sync.** Config is local. (User can sync via Git/Syncthing/Dropbox if desired.)
- **No update server.** Manual upgrades in v1; `askmefirst update` command deferred to Phase 9.