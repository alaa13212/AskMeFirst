# Phase 3 — Picker UI Design

**Status**: 🚧 Implementation in progress (vertical slice started 2026-06-28)
**Source**: grill-me session 2026-06-28
**Decisions**: 55–70 in [`docs/decisions-log.md`](./decisions-log.md)

## Overview

Phase 3 ships the **Avalonia picker** — the GUI fallback that fires when no rule conclusively routes a URL. The picker is the **learning** mechanism, not the daily driver: rules accumulate via "remember" actions, and after a week of use the picker rarely fires.

The picker:
- Shows the source app, original URL, and live-resolved URL (when unshortener is in flight).
- Presents `(browser, profile)` tuples as buttons. Pinned profiles only by default.
- Presents 3–5 "remember" radios (dynamic count based on context).
- Captures user decision: launch browser, optionally write a remember rule, return result to parent.

## Architecture

### Routing integration: `ShowPicker(PickerRequest)` outcome variant

The picker is a third routing outcome, not a routing intent:

```csharp
public abstract record RoutingOutcome;
public sealed record Success(Browser Browser, Uri FinalUrl, Uri OriginalUrl) : RoutingOutcome;
public sealed record Failure(RoutingExitCode Code, string Message) : RoutingOutcome;
public sealed record ShowPicker(PickerRequest Request) : RoutingOutcome;   // ← NEW
```

The resolver pipeline (Explicit / Rule / Fallback) stays at 3 members. When all three return `null`, `RuleRouter.Route()` constructs a `PickerRequest` and returns `ShowPicker` directly — the picker is the **catch-all** for unruled URLs.

Symmetric with how `Failure` already works: resolvers never produce `Failure` directly; the executor does. Here the router itself produces `ShowPicker` when no resolver matched.

### Packaging: single binary, in-process Avalonia, deferred init

```csharp
// Program.cs (sketch)
if (args.Length > 0 && args[0] == "pick")
    return PickerEntryPoint.Run(args[1..]);   // builds Avalonia app, blocks

return CliEntryPoint.Run(args);               // today's path — Avalonia untouched
```

- CLI cold-start: **~46ms** (unchanged — Avalonia is statically linked but never initialized).
- Picker cold-start: **~150ms** (Avalonia init). Acceptable — user will spend many seconds deciding.
- Binary size: ~12–18 MB (Avalonia + CommunityToolkit.Mvvm + FluentTheme).
- Distribution: single file. Phase 9 (winget / brew / apt) stays clean.

`IPickerLauncher` is the only thing that touches Avalonia types. `Composition.cs` only wires it on the `pick` command path.

## Project structure

```
src/
├── AskMeFirst.Core/                  ← existing, no GUI deps
│   └── Routing/
│       ├── RoutingOutcome.cs         ← + ShowPicker variant
│       ├── PickerRequest.cs          ← NEW
│       ├── PickerResult.cs           ← NEW (result returned from picker)
│       └── RuleRouter.cs             ← + catch-all path → ShowPicker
│
├── AskMeFirst.Picker/                ← NEW
│   ├── AskMeFirst.Picker.csproj      ← Avalonia + CommunityToolkit.Mvvm
│   ├── PickerEntryPoint.cs           ← Avalonia app builder
│   ├── ViewModels/
│   │   ├── PickerWindowViewModel.cs  ← main VM, ~120 LOC
│   │   ├── BrowserOptionViewModel.cs
│   │   ├── RememberOptionViewModel.cs
│   │   └── PickerStatus.cs           ← enum
│   ├── Views/
│   │   ├── PickerWindow.axaml
│   │   └── PickerWindow.axaml.cs
│   ├── Services/
│   │   ├── IPickerLauncher.cs
│   │   ├── PickerLauncher.cs
│   │   ├── WindowPositionProvider.cs
│   │   ├── BrowserLaunchFailureNotifier.cs
│   │   └── PinnedProfileFilter.cs
│   └── Resources/
│       └── (icons, themes)
│
└── AskMeFirst/                       ← thin CLI host
    └── Program.cs                    ← dispatches CLI vs Picker
```

**Why a separate project?** Clean AOT boundary. `AskMeFirst.Core` stays free of GUI deps so its tests don't pull Avalonia. `AskMeFirst.Picker` is conditionally published — for the picker-mode build only.

## Routing pipeline integration

### `PickerRequest` — what the router hands the picker

```csharp
public sealed record PickerRequest(
    Uri OriginalUrl,
    string? SourceApp,                            // OS-normalized, null if undetectable
    Task<string?>? UnshortenTask,                 // null if URL not from known shortener
    IReadOnlyList<BrowserOption> AvailableBrowsers);
```

