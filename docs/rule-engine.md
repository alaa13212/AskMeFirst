# Rule Engine

The rule engine is the heart of AskMeFirst. It takes a URL plus config and returns a routing decision.

## Config file

A single JSON file at the OS-standard config location:

- Windows: `%APPDATA%\AskMeFirst\config.json`
- macOS: `~/Library/Application Support/AskMeFirst/config.json`
- Linux: `~/.config/AskMeFirst/config.json`

Comments (`//` and `/* */`) are accepted by the parser. The file extension stays `.json` for tooling compatibility.

## Schema

```jsonc
{
  "settings": {
    "DefaultBrowserId": null,
    "StripTracking": true,
    "Unshorten": false,
    "InventoryCacheTtl": 24
  },

  "TrackingParamsExtra": ["si", "feature", "igshid"],
  "TrackingParamsOverride": false,

  "UnshortenDomainsExtra": ["myshortener.company.com"],
  "Unshorten_domains_override": false,

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

  "rules": [
    {
      "name": "Work domains to Firefox Work profile",
      "priority": 100,
      "when": {
        "UrlMatchesAny": ["*.atlassian.net", "*.github.com", "*.slack.com"]
      },
      "then": {
        "browser": "firefox-work",
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

1. Sort rules by `priority` descending. Ties are broken by array order.
2. For each rule in order, evaluate all `when` predicates as logical AND.
3. First rule with all predicates matching wins.
4. Apply the rule's `then`. Missing fields fall back to `settings` defaults.
5. If no rule matched, the implicit catchall shows the picker.

## Predicates

All predicates are optional. Omitted = match anything.

| Predicate | Matches when... | Example |
|---|---|---|
| `UrlMatchesAny: [globs]` | URL host+path matches any glob | `["*.github.com", "github.com"]` |
| `UrlMatchesAll: [globs]` | URL host+path matches every glob | `["https://*", "*.amazon.com/*"]` |
| `UrlRegex: "..."` | URL matches a regex | `"^https://github\\.com/[^/]+/[^/]+/issues/\\d+"` |
| `SchemeIn: [list]` | URL scheme is in the list | `["https"]` |

**Globs** use `*` for one segment and `**` across dots or path separators. Patterns match against the URL's host + path.

## Actions

All actions are optional. Omitted = use sensible default.

| Action | Effect | Default |
|---|---|---|
| `browser: <id>` | which browser to use | required |
| `profile: <name>` | which browser profile | browser's default profile |
| `private: bool` | open in private/incognito | `false` |
| `StripTracking: bool` | override `settings.StripTracking` | inherited |
| `Unshorten: bool` | override `settings.Unshorten` | inherited |

## Built-in tracking parameter list

These are always stripped when `StripTracking: true`. User config can extend or replace the list.

```
utm_source, utm_medium, utm_campaign, utm_term, utm_content,
utm_id, utm_name, utm_brand, utm_social, utm_creative_format,
fbclid, gclid, gbraid, wbraid, msclkid, dclid,
_ga, _gl, mc_eid, mc_cid, yclid, ref, ref_src
```

## Built-in shortener domain list

Triggers the Unshortener when picker would show and the URL matches. User config can extend or replace the list. See [link-processing.md](./link-processing.md#Unshortener-triggering-rules).

```
t.co, bit.ly, tinyurl.com, goo.gl, ow.ly, is.gd, buff.ly,
lnkd.in, rebrand.ly, shorturl.at, cutt.ly, rb.gy, ift.tt,
fb.me, t.me, dlvr.it, snip.ly, po.st, mcaf.ee, tr.im,
v.gd, adf.ly, sh.st, tcrn.ch, bl.ink, clck.ru, short.io
```

## Rule generation from picker "remember"

When the user picks a browser in the picker with a non-default remember option, the picker writes a URL-based rule:

| Remember choice | Generated rule shape |
|---|---|
| `Just this once` | no rule |
| `Always company.atlassian.net` | `UrlMatchesAny: ["company.atlassian.net"]` |
| `Always *.atlassian.net` | `UrlMatchesAny: ["*.atlassian.net"]` |

Generated rules get `priority: 50` and `origin: "remember"`.

```jsonc
{
  "name": "Remembered: * *.atlassian.net",
  "priority": 50,
  "origin": "remember",
  "when": { "UrlMatchesAny": ["*.atlassian.net"] },
  "then": { "browser": "firefox-work" }
}
```

`origin` defaults to `"user"` when omitted.

## Selector tips

- **Globs** are matched against host + path. `*.amazon.com` matches `amazon.com`, `www.amazon.com`, `smile.amazon.com`.
- **Regexes** are matched against the full URL with no anchors added.
- **Order matters within the same priority.** Earlier in the array wins.

## Validation

Config is validated at load. Errors include file path and a clear message:

```
config.json:42 - unknown browser id 'firefox_wok'
config.json:58 - UrlRegex 'invalid[' has parse error: unterminated character class
```

On invalid config, router mode falls back to embedded defaults and logs a warning. Picker mode shows a notification.

## Re-evaluation

v1: config is re-read on every CLI invocation. mtime check is <1 ms.

Phase 7+ management UI can hot-reload via filesystem watcher.
