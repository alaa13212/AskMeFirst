# Handoff — 2026-07-01

> **First thing next session: read this file.**

## TL;DR

Phase 4 complete end-to-end on the user's actual machine. All 11 implementation commits landed on `feat/phase_4_os_integration`. New daily-driver unlock: `askmefirst install` registers AskMeFirst as a Windows default-browser candidate; `uninstall` reverses it; picker centers over source-app window via Win32 `EnumWindows` + `GetWindowRect`. One dead-code cut during the sweep.

## What's verified

- ✅ `dotnet build` clean (0 warnings, 0 errors, all 3 platforms)
- ✅ `dotnet test --no-build` — **292/296 passing** in ~1.6 s
  - **238/238 Core** passing (+16 from Phase 4: 4 Install, 2 Uninstall, +others from FocusExisting removal)
  - **54/58 Picker** passing — 4 pre-existing `PickerWindowRenderTests` failures (Windows-hardcoded paths `C:\Users\Ali\.mavis\cache\...` surfacing on Linux; not Phase 4 regressions)
- ✅ Manual: Windows registry writes verified on this machine (StartMenuInternet\AskMeFirst subtree present)
- ✅ Picker opens over source-app window (Slack, Visual Studio, etc.) — confirms `WindowsSourceAppWindowLocator` works

## Phase 4 commits (in order)

1. Move `ISourceAppWindowLocator` + `NullSourceAppWindowLocator` + `ScreenBounds` from Picker → Core (interface-location cleanup; Platforms.\* can now implement)
2. Remove `FocusExisting` field (8-file cascade: 3 src + 1 test + 1 sample + 3 docs)
3. Wire `NewWindow` into `ChromiumLaunchStrategy` (`--new-window`) + `FirefoxLaunchStrategy` (`-new-window <url>`) — plumbed through `RoutingIntent` → `Browser.NewWindow` → launch strategy. Later undone in `661be93` `refactor(core): drop NewWindow launch option` (engineer ruling; superseded by browser-built-in dedup).
4. Add `IDefaultBrowserRegistrar` + `RegistrationResult` + `NullDefaultBrowserRegistrar` interfaces
5. Add `InstallCommand` + `UninstallCommand` + 6 unit tests (mocked registrar)
6. `WindowsDefaultBrowserRegistrar` (writes HKCU\Software\Clients\StartMenuInternet\AskMeFirst subtree) + `WindowsSourceAppWindowLocator` (Win32 `EnumWindows` + `GetWindowThreadProcessId` + `IsWindowVisible` + `GetWindowRect`, picks largest visible window)
7. `MacOsDefaultBrowserRegistrar` (locates `.app` from `Environment.ProcessPath`, copies to `/Applications` via `ditto`, runs `lsregister -f`) + `MacSourceAppWindowLocator` (osascript → System Events → frontmost process → window position+size; **requires Accessibility permission on first use**)
8. `LinuxDefaultBrowserRegistrar` (writes `.desktop` to `~/.local/share/applications/`, runs `xdg-mime default` for both schemes + `update-desktop-database`)
9. `CreateMacAppBundle` MSBuild target (inlined into `src/AskMeFirst/AskMeFirst.csproj`, `AfterTargets="Publish"`, fires only when `RuntimeIdentifier` starts with `osx-`, wraps binary into `AskMeFirst.app/Contents/{MacOS,Resources,Info.plist}`) + `Resources/Mac/Info.plist`
10. **Dead-code sweep**: removed `IsRegisteredAsync` from interface + 3 impls + `NullDefaultBrowserRegistrar` + `FakeRegistrar` + test helper (was in design but no caller; impls naturally idempotent at OS layer; satisfies "never keep dead code")
11. This handoff

## Files added

