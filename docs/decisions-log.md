# Decisions Log

All design decisions made during planning, with rationale. New contributors should read this first.

**Session date**: 2026-06-26
**Status**: Planning closed. Phase 0 (bootstrap) starting.

---

## 1. Project name: AskMeFirst

**Rationale**: The name is the pitch. AskMeFirst is a rebuttal to the default-browser behavior of opening links immediately, ignoring user intent. The picker "asks first" before defaulting; "remember" actions lock in decisions forever.

## 2. Language: C#

**Rationale**: Mature cross-platform GUI (Avalonia), clean system integration on all 3 OSes, mainstream readability. See [language-decision.md](./language-decision.md).

**Alternatives considered**: Kotlin (JVM cold start kills the <3s budget; Kotlin/Native desktop tooling too immature).

## 3. Runtime: .NET 10 LTS + Native AOT

**Rationale**: .NET 10 is the current LTS (released Nov 2025, supported until Nov 2028). .NET 9 went EOL in May 2026, just before this project started. Native AOT gives sub-100 ms cold start, single binary distribution, no runtime dependency.

**Alternatives considered**: .NET 9 (EOL'd), .NET 8 LTS (would also work, shorter runway), .NET 11 preview (not stable).

## 4. Daemon mode: Dropped for v1

**Rationale**: Every daemon-driven feature has a stateless equivalent that works fine. No long-running process = no crash recovery, no memory leaks, no in-place upgrade headaches. Add a daemon later in Phase 8+ if a use case (e.g. system tray icon) demands it.

**Reconsider**: if a system tray icon becomes a daily-driver requirement.

## 5. Config format: JSON (with comment-tolerant parser)

**Rationale**: `System.Text.Json` native, AOT-safe, fastest parsing. We configure `JsonCommentHandling.Skip` so the file accepts JSONC-style `//` and `/* */` comments — addresses the main reason people pick YAML over JSON for config. Zero new dependencies.

**Alternatives considered**: YAML (3-10× slower parsing, no .NET BCL support, AOT caveats, Norway/billion-laughs pitfalls), TOML (also a clean pick, slightly less common in tooling).

**Note**: User said manual config editing is not needed once the management UI ships. Comments still supported for the rare hand-edit case.

## 6. App tags: Dropped

**Rationale**: Grouping benefit only matters at scale. Direct `ProcessIn: [list]` references in rules are equally readable for <10 source apps per category. OS-specific process name normalization happens in the platform layer (one file, ~50 LOC per OS), decoupling rules from `Slack.exe` vs `slack` vs `com.tinyspeck.chatlyo`.

**Reconsider**: if rule schemas grow painful with long process lists. Add tags back as an *optional* convenience layer — additive change.

## 7. Config sync across machines: Not considered for v1

**Rationale**: User explicitly deprioritized cross-OS config sharing. Per-OS configs are fine.

## 8. Rule schema: Rich (per docs, minus dropped predicates)

**Rationale**: User pushed back on my initial simplification proposal. The richer schema (multiple predicates, regex, time-of-day, weekday, scheme, etc.) can do everything a simpler one can and more. Picker-generated rules just produce simple entries in the richer schema.

**Predicates included**: `ProcessIn`, `UrlMatchesAny`, `UrlMatchesAll`, `UrlRegex`, `SchemeIn`, `TimeBetween`, `WeekdayIn`, `BrowserRunning`.
**Predicates excluded**: `tag_in` (tags dropped).

## 9. Source-app detection depth: L1 (process name only)

**Rationale**: L1 covers 95% of the case ("came from Slack → route accordingly"). L2 (window title) has real Linux Wayland fragmentation problems (no portable API across Sway, Hyprland, GNOME, KDE) and macOS Accessibility permission prompts. L3 (IPC / app context) is per-app bespoke work with low coverage.

**Reconsider**: add L2 in v2 as opt-in per-platform if a use case demands it (Outlook email-domain detection is the most likely ask).

## 10. Browser profile management: P2 (auto-discovered)

**Rationale**: P0 (no profiles) too coarse — Firefox users with work/personal profiles can't route per-profile. P1 (hardcoded config) requires manual maintenance when profiles change. P2 auto-discovers via `Local State` JSON (Chromium) or `profiles.ini` (Firefox) — surfaces all profiles automatically. User pins/hides via config or management UI (Phase 7+).

**User mentioned**: Firefox Containers support is a future extension. Schema can accommodate via `container` field when ready.

## 11. Picker triggering philosophy: A — rules-first fallback

**Rationale**: "Ask me first" is the *learning* philosophy, not the *daily-driving* philosophy. The picker's job is to gather decisions that *become* rules via "remember." After a week of "remember" actions, picker rarely fires. That's success.

**Alternatives considered**: B (always ask — too annoying), C (confidence threshold — premature optimization).

## 12. Dependency injection: A — hand-rolled composition root

**Rationale**: v1 has ~10 services, all effectively singleton. Manual wiring is ~50 LOC and totally readable. Zero new dependencies, AOT-friendly, "understand all the code." M.E.DI's transitive deps (logging, options, hosting) bump binary size and complicate AOT trim.

**Reconsider**: if dep graph grows past ~20 services with mixed lifetimes. Migrate to M.E.DI later — pure additive change.

## 13. Windows registration: A — StartMenuInternet + Default Apps prompt

**Rationale**: Clean Microsoft-blessed path. One-time 10s user friction. No fragile `UserChoice` registry hacks that break on Windows updates.

**Alternatives considered**: B (URL protocol handler only — too invisible), C (hybrid — doubles registration code), D (UserChoice hack — fragile, actively worked against by Microsoft).

## 14. macOS registration: A — bundle + System Settings prompt

**Rationale**: Mirrors the Windows decision for consistency. Apple's supported path. The one-time System Settings click is small friction.

**Alternatives considered**: B (programmatic — Apple still requires first manual set; brittle), C (handler-only — too invisible).

**Note**: v1 ships unsigned. Right-click → Open on first launch. Phase 9 adds signing + notarization if sharing with others.

## 15. Linux registration: A — xdg-mime + .desktop file

**Rationale**: Standard freedesktop path, zero user action, works on GNOME/KDE/XFCE/Sway/Hyprland/etc. C (per-DE deep integration) is an unmaintainable nightmare.

**Edge cases handled**: Flatpak/Snap browsers (`.desktop` files scanned from their export paths), Wayland (no special handling needed), per-user vs system-wide install (`--system` flag in Phase 9).

## 16. Picker UI: Centered modal, single screen, browser buttons + remember radios

**Rationale**: "Ask me first" demands attention — modal is the right shape. Browser pane uses buttons (click or Enter commits immediately — no phantom "select then confirm"). Remember pane uses radios (state-only — clicking doesn't commit). Default remember = "Just this once" so plain Enter does the safe thing.

**Two flows**:
- *Ignore*: 1 action (default Enter opens with "Just this once")
- *Remember*: 2 actions (set remember radio + click browser)

**Layout**: ~720×440 px, two side-by-side panels. Smaller displays stack vertically. Live URL display (short URL → resolved URL) when Unshortener is in flight.

## 17. Unshortener triggering: Picker-only + known shortener domain + async

**Rationale**: User clarification. Unshortener runs ONLY when:
1. Picker would show (no rule matched)
2. URL is from known shortener domain (built-in list + user extensions)
3. Async, with live UI update — picker is fully interactive during Unshortening
4. User choice stands regardless of Unshortening state

**Why this matters**: Short URLs hide their destination. Without Unshortening, `t.co/abc` would route based on the short domain (Twitter = personal Chrome). With Unshortening, the picker shows the resolved URL (e.g. `company.atlassian.net/...`), letting the user pick based on the actual destination.

**No browser extension**: The browser itself follows redirects natively after we hand it the URL. We don't need to swap tabs.

## 18. Known shortener domains: Built-in default + user extensions

**Rationale**: Built-in list (~27 entries: t.co, bit.ly, tinyurl.com, etc.) covers 95% of cases. User extension via `UnshortenDomainsExtra` handles the long tail (internal company shorteners). `Unshorten_domains_override: true` replaces entirely for power users.

**Management UI**: configurable in Phase 7+.

## 19. Tracking parameters: Built-in default + user extensions (parallel to shorteners)

**Rationale**: Same pattern. Built-in list (~30 entries: utm_*, fbclid, gclid, mc_eid, _ga, ref, etc.) covers 95%. User extension via `TrackingParamsExtra`. `TrackingParamsOverride: true` replaces entirely.

**Trigger**: `settings.StripTracking: true` by default. Per-rule override via `then.StripTracking: bool`.

## 20. "Just this once" semantics: Forever

**Rationale**: User explicitly didn't want to remember, so the URL gets no rule and the picker shows again on the next click. No session cache, no TTL. Stays consistent with the "I didn't want to remember" intent.

## 21. Picker dismissal: Nothing opens

**Rationale**: `Esc` and closing the window cancel — no browser opens, URL is dropped. No auto-timeout. No "fall back to system default." User keeps full control. That's the "ask me first" promise.

## 22. License: MIT

**Rationale**: Permissive, standard for tools, ~everyone understands it. If you ever want to share or accept outside contributions, MIT is the path of least friction.

## 23. Telemetry: None

**Rationale**: This tool sees every URL the user clicks — including work URLs, auth tokens in query strings, internal tools. Even "anonymous" telemetry is a poor fit. Logging goes to stderr (`--verbose`) and a local usage log for the suggestion engine (Phase 7+). No HTTP calls to any server, ever, in v1.

## 24. macOS code signing: Unsigned for v1

**Rationale**: Right-click → Open on first launch is acceptable friction. Avoids $99/yr Apple Developer account + CI signing/notarization setup. Phase 9 revisits if/when sharing with others.

## 25. Update mechanism: Manual download for v1

**Rationale**: User direction. Manual download for v1; package managers (**winget, brew, apt**) on the Phase 9 roadmap. `askmefirst update` command also deferred to Phase 9, behind explicit opt-in (no background checks for privacy).

---

## Summary table

| # | Decision | Pick |
|---|---|---|
| 1 | Project name | AskMeFirst |
| 2 | Language | C# |
| 3 | Runtime | .NET 10 LTS + Native AOT |
| 4 | Daemon mode | Dropped for v1 |
| 5 | Config format | JSON (comment-tolerant parser) |
| 6 | App tags | Dropped |
| 7 | Cross-OS config sync | Not considered for v1 |
| 8 | Rule schema | Rich (per docs) |
| 9 | Source-app detection | L1 (process name only) |
| 10 | Browser profile management | P2 (auto-discovered) |
| 11 | Picker triggering philosophy | A — rules-first fallback |
| 12 | Dependency injection | A — hand-rolled composition root |
| 13 | Windows registration | A — StartMenuInternet + Default Apps prompt |
| 14 | macOS registration | A — bundle + System Settings prompt |
| 15 | Linux registration | A — xdg-mime + .desktop |
| 16 | Picker UI | Centered modal, single screen, browser buttons + remember radios |
| 17 | Unshortener triggering | Picker-only + known shortener + async |
| 18 | Shortener domains | Built-in default + user extensions |
| 19 | Tracking parameters | Built-in default + user extensions |
| 20 | "Just this once" | Forever (no rule, picker reappears) |
| 21 | Picker dismissal | Nothing opens |
| 22 | License | MIT |
| 23 | Telemetry | None |
| 24 | macOS signing | Unsigned |
| 25 | Update mechanism | Manual for v1; winget/brew/apt in Phase 9 |