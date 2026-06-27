# AGENTS.md — project instructions for AskMeFirst

> **First action on starting any session in this repo: read `docs/HANDOFF.md`.**

## Project snapshot

Cross-platform browser router (Windows / macOS / Linux). Installs as the OS default for `http` / `https`, picks the right browser per link based on rules (source app + URL pattern + what's running + user prefs), optionally unshortens and strips tracking query params.

Tagline / philosophy: *"Ask me first, then remember forever."* The picker is the **learning** mechanism, not the daily driver — rules accumulate via "remember" actions.

## Tech stack (locked)

- **Language**: C# on .NET 10 LTS
- **Distribution**: Native AOT, single self-contained binary per RID (~1.4 MB)
- **Config format**: JSON with comment-tolerant parser (`System.Text.Json` + `JsonCommentHandling.Skip`)
- **GUI** (Phase 3+): Avalonia, single-screen centered modal
- **DI**: hand-rolled `Composition.cs` per platform — **no DI framework**
- **No daemon** in v1 — stateless CLI per link click
- **No telemetry** — local-only tool
- **License**: MIT

Full decision rationale: [`docs/decisions-log.md`](./docs/decisions-log.md).

## Folder layout

```
AskMeFirst/
├── AGENTS.md                          ← you are here
├── README.md                          ← user-facing
├── LICENSE                            ← MIT
├── global.json                        ← .NET 10 SDK pin
├── Directory.Build.props              ← shared MSBuild properties
├── .editorconfig  .gitattributes  .gitignore
├── AskMeFirst.slnx
├── samples/askmefirst.example.json
├── src/AskMeFirst/                    ← CLI binary (Phase 0 single project)
├── tests/AskMeFirst.Tests/            ← xUnit
├── docs/                              ← planning docs + HANDOFF.md
│   ├── HANDOFF.md                     ← read first on session start
│   ├── decisions-log.md               ← 25 locked decisions
│   ├── architecture.md
│   ├── rule-engine.md
│   ├── link-processing.md
│   ├── platform-integration.md
│   ├── performance.md
│   ├── project-structure.md
│   ├── roadmap.md
│   ├── language-decision.md
│   └── README.md
└── .github/workflows/ci.yml
```

## Build / test / publish

```bash
# Build + test (framework-dependent binary; fast iteration)
dotnet build
dotnet test

# Native AOT publish (single self-contained binary per RID)
dotnet publish src/AskMeFirst -c Release -r <RID> -p:PublishProfile=Aot

# Test the AOT binary
./publish/<RID>/askmefirst.exe --version    # Windows
./publish/<RID>/askmefirst --version         # macOS / Linux
```

## Code style

**User preference** (also saved in `~/.mavis/memory/user.md`):

> Comments describe **WHAT**, never **WHY/HOW**. No plan-phase references, no requirement-doc references, no decision-history, no trivial info. Comment on symbols (classes / methods), not on instructions inside methods. Default to no comment.

When editing any file, prune comments that violate this.

## Phases

Per [`docs/roadmap.md`](./docs/roadmap.md):

| Phase | Status | Target |
|---|---|---|
| 0 — Bootstrap | ✅ Done | Build, test, AOT publish |
| 1 — MVP router | ⏭ Next | CLI takes URL, launches in hardcoded browser |
| 2 — Rule engine | 📋 Planned | JSON config + predicates + actions |
| 3 — Picker UI | 📋 Planned | Avalonia, two-panel layout |
| 4 — OS integration | 📋 Planned | Register as default on Win/Mac/Linux |
| 5 — Link processing | 📋 Planned | Async unshortener + tracking strip |
| 6 — Polish | 📋 Planned | Bench command, README, examples |
| 7 — Management UI | 📋 Optional | `askmefirst config` webview |
| 8 — Daemon | 📋 Optional | System tray, hot-reload |
| 9 — Installers | 📋 Optional | MSI / .pkg / .deb / AppImage + winget / brew / apt |

## On "handoff"

If the user says **"handoff"** in chat, treat it as "I'm about to close this session." Your job:

1. Summarize everything done in `docs/HANDOFF.md` (overwrite previous).
2. Include: where we are in the phase plan, what's verified, what's next, style rules, decisions recap, bugs caught, toolchain notes.
3. Update this file if anything structural changed (folder layout, deps, etc.).
4. Confirm the next session will see the up-to-date handoff first.

This pattern is saved in user memory (`~/.mavis/memory/user.md`).