# AskMeFirst

A cross-platform **browser router** — install once as the OS default browser, and AskMeFirst picks the right browser for every link based on your rules. Optional unshortening keeps t.co / bit.ly / etc. transparent. Tracking query params get stripped at the door.

> *Ask me first, then remember forever.* The picker is the learning mechanism — when you pick, AskMeFirst writes a "remember" rule so the same choice is automatic next time.

## Quick start

```bash
# 1. Install — registers AskMeFirst as a default-browser candidate on this OS.
askmefirst install

# 2. Seed your config — one starter rule, fully commented.
askmefirst init

# 3. Open the OS Default Apps picker and choose AskMeFirst.
# Windows: Settings → Apps → Default apps → AskMeFirst
# macOS:   System Settings → Desktop & Dock → Default web browser
# Linux:   usually auto-claimed via xdg-mime

# 4. Edit the config to your taste.
$EDITOR ~/.config/AskMeFirst/config.json   # Linux
$EDITOR ~/Library/Application\ Support/AskMeFirst/config.json   # macOS
notepad %APPDATA%\AskMeFirst\config.json   # Windows

# 5. Click any link. AskMeFirst routes it. Done.
```

## What it does

- **Routes URLs to browsers** by a JSON config of rules (glob, regex, scheme).
- **Asks first** via a picker window for URLs no rule covers, then **remembers forever** by writing a new rule.
- **Unshortens** links (t.co, bit.ly, etc.) inside the picker so you see the destination before committing.
- **Strips tracking** query params (`utm_*`, `fbclid`, `gclid`, …) on the way through.
- **Installs once** per OS; uninstalls cleanly.
- **Single self-contained binary** per platform via .NET 10 Native AOT.

## Commands

```
askmefirst <url> [--browser <id>] [--profile <profileId>] [--verbose]
    Route a URL to the chosen browser. (default — anything without a flag is a URL)

askmefirst init
    Write a starter config to the OS-standard path. Skips if one already exists.

askmefirst refresh
    Re-scan installed browsers and rewrite the discovery cache.

askmefirst list
    List discovered browsers and their profiles.

askmefirst pick <url>
    Open the picker for a URL, bypassing routing rules.

askmefirst install
    Register as the OS default browser candidate. Idempotent.

askmefirst uninstall
    Remove the default-browser registration. Idempotent.

askmefirst --bench
    Run a routing workload and check against per-phase budgets. Exits non-zero on breach.

askmefirst --help
askmefirst --version
```

## Configuration

A single JSON file at the OS-standard location, with `//` and `/* */` comments allowed:

- Windows: `%APPDATA%\AskMeFirst\config.json`
- macOS: `~/Library/Application Support/AskMeFirst/config.json`
- Linux: `~/.config/AskMeFirst/config.json`

Full schema and walkthrough: [`docs/rule-engine.md`](./docs/rule-engine.md).

Minimal schema tour — copy-paste starting point:

```jsonc
{
  "settings": { "stripTracking": true },
  "browsers": [
    { "id": "chrome", "displayName": "Chrome", "executable": "auto" }
  ],
  "rules": [
    {
      "name": "GitHub PRs go to Chrome",
      "priority": 100,
      "when": { "urlRegex": "^https://github\\.com/[^/]+/[^/]+/pull/\\d+" },
      "then":  { "browser": "chrome" }
    }
  ]
}
```

The `executable: "auto"` form resolves via OS discovery (browsers in standard install locations). Use an absolute path to pin a specific install.

## How does the picker work?

When no rule matches, the picker window opens with three sections:

1. **Browser buttons** — one per known browser/profile. Press `1`–`9` to commit.
2. **Remember options** — "Just this once", "Always this host", "Always this domain".
3. **Live URL** — the original URL, plus the resolved URL if it's a known shortener (t.co etc.) and the request completes within 1 s.

`Esc` or `X` cancels without launching. `Enter` commits the highlighted browser.

## Build & test (developers)

Requires .NET 10 LTS SDK (we pin the version in `global.json`).

```bash
dotnet build                  # build everything
dotnet test                   # 254 tests, ~400 ms

# Publish a single self-contained binary per platform
dotnet publish src/AskMeFirst -c Release -r win-x64   --self-contained -p:PublishAot=true
dotnet publish src/AskMeFirst -c Release -r osx-arm64 --self-contained -p:PublishAot=true
dotnet publish src/AskMeFirst -c Release -r linux-x64 --self-contained -p:PublishAot=true

# Try the local binary
./publish/<RID>/askmefirst --version
./publish/<RID>/askmefirst --bench       # self-enforces per-phase budgets
```

## Layout

```
docs/                 ← planning + handoff (read first)
samples/              ← example configs
src/AskMeFirst/       ← CLI binary
src/AskMeFirst.Core/  ← domain logic (no UI deps, AOT-safe)
src/AskMeFirst.Picker/← Avalonia picker window
src/AskMeFirst.Platforms.*/
                      ← per-OS inventory / launcher / registrar
tests/                ← xUnit
.github/workflows/    ← CI: build, test, publish, --bench gate
```

## Constraints

| Constraint | Target |
|---|---|
| Cold-start latency | < 1.5 s wall-clock (well under the 3 s ceiling) |
| Warm routing (cache hit) | < 50 ms internal |
| Platforms | Windows, macOS, Linux |
| Runtime | C# / .NET 10 LTS / Native AOT |
| Distribution | Single self-contained binary per platform |
| Telemetry | None — local-only tool |
| License | MIT |

## License

MIT — see [LICENSE](./LICENSE).
