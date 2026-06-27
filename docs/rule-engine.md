# Rule Engine

The rule engine is the heart of AskMeFirst. It takes `(source_process, url, environment)` and returns a routing decision.

## Config file

A single JSON file at the OS-standard config location:

- Windows: `%APPDATA%\AskMeFirst\config.json`
- macOS: `~/Library/Application Support/AskMeFirst/config.json`
- Linux: `~/.config/AskMeFirst/config.json`

Comments (`//` and `/* */`) are accepted by the parser — JSONC-style. The file extension stays `.json` for tooling compatibility, but you can write comments freely.

Source-app mapping uses **OS-normalized process names** directly — no tag indirection. (See [decisions-log.md](./decisions-log.md#6-app-tags).)

## Schema

```jsonc
{
  "settings": {
    // Default behavior when no rule matches.
    // v1 always shows the picker in this case (catchall).
    // This field is reserved for Phase 7+ when we may support
    // a "default browser" fallback.
    "DefaultBrowserId": null,

    // Global toggles (overridable per-rule).
    "StripTracking": true,
    "Unshorten": false,

    // Browser inventory cache freshness.
    "InventoryCacheTtl": 24
  },

  // Tracking parameters to strip. Appends to or replaces the built-in list.
  "TrackingParamsExtra": [
    "si",                  // YouTube share id
    "feature",             // YouTube share feature
    "igshid"               // Instagram share id
  ],
  "TrackingParamsOverride": false,   // true = replace built-in entirely

  // Shortener domains that trigger the Unshortener.
  // Only relevant when picker is about to show.
  "UnshortenDomainsExtra": [
    "myshortener.company.com"
  ],
  "Unshorten_domains_override": false, // true = replace built-in entirely

  // Browser / profile definitions. P2 = auto-discovered, this block overrides.
  "browsers": [
    {
      "id": "chrome-personal",
      "DisplayName": "Chrome (Personal)",
      "executable": "auto",
      "profile": "Default"
    },
    {
      "id": "chrome-work",
      "DisplayName": "Chrome (Work)",
      "executable": "auto",
      "profile": "Profile 1"
    },
    {
      "id": "firefox-work",
      "DisplayName": "Firefox (Work)",
      "executable": "/Applications/Firefox Work.app/Contents/MacOS/firefox",
      "profile": "work"
    }
  ],

  // Rules. Highest priority first. First match wins.
  "rules": [
    {
      "name": "Work links to Firefox Work profile",
      "priority": 100,
      "when": {
        "ProcessIn": ["slack", "outlook", "code", "teams"],
        "UrlMatchesAny": [
          "*.atlassian.net",
          "*.github.com",
          "*.slack.com"
        ]
      },
      "then": {
        "browser": "firefox-work",
        "FocusExisting": true,
        "StripTracking": true,
        "Unshorten": false
      }
    },
    {
      "name": "YouTube always to personal Chrome",
      "priority": 80,
      "when": {
        "UrlMatchesAny": ["youtube.com", "youtu.be"]
      },
      "then": {
        "browser": "chrome-personal"
      }
    },
    {
      "name": "GitHub PRs to Chrome Work",
      "priority": 90,
      "when": {
        "ProcessIn": ["slack", "outlook"],
        "UrlRegex": "^https://github\\.com/[^/]+/[^/]+/pull/\\d+"
      },
      "then": {
        "browser": "chrome-work",
        "profile": "Work"
      }
    }
  ]
}
```

## Rule evaluation

1. Sort rules by `priority` descending. Ties broken by array order (earlier wins).
2. For each rule in order:
   - Evaluate all `when` predicates. All must match (logical AND).
   - If any predicate references data we don't have (e.g. `ProcessIn` requires the source process), the predicate is considered not matched → skip this rule.
   - First rule with all predicates matching wins.
3. Apply the rule's `then`. Missing fields fall back to `settings` defaults.
4. If no rule matched → implicit catchall shows the picker.

## Predicates

All predicates are optional. Omitted = match anything.

| Predicate | Matches when... | Example |
|---|---|---|
| `ProcessIn: [list]` | source process name (canonical, case-insensitive) is in the list | `["slack", "outlook", "code"]` |
| `UrlMatchesAny: [globs]` | URL host+path matches any glob | `["*.github.com", "github.com"]` |
| `UrlMatchesAll: [globs]` | URL host+path matches every glob | `["https://*", "*.amazon.com/*"]` |
| `UrlRegex: "..."` | URL matches a regex | `"^https://github\\.com/[^/]+/[^/]+/issues/\\d+"` |
| `SchemeIn: [list]` | URL scheme in list | `["https"]` |
| `TimeBetween: "HH:MM-HH:MM"` | current local time in range | `"09:00-18:00"` |
| `WeekdayIn: [list]` | weekday in list | `["Mon", "Tue", "Wed", "Thu", "Fri"]` |
| `BrowserRunning: bool` | the *then* browser is currently running | `true` |

**Process name normalization** is OS-specific and happens in the platform layer. The rule author writes `slack` and the platform layer maps `Slack.exe` / `slack` / `com.tinyspeck.chatlyo` to `slack`. See [platform-integration.md](./platform-integration.md#source-app-detection-l1).

**Globs** use `*` (any chars except `.`) and `**` (any chars including `.`). Patterns match against the URL's host + path (no scheme, no query by default).

## Actions

All actions are optional. Omitted = use sensible default.

| Action | Effect | Default |
|---|---|---|
| `browser: <id>` | which browser to use (must exist in `browsers:`) | required |
| `profile: <name>` | which browser profile | browser's `DefaultProfile` |
| `FocusExisting: bool` | route to already-running instance | `true` |
| `NewWindow: bool` | open new browser window vs new tab | `false` (new tab) |
| `private: bool` | open in private/incognito | `false` |
| `StripTracking: bool` | override `settings.StripTracking` | inherited |
| `Unshorten: bool` | override `settings.Unshorten` | inherited |

## Built-in tracking parameter list

These are always stripped when `StripTracking: true`. User can extend via `TrackingParamsExtra` or replace via `TrackingParamsOverride: true`.

```
utm_source, utm_medium, utm_campaign, utm_term, utm_content,
utm_id, utm_name, utm_brand, utm_social, utm_creative_format,
fbclid, gclid, gbraid, wbraid, msclkid, dclid,
_ga, _gl, mc_eid, mc_cid, yclid, ref, ref_src
```

## Built-in shortener domain list

Triggers the Unshortener when picker would show AND URL matches. User can extend via `UnshortenDomainsExtra` or replace via `Unshorten_domains_override: true`. See [link-processing.md](./link-processing.md#Unshortener-triggering-rules).

```
t.co, bit.ly, tinyurl.com, goo.gl, ow.ly, is.gd, buff.ly,
lnkd.in, rebrand.ly, shorturl.at, cutt.ly, rb.gy, ift.tt,
fb.me, t.me, dlvr.it, snip.ly, po.st, mcaf.ee, tr.im,
v.gd, adf.ly, sh.st, tcrn.ch, bl.ink, clck.ru, short.io
```

## Rule generation from picker "remember"

When the user picks a browser in the picker with a non-default remember option, the picker writes a rule:

| Remember choice | Generated rule shape |
|---|---|
| `Just this once` | (no rule — transient decision) |
| `Always company.atlassian.net` | `UrlMatchesAny: ["company.atlassian.net"]` |
| `Always *.atlassian.net` | `UrlMatchesAny: ["*.atlassian.net"]` |
| `Always Slack` | `ProcessIn: ["slack"]` |
| `Slack + company.atlassian.net` | `ProcessIn: ["slack"], UrlMatchesAny: ["company.atlassian.net"]` |

Generated rules get `priority: 50` (below user-written rules at 100+) and `origin: "remember"` for filtering in the management UI.

## Selector tips

- **Process names** are OS-normalized — write `slack`, not `Slack.exe`.
- **Globs** are matched against host + path. `*.amazon.com` matches `amazon.com`, `www.amazon.com`, `smile.amazon.com`.
- **Regexes** are matched against full URL with no anchors added; you control them.
- **Order matters within the same priority.** Earlier in the array wins. Use priority for the big differences, order for tiebreaks.

## Validation

Config is validated at load. Errors include file path and a clear message:

```
config.json:42 - unknown browser id 'firefox_wok' (typo?)
config.json:58 - UrlRegex 'invalid[' has parse error: unterminated character class
```

On invalid config, router mode falls back to embedded defaults (still functional, just generic) and logs a warning. Picker mode shows a notification.

## Re-evaluation

v1: config is re-read on every CLI invocation. mtime check is <1 ms. Pure stateless.

Phase 7+ (management UI): config is hot-reloaded via filesystem watcher while the UI is open.