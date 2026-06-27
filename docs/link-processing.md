# Link Processing

Two optional transformations applied to the URL before it reaches the chosen browser.

## 1. Tracking parameter stripping

**Always synchronous, always local. Adds < 1 ms per URL.**

### What gets stripped

Built-in default list (see [rule-engine.md](./rule-engine.md#built-in-tracking-parameter-list)). User can extend via `tracking_params_extra` or replace via `tracking_params_override: true`.

### Algorithm

1. Parse URL into `(scheme, host, path, query, fragment)` using `Uri` (BCL).
2. If `query` is null/empty, return URL unchanged.
3. Split query by `&`. For each `key=value`:
   - If `key` matches a tracking pattern (exact match, case-sensitive), drop it.
   - Otherwise, keep it.
4. Reassemble URL preserving order of non-stripped params.
5. Strip fragment if it's a tracking fragment (rare; `#utm_...`).

### Edge cases

- Keys without values: `?utm_source` → stripped.
- Keys with `=` in value: `?ref=a=b` → keep `ref=a=b` if `ref` is not a tracker.
- Duplicate keys: keep first occurrence, drop subsequent ones if any is a tracker.
- Case sensitivity: query keys are case-sensitive per RFC 3986; we match exactly.

### Performance

For a typical URL with 3–10 query params, this takes **< 100 microseconds**. Effectively free.

---

## 2. Unshortening

**Network call. Picker-only. Async with live UI update.**

### Triggering rules

The unshortener runs **only when ALL of these are true**:

1. The picker is about to show (no rule conclusively resolved the URL).
2. The URL is from a **known shortener domain** (built-in list + user extensions).
3. The chosen rule (or default settings) has `unshorten: true`, OR no rule matched and the picker is showing (in which case `unshorten: true` is implicit).

If the rule matched and decided `unshorten: false`, we **skip unshortening entirely** — the rule already decided, the user knows the destination.

### Known shortener domains

Built-in default list (~27 entries) plus user extensions. See [rule-engine.md](./rule-engine.md#built-in-shortener-domain-list) for the full list. User can extend via `unshorten_domains_extra` or replace via `unshorten_domains_override: true`. Management UI exposes this in Phase 7+.

### Strategy: async, non-blocking, live UI update

The unshortener runs in a background task. The picker shows immediately with:

- **Original short URL** displayed in the URL area
- **"⟳ resolving..."** indicator next to it
- The picker is **fully interactive** while unshortening runs

When unshortening completes:

- The URL display **updates live**: `t.co/abc → example.com/article?lang=en`
- The "resolving..." indicator disappears
- If unshortening **fails** (timeout, network error, redirect loop): keep showing short URL, log a warning

User can commit their choice **at any time**. The choice stands regardless of unshortening state.

### Algorithm

```csharp
public static async Task<string?> UnshortenAsync(
    string url,
    TimeSpan timeout,
    CancellationToken ct)
{
    using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
    cts.CancelAfter(timeout);

    using var handler = new SocketsHttpHandler
    {
        AllowAutoRedirect = true,
        MaxAutomaticRedirections = 10,
    };
    using var client = new HttpClient(handler) { Timeout = timeout };

    try
    {
        using var req = new HttpRequestMessage(HttpMethod.Head, url);
        using var resp = await client.SendAsync(
            req, HttpCompletionOption.ResponseHeadersRead, cts.Token);
        return resp.RequestMessage?.RequestUri?.ToString() ?? url;
    }
    catch (OperationCanceledException) { return null; }
    catch (HttpRequestException) { return null; }
}
```

### Timeout

Default **1 second**. Configurable via `settings.unshorten_timeout_ms`. If exceeded, fall back to short URL.

### Why this matters for routing

Without unshortening, `t.co/abc` would route based on the short domain (Twitter = personal Chrome). With unshortening, the picker shows the resolved URL (e.g. `company.atlassian.net/...`), letting the user pick based on the **actual destination**.

### Privacy

Unshortening sends an HTTP request to the shortener service. Implications:

- Shortener sees our IP. Standard for any HTTP traffic.
- User-Agent: `"AskMeFirst/1.0 (unshortening)"` — explicit, no .NET version leakage.
- **No cookies sent.** `HttpClient` defaults are fine.
- We never read response body — only `RequestMessage.RequestUri` after redirects.

### What unshortening does NOT do

- Does **not** modify the open browser tab. The browser itself follows redirects natively; no extension or remote-debugging needed.
- Does **not** block the picker's user interaction. User picks freely.
- Does **not** cache results. (Defer to v2 if needed — cache invalidation is tricky.)

---

## Order of operations

```
URL arrives from OS
  │
  ▼
1. Identify source process (parent PID → normalize)              (~5 ms)
  │
  ▼
2. Evaluate rules  →  decision = { browser, profile, unshorten, strip_tracking, ... }
  │
  ├── Rule matched:
  │     ├── if decision.strip_tracking: strip params            (~1 ms)
  │     ├── if decision.unshorten AND URL is known shortener:
  │     │     (this branch is rare — unshorten when rule matched
  │     │      is mostly a v2 feature; v1 unshortens on picker only)
  │     └── Launch browser with final URL                       (~300 ms)
  │
  └── No rule matched → show picker:
        ├── Fire async unshorten (if known shortener)
        ├── if strip_tracking: strip params                     (~1 ms)
        ├── Show picker window with live URL display
        ├── On commit: launch browser with chosen URL
        └── Exit
```

### Picker case timing

```
t=0      OS spawn
t=50     process ready
t=80     rule eval done (no match)
t=80     fire async unshortener, show picker window
t=80     picker visible to user (short URL + "resolving...")
t=80-500 typical unshortener resolution
t=500    URL display updates (if user hasn't picked yet)
variable user interaction
```

The picker's perceived startup is **~80 ms** (process + rule eval + window show). Unshortening is fully backgrounded.

---

## Edge cases to handle

- **Already-final URLs**: unshortener returns same URL → no UI update.
- **Redirect loops** (A → B → A → ...): `MaxAutomaticRedirections = 10` cuts off. We log and use the last non-looping URL.
- **Blocked shorteners** (some CDNs block headless User-Agents): fall back to GET with `Range: bytes=0-0` to fetch only headers.
- **Tracking-protected URLs** (require cookie consent before redirect): can't unshorten, use original.
- **URL with auth** (`https://user:pass@example.com`): strip and use as-is — don't unshorten.
- **Multiple redirects across domains**: just follow them all; we only care about the final URL.