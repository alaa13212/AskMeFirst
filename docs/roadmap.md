# Roadmap

Phased build plan. Each phase ships a working artifact you can use daily. No "v1 won't compile until phase 5."

**Currently in: Phase 0 — Bootstrap.**

---

## Phase 0 — Repo bootstrap (1 day) ← IN PROGRESS

- [x] .NET 10 solution scaffolding (csproj, props, editorconfig, .gitignore)
- [ ] CI workflow (build + test on all 3 OSes)
- [ ] Native AOT publish working end-to-end
- [ ] First binary published to GitHub Actions artifact
- [ ] `askmefirst --version` prints version in < 100 ms on each OS

**Exit criteria**: `askmefirst --version` prints a version number on each OS in < 100 ms.

---

## Phase 1 — MVP router (1 week)

Goal: a working CLI that takes a URL and launches it in a hardcoded browser. No rules, no picker, no platform integration magic. Just the core data flow.

- [ ] `Program.cs` + args parser
- [ ] `UrlRouter` orchestrator (no rule engine yet — hardcoded decision)
- [ ] `BrowserInventory` per platform (read installed browsers)
- [ ] `Launcher` per platform (launch URL in browser)
- [ ] Embedded default config
- [ ] Basic logging
- [ ] Split project into `Core` + `Platforms.*`

**Exit criteria**: `askmefirst https://example.com --browser chrome` opens Chrome with the URL on all 3 OSes in < 1 s.

---

## Phase 2 — Rule engine + source detection (1 week)

- [ ] JSON config parser + validator
- [ ] Rule evaluation with priority + predicates + actions
- [ ] Source-app detection per platform (parent process / NSWorkspace / /proc)
- [ ] OS-normalized process names
- [ ] Tracking-param stripping (on by default)
- [ ] Hand-rolled `Composition.cs` per platform

**Exit criteria**: a config with 10 rules routes correctly via unit tests + manual checks on each OS.

---

## Phase 3 — Picker UI (1 week)

**Status**: 🚧 Started 2026-06-28. Design locked via grill session. Vertical slice in progress.

Full design: [`docs/phase-3-design.md`](./phase-3-design.md).

- [ ] AskMeFirst.Picker project + Avalonia + CommunityToolkit.Mvvm packages
- [ ] `RoutingOutcome.ShowPicker(PickerRequest)` variant + tests
- [ ] `ProfileSpec.Pinned` field + picker filters to pinned-only
- [ ] Picker window with two-panel layout (browser buttons + remember radios)
- [ ] Keyboard navigation (Tab cycles, arrows within panel, 1-9 hotkeys, Enter commits, Esc cancels)
- [ ] Live URL display with unshortener status (1s timeout, cancel on commit, silent fallback on error)
- [ ] MVVM ViewModels (`PickerWindowViewModel`, `BrowserOptionViewModel`, `RememberOptionViewModel`)
- [ ] Window position: source-app-center where easy, else active-monitor center
- [ ] Modeless + always-on-top; X / Esc / Cancel all close without launching
- [ ] Picker writes "remember" rules per `docs/rule-engine.md` table (5 radios)
- [ ] Recent-picks JSONL append-only log
- [ ] Post-commit browser-launch failure → OS notification (not silent)

**Exit criteria**: ambiguous URLs show the picker; selecting a browser opens it and remembers the choice via "remember" rules.

---

## Phase 4 — OS integration (1 week)

- [x] `install` command — register as default browser
  - Windows: registry + StartMenuInternet + Default Apps prompt
  - macOS: .app bundle + Info.plist + System Settings prompt
  - Linux: .desktop file + xdg-mime default
- [x] `uninstall` command — reverse
- [x] `NewWindow` action wired into browser-family launch strategies

**Exit criteria**: install registers AskMeFirst as a default-browser candidate on all 3 OSes; uninstall removes it; user gets the OS-standard "make this the default" prompt; `NewWindow` works for Chrome + Firefox; picker centers over source-app window on Win/Mac.

---

## Phase 5 — Link processing (3 days)

