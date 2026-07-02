# Decisions Log

All design decisions made during planning, with rationale. New contributors should read this first.

**Session date**: 2026-06-26
**Status**: Planning closed. Phase 0 (bootstrap) starting.

Phase 2 update (2026-06-28): decisions 45–54 added during Phase 2 code-quality pass. See [`docs/architecture.md`](./architecture.md) and the cumulative count below.

Phase 3 update (2026-06-28): decisions 55–70 added after grill session. See [`docs/phase-3-design.md`](./phase-3-design.md).

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

## Phase 2 decisions (added 2026-06-28)

## 45. Predicate evaluation: Strategy pattern (`IPredicateMatcher` per field)

**Rationale**: Phase 2's `PredicateEvaluator.Matches` had a 237-line if-chain over 8 predicate fields. Strategy pattern lets each field own its match logic. Adding a predicate = one new class + register in `RoutingDefaults.Matchers()`. No edit to `PredicateEvaluator`.

## 46. Rule routing: Strategy + Pipeline + Result type mix

**Rationale**: `RuleRouter` had grown 9 dependencies and mixed responsibilities. Split into:
- `ITargetResolver` strategy per selection mode (Explicit / Rule / Fallback)
- `IRoutingExecutor` extracted as separate component
- `RoutingOutcome` as discriminated union (Success / Failure)
- `RoutingIntent` carries per-resolver metadata

## 47. `RoutingIntent` carries `NotFoundExitCode` + `NotFoundMessagePrefix`

**Rationale**: `IntentSource` enum inside `ResolveOutcome` leaked chain knowledge into the executor. Replaced with intent metadata that the executor reads opaquely — resolvers self-describe "if my target browser is missing, here's the exit code + message prefix."

## 48. `RoutingOutcome` as discriminated union

**Rationale**: Scattered `return 4` / `return 5` / `return 0` made routing exit codes hard to track. Abstract record + `Success` / `Failure` sealed records force callers to switch on outcome kind.

## 49. `IRoutingExecutor` extracted as a separate component

**Rationale**: Owns the lookup → profile → strip pipeline. `RuleRouter` no longer knows about inventory or stripper; one fewer responsibility per component.

## 50. `RoutingExitCode` enum replaces magic ints

**Rationale**: 0/2/3/4/5 scattered across routing logic was unreadable. Named members (`Success`, `NoBrowsersDiscovered`, `BrowserNotFound`, `RuleBrowserNotFound`, `NoRouteFound`) make switch statements self-documenting.

## 51. `ProfileSpec` (config) and `BrowserProfile` (runtime) are distinct types

**Rationale**: Config-side spec is the user's stable identity; runtime-side profile is what the platform layer discovered. Conflating them muddled ownership. Two types, clean mapping.

## 52. Top-level `profiles:` section + `then.profileId` (stable ID)

**Rationale**: Old `then.profile` matched on string (name/directory) — fragile. New `ProfileSpec.Id` is the stable handle. Rules reference profiles by ID, not by display string.

## 53. `ConfigValidator` runs at load time

**Rationale**: Unique IDs and reference resolution are best checked once at load. On any validation error → fall back to embedded defaults + log all errors. Keeps runtime routing logic trusting validated input.

## 54. Profile mismatch is soft-fail; browser mismatch is hard-fail

**Rationale**: A rule pointing at a missing profile should warn + fall back to default profile (user might have renamed it). A rule pointing at a missing browser should hard-fail with distinct exit code (user intent is unambiguous).

---

## Phase 3 decisions (added 2026-06-28)

## 55. Picker integration: new `ShowPicker(PickerRequest)` outcome variant