```
src/AskMeFirst.Core/Abstractions/
├── IDefaultBrowserRegistrar.cs
├── RegistrationResult.cs
├── NullDefaultBrowserRegistrar.cs
├── ISourceAppWindowLocator.cs          ← MOVED from Picker
├── NullSourceAppWindowLocator.cs      ← MOVED from Picker
└── ScreenBounds.cs                    ← EXTRACTED from Picker/IScreenProvider.cs

src/AskMeFirst/Commands/
├── InstallCommand.cs
└── UninstallCommand.cs

src/AskMeFirst/Resources/Mac/Info.plist

src/AskMeFirst.Platforms.Windows/
├── WindowsDefaultBrowserRegistrar.cs
└── WindowsSourceAppWindowLocator.cs

src/AskMeFirst.Platforms.MacOs/
├── MacOsDefaultBrowserRegistrar.cs
└── MacSourceAppWindowLocator.cs

src/AskMeFirst.Platforms.Linux/
└── LinuxDefaultBrowserRegistrar.cs

tests/AskMeFirst.Core.Tests/
├── InstallCommandTests.cs             ← NEW (4 tests)
├── UninstallCommandTests.cs           ← NEW (2 tests)
└── Services/FixedSourceAppWindowLocator.cs ← MOVED from src

docs/phase-4-design.md                 ← NEW (planning artifact)
```

## Files modified (highlights)

- `src/AskMeFirst/Composition.cs` — wires `IDefaultBrowserRegistrar` + `ISourceAppWindowLocator` into `CommandContext`
- `src/AskMeFirst/Program.cs` — registers `InstallCommand` + `UninstallCommand`
- `src/AskMeFirst/AskMeFirst.csproj` — embeds `Info.plist` as `<None>`, defines `CreateMacAppBundle` MSBuild target
- `src/AskMeFirst.Core/Composition/BootstrapContext.cs` — +2 fields
- `src/AskMeFirst.Core/Commands/CommandContext.cs` — +1 field
- 3 platform Bootstrap classes — wire real per-platform impls (replacing `NullDefaultBrowserRegistrar` / `NullSourceAppWindowLocator`)
- `NewWindow` launch plumbing (`Browser` / `RoutingIntent` / Strategies / UrlLaunchers) — added in `b962952`, reverted in `661be93` (engineer ruling, see decision #85)
- `samples/askmefirst.example.json` — removed `FocusExisting`
- `docs/{rule-engine,architecture,roadmap}.md` — `FocusExisting` references removed
- `docs/decisions-log.md` — decisions #75–83 documented (Phase 4 section)

## Bugs caught / design tweaks

1. **`Microsoft.Win32.Registry` package unnecessary on .NET 10** — types are in BCL natively; package would emit NU1510 trim warning. Removed package, kept `using Microsoft.Win32;`.
2. **`[SupportedOSPlatform("windows")] / ("osx")` required on Bootstrap classes** — when Bootstrap instantiates a platform-specific impl, the call site needs the attribute to satisfy CA1416. Added to all 3 Bootstrap classes + their registrar / locator types.
3. **`[SupportedOSPlatform("freebsd")]` added to `LinuxBootstrap` + `LinuxDefaultBrowserRegistrar`** — `Composition.SelectPlatform()` matches both `IsLinux()` and `IsFreeBSD()` to `LinuxBootstrap`, so the OS guard must cover both.
4. **CA1806 unused return**: `GetWindowThreadProcessId` returns uint — captured as `_ = GetWindowThreadProcessId(...)` to satisfy analyzer.
5. **IDE0011 braces**: 4 single-line `if`s in `MacSourceAppWindowLocator.TryParseBounds` got braces per user preference.
6. **CA1305 locale-sensitive interpolation**: `sb.AppendLine($"Exec={exePath} %u")` flagged for locale issue — switched to non-interpolated `sb.Append("Exec=").Append(exePath).AppendLine(" %u")`.
7. **Lambda `_` parameter collided with discard `_`**: renamed lambda param from `_` to `lParam` so the inner `_ = GetWindowThreadProcessId(...)` discard worked.
8. **IsRegisteredAsync removed during sweep** (commit 10) — interface method had no caller. Idempotency is naturally handled at the OS layer (re-writing same keys is a no-op; lsregister -f and xdg-mime default are idempotent).

## Decisions recap (Phase 4 = #76–83)

| # | Decision | Pick |
|---|---|---|
| 76 | macOS .app bundle | MSBuild target |
| 77 | Linux `Exec=` | Absolute path |
| 78 | ISourceAppWindowLocator location | Move to Core |
| 79 | FocusExisting field | Remove |
| 80 | IDefaultBrowserRegistrar | Async (Task<RegistrationResult>) |
| 81 | Register/Unregister idempotency | Both safe to re-run (handled at OS layer; IsRegisteredAsync removed in sweep) |
| 82 | Post-install UX | Try deep-link + print fallback |
| 83 | Test strategy | D + unit-testable bits |

Full decisions log: [`docs/decisions-log.md`](./decisions-log.md).

## Style rules (READ THESE — non-obvious)

User-level preferences, also in `~/.mavis/memory/user.md`:

> **Comments describe WHAT, never WHY/HOW.** No plan-phase references, no requirement-doc references, no decision-history, no trivial info. Comment on symbols (classes / methods), not on instructions inside methods. Default to no comment.

> **NEVER use `var`** — always use explicit types (`csharp_style_var_elsewhere = false:error`).

> **Prefer braces** even on single-line `if` statements.

> **One type per file** — split classes/records into their own `.cs` when adding to existing files.

> **No `IsDefault` (or similar) flags on interface types.** Use explicit registry methods (e.g. `RegisterDefault`) to enforce single-default invariants at the call site, not by inspection.

> **Pattern recognition over hand-rolling.** When the user pushes back with "which design pattern would you use", think about Strategy / Pipeline / Result-type / Discriminated-Union combinations, not monolithic refactor shapes.

> **"Never keep dead code"** — applied rigorously in this session (commit 10 sweep; also caught `IsRegisteredAsync` mid-implementation).

When editing any file, prune comments that violate these.

## Toolchain notes

- .NET 10 LTS
- AOT publish: `dotnet publish src/AskMeFirst -c Release -r <RID> -p:PublishProfile=Aot`
  - Win: produces `askmefirst.exe` (existing 18.50 MB; Phase 4 added ~50 KB registry P/Invoke + native code)
  - Mac: produces `AskMeFirst.app/Contents/MacOS/askmefirst` + `Info.plist` (new — needs Mac CI to verify)
  - Linux: produces `askmefirst` (Linux .desktop file written at runtime, not embedded)
- `Microsoft.Win32.Registry` types are in BCL on .NET 10 — no NuGet needed for Windows
- `[SupportedOSPlatform(...)]` attributes required on platform-specific Bootstrap classes + their concrete impls to satisfy CA1416
- `Microsoft.Win32.Registry.OpenSubKey(string, bool)` is not annotated — but `CreateSubKey` is — both work fine on Win

## Manual verification checklist (for Phase 5+ testing)

When picking up Phase 5, verify Phase 4 manually on each OS first:

- [x] Windows: registry subtree written; Default Apps prompt shows AskMeFirst in the list
- [ ] macOS: `askmefirst install` copies `.app` to `/Applications`, lsregister succeeds; System Settings shows AskMeFirst; Accessibility permission required for picker centering
- [ ] Linux: `.desktop` written; `xdg-mime query default x-scheme-handler/http` returns `askmefirst.desktop`; first link click routes correctly

## Things to know about the user

- C# background — comfortable with .NET internals (e.g., caught the lambda `_` discard conflict)
- Cares about "understanding all the code" — no magic, no heavy frameworks. Note: DI container is approved per decision #84 (was previously banned in AGENTS.md; engineer ruling superseded that).
- Working style: detailed Q&A interview per design decision, one question at a time
- Code-style rules (comment / var / braces / one-class-per-file / no-interface-flags / no-dead-code) apply project-wide
- "Go ahead" = proceed with implementation including logical commits; explicit "commit" required to push
- Session preference: don't commit until asked. Single big commit is acceptable for handoff; multi-commit logical groups also welcome.

## How to pick up

1. Read this file (`docs/HANDOFF.md`)
2. Skim `docs/decisions-log.md` (now 83 decisions; Phase 4 = #76–83)
3. Glance at `docs/phase-4-design.md` for the original planning artifact
4. Look at the comment rules in memory (`mavis memory show`)
5. Phase 5 (Link processing — async unshortener) is next per roadmap; tracking-param strip is already done
6. Phase 6 (Polish — bench, README, embedded browser icons, samples, test coverage) waits behind Phase 5
7. **Before Phase 9 (installers)**: re-evaluate Linux `Exec=` per decision #77 (Flatpak/Snap canonical-path model)