- [ ] Async unshortener with 1s timeout
- [ ] Triggered only when picker would show + known shortener domain
- [ ] Live URL update in picker
- [ ] Per-rule `unshorten` toggle
- [ ] Configurable shortener domain list (`UnshortenDomainsExtra` / `UnshortenDomainsOverride`)

**Exit criteria**: t.co / bit.ly links show resolved URL in picker as user decides.

---

## Phase 6 — Polish (1 week)

- [ ] `--bench` command with CI-enforced budgets
- [ ] Browser profile auto-discovery (P2 implementation)
- [ ] Inventory cache: persist discovered browsers + profiles to `config.browsers` / `config.profiles` so repeat invocations skip re-discovery. Per-platform cache file (executable paths differ across OS); mtime + manual `askmefirst refresh` for invalidation; cache merges with user-written specs (user wins). Replaces the current "every CLI invocation re-discovers" pattern from `rule-engine.md:218`.
- [ ] Embedded browser icons in picker
- [ ] User-facing README + screenshots
- [ ] `samples/askmefirst.example.json` polished
- [ ] Test coverage > 80 %

**Exit criteria**: a friend can install and use AskMeFirst following only the README. No bugs filed in the first week of dogfooding.

---

## Phase 7 (optional) — Management UI (1-2 weeks)

- [ ] `askmefirst config` opens a avaloniaui
- [ ] Browse / edit / sort / pin browsers and profiles
- [ ] Edit rule command and add custom
- [ ] Test browsers (open https://example.com)
- [ ] View / edit / sort rules + test (URL input → show matched rule)
- [ ] Suggest rules based on usage (read `usage.jsonl`)
- [ ] Configurable unshortener domains + tracking params in UI

**Exit criteria**: full config management without touching JSON files.

---

## Phase 8 (optional) — Daemon mode + tray icon (1 week)

- [ ] Long-running background process
- [ ] System tray icon (Win/Mac) / status notifier (Linux)
- [ ] Hot-reload of rules via filesystem watcher
- [ ] Browser inventory refresh on install/uninstall events

**Exit criteria**: daemon runs stably, restarts on crash, config changes take effect without restart.

---

## Phase 9 (optional) — Installers + package managers

- [ ] Windows: MSI or simple zip + register-on-first-run
- [ ] macOS: signed + notarized .app in a .dmg
- [ ] Linux: .deb + .rpm + AppImage
- [ ] Package manager submissions: **winget, brew, apt** (per user direction)
- [ ] `askmefirst update` command (opt-in check against GitHub Releases)

---

## Total effort estimate

| Phase | Duration | Cumulative |
|---|---|---|
| 0 — bootstrap | 1 day | 1 day |
| 1 — MVP | 1 week | 1.5 weeks |
| 2 — rules + source | 1 week | 2.5 weeks |
| 3 — picker | 1 week | 3.5 weeks |
| 4 — OS integration | 1 week | 4.5 weeks |
| 5 — link processing | 3 days | 5 weeks |
| 6 — polish | 1 week | 6 weeks |
| 7 — management UI (opt) | 1-2 weeks | 7-8 weeks |
| 8 — daemon (opt) | 1 week | 8-9 weeks |
| 9 — installers (opt) | 2 weeks | 10-11 weeks |

Realistic total for a personal project: **6 weeks of part-time work** to a solid v1 (phases 0–6). Everything past that is iterative.

## Risks and mitigations

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| Native AOT breaks a critical dep | Medium | High | Vet deps early (System.Text.Json is AOT-safe). Have fallback to non-AOT publish for the management UI only. |
| macOS bundle signing annoyances | Medium | Medium | v1 ships unsigned + right-click-Open. Real signing in Phase 9. |
| Wayland browser detection is hard | High | Low | v1 only does "is process running" check on Wayland. |
| Browser auto-update breaks our launcher | Medium | Medium | Cache inventory; test against Chrome 130+, Firefox 130+, etc. |
| Performance regression unnoticed | Medium | Medium | CI-enforced budgets + `--bench` command. Block PRs that regress. |
| User installs multiple versions of same browser | Medium | Low | P2 profile discovery enumerates all; user picks via config or UI. |