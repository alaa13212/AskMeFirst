# AskMeFirst

A cross-platform **browser router** — install once as the OS default, and it picks the right browser for every link based on rules (source app, URL pattern, what's already open, your prefs). Optionally unshortens links and strips tracking query params.

> The name is the pitch. AskMeFirst is a rebuttal to the default-browser behavior of opening links immediately, ignoring your intent. It asks first, then remembers your decisions forever.

## Status

**Phase 0 — bootstrap.** The binary prints `--version`, `--help`, and `--bench`. URL routing arrives in Phase 1.

See [docs/roadmap.md](./docs/roadmap.md) for the full plan and [docs/decisions-log.md](./docs/decisions-log.md) for the design decisions.

## Quick start (developers)

Requires .NET 10 SDK (we pin `10.0.100` minimum in `global.json`).

```bash
# Build & test
dotnet build
dotnet test

# Try the bootstrap binary
dotnet run --project src/AskMeFirst -- --version

# Native AOT publish (single self-contained binary)
dotnet publish src/AskMeFirst -c Release -r win-x64   --self-contained -p:PublishAot=true
dotnet publish src/AskMeFirst -c Release -r osx-arm64 --self-contained -p:PublishAot=true
dotnet publish src/AskMeFirst -c Release -r linux-x64 --self-contained -p:PublishAot=true
```

## Layout

```
AskMeFirst/
├── docs/                    ← planning docs (read first)
├── src/AskMeFirst/          ← the router binary
├── tests/AskMeFirst.Tests/  ← xUnit tests
├── samples/                 ← example configs
└── .github/workflows/       ← CI
```

## Constraints

| Constraint | Target |
|---|---|
| Cold-start latency | < 1500 ms (well under the 3 s ceiling) |
| Platforms | Windows, macOS, Linux |
| Runtime | C# / .NET 10 LTS / Native AOT |
| Distribution | Single self-contained binary per platform |
| Telemetry | None |
| License | MIT |

## License

MIT — see [LICENSE](./LICENSE).