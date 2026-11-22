# Phase 7 — Management UI design

**Status**: 🚧 Grill complete (2026-07-10). Implementation-ready.
**Source**: HTML prototypes at `prototypes/phase-7-mgmt-ui/` (variants A / B / C / D; D chosen)
**Decisions**: 86–96 in [`docs/decisions-log.md`](./docs/decisions-log.md)
**Roadmap section**: Phase 7 (optional) — Management UI (1–2 weeks)

## Overview

Phase 7 adds an Avalonia-based **Management UI** launched by `askmefirst config` — for browsing, editing, sorting, and pinning browsers, profiles, and rules; testing URLs against the rule engine; reviewing usage-derived rule suggestions; and editing unshortener / tracking-param config without touching JSON.

The Management UI runs in its own process via a second Avalonia entry point, parallel to the existing picker (`askmefirst pick`). Cold-start is unconstrained (user opens the UI deliberately, not on every URL click).

## Locked decisions (pre-grill fold-in)

### Layout — variant D (sidebar + cards + drill-replaces-grid)

Three prototypes were rendered and compared:

| Variant | Pattern | Source |
|---|---|---|
| A — Tabs | Familiar settings page | [`variant-a-tabs.html`](../../prototypes/phase-7-mgmt-ui/variant-a-tabs.html) |
| B — Sidebar | IDE-style left tree + master/detail right pane | [`variant-b-sidebar.html`](../../prototypes/phase-7-mgmt-ui/variant-b-sidebar.html) |
| C — Cards | Card-grid dashboard, drill into detail | [`variant-c-cards.html`](../../prototypes/phase-7-mgmt-ui/variant-c-cards.html) |
| **D — Mixed** | **B's tree + C's card grid + drill replaces grid (prototype showed inline drill)** | [`variant-d-mixed.html`](../../prototypes/phase-7-mgmt-ui/variant-d-mixed.html) |

Selected: **D**. Combines B's persistent navigation with C's at-a-glance grid. Drill behavior refined in grill: drilled card **replaces** the grid (full-pane detail view), not the prototype's inline-expand. See decision #92.

### Theme — follow OS Light/Dark via `ThemeVariant.Default`

Avalonia already supports system-theme following via `RequestedThemeVariant = ThemeVariant.Default`. The picker currently forces `ThemeVariant.Light` at runtime (`src/AskMeFirst.Picker/Services/AvaloniaPickerLauncher.cs:68`) and has hardcoded light-only colors in `src/AskMeFirst.Picker/Views/PickerWindow.axaml`.

Phase 7 ships both surfaces on system theme:

- **MgmtUi**: theme-aware from the start via `DesignTokens.axaml` `ResourceDictionary.ThemeDictionaries`.
- **Picker**: hardcoded colors → `{DynamicResource Token.*}`; drop the `RequestedThemeVariant = ThemeVariant.Light` line.
- **No user-visible theme toggle in v1.** Future enhancement if needed.
- **Test harness stays pinned to Light** for screenshot regression; new MgmtUi tests get a parallel `TestAppBuilderDark` harness.

## Locked decisions (grill session 2026-07-10)

| # | Decision | Pick | See |
|---|---|---|---|
| 91 | Apply model | Hybrid — Forms need Save, States (pin / enabled / order) autosave | below |
| 92 | Drill behavior | Drill **replaces** the grid (full detail pane), not inline expand; tree leaves drill to deeper sub-areas (2 drill depths) | below |
| 93 | Keyboard navigation | Alt+letter mnemonics + Esc=back to dashboard + Alt+←/→=prev/next drilled area | below |
| 94 | Hot-reload | `FileSystemWatcher` on `config.json` with debounce; auto-reload when no draft in flight; conflict prompt when a draft exists | below |
| 95 | Close behavior | Save / Discard / Cancel prompt when closing with drafts | below |
| 96 | First-run | Auto-init silently when `config.json` is missing | below |

### #91 Apply model — Hybrid with per-form Save/Discard

**States** (pinned flag, enabled flag, row order, profile visibility toggle) commit immediately through `JsonConfigWriter`. Reversible booleans; no half-state value to land in.

**Forms** (text inputs — rule patterns, regex strings, browser paths, unshortener domain entries, tracking-param entries) go through **per-form Save + Discard** pairs. Each editable row carries its own Save/Discard buttons; no global Apply button anywhere. Save writes via `JsonConfigWriter`; Discard rolls the row's text fields back to the on-disk value. Per-form drafts are isolated — editing rule 3 doesn't entangle rule 5's draft.

**Rationale:** booleans have no "broken half-state" to worry about; a half-typed regex does. The boundary is "can the value land in a broken state?" — yes → Save gate. Per-form granularity matches the visual hierarchy (one editable row = one form) and keeps Save/Discard discoverable without zooming out to a global action bar.

### #92 Drill behavior — drill replaces the grid; tree leaves drill deeper

Two drill depths:

- **Level 1 (card click):** clicking a card on the dashboard replaces the grid with that area's detail view (sortable table, edit controls). Active card state shows via sidebar highlight and a "Back to dashboard" affordance.
- **Level 2 (tree-leaf click):** after drilling into "Browsers", the sidebar tree shows its children (`Browsers > Firefox 132`, `Browsers > Chrome 130`, …). Clicking a leaf drills again into that single entity's detail view (e.g. Firefox's profile editor).

Tree nodes that are not leaves (i.e., section headers like "Inventory" / "Rules" / "Advanced") are expand/collapse controls only, with no drilling — those sections aren't drilled to as wholes, since their sub-areas are the actual cards.

### #93 Keyboard navigation — Alt+letter mnemonics + Esc/Alt-arrows

- **`Alt+B`** opens Browsers · **`Alt+R`** opens Rules · **`Alt+S`** opens Suggestions · … one letter per area card. Underlines on hover advertise the mnemonic.
- **Esc** = exit current drill (level 2 → level 1 → dashboard).
- **Alt+←** / **Alt+→** = prev / next drilled area (linear cycling through the 8 cards).
- The picker already uses 1–9 hotkeys (decision #67b); Alt+letter is the cross-platform desktop convention for menu-style nav.

### #94 Hot-reload — FileSystemWatcher + conflict prompt

`MgmtUiEntryPoint` subscribes a debounced `FileSystemWatcher` (~200 ms) on the resolved `config.json` path. On change:

- **No draft in flight** → re-read silently via `ConfigLoader`, swap the in-memory config, re-render all open views. State changes that the user has toggled since the last write are already on disk; auto-reload lands them back into the UI consistently.
- **Draft exists in some form** → show a modal prompt: "Config changed on disk. Keep your unsaved edits, or discard them and load the new file?" Keep = current draft wins, watcher event swallowed. Discard = drafts are dropped, new config loaded.

The prompt is two buttons (Keep / Discard), not three (no Cancel — that would leave the model in a half-consistent state).

### #95 Close behavior — Save / Discard / Cancel prompt

The window's `Closing` handler intercepts close-with-drafts. Three-button prompt:

- **Save** → apply all per-form drafts in one batched write; close.
- **Discard** → drop all drafts without writing; close.
- **Cancel** → keep the window open, drafts intact.

If no drafts are in flight (only state autosaves since startup), the close happens silently — no prompt.

### #96 First-run — auto-init silently

`MgmtUiEntryPoint` checks `File.Exists(configPath)` before constructing the window. If absent, calls the same internal logic as `InitCommand` (writes the polished sample to the OS config dir), then proceeds to load and show the UI. The user sees the post-init state on first launch. Subsequent launches find the file and skip.

Sample is pre-vetted schema from Phase 6; auto-init cannot write a corrupt config. Reversible (user can `git revert` or `rm` the file).

## Scope

Per [`docs/roadmap.md`](./roadmap.md#phase-7-optional--management-ui-1-2-weeks):

| Roadmap bullet | Phase 7 v1 | Notes |
|---|---|---|
| `askmefirst config` opens an Avalonia UI | ✅ | `config` entry point parallel to `pick` |
| Browse / edit / sort / pin browsers and profiles | ✅ | Card → table → per-row form |
| Edit rule command and add custom | ✅ | Per-rule form with Save/Discard |
| Test browsers (open https://example.com) | ✅ | Each browser row has a "Test" action |
| View / edit / sort rules + test URL → show matched rule | ✅ | Bottom action bar with Test URL field |
| Suggest rules based on usage (read `usage.jsonl`) | ✅ | Suggestions card; drill shows table + Add-rule |
| Configurable unshortener domains + tracking params in UI | ✅ | Two cards under Advanced |
| Hot-reload via `FileSystemWatcher` | ✅ | Decision #94 (was Phase 9 / `rule-engine.md:191`; promoted to v1) |

Deferred (out of v1):

- ❌ Bulk delete by `Rule.Origin` (decision #75 supports it; UI surface deferred — provide a per-row delete only in v1)
- ❌ In-app "Forget" affordance for picker-generated rules (decision #58) — no UI verb yet
- ❌ Manual theme toggle (decision #87)

## Architecture

### Entry points and dispatch

```
askmefirst <url>          → CliEntryPoint   (existing — ~46 ms cold start)
askmefirst pick           → PickerEntryPoint (existing — ~150 ms cold start)
askmefirst config         → MgmtUiEntryPoint (new — ~150 ms cold start, on-demand)
```

`Program.cs` dispatches on `args[0]`. `MgmtUiEntryPoint` mirrors `PickerEntryPoint` (separate Avalonia `AppBuilder`, blocks until window closes).

`MgmtUiEntryPoint` is responsible for:
1. Resolving the config path via `IConfigPathResolver`.
2. If `!File.Exists(configPath)` → invoke the same logic as `InitCommand`.
3. Constructing the DI graph (or `MgmtUiWindowViewModel` directly via composition root).
4. Subscribing the `FileSystemWatcher` on `configPath` with conflict-prompt plumbing.
5. Calling `StartWithClassicDesktopLifetime`.

### Project layout

```
src/AskMeFirst.MgmtUi/                                ← NEW (Phase 7)
├── AskMeFirst.MgmtUi.csproj                          ← Avalonia + CommunityToolkit.Mvvm
├── MgmtUiEntryPoint.cs                               ← Avalonia app builder; auto-init; FileSystemWatcher
├── AvaloniaApp.cs                                    ← Application subclass with FluentTheme + DesignTokens
├── ViewModels/
│   ├── MgmtUiWindowViewModel.cs                     ← root VM
│   ├── AreaCardViewModel.cs                         ← card on dashboard
│   ├── TreeNodeViewModel.cs                         ← sidebar tree node (header or leaf)
│   ├── BrowserTableViewModel.cs                     ← level-1 drill (browsers area)
│   ├── BrowserDetailViewModel.cs                    ← level-2 drill (per-browser profile editor)
│   ├── ProfileTableViewModel.cs
│   ├── RuleListViewModel.cs
│   ├── RuleDetailViewModel.cs                       ← level-2 (single rule editor)
│   ├── SuggestionsViewModel.cs
│   ├── UnshortenerConfigViewModel.cs
│   ├── TrackingConfigViewModel.cs
│   ├── CacheViewModel.cs
│   ├── UrlTesterViewModel.cs                        ← bottom bar
│   └── Conflicts/ConflictPromptViewModel.cs         ← modal for #94
├── Views/
│   ├── MgmtUiWindow.axaml/.cs                       ← 2-col Grid: Sidebar | MainPane, bottom TestURL bar
│   ├── AreaCardView.axaml/.cs
│   ├── TreeNodeView.axaml/.cs
│   ├── BrowserTableView.axaml/.cs                   ← sortable table, per-row Save/Discard
│   ├── BrowserDetailView.axaml/.cs
│   ├── RuleListView.axaml/.cs
│   ├── RuleDetailView.axaml/.cs
│   ├── SuggestionsView.axaml/.cs
│   ├── UnshortenerConfigView.axaml/.cs
│   ├── TrackingConfigView.axaml/.cs
│   ├── CacheView.axaml/.cs
│   ├── UrlTesterView.axaml/.cs
│   └── ConflictPromptView.axaml/.cs                 ← modal for #94
├── Resources/
│   └── DesignTokens.axaml                           ← Light + Dark palettes (shared with Picker)
└── Services/
    └── (MgmtUi-specific services, if any)
```

MgmtUi consumes only `Core` abstractions — same dependency direction as Picker.

### Theme resource strategy

`Resources/DesignTokens.axaml` defines named brushes for both palettes:

```xml
<ResourceDictionary xmlns="https://github.com/avaloniaui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <ResourceDictionary.ThemeDictionaries>
        <ResourceDictionary x:Key="Light">
            <SolidColorBrush x:Key="Surface.Primary"  Color="#0D1117" />
            <SolidColorBrush x:Key="Surface.Card"     Color="#161B22" />
            <SolidColorBrush x:Key="Surface.Elevated" Color="#21262D" />
            <SolidColorBrush x:Key="Border.Default"  Color="#30363D" />
            <SolidColorBrush x:Key="Text.Primary"    Color="#E6EDF3" />
            <SolidColorBrush x:Key="Text.Dim"        Color="#8B949E" />
            <SolidColorBrush x:Key="Accent.Default"  Color="#58A6FF" />
            ...
        </ResourceDictionary>
        <ResourceDictionary x:Key="Dark">
            <!-- same keys, Light-tuned values -->
        </ResourceDictionary>
    </ResourceDictionary.ThemeDictionaries>
</ResourceDictionary>
```

Both `AskMeFirst.Picker` and `AskMeFirst.MgmtUi` ship a `Resources/DesignTokens.axaml` with **identical** token definitions — kept in sync manually for now. A future shared-resource mechanism (or a single `<Resource Include Link="…">` between sibling projects) can collapse them; v1 keeps each project self-contained. Avalonia's asset URI is `avares://<AssemblyName>/Resources/DesignTokens.axaml`; the project's `AvaloniaApp` loads it via `AvaloniaXamlLoader.Load(uri)` and adds it to `Application.Resources.MergedDictionaries`. Custom controls reference tokens via `{DynamicResource Token.*}` — Avalonia resolves against `ActualThemeVariant` automatically.

### `MgmtUiWindowViewModel` sketch

```csharp
public sealed class MgmtUiWindowViewModel : ObservableObject
{
    // Sidebar (status block + tree)
    public ObservableCollection<StatusTip> StatusTips { get; }
    public ObservableCollection<TreeNodeViewModel> Tree { get; }

    // Main pane — either dashboard or drilled detail
    public ObservableCollection<AreaCardViewModel> DashboardCards { get; }
    public ViewModelBase? ActiveDrill { get; set; }       // null = dashboard; set = level-1 or level-2 VM
    public string ActiveDrillTitle { get; set; }          // "Browsers" / "Browsers / Firefox 132"

    // Bottom action bar (Test URL only — no global Apply)
    public UrlTesterViewModel UrlTester { get; }
    public RelayCommand BackToDashboardCommand { get; }

    // Conflict prompt modal (for #94)
    public ConflictPromptViewModel? PendingConflict { get; set; }

    public MgmtUiWindowViewModel(
        IBrowserInventory inventory,
        ConfigLoader configLoader,
        IConfigWriter configWriter,
        IRuleEngine ruleEngine,
        IUsageLogReader usageReader,
        ILogger logger,
        IFileSystemWatcherFactory watcherFactory);     // injectable for tests
}
```

The bottom action bar carries **only** the Test URL field + match feedback; no Apply, no Save All, no Discard. Save and Discard live per-row in drilled views.

Per-form drafts live inside each row VM as `DraftValues` (the in-progress text). Save commits the row's draft via `configWriter`; Discard rolls it back to the row's `OriginalValues`. State changes (pin / enabled / order) bypass drafts entirely and write immediately through the same `configWriter`.

### Composition root

MgmtUi has its own composition root (kept inside the project, no `Composition.cs` cross-dep):

```
MgmtUiEntryPoint.BuildGraph()
    → new MgmtUiWindowViewModel(
        inventory: new CachingBrowserInventory(realInv, cache),
        configLoader: new ConfigLoader(path),
        configWriter: new JsonConfigWriter(path),
        ruleEngine: new RuleRouter(...),
        usageReader: new UsageJsonlReader(...),
        logger: new StderrLogger(),
        watcherFactory: new FileSystemWatcherFactory());
```

DI container (decision #84) is used inside MgmtUi for VMs that have nested service needs (e.g. RuleDetailViewModel gets a `RuleValidator`). The composition root is still the only place new instances are constructed explicitly.

## Files modified (out-of-project)

- `src/AskMeFirst.Picker/Views/PickerWindow.axaml` — replace 8 hardcoded `#…` colors with `{DynamicResource Token.*}` lookups
- `src/AskMeFirst.Picker/Services/AvaloniaPickerLauncher.cs:68` — drop the `RequestedThemeVariant = ThemeVariant.Light` line
- `src/AskMeFirst/AskMeFirst.csproj` — new project reference to `AskMeFirst.MgmtUi`
- `src/AskMeFirst/Program.cs` — third dispatch arm for `config` command
- `src/AskMeFirst/Composition.cs` — adjust if MgmtUi needs anything routed through it (likely none, given the project split from decision #89)
- `.github/workflows/ci.yml` — MgmtUi smoke test gate (parallel to picker smoke); render-test in Light + Dark
- `docs/roadmap.md` — tick Phase 7 exit-criteria lines as they ship
- `docs/HANDOFF.md` — refresh after Phase 7 ships

## Exit criteria

1. `askmefirst config` opens the MgmtUi window within 400 ms cold start on each OS.
2. All 8 area cards render in both Light and Dark themes (verified by render-test, not just visual check).
3. Toggling a browser's pin flag persists to `~/.config/askmefirst/config.json` immediately on click (autosave path).
4. Editing a rule's URL pattern, then clicking that row's Save button, persists the new pattern; the row's Discard button rolls it back.
5. Bottom-bar URL tester shows which rule would match the entered URL (or "no rule — picker would fire").
6. Card click drills to a level-1 detail view (table editor); clicking a tree leaf inside that view drills to a level-2 detail view (per-entity editor).
7. Suggestions card shows the top 3 patterns derived from `usage.jsonl`; clicking "Add rule" writes a `Origin = "remember"` rule to config.
8. Picker window also follows system theme (no regression test failures from the swap).
9. Hot-reload: writing to `config.json` from an external editor within ~200 ms triggers either a silent re-read (no drafts) or a Keep/Discard prompt (drafts exist).
10. Closing the window with drafts triggers a Save / Discard / Cancel prompt; closing without drafts exits silently.
11. First launch on a fresh machine with no `config.json` auto-creates one from the polished sample and shows the populated UI.
12. AOT-clean: no reflection-heavy theme loading; MgmtUi ships as part of the single AOT binary.

## Remaining minor decisions

Animation and packaging — small enough to defer to the implementation step.

1. **Card flip / drill transition animation.** None in v1; transitions are instant. Adds visual polish if there's appetite later. ~50 LOC.
2. **Single AOT binary or split per project.** Phase 7 ships single binary (MgmtUi is an Avalonia entry point on the same `askmefirst` binary). Re-evaluate at Phase 9 (installers).
3. **Multiple MgmtUi instances.** No prevention; user can `askmefirst config` twice and get two windows. Trivial to add a single-instance guard later if it bugs anyone.
4. **State persistence across restarts.** Just window position (Avalonia's built-in). Drill state, scroll position, sort columns — fresh on each open.

## Test strategy

- **MgmtUi.Tests project** (new):
  - VM unit tests for each area's primary VM (10 VMs, ~3 tests each ≈ 30 tests).
  - Per-form draft tests: edit-discard-rollback, edit-save-commits, save-error-surfaced.
  - Hot-reload tests: file-watcher-with-no-draft path, file-watcher-with-draft → conflict-prompt.
  - Close-with-drafts tests: Save closes, Discard closes, Cancel reopens.
  - First-run tests: missing-config-auto-init.
- **Visual regression** (headless Avalonia): one smoke render per card in Light, one in Dark. No pixel-compare (use stable layout, not stable pixels). Test harness pinned to Light by default; parallel `TestAppBuilderDark` swap-in for the Dark pass.
- **Picker render tests**: continue pinned to Light; unchanged by the token swap (old hex values resolve identically under Light through the new tokens).
- **CI smoke**: a step that runs `askmefirst config` (or its headless test entry point) on each OS; must exit 0; analogous to the existing picker smoke gate.

## Style rules

No change from `~/.mavis/memory/user.md`:

- No `var`
- Braces on single-line `if`
- One type per file
- Comments describe WHAT, not WHY/HOW
- No dead code (Phase 7 must NOT carry prototype-only styling hacks into production)

## Prototype capture

The prototypes at `prototypes/phase-7-mgmt-ui/` are the primary source for the layout decision. They live alongside the source tree (not in a separate throwaway branch) because this repo has no branch-rotation pattern for prototypes; git history + the linked `.html` files are sufficient. Variant D's prototype showed inline-expand; the implementation replaces that with full-pane drill per decision #92.

## Verification (Phase 7 done = true)

```bash
# Local
dotnet build
dotnet test

# Manual on each OS:
askmefirst config                                  # opens MgmtUi (auto-inits if config missing)
# In MgmtUi: flip OS theme to Dark → app re-themes immediately
# In MgmtUi: click Suggestions card → drill to level 1; click a suggestion → Add rule via row Save
# In MgmtUi: edit a rule's URL pattern, click Save on that row → config.json updated; click Discard → reverts
# In MgmtUi: toggle a browser's pin flag → config.json updated immediately (no Save button needed)
# In MgmtUi: click a tree leaf under Browsers (e.g. Firefox 132) → drill to level 2 (profile editor)
# Edit config.json externally (echo '...' >> config.json) while MgmtUi has drafts → conflict prompt
askmefirst pick                                   # picker also follows OS theme

# CI
gh run watch                                      # MgmtUi smoke green on all 3 platforms
```
