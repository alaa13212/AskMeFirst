# Phase 5 — Link processing (unshortener)

Planning artifact for Phase 5. Captures decisions from the grill session that landed this design.

## Scope (locked)

Unshortener only, picker UI only. Per the Q1 decision:

- ✅ Async unshortener with 1 s timeout, picker-side live URL update
- ✅ Known-shortener-domain gating (built-in list, sourced from config.json)
- ✅ Engine strips tracking params from the resolved URL before it reaches the picker
- ⏸ Per-rule `Unshorten` toggle (deferred — Phase 2 already gates via the rule)
- ⏸ `UnshortenDomainsExtra` / `UnshortenDomainsOverride` config knobs (deferred — config schema is in `AppConfig` but not wired into the unshortener this phase)
- ⏸ HEAD → GET Range fallback (deferred — HEAD-only first; GET fallback if dogfooding surfaces HEAD-blocked shorteners)

## Decisions (from the grill)

| # | Decision | Choice | Rationale |
|---|---|---|---|
| 1 | Lifecycle owner | **Engine** (RuleRouter) | Orchestrator already decides to show picker — it should also own the unshortener task. Picker stays UI-only. |
| 2 | Push shape | **`Task<string?>` in `PickerRequest.UnshortenTask`** | Race-free; survives picker VM init that may race unshorten completion. |
| 3 | Cancellation | **Engine scope, picker never sees the token** | `HttpClient.Timeout` (1 s) bounds the in-flight request; no explicit CT plumbing needed in the engine. |
| 4 | HTTP method | **HEAD only** | Matches the doc's primary algorithm. GET fallback deferred. |
| 5 | HttpClient lifetime | **DI singleton, owned by `HttpUnshortener` for life** | Connection reuse, no socket exhaustion, AOT-safe. |
| 6 | Shortener list source | **`IShortenerDomainList` interface + `ConfigShortenerDomainList` reading `AppConfig`** | Testable; honors built-in defaults + extra/override already in the schema. |
| 7 | Strip on resolved URL | **Engine strips both original and resolved** | Consistent privacy wherever the URL ends up; picker stays dumb. |

## Engine flow (the only place that changes)

```
URL arrives
  → rule eval → no match → RuleRouter catchall path
    → BuildPickerRequest(url):
        if (IShortenerDomainList.IsKnown(url.Host))
            task = HttpUnshortener.ResolveAsync(url).ContinueWith(StripIfNotNull)
        else
            task = null
        return PickerRequest(OriginalUrl=url, UnshortenTask=task, ...)
    → pickerLauncher.Show(request)
      → picker VM awaits UnshortenTask with 1 s WaitAsync timeout
      → DisplayUrl updates live to resolved (stripped) URL
    → on picker dismiss: commit launches with the (possibly resolved) URL
```

## Picker VM bug fix (caught during this design)

`PickerWindowViewModel.Commit` currently launches with `_request.OriginalUrl` (line 121). After unshortening, this defeats the feature — the user picked based on the resolved URL, but the browser opens the short URL. Phase 5 fixes this: VM tracks `_finalUrl` (starts as `OriginalUrl`, updates when unshorten resolves), `Commit` launches `_finalUrl`.

## Files added

```
src/AskMeFirst.Core/Routing/
├── BuiltInShortenerDomains.cs       ← static IReadOnlySet<string> of 27 entries
├── IShortenerDomainList.cs          ← interface (testability seam)
├── ConfigShortenerDomainList.cs     ← reads AppConfig + built-in defaults
└── HttpUnshortener.cs               ← ctor(HttpMessageHandler, TimeSpan, ILogger)

tests/AskMeFirst.Core.Tests/Services/
├── HttpUnshortenerTests.cs          ← fake DelegatingHandler: redirect chain, 404, timeout, cancel
├── ConfigShortenerDomainListTests.cs
└── RuleRouterShortenerTests.cs      ← shortener URL fires unshortener; non-shortener doesn't
```

## Files modified

- `src/AskMeFirst.Core/RuleRouter.cs` — inject `IUnshortener` + `IShortenerDomainList` + `TrackingStripper`; build unshorten task in `BuildPickerRequest`
- `src/AskMeFirst.Core/Commands/PickCommand.cs` — wire the same unshorten task (the `pick` command also pops the picker)
- `src/AskMeFirst.Picker/ViewModels/PickerWindowViewModel.cs` — track `_finalUrl`; `Commit` launches `_finalUrl` not `OriginalUrl`
- `src/AskMeFirst/Composition.cs` — register `IUnshortener` (with `SocketsHttpHandler` + 1 s timeout) and `IShortenerDomainList`
- `src/AskMeFirst.Core/Resources/DefaultConfig.jsonc` — already has `UnshortenDomainsExtra` / `UnshortenDomainsOverride`; no edit needed
- `docs/link-processing.md` — update with Phase 5 implementation choices (mostly already correct)

## Built-in shortener list (27 entries)

From `docs/rule-engine.md`:

```
t.co, bit.ly, tinyurl.com, goo.gl, ow.ly, is.gd, buff.ly,
lnkd.in, rebrand.ly, shorturl.at, cutt.ly, rb.gy, ift.tt,
fb.me, t.me, dlvr.it, snip.ly, po.st, mcaf.ee, tr.im,
v.gd, adf.ly, sh.st, tcrn.ch, bl.ink, clck.ru, short.io
```

Stored in `BuiltInShortenerDomains.Hosts` as `HashSet<string>` with `OrdinalIgnoreCase` comparer (hostnames are case-insensitive per RFC).

## `IUnshortener` contract (unchanged from existing)

```csharp
public interface IUnshortener
{
    Task<string?> ResolveAsync(Uri url, CancellationToken ct);
}
```

- `null` return = "could not resolve" (timeout, network error, HEAD blocked, redirect loop). Picker keeps showing the original URL.
- Never throws. OEC is caught internally. Other exceptions are caught and logged at Warn.
- Cancellation via the passed `CancellationToken` returns `null`.

## `HttpUnshortener` details

- `User-Agent: AskMeFirst/1.0 (Unshortening)` (set once on the singleton `HttpClient`)
- `HttpClient.Timeout` = 1 s (configurable per construction)
- `SocketsHttpHandler { AllowAutoRedirect = true, MaxAutomaticRedirections = 10 }`
- Single HEAD request, `HttpCompletionOption.ResponseHeadersRead`
- Returns `resp.RequestMessage?.RequestUri?.ToString()` after redirects
- Test seam: constructor takes `HttpMessageHandler` directly; tests inject a `DelegatingHandler` fake

## Test strategy

- **Unit**: `HttpUnshortener` with a fake `DelegatingHandler` returning canned responses (302 chain, 404, 405, network error). Verify returned URL / null / cancellation.
- **Unit**: `ConfigShortenerDomainList` — built-in only, extra extends, override replaces.
- **Integration**: `RuleRouter` with a fake `IUnshortener` — verify shortener URL passes a non-null task; non-shortener URL passes null.
- **Picker VM**: extend `PickerWindowViewModelTests` with cases for (a) `UnshortenTask` resolving to a URL → `DisplayUrl` updates and `Commit` launches the resolved URL; (b) `UnshortenTask` resolving to null → `DisplayUrl` stays and `Commit` launches `OriginalUrl`.

## Style rules

(No change from `~/.mavis/memory/user.md`.)

- No `var`
- Braces on single-line `if`
- One type per file
- Comments describe WHAT, not WHY/HOW
- No dead code
- No `IsDefault`-style flags on interfaces