**Rationale**: The picker is a third routing outcome (gates on user input, doesn't launch anything), not a routing intent. Putting it in `RoutingOutcome` admits that to the type system — symmetric with the existing `Success`/`Failure` split. Resolvers stay focused on routing intent; the router handles the picker at the outcome-switch level.

**Alternative rejected**: `PickerFallbackResolver` returning a sentinel `__picker__` `RoutingIntent`. Rejected because the executor would have to special-case the sentinel — same kind of chain-knowledge leak that Phase 2 decision #47 just removed.

## 56. Single binary, in-process Avalonia, deferred init on `pick` command

**Rationale**: Phase 9 distribution story (winget / brew / apt) requires a single binary. Splitting CLI and picker into two artifacts breaks that. Avalonia is statically linked but only initialized when the `pick` command fires — CLI cold-start (~46ms) is unchanged.

**Implementation**: `Program.cs` dispatches: `pick` → builds Avalonia app, blocks on window close; everything else → today's CLI path, Avalonia untouched.

## 57. Five remember radios per existing `rule-engine.md` table

**Rationale**: Already documented in [`docs/rule-engine.md`](./rule-engine.md#rule-generation-from-picker-remember). Picker implements the documented schema; no new design needed. Radios are dynamic (3, 4, or 5 visible depending on whether source-app detection succeeded and whether URL has a host).

## 58. Forget mechanism deferred to Phase 7+ management UI

**Rationale**: The picker only fires on unruled URLs. Once a rule exists, the picker vanishes for that URL. v1 has no in-app path to "I changed my mind about a rule" — user must edit JSON. Phase 7+ management UI provides proper rule editing. Adding a half-baked forget CLI command now is scope creep.

## 59. MVVM with CommunityToolkit.Mvvm (UI surfaces use standard toolkits)

**Rationale**: Hot-path CLI uses minimalism (no DI framework, no CommunityToolkit.Mvvm). UI surfaces (picker, future management UI) use the standard MVVM toolkit — speed is no longer relevant because user latency dwarfs startup cost. Code structure matters more once a user is in front of the screen.

**Cross-cutting principle**: "Hot-path CLI ≠ UI surface." CLI = no deps, hand-rolled composition. UI = standard toolkits, idiomatic patterns.

## 60. State machine: flat `PickerStatus` enum + `[ObservableProperty]` + raw `Task`

**Rationale**: ~5 transitions in one method (Loading → Resolving → Ready → Committing → Done). GoF state pattern earns its keep when state objects have entry/exit behavior. Here the only behavior is "set property," which `[ObservableProperty]` does for free. State pattern would be over-engineering.

## 61. Unshortener cancellation on commit via `CancellationToken`

**Rationale**: The unshortener Task starts before the picker shows and races against user input. If user commits before it resolves, cancel the Task — don't leave it running in the background. User choice stands regardless of resolution state (per architecture.md).

## 62. Config write off-thread; UI closes immediately on commit

**Rationale**: JSON write is ~ms but on slow disks or contention it could block. User shouldn't wait for the picker to write to disk. Close UI immediately, fire config write in background. Browser launch is sync (parent blocks until browser process starts).

## 63. Silent fallback to original URL on unshortener failure

**Rationale**: Network errors, DNS failures, redirect loops — picker shouldn't expose plumbing. On any unshortener exception or timeout, fall back to the original URL silently. User makes the decision based on the URL they clicked.

## 64. Window position: source-app-center where easy, else active-monitor center

**Rationale**: "Ask me first" needs visibility. Best position is over the source app (user just clicked a link there). Fall back to active-monitor center when source-app-window detection is hard (Linux DE-dependent, no portable API).

**Implementation**:
- Windows: `GetWindowRect` on parent PID (easy — we already have parent PID from source-app detection).
- macOS: `CGWindowListCopyWindowInfo` with source app's PID (easy).
- Linux: fall back to active-monitor center. Per-DE source-window detection is unmaintainable.

## 65. Window modeless + always-on-top (not modal)

**Rationale**: Modal blocks the user from the very thing they're routing to (e.g., they can't see the Slack link they're trying to remember context for). Modeless + always-on-top lets them glance at the source app, return, and pick. Visibility comes from always-on-top, not modal blocking.

## 66. X / Esc / Cancel all → `PickerResult.Cancelled`

**Rationale**: Single semantic for "user wants out." Reinforces existing decision #21: "Esc and closing the window cancel — no browser opens, URL is dropped."

## 67. Standard Windows keyboard navigation

**Rationale**: Tab cycles controls, arrows cycle within current group, 1-9 hotkeys for top 9 browser buttons, Enter on focused control commits, Esc cancels. Zero learning curve — every Windows dialog works this way.

**Why not vim-style**: Single-screen picker doesn't warrant dual-mode complexity. Power users can still drive everything via Tab + arrows.

**Note (2026-06-29)**: Split-role arrow navigation — Up/Down cycles within the currently focused section only (browsers among themselves with wrap, radios among themselves with wrap); Left/Right switches sections using each section's own remembered cursor (no cross-section index transfer). Per-section `_browserCursor` + `_rememberCursor` fields; `SyncCursorToFocus()` reconciles them at the start of every keypress so external focus changes (Tab, click) don't drift. See [PickerWindow.axaml.cs](../../src/AskMeFirst.Picker/Views/PickerWindow.axaml.cs).

## 67b. Radio option focus highlight mirrors browser button

**Rationale**: Default Fluent `RadioButton` focus ring is too subtle for users to spot which remember option is currently keyboard-focused. Wrapped each radio's content in a `Border` with class `rememberOptionCard`; styled it identically to `PART_BrowserButton` (light blue `#E5F1FB` background + accent `#0F6CBD` border on focus, plus hover state). Same problem we solved for buttons in the prior session; applied the fix to radios for parity.

**Alternative considered**: set `RadioButton.Background` / `BorderBrush` directly via `:focus` style. Tested mentally — Avalonia's default RadioButton template doesn't render those properties as a visible border, so a Border wrapper is required.

## 68. Initial focus on first browser button; "Just this once" preselected

**Rationale**: Default Enter = safe ignore-flow (one action: launch first browser with no rule written). Matches the existing decision #20: "Just this once" is the default remember semantic.

## 69. `ProfileSpec.Pinned: bool` (default false)

**Rationale**: Sidesteps the 1-9 hotkey limit cleanly. Users with >9 (browser, profile) tuples pin their favorites; picker shows pinned-only. Phase 7+ management UI provides pin/unpin surface.

**Default behavior**: if no profiles are pinned, picker shows ALL profiles (sensible first-time-user default). Users opt into pinning via management UI.

## 70. Post-commit browser-launch failure → OS notification (not silent, not picker re-show)

**Rationale**: User clicked a link and made a decision. Silent failure is a UX hole. Re-showing the picker disrespects the user's commit. System-default-browser fallback surprises the user (they explicitly picked X, not default). OS notification is modern, non-intrusive, and informative ("Couldn't open Chrome. URL is in usage log so you can re-click.")

## 71. Arrow-key roles split between sections

**Rationale**: Two-column picker needs two distinct roles — Up/Down cycle within the focused section (vertical list nav, with wrap), Left/Right switch sections (column nav, with each section's own remembered cursor). This matches every 2D grid picker / settings dialog in mainstream UIs and avoids the awkward "Up from last browser jumps to first radio" surprise of a flat list. Per-section cursors (`_browserCursor`, `_rememberCursor`) mean Right→Left round-trips restore the user's last position in each list independently.

**Alternatives considered**:
- **Flat ordered list** (earlier draft) — same key handles all four directions, walks `[browsers...] + [radios...]` with wrap. Rejected: conflated roles, jumped sections on Up/Down when user didn't expect it.
- **2D grid with row-wrap** — Up/Down in browsers stays, but Right from last browser goes to first browser in next row. Rejected: 2-row layout doesn't justify the complexity.

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
| 45 | Predicate evaluation | Strategy pattern (`IPredicateMatcher` per field) |
| 46 | Rule routing | Strategy + Pipeline + Result type mix |
| 47 | RoutingIntent metadata | `NotFoundExitCode` + `NotFoundMessagePrefix` (no enum leak) |
| 48 | RoutingOutcome | Discriminated union (abstract record + Success/Failure) |
| 49 | RoutingExecutor | Extracted as `IRoutingExecutor` separate component |
| 50 | RoutingExitCode | Enum replaces magic ints |
| 51 | ProfileSpec vs BrowserProfile | Distinct types (config-side vs runtime-side) |
| 52 | Profile reference | Top-level `profiles:` section + `then.profileId` (stable ID) |
| 53 | ConfigValidator | Runs at load time; fall back to defaults on error |
| 54 | Profile mismatch | Soft-fail (warn + default); browser mismatch hard-fail |
| 55 | Picker integration | `ShowPicker(PickerRequest)` outcome variant |
| 56 | Packaging | Single binary, in-process Avalonia, deferred init |
| 57 | Remember radios | 5 per `rule-engine.md` (dynamic count 3/4/5) |
| 58 | Forget mechanism | Deferred to Phase 7+ management UI |
| 59 | Picker MVVM | CommunityToolkit.Mvvm (UI surfaces = standard toolkits) |
| 60 | State machine | Flat enum + `[ObservableProperty]` + raw `Task` |
| 61 | Unshortener cancellation | `CancellationToken` cancel on commit |
| 62 | Config write timing | Off-thread; UI closes immediately |
| 63 | Unshortener error | Silent fallback to original URL |
| 64 | Window position | Source-app-center where easy, else active-monitor center |
| 65 | Window modal-ness | Modeless + always-on-top |
| 66 | Cancel semantics | X / Esc / Cancel all → `PickerResult.Cancelled` |
| 67 | Keyboard nav | Standard Windows (Tab, arrows, 1-9, Enter, Esc) |
| 67b | Radio focus highlight | Wrapped `Border.rememberOptionCard` styled identically to `PART_BrowserButton` |
| 68 | Initial focus | First browser button; "Just this once" preselected |
| 69 | ProfileSpec.Pinned | `bool` (default false); picker filters to pinned |
| 70 | Post-commit failure | OS notification (not silent, not picker re-show) |
| 71 | Arrow-key roles | Up/Down within section, Left/Right switch sections (per-section cursors) |
| 72 | IIconProvider seam | Cross-OS surface; NullIconProvider on Linux/macOS pending real implementations |
| 73 | SQLitePCLRaw.bundle_e_sqlite3 | Explicit reference overrides Microsoft.Data.Sqlite 10.x default SourceGear native |
| 74 | SkiaSharp in WindowsIconProvider | PNG-encodes GDI bitmap bytes; Avalonia's internal Skia isn't exposed publicly |
| 75 | Rule.Origin field | `"user"` (default) vs `"remember"`; for Phase 7 management UI |
| 76 | macOS `.app` bundle | MSBuild `MacOsBundle.targets` wraps `dotnet publish` output into `AskMeFirst.app` |
| 77 | Linux `.desktop` Exec= | Absolute path via `Environment.ProcessPath`; re-evaluate for Flatpak in Phase 9 |
| 78 | ISourceAppWindowLocator location | Moved from Picker to `Core/Abstractions/`; Platforms.\* can implement |
| 79 | FocusExisting field removed | Per "never keep dead code"; browser built-in dedup handles it |
| 80 | IDefaultBrowserRegistrar | Async (`Task<RegistrationResult>`); `lsregister` slowness on Mac justifies async |
| 81 | Register/Unregister idempotency | Register checks IsRegistered first; Unregister returns success if absent |
| 82 | Post-install UX | Try deep-link (try/catch), print instructions as fallback |
| 83 | Test strategy | D + unit-testable bits; ~6 unit tests; no CI integration smoke tests for OS impls |

## Phase 3 review feedback (added 2026-06-30)

## 72. `IIconProvider` stays as a cross-OS surface

**Rationale**: Resolved by keeping it. Folding icon resolution into `IBrowserInventory` or `Browser` would force every `Browser` record to carry a potentially large byte array eagerly, on platforms (Linux/macOS) where the picker still has to handle the "no icon" case. Per-platform implementations (`WindowsIconProvider`, future `LinuxIconProvider`/`MacIconProvider`) plus `NullIconProvider` for stubs keeps the picker code path-agnostic.

## 73. `SQLitePCLRaw.bundle_e_sqlite3` kept as an explicit `PackageReference`

**Rationale**: Microsoft.Data.Sqlite 10.x resolves to the SourceGear native by default. `SQLitePCLRaw.bundle_e_sqlite3` overrides that with the bundled `e_sqlite3` native so the Core library ships one consistent SQLite implementation regardless of the host OS's installed libsqlite. Without the explicit reference the Core library would dynamically link against whatever the consumer provides via `SQLitePCLRaw.provider.*` registration — fine for apps, fragile for a redistributable library distributed as NativeAOT.

## 74. `SkiaSharp` retained in the Windows platform project

**Rationale**: `WindowsIconProvider` extracts an `HICON` via Win32 then PNG-encodes via `SKImage.Encode(SKEncodedImageFormat.Png, 100)`. Avalonia's rendering layer uses Skia internally but does not expose `SKImage`/`SKBitmap`/`SKEncodedImageFormat` publicly, so referencing `SkiaSharp` directly in the platform project is the minimum path to PNG output. Not used by Avalonia; not used by picker rendering.

## 75. `Rule.Origin` field added to the config schema

**Rationale**: Distinguishes user-authored rules (`"user"`, the default) from picker-generated remember rules (`"remember"`). Forwards-compatible — existing configs without the field round-trip fine since `Origin` defaults to `"user"`. The Phase 7 management UI will filter / sort / bulk-delete by origin; Phase 3 picker writes `Origin = "remember"` on every persisted rule.

---

## Phase 4 decisions (added 2026-07-01)

## 76. macOS `.app` bundle via MSBuild target

**Rationale**: Native AOT produces a Mach-O binary, not a `.app` bundle. macOS won't accept the binary as a URL handler until wrapped with `Info.plist` declaring `CFBundleURLTypes`. Solution: `src/AskMeFirst/Properties/PublishProfiles/MacOsBundle.targets` exposes a `<Target Name="CreateMacBundle" AfterTargets="Publish">` that wraps `dotnet publish` output into `AskMeFirst.app/Contents/{MacOS,Resources,Info.plist}`. Same command produces the bundle across the CI matrix.

**Rejected**: hand-rolled script (`scripts/build-mac-bundle.sh`, splits publish + bundle into two steps, less integrated); defer Mac to Phase 9 (skips an OS entirely, blocks Mac users); README-only instructions (defeats the `install` command's purpose).

## 77. Linux `.desktop` `Exec=` = absolute path of running binary

**Rationale**: `install` reads its own location via `Environment.ProcessPath` and writes it verbatim into `Exec=<absolute path> %u`. Matches freedesktop convention; works regardless of `$PATH` setup. If the user moves the binary, the `.desktop` file goes stale until they re-run `install` — acceptable for v1.

**Re-evaluate**: when Flatpak/Snap packaging begins (Phase 9). Canonical-location-with-symlink becomes the right shape then.

## 78. `ISourceAppWindowLocator` + `NullSourceAppWindowLocator` + `ScreenBounds` moved to `Core/Abstractions/`

**Rationale**: Phase 4 adds real per-platform implementations in `AskMeFirst.Platforms.*` (Windows: Win32 `GetWindowRect`; macOS: `CGWindowListCopyWindowInfo`). Platforms.\* → Picker would be a reverse dep (Platforms.\* projects are meant to be leaves that implement Core abstractions). Moving the interface to Core matches the existing pattern (`IBrowserInventory`, `IUrlLauncher`, `INormalizer`, `IIconProvider`, `INotifier`, etc. all live in Core). `ScreenBounds` moved too because it's used by both `IScreenProvider` (Picker, Avalonia-coupled) and `ISourceAppWindowLocator` (now Core); Core can't depend on Picker. `IScreenProvider` + `ScreenInfo` + `IWindowPositionProvider` stay in Picker (Avalonia-coupled).

**Test stub** `FixedSourceAppWindowLocator` moves to `tests/AskMeFirst.Picker.Tests/Services/` (was in `src/AskMeFirst.Picker/Services/`, never used in production).

## 79. `FocusExisting: bool` field removed from `RuleThen` + `RoutingDecision`

**Rationale**: Per "never keep dead code." The field was plumbing with no implementation — no `IRunningBrowserDetector`, no wiring into the launch path. Browser-built-in dedup handles the daily case: Chrome reuses the running instance when launched with a URL, Firefox's `-new-tab` does the same. Removing the field (8-file cascade: 3 src + 1 test + 1 sample + 3 docs) is one logical commit.

**Rejected**: leave the field for "future Phase 4+ might want it" — YAGNI, no concrete plan.

## 80. `IDefaultBrowserRegistrar` = async interface

**Rationale**: `lsregister` on macOS is genuinely slow (1–3 s scanning the Launch Services DB). All three methods return `Task<T>` for uniformity: `Task<RegistrationResult> RegisterAsync(CancellationToken ct = default)`, `Task<RegistrationResult> UnregisterAsync(...)`, `Task<bool> IsRegisteredAsync(...)`. Pattern precedent: picker's Unshortener returns `Task<string?>` with `CancellationToken` (decision #61).

**Rejected**: sync (ignores the real perf concern); `IsDefault()` query method (YAGNI — defer until `askmefirst status` shows up; querying `UserChoice` on Windows is fragile).

## 81. `RegisterAsync` + `UnregisterAsync` are idempotent

**Rationale**: Install/uninstall commands should be safe to re-run. `RegisterAsync` checks `IsRegisteredAsync` first; if true, returns `RegistrationResult(Success: true, "Already registered.")` without writing. `UnregisterAsync` is best-effort: returns `RegistrationResult(Success: true, "Nothing to do.")` if not registered. Both methods never throw on "already/not" state — only on real OS errors.

## 82. Post-install UX = try deep-link + print fallback

**Rationale**: Deep-link gives one-click UX on both Win (`ms-settings:defaultapps`) and Mac (`x-apple.systempreferences:com.apple.preference.general?DefaultWebBrowser`). Print instructions are the reliable fallback across OS versions (especially macOS where Apple has been removing these URIs). Pattern: `TryOpenOsSettings()` wraps `Process.Start` in try/catch, logs to stderr, always prints instructions afterward. Linux auto-claims via `xdg-mime`, no UX step needed.

## 83. Test strategy = D + unit-testable bits

**Rationale**: No CI integration smoke tests for OS impls (`RegisterAsync`/`UnregisterAsync`/real `ISourceAppWindowLocator`) — manual verify via Phase 4 implementation checklist. ~6 unit tests for unit-testable logic, no new test project: `NewWindow=true` → `--new-window` (Chromium) and `-new-window <url>` (Firefox); `NewWindow` default = `false`; `install`/`uninstall` orchestration with mocked `IDefaultBrowserRegistrar`; `TryOpenOsSettings` deep-link fallback on `Process.Start` exception.

**Rejected**: full CI integration per OS (high flakiness, maintenance burden); mock-heavy unit tests only (doesn't catch impl bugs, which is where the bugs will be); no tests at all (violates "non-trivial logic leaves one runnable check behind").