### `PickerResult` — what the picker returns

```csharp
public abstract record PickerResult;
public sealed record Cancelled : PickerResult;                                                  // X / Esc / Cancel
public sealed record Launched(Browser Browser, Uri Url) : PickerResult;                         // commit, no rule
public sealed record LaunchedWithRule(Browser Browser, Uri Url, RuleSpec Rule) : PickerResult;  // commit + remember

public sealed record BrowserOption(Browser Browser, BrowserProfile? Profile);  // (browser, profile) tuple
```

### `RuleRouter.Route()` catch-all

```csharp
public int Route(Uri url, string? explicitBrowserId, string? explicitProfileId)
{
    RoutingContext ctx = BuildContext(url, explicitBrowserId, explicitProfileId);

    foreach (ITargetResolver resolver in resolvers)
    {
        RoutingIntent? intent = resolver.Resolve(ctx);
        if (intent is not null)
        {
            return executor.Execute(intent, url) switch
            {
                Success s    => Launch(s),
                Failure f    => Log(f),
                ShowPicker p => LaunchPicker(p.Request),
                _ => throw new InvalidOperationException(),
            };
        }
    }

    // No resolver matched → picker is the catch-all
    return LaunchPicker(BuildPickerRequest(ctx, url));
}

private int LaunchPicker(PickerRequest request)
{
    PickerResult result = pickerLauncher.ShowModal(request);
    return result switch
    {
        Cancelled           => ExitCode.UserCancelled,
        Launched l          => Launch(l.Browser, l.Url),
        LaunchedWithRule lr => WriteRuleAndLaunch(lr.Rule, lr.Browser, lr.Url),
        _ => throw new InvalidOperationException(),
    };
}
```

## ViewModels

### `PickerWindowViewModel`

```csharp
public sealed partial class PickerWindowViewModel : ObservableObject
{
    private readonly PickerRequest _request;
    private readonly IUnshortener _unshortener;
    private readonly IConfigWriter _configWriter;
    private readonly CancellationTokenSource _cts = new();

    public PickerWindowViewModel(
        PickerRequest request,
        IUnshortener unshortener,
        IConfigWriter configWriter)
    {
        _request = request;
        _unshortener = unshortener;
        _configWriter = configWriter;
        _displayUrl = request.OriginalUrl.ToString();
        BrowserOptions = BuildBrowserOptions(request.AvailableBrowsers);
        RememberOptions = BuildRememberOptions(request);
        _ = ResolveUnshortenerAsync();
    }

    [ObservableProperty] private PickerStatus _status = PickerStatus.Loading;
    [ObservableProperty] private string _displayUrl;
    [ObservableProperty] private bool _isResolving;
    [ObservableProperty] private int _selectedBrowserIndex;
    [ObservableProperty] private int _selectedRememberIndex;   // default 0 = "Just this once"

    public IReadOnlyList<BrowserOptionViewModel> BrowserOptions { get; }
    public IReadOnlyList<RememberOptionViewModel> RememberOptions { get; }

    [RelayCommand] private async Task CommitAsync() { ... }
    [RelayCommand] private void Cancel() { ... }

    private async Task ResolveUnshortenerAsync() { ... }   // see Unshortener Integration
}
```

### `BrowserOptionViewModel`

```csharp
public sealed class BrowserOptionViewModel
{
    public Browser Browser { get; }
    public BrowserProfile? Profile { get; }
    public string DisplayLabel { get; }    // "Chrome (Personal)" or "Firefox (Work)"
    public string HotkeyLabel => Hotkey >= 0 ? Hotkey.ToString() : "";
    public int Hotkey { get; }             // 1-9 for hotkey, -1 for >9

    public BrowserOptionViewModel(Browser browser, BrowserProfile? profile, int hotkey) { ... }
}
```

### `RememberOptionViewModel`

```csharp
public sealed class RememberOptionViewModel
{
    public string Label { get; }                                  // "Just this once", etc.
    public RememberKind Kind { get; }                             // Once, AlwaysExactHost, AlwaysWildcardHost, AlwaysSource, SourcePlusHost
    public string? DisplayUrlPattern { get; }                     // "company.atlassian.net" or "*.atlassian.net"
    public string? DisplaySourceName { get; }                     // "Slack"

    public bool IsAvailable { get; }                              // false if context-dependent (no source / no host)
    public string UnavailableReason { get; } = "";                // shown when IsAvailable == false
}
```

## State machine

```
Initial
  ↓ (constructor)
Loading
  ↓ (BrowserOptions bound)
Resolving        ← only if _request.UnshortenTask != null
  ↓ (1s timeout OR resolved)
Ready
  ↓ (user commits OR cancels)
Committing | Cancelled
  ↓
Done
```

```csharp
public enum PickerStatus
{
    Loading,
    Resolving,
    Ready,
    Committing,
    Done,
}
```

Implementation: flat enum + `[ObservableProperty]`. View binds to `Status` to drive visibility of progress indicators. No state pattern — over-engineered for 5 transitions.

## Window behavior

### Position

```csharp
public PixelPoint ComputeWindowPosition(Size windowSize, IScreenProvider screens, ISourceAppWindowLocator? locator)
{
    // 1. Try source-app-center (where easy)
    if (locator?.TryGetSourceWindowBounds(out Rect sourceBounds) == true)
        return CenterOver(sourceBounds, windowSize);

    // 2. Fall back to active-monitor center
    Screen activeScreen = screens.Primary;   // or screens.ScreenFromPoint(cursor) if available
    return new PixelPoint(
        activeScreen.Bounds.X + (activeScreen.Bounds.Width - (int)windowSize.Width) / 2,
        activeScreen.Bounds.Y + (activeScreen.Bounds.Height - (int)windowSize.Height) / 2);
}
```

**Per-platform `ISourceAppWindowLocator`**:
- Windows: `GetWindowRect` on parent PID (easy — already have PID).
- macOS: `CGWindowListCopyWindowInfo` with parent PID (easy).
- Linux: returns `false`. Falls back to active-monitor center.

### Modal-ness

- `Topmost = true` (always-on-top).
- **Not** modal. User can alt-tab to source app without canceling the picker.
- `CanResize = false`.

### Cancel semantics

X button, Esc key, and Cancel button all → `PickerResult.Cancelled`. Single semantic per existing decision #21.

## Mouse Control

Choose browser: One click commit
Choose remember => choose browser: 2 clicks commit

## Keyboard navigation

Standard Windows dialog feel:

| Key | Action                                                                  |
|---|-------------------------------------------------------------------------|
| `Tab` / `Shift+Tab` | Cycle controls (browser buttons → remember radios → close button → wrap) |
| `↑` / `↓` | Cycle within current panel (browser buttons OR remember radios)         |
| `←` / `→` | Same as Tab / Shift+Tab                                                 |
| `1`–`9` | Direct-commit browser button (only first 9 have hotkeys)                |
| `Enter` | Commit current selection                                                |
| `Esc` | Cancel                                                                  |

### Initial focus

First browser button focused. First remember radio (`Just this once`) preselected. Plain Enter = safe ignore-flow (launch first browser, no rule).

### 1-9 hotkey limit

Most users have ≤9 `(browser, profile)` tuples. Power users with >9 pin their favorites (see [Profile Pinning](#profile-pinning)) — picker shows pinned-only, fits within 1-9.

## Profile pinning

`ProfileSpec` gains `Pinned` field:

```csharp
public sealed record ProfileSpec(
    string Id,
    string BrowserId,
    string? Name,
    string? Directory,
    string? DisplayName,
    bool Pinned = false);   // NEW
```

`PinnedProfileFilter` reduces the available browser list:

```csharp
public static class PinnedProfileFilter
{
    public static IReadOnlyList<BrowserOption> Filter(IReadOnlyList<BrowserOption> all, IReadOnlyList<ProfileSpec> specs)
    {
        IReadOnlyList<string> pinnedIds = specs.Where(s => s.Pinned).Select(s => s.Id).ToList();
        if (pinnedIds.Count == 0) return all;   // no pins → show all (sensible default)

        return all.Where(opt => opt.Profile is null   // default profile (no ProfileSpec) — always shown
                                || pinnedIds.Contains(opt.Profile.Id)).ToList();
    }
}
```

**Default behavior**: if no profiles are pinned, picker shows all (first-time-user friendly). Pin/unpin surface ships in Phase 7+ management UI.

## Unshortener integration

### Trigger

Only fires when picker is about to show AND URL is from known shortener domain. Per architecture.md decision #17.

### Lifecycle

```csharp
private async Task ResolveUnshortenerAsync()
{
    if (_request.UnshortenTask is null) { Status = PickerStatus.Ready; return; }

    IsResolving = true;
    try
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(1));

        string? resolved = await _request.UnshortenTask.WaitAsync(timeoutCts.Token);
        if (resolved is not null) DisplayUrl = resolved;
    }
    catch (OperationCanceledException) { /* timeout or commit-cancel — keep original */ }
    catch (Exception ex) { logger.LogWarning($"Unshortener failed: {ex.Message}"); /* silent fallback */ }
    finally
    {
        IsResolving = false;
        Status = PickerStatus.Ready;
    }
}
```

### Cancellation on commit

`CommitAsync()` calls `_cts.Cancel()` before returning. Unshortener Task observes cancellation and stops. Config write happens off-thread.

### Error handling

Silent fallback to original URL. Picker doesn't expose plumbing. Per decision #63.

## Remember rule schema

5 radios per [`docs/rule-engine.md`](./rule-engine.md#rule-generation-from-picker-remember):

| Radio | Generated `when` | Dynamic availability |
|---|---|---|
| `Just this once` | (no rule) | always |
| `Always company.atlassian.net` | `UrlMatchesAny: ["company.atlassian.net"]` | always if URL has host |
| `Always *.atlassian.net` | `UrlMatchesAny: ["*.atlassian.net"]` | always if URL has host |
| `Always Slack` | `ProcessIn: ["slack"]` | only if source app detected |
| `Slack + company.atlassian.net` | `ProcessIn: ["slack"], UrlMatchesAny: ["company.atlassian.net"]` | only if source app + host |

3, 4, or 5 visible radios depending on context. Disabled radios show `UnavailableReason` (e.g. "No source app detected").

Generated rules: `priority: 50`, `origin: "remember"`, auto-generated `name` like `"Slack + *.atlassian.net → firefox-work"`.

## Recent-picks JSONL

Append-only audit log at `<config-dir>/recent-picks.jsonl`:

```jsonl
{"ts":"2026-06-28T22:15:33Z","url":"https://company.atlassian.net/wiki","sourceApp":"slack","browserId":"firefox-work","profileId":"firefox-work-profile","ruleWritten":true}
{"ts":"2026-06-28T22:16:01Z","url":"https://github.com/foo/bar","sourceApp":"code","browserId":"chrome-personal","profileId":null,"ruleWritten":false}
```

Schema: `{ts, url, sourceApp, browserId, profileId, ruleWritten}`. One line per picker commit. Phase 7+ `askmefirst suggest` reads this for rule suggestions.

## Post-commit browser-launch failure

If browser launch fails after commit (browser.exe missing, profile gone, etc.):

```csharp
private int Launch(Browser browser, Uri url)
{
    try
    {
        urlLauncher.Open(browser, url);
        return (int)RoutingExitCode.Success;
    }
    catch (Exception ex)
    {
        logger.LogError($"Browser launch failed: {ex.Message}");
        notifier.Show(
            title: "Couldn't open browser",
            message: $"Couldn't open {browser.DisplayName}. The URL is in your recent picks; try again.");
        return (int)RoutingExitCode.BrowserNotFound;
    }
}
```

`BrowserLaunchFailureNotifier` is per-platform:
- Windows: `Windows.UI.Notifications.ToastNotificationManager`.
- macOS: `NSUserNotificationCenter` (deprecated but still works; `UserNotifications` framework in future).
- Linux: `notify-send` via `Process.Start`.

## Testing strategy

### Unit tests (no Avalonia)

- `PickerWindowViewModelTests`: state transitions, commit/cancel commands, unshortener cancellation.
- `PinnedProfileFilterTests`: filter logic, default-show-all when no pins.
- `RoutingOutcomeTests`: `ShowPicker` variant dispatch.
- `RuleRouterTests`: catch-all path constructs `ShowPicker`.
- `PickerRequestBuilderTests`: dynamic radio count (3/4/5) based on context.

### Headless Avalonia tests (optional)

Snapshot test of `PickerWindow.axaml` rendered output. Skipped in default CI; run manually for visual regression.

### Integration test

End-to-end: `askmefirst <unruled-url>` opens picker, user picks browser via headless input, browser launches. Slow; tagged `Integration` so CI skips by default.

## Open items / deferred

### To Phase 7+ management UI
- Pin/unpin UI surface (`ProfileSpec.Pinned` is settable in JSON now).
- Forget rule mechanism (picker can't show for already-ruled URLs).
- Rule sorting / reordering.
- Test browser buttons.

### To Phase 4 (OS integration)
- Source-app-window locator implementation (currently Win + Mac only).
- Per-platform toast notification implementation (currently sketched).

### To Phase 6 (polish)
- Browser icons in picker buttons (extracted from each browser's resources).
- Recent-picks UI display in picker (currently JSONL only).
- `askmefirst suggest` reading the JSONL.

### Tactic / implementation-time decisions
- Exact AXAML styling (Fluent theme default; customization deferred).
- Cursor offset when positioning near source app.
- Default window size (start with 720×440 from architecture.md decision #16).
