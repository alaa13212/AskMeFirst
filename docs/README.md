# AskMeFirst

A cross-platform **browser router** — install once as the OS default, and it picks the right browser for every link based on URL rules and your preferences. Optionally unshortens links and strips tracking query params.

> **The name is the pitch.** AskMeFirst is a rebuttal to the default-browser behavior of opening links immediately, ignoring your intent. It asks first, then remembers your decisions forever — so the more you use it, the less it interrupts.

## Headline constraints

| Constraint | Target |
|---|---|
| Cold-start latency (link click → browser opens) | **< 1500 ms** (well under the 3 s ceiling) |
| Platforms | Windows, macOS, Linux (all first-class) |
| Language | C# (.NET 10 LTS + Native AOT) — see [language-decision.md](./language-decision.md) |
| Distribution | Single self-contained binary per platform, no runtime required |
| Telemetry | **None.** Local-only tool. |
| License | MIT |

## Decisions log

All design decisions are recorded in [decisions-log.md](./decisions-log.md). Read that first if you want context on *why* the tool is shaped this way.

## Plan docs

1. [architecture.md](./architecture.md) — system design, components, data flow
2. [language-decision.md](./language-decision.md) — C# + .NET 10 + Native AOT analysis
3. [platform-integration.md](./platform-integration.md) — register as default browser on each OS
4. [performance.md](./performance.md) — how we hit the <3 s / target 1.5 s goal
5. [rule-engine.md](./rule-engine.md) — rule format, predicates, actions, examples
6. [link-processing.md](./link-processing.md) — unshortening + tracking-param stripping
7. [project-structure.md](./project-structure.md) — folder layout, modules, build
8. [roadmap.md](./roadmap.md) — phased build plan
9. [decisions-log.md](./decisions-log.md) — every locked decision with rationale

## Quick sketch

```
┌──────────────┐     spawns with URL      ┌──────────────────┐
│  OS handler  │ ───────────────────────▶ │  AskMeFirst CLI  │
│  (Win/Mac/   │                          │  (Native AOT,    │
│   Linux)     │                          │   ~50 ms start)  │
└──────────────┘                          └────────┬─────────┘
                                                  │
                                  ┌───────────────┼────────────────┐
                                  ▼               ▼                ▼
                            ┌──────────┐   ┌──────────────┐  ┌────────────┐
                            │  Parse + │   │  Rule Engine │  │  Browser   │
                            │  clean   │   │  (JSON, in-  │  │  Inventory │
                            │  URL     │   │   memory)    │  │  (cache)   │
                            └──────────┘   └──────────────┘  └────────────┘
                                                  │
                          ┌───────────────────────┴─────────────────────┐
                          ▼                                             ▼
                  ┌──────────────┐                              ┌────────────────┐
                  │  Rule hits   │                              │  No rule hit   │
                  │  → launch    │                              │  → show picker │
                  └──────────────┘                              └────────────────┘
```

## Status

**Planning closed. Phase 0 (bootstrap) starting.** No URL routing yet — just the binary that prints `--version` and exits fast on all three OSes.