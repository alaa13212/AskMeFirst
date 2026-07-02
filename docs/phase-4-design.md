# Phase 4 — OS Integration Design

**Status**: 📋 Planning locked (2026-07-01)
**Branch**: `feat/phase_4_os_integration`
**Source**: grill session 2026-07-01
**Decisions**: 76–83 in [`docs/decisions-log.md`](./decisions-log.md)

## Scope (with cuts applied)

Full roadmap with two cuts:

- ✅ **`install` / `uninstall` commands** + `IDefaultBrowserRegistrar` (3 platform impls)
- ✅ **`NewWindow` wire-up** in `ChromiumLaunchStrategy` + `FirefoxLaunchStrategy`
- ✅ **`ISourceAppWindowLocator` real impls** for Windows (`GetWindowRect`) + macOS (`CGWindowListCopyWindowInfo`); Linux stays Null
- ✅ **Tests**: ~6 unit tests for `NewWindow` + install/uninstall orchestration + deep-link fallback. No CI integration smoke tests for OS impls (per locked test strategy).

Cut from Phase 4:
- ❌ **Previous-default restoration** — `Unregister` is best-effort, leaves OS in "no default" state.
- ❌ **`FocusExisting` Phase 4 work** — field removed (decision #79); browser-built-in dedup handles it.

## Exit criteria

1. `askmefirst install` registers AskMeFirst as a default-browser candidate on Win/Mac, auto-claims on Linux.
2. `askmefirst install` tries to deep-link to OS settings (Win: `ms-settings:defaultapps`, Mac: `x-apple.systempreferences:...`), prints instructions as fallback on failure.
3. `askmefirst uninstall` removes the registration. Idempotent on both ends.
4. `NewWindow=true` produces `--new-window` for Chromium and `-new-window <url>` for Firefox.
5. Picker centers over the source-app window on Win/Mac; falls back to primary-screen center on Linux.

## New types

### `Core/Abstractions/IDefaultBrowserRegistrar.cs`

```csharp
public interface IDefaultBrowserRegistrar
{
    Task<RegistrationResult> RegisterAsync(CancellationToken ct = default);
    Task<RegistrationResult> UnregisterAsync(CancellationToken ct = default);
    Task<bool> IsRegisteredAsync(CancellationToken ct = default);
}

public sealed record RegistrationResult(bool Success, string Message);
```

Idempotent: `RegisterAsync` checks `IsRegisteredAsync` first; returns `RegistrationResult(Success: true, "Already registered.")` if so. `UnregisterAsync` is best-effort: returns `RegistrationResult(Success: true, "Nothing to do.")` if not registered.

### `Core/Abstractions/NullDefaultBrowserRegistrar.cs`

Test/Composition default. Returns `RegistrationResult(Success: false, "No registrar registered for this platform.")` for both methods. Used when `IDefaultBrowserRegistrar` is not wired (e.g. tests, future Composition without registrar).

### `Core/Abstractions/ISourceAppWindowLocator.cs` (moved from Picker)

Already exists in `AskMeFirst.Picker/Services/`. Move to `Core/Abstractions/`. `NullSourceAppWindowLocator` (production null impl) moves with it.

### `Core/Abstractions/ScreenBounds.cs` (new file, extracted from Picker)

Was defined inline in `Picker/Services/IScreenProvider.cs`. Extracted to its own file per one-type-per-file rule.

### `Core/Config/RuleThen.cs` + `Core/Routing/RoutingDecision.cs` — remove `FocusExisting`

Delete the field from both. Update `RuleEngine.cs` mapper. Fix `RuleEngineTests.cs`. Remove from `samples/askmefirst.example.json` and docs.

## Per-platform implementation

### Windows (`AskMeFirst.Platforms.Windows`)

**`WindowsDefaultBrowserRegistrar`** writes:
- `HKCU\Software\Clients\StartMenuInternet\AskMeFirst\(shell\open\command)` = `"<exe path>" "%1"`
- `HKCU\Software\Clients\StartMenuInternet\AskMeFirst\Capabilities\StartMenu`
- `HKCU\Software\Clients\StartMenuInternet\AskMeFirst\Capabilities\URLAssociations` = `{ http: "AskMeFirst", https: "AskMeFirst" }`
- `HKCU\Software\Clients\StartMenuInternet\AskMeFirst\DefaultIcon` = `<exe path>,0`
- `HKCU\Software\Clients\StartMenuInternet` default value = `AskMeFirst`

Does NOT write `UserChoice`. `Unregister` deletes the `AskMeFirst` subtree.

**`WindowsSourceAppWindowLocator`** uses P/Invoke: `EnumWindows` → filter by `GetWindowThreadProcessId` matching parent PID → `GetWindowRect`.

### macOS (`AskMeFirst.Platforms.MacOs`)

**`MacOsDefaultBrowserRegistrar`**:
1. Locate `.app` from running binary path: walk up from `Environment.ProcessPath` until `Contents/MacOS/askmefirst` → `.app` root.
2. If `.app` not in `/Applications`, copy it there.
3. Run `/System/Library/Frameworks/CoreServices.framework/Versions/A/Frameworks/LaunchServices.framework/Versions/A/Support/lsregister -f <app>`.

**`MacSourceAppWindowLocator`** uses `CGWindowListCopyWindowInfo` filtered by source-app PID (P/Invoke via `CoreGraphics`).

**.app bundle production** (`src/AskMeFirst/Properties/PublishProfiles/MacOsBundle.targets`):
- `<Target Name="CreateMacBundle" AfterTargets="Publish">` wraps `dotnet publish` output into `AskMeFirst.app/Contents/{MacOS,Resources,Info.plist}`.
- `Info.plist` declares `CFBundleURLTypes` for `http`/`https` + `LSHandlerRank=Owner`.

### Linux (`AskMeFirst.Platforms.Linux`)

**`LinuxDefaultBrowserRegistrar`**:
1. Write `~/.local/share/applications/askmefirst.desktop` from embedded template, substituting `Exec=<absolute path of running binary> %u` (decision #77).
2. Run `xdg-mime default askmefirst.desktop x-scheme-handler/http`.
3. Run `xdg-mime default askmefirst.desktop x-scheme-handler/https`.
4. Run `update-desktop-database ~/.local/share/applications/`.

`Unregister` reverses: `xdg-mime unset-default` for both schemes + delete `.desktop` file.

**No source-app locator** — `NullSourceAppWindowLocator` (Linux stays on primary-screen-center fallback).

## New commands

### `install`

```
askmefirst install
```

Flow:
1. `RegisterAsync` from `IDefaultBrowserRegistrar`.
2. If `Success`: `TryOpenOsSettings()` (try/catch around `Process.Start` for deep-link).
3. Print result message.
4. Exit 0 on success, 1 on failure.

### `uninstall`

```
askmefirst uninstall
```

Flow:
1. `UnregisterAsync`.
2. Print result.
3. Exit 0 on success, 1 on failure.

Both idempotent.

## File-level change list

### New files

```
src/AskMeFirst.Core/Abstractions/IDefaultBrowserRegistrar.cs
src/AskMeFirst.Core/Abstractions/RegistrationResult.cs
src/AskMeFirst.Core/Abstractions/NullDefaultBrowserRegistrar.cs
src/AskMeFirst.Core/Abstractions/ISourceAppWindowLocator.cs          ← MOVED from Picker
src/AskMeFirst.Core/Abstractions/NullSourceAppWindowLocator.cs      ← MOVED from Picker
src/AskMeFirst.Core/Abstractions/ScreenBounds.cs                    ← EXTRACTED from Picker

src/AskMeFirst/Commands/InstallCommand.cs                           ← NEW
src/AskMeFirst/Commands/UninstallCommand.cs                         ← NEW

src/AskMeFirst.Platforms.Windows/WindowsDefaultBrowserRegistrar.cs  ← NEW
src/AskMeFirst.Platforms.Windows/WindowsSourceAppWindowLocator.cs   ← NEW

src/AskMeFirst.Platforms.MacOs/MacOsDefaultBrowserRegistrar.cs      ← NEW
src/AskMeFirst.Platforms.MacOs/MacSourceAppWindowLocator.cs         ← NEW

src/AskMeFirst.Platforms.Linux/LinuxDefaultBrowserRegistrar.cs      ← NEW

src/AskMeFirst/Properties/PublishProfiles/MacOsBundle.targets       ← NEW
src/AskMeFirst.Platforms.MacOs/Info.plist                           ← NEW (embedded resource)
```

### Modified files

```
src/AskMeFirst.Core/Composition/BootstrapContext.cs                  ← +IDefaultBrowserRegistrar
src/AskMeFirst/Composition.cs                                       ← wire InstallCommand/UninstallCommand + IDefaultBrowserRegistrar
src/AskMeFirst/Program.cs                                           ← register InstallCommand + UninstallCommand
src/AskMeFirst.Core/Config/RuleThen.cs                              ← REMOVE FocusExisting
src/AskMeFirst.Core/Routing/RoutingDecision.cs                      ← REMOVE FocusExisting
src/AskMeFirst.Core/Routing/RuleEngine.cs                           ← REMOVE FocusExisting mapper
src/AskMeFirst.Core/Launch/ChromiumLaunchStrategy.cs                ← +NewWindow → --new-window
src/AskMeFirst.Core/Launch/FirefoxLaunchStrategy.cs                 ← +NewWindow → -new-window <url>
src/AskMeFirst.Platforms.Windows/WindowsBootstrap.cs                 ← +registrar + source-app locator
src/AskMeFirst.Platforms.MacOs/MacOsBootstrap.cs                    ← +registrar + source-app locator
src/AskMeFirst.Platforms.Linux/LinuxBootstrap.cs                    ← +registrar

src/AskMeFirst.Picker/Services/IScreenProvider.cs                   ← remove ScreenBounds inline def (now from Core)
src/AskMeFirst.Picker/Services/NullSourceAppWindowLocator.cs        ← DELETED (moved to Core)
src/AskMeFirst.Picker/Services/ISourceAppWindowLocator.cs           ← DELETED (moved to Core)
src/AskMeFirst.Picker/Services/FixedSourceAppWindowLocator.cs       ← DELETED (move to tests)
```

### Test files

```
tests/AskMeFirst.Core.Tests/RuleEngineTests.cs                     ← remove FocusExisting test refs
tests/AskMeFirst.Core.Tests/NewWindowLaunchTests.cs                ← NEW (~3 tests)
tests/AskMeFirst.Core.Tests/InstallCommandTests.cs                 ← NEW (~2 tests, mocked registrar)
tests/AskMeFirst.Core.Tests/UninstallCommandTests.cs               ← NEW (~1 test)
tests/AskMeFirst.Picker.Tests/Services/FixedSourceAppWindowLocator.cs ← MOVED from src
```

### Docs

```
docs/decisions-log.md                                               ← +decisions 76-83
docs/architecture.md                                                ← remove FocusExisting mentions
docs/rule-engine.md                                                 ← remove FocusExisting mentions
docs/roadmap.md                                                     ← mark Phase 4 done
samples/askmefirst.example.json                                     ← remove FocusExisting
```

## Implementation order (~11 commits)

1. Move `ISourceAppWindowLocator` + `NullSourceAppWindowLocator` + `ScreenBounds` from Picker to Core.
2. Remove `FocusExisting` field (8-file cascade).
3. Wire `NewWindow` into `ChromiumLaunchStrategy` + `FirefoxLaunchStrategy` + unit tests.
4. Add `IDefaultBrowserRegistrar` + `RegistrationResult` + `NullDefaultBrowserRegistrar` interfaces to Core.
5. Add `InstallCommand` + `UninstallCommand` + unit tests.
6. Implement `WindowsDefaultBrowserRegistrar` + `WindowsSourceAppWindowLocator`.
7. Implement `MacOsDefaultBrowserRegistrar` + `MacSourceAppWindowLocator`.
8. Implement `LinuxDefaultBrowserRegistrar`.
9. Add `MacOsBundle.targets` MSBuild target + `Info.plist` resource.
10. Dead-code sweep (grep for unused public surface).
11. Update docs + decisions log + `HANDOFF.md`.

Each commit independently builds + tests pass.

## Out of scope (deferred)

- `IsDefault()` query method on registrar (decision #80).
- CI integration smoke tests for OS impls (decision #83).
- Previous-default restoration (cut from Phase 4).
- Flatpak/Snap canonical install paths (Phase 9 territory).
- `askmefirst status` command.
- macOS code signing / notarization (Phase 9).
- Winget/brew/apt packages (Phase 9).