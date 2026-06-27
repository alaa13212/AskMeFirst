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
    "default_browser": null,

    // Global toggles (overridable per-rule).
    "strip_tracking": true,
    "unshorten": false,

    // Browser inventory cache freshness.
    "inventory_cache_hours": 24
  },

  // Tracking parameters to strip. Appends to or replaces the built-in list.
  "tracking_params_extra": [
    "si",                  // YouTube share id
    "feature",             // YouTube share feature
    "igshid"               // Instagram share id
  ],
  "tracking_params_override": false,   // true = replace built-in entirely

  // Shortener domains that trigger the unshortener.
  // Only relevant when picker is about to show.
  "unshorten_domains_extra": [
    "myshortener.company.com"
  ],
  "unshorten_domains_override": false, // true = replace built-in entirely

  // Browser / profile definitions. P2 = auto-discovered, this block overrides.
  "browsers": [
    {
      "id": "chrome-personal",
      "display_name": "Chrome (Personal)",
      "executable": "auto",
      "profile": "Default"
    },
    {
      "id": "chrome-work",
      "display_name": "Chrome (Work)",
      "executable": "auto",
      "profile": "Profile 1"
    },
    {
      "id": "firefox-work",
      "display_name": "Firefox (Work)",
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
        "process_in": ["slack", "outlook", "code", "teams"],
        "url_matches_any": [
          "*.atlassian.net",
          "*.github.com",
          "*.slack.com"
        ]
      },
      "then": {
        "browser": "firefox-work",
        "focus_existing": true,
        "strip_tracking": true,
        "unshorten": false
      }
    },
    {
      "name": "YouTube always to personal Chrome",
      "priority": 80,
      "when": {
        "url_matches_any": ["youtube.com", "youtu.be"]
      },
      "then": {
        "browser": "chrome-personal"
      }
    },
    {
      "name": "GitHub PRs to Chrome Work",
      "priority": 90,
      "when": {
        "process_in": ["slack", "outlook"],
        "url_regex": "^https://github\\.com/[^/]+/[^/]+/pull/\\d+"
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
   - If any predicate references data we don't have (e.g. `process_in` requires the source process), the predicate is considered not matched → skip this rule.
   - First rule with all predicates matching wins.
3. Apply the rule's `then`. Missing fields fall back to `settings` defaults.
4. If no rule matched → implicit catchall shows the picker.

## Predicates

All predicates are optional. Omitted = match anything.

| Predicate | Matches when... | Example |
|---|---|---|
| `process_in: [list]` | source process name (canonical, case-insensitive) is in the list | `["slack", "outlook", "code"]` |
| `url_matches_any: [globs]` | URL host+path matches any glob | `["*.github.com", "github.com"]` |
| `url_matches_all: [globs]` | URL host+path matches every glob | `["https://*", "*.amazon.com/*"]` |
| `url_regex: "..."` | URL matches a regex | `"^https://github\\.com/[^/]+/[^/]+/issues/\\d+"` |
| `scheme_in: [list]` | URL scheme in list | `["https"]` |
| `time_between: "HH:MM-HH:MM"` | current local time in range | `"09:00-18:00"` |
| `weekday_in: [list]` | weekday in list | `["Mon", "Tue", "Wed", "Thu", "Fri"]` |
| `browser_running: bool` | the *then* browser is currently running | `true` |

**Process name normalization** is OS-specific and happens in the platform layer. The rule author writes `slack` and the platform layer maps `Slack.exe` / `slack` / `com.tinyspeck.chatlyo` to `slack`. See [platform-integration.md](./platform-integration.md#source-app-detection-l1).

**Globs** use `*` (any chars except `.`) and `**` (any chars including `.`). Patterns match against the URL's host + path (no scheme, no query by default).

## Actions

All actions are optional. Omitted = use sensible default.

| Action | Effect | Default |
|---|---|---|
| `browser: <id>` | which browser to use (must exist in `browsers:`) | required |
| `profile: <name>` | which browser profile | browser's `default_profile` |
| `focus_existing: bool` | route to already-running instance | `true` |
| `new_window: bool` | open new browser window vs new tab | `false` (new tab) |
| `private: bool` | open in private/incognito | `false` |
| `strip_tracking: bool` | override `settings.strip_tracking` | inherited |
| `unshorten: bool` | override `settings.unshorten` | inherited |

## Built-in tracking parameter list

These are always stripped when `strip_tracking: true`. User can extend via `tracking_params_extra` or replace via `tracking_params_override: true`.

```
utm_source, utm_medium, utm_campaign, utm_term, utm_content,
utm_id, utm_name, utm_brand, utm_social, utm_creative_format,
fbclid, gclid, gbraid, wbraid, msclkid, dclid,
_ga, _gl, mc_eid, mc_cid, yclid, ref, ref_src
```

## Built-in shortener domain list

Triggers the unshortener when picker would show AND URL matches. User can extend via `unshorten_domains_extra` or replace via `unshorten_domains_override: true`. See [link-processing.md](./link-processing.md#unshortener-triggering-rules).

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
| `Always company.atlassian.net` | `url_matches_any: ["company.atlassian.net"]` |
| `Always *.atlassian.net` | `url_matches_any: ["*.atlassian.net"]` |
| `Always Slack` | `process_in: ["slack"]` |
| `Slack + company.atlassian.net` | `process_in: ["slack"], url_matches_any: ["company.atlassian.net"]` |

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
config.json:58 - url_regex 'invalid[' has parse error: unterminated character class
```

On invalid config, router mode falls back to embedded defaults (still functional, just generic) and logs a warning. Picker mode shows a notification.

## Re-evaluation

v1: config is re-read on every CLI invocation. mtime check is <1 ms. Pure stateless.

Phase 7+ (management UI): config is hot-reloaded via filesystem watcher while the UI is open.