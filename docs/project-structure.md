# Project Structure

## Phase 0 layout (current)

The bootstrap phase ships a single project to keep things minimal. We split into Core + Platforms in Phase 1.

```
AskMeFirst/
├── docs/                          ← planning docs
│   ├── README.md
│   ├── architecture.md
│   ├── language-decision.md
│   ├── platform-integration.md
│   ├── performance.md
│   ├── rule-engine.md
│   ├── link-processing.md
│   ├── project-structure.md       ← you are here
│   ├── roadmap.md
│   └── decisions-log.md
│
├── .github/
│   └── workflows/
│       └── ci.yml                 ← GitHub Actions: build + test + AOT publish
│
├── .editorconfig                  ← coding style
├── .gitattributes                 ← line endings per file type
├── .gitignore
├── global.json                    ← pin .NET 10 SDK
├── Directory.Build.props          ← shared MSBuild properties
├── AskMeFirst.sln                 ← solution file
│
├── README.md                      ← user-facing README (not the docs README)
├── LICENSE                        ← MIT
│
├── samples/
│   └── askmefirst.example.json    ← example user config
│
├── src/
│   └── AskMeFirst/                ← single project for Phase 0
│       ├── AskMeFirst.csproj
│       └── Program.cs             ← --version, --help
│
└── tests/
    └── AskMeFirst.Tests/
        ├── AskMeFirst.Tests.csproj
        └── CliTests.cs
```

## Phase 1+ layout (target)

When we split into Core + Platforms:

```
AskMeFirst/
├── docs/
├── .github/workflows/ci.yml
├── (root config files)
├── samples/
│
├── src/
│   ├── AskMeFirst.Core/           ← cross-platform business logic
│   │   ├── AskMeFirst.Core.csproj
│   │   ├── Program.cs
│   │   ├── Cli/
│   │   ├── UrlHandler/
│   │   ├── RuleEngine/
│   │   ├── BrowserManager/
│   │   ├── LinkProcessor/
│   │   ├── Config/
│   │   ├── Composition.cs         ← hand-rolled DI per platform
│   │   └── Platform/
│   │
│   ├── AskMeFirst.Platforms.Windows/
│   │   └── (P/Invoke + Windows-specific implementations)
│   │
│   ├── AskMeFirst.Platforms.MacOs/
│   │   └── (ObjC bindings + Mac-specific implementations)
│   │
│   ├── AskMeFirst.Platforms.Linux/
│   │   └── (Linux-specific implementations)
│   │
│   └── AskMeFirst.Picker/         ← Phase 7+ only
│       └── (Avalonia UI)
│
└── tests/
    ├── AskMeFirst.Core.Tests/
    ├── AskMeFirst.Platforms.Windows.Tests/
    ├── AskMeFirst.Platforms.MacOs.Tests/
    └── AskMeFirst.Platforms.Linux.Tests/
```

## Project dependencies (Phase 1+)

```
AskMeFirst.Core
  └─ (nothing else — pure BCL)

AskMeFirst.Platforms.Windows    → AskMeFirst.Core
AskMeFirst.Platforms.MacOs      → AskMeFirst.Core
AskMeFirst.Platforms.Linux      → AskMeFirst.Core

AskMeFirst.Picker (Phase 7+)    → AskMeFirst.Core
  ├─ Avalonia
  ├─ Avalonia.Desktop
  └─ Avalonia.Themes.Fluent
```

**Router mode** ships only `Core` + one `Platforms.*` linked at build time. No Avalonia. Single binary, tiny.

## Build configuration

### `global.json`

```json
{
  "sdk": {
    "version": "10.0.100",
    "rollForward": "latestFeature"
  }
}
```

### `Directory.Build.props`

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <AnalysisLevel>latest-recommended</AnalysisLevel>
  </PropertyGroup>
</Project>
```

### AOT publish profile

```xml
<!-- AskMeFirst.csproj -->
<PropertyGroup>
  <PublishAot>true</PublishAot>
  <PublishSingleFile>true</PublishSingleFile>
  <SelfContained>true</SelfContained>
  <PublishTrimmed>true</PublishTrimmed>
  <IlcOptimizationPreference>Speed</IlcOptimizationPreference>
  <IlcGenerateStackTraceData>false</IlcGenerateStackTraceData>
</PropertyGroup>
```

RuntimeIdentifier is set per-publish via `-r <RID>`.

### Cross-platform compilation matrix

| RID | OS | Architecture |
|---|---|---|
| `win-x64` | Windows | x86-64 |
| `win-arm64` | Windows | ARM64 |
| `osx-arm64` | macOS | Apple Silicon |
| `osx-x64` | macOS | Intel (legacy) |
| `linux-x64` | Linux | x86-64 |
| `linux-arm64` | Linux | ARM64 (Pi 5, Asahi, etc.) |

CI builds the four primary RIDs (`win-x64`, `osx-arm64`, `linux-x64`, `linux-arm64`). Win-ARM64 and osx-x64 are stretch goals.

## Testing strategy

- **Unit tests**: xUnit, fast, no I/O. Mock-free — pure logic.
- **Platform tests**: real-OS integration tests. Tagged so CI skips platform tests for wrong OS.
- **Benchmark tests**: BenchmarkDotNet for hot paths. Run nightly, not on every PR.
- **Performance budget tests**: `--bench` with budget assertions. Fails CI if cold start > 150 ms.

## CI/CD

GitHub Actions, single workflow:

```yaml
name: build
on: [push, pull_request]
jobs:
  test:
    strategy:
      matrix:
        os: [ubuntu-latest, windows-latest, macos-latest]
    runs-on: ${{ matrix.os }}
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 10.0.x
      - run: dotnet test
      - run: dotnet publish -c Release -r ${{ matrix.rid }} --self-contained -p:PublishAot=true
```

Releases triggered by tag push → build all RIDs → upload to GitHub Releases. No installer in v1; just the binary + a brief README for each platform. Add installer (MSI / .pkg / AppImage / deb) in Phase 9.

## Coding conventions

- **Style**: standard .NET (`dotnet format`), EditorConfig-driven.
- **Naming**: PascalCase for types/methods, camelCase for locals/params, _camelCase for private fields.
- **File-scoped namespaces**.
- **Records** for value types and DTOs.
- **Sealed** classes by default; `internal` where possible.
- **Async only at I/O boundaries** — don't pollute pure logic with `async`/`await`.
- **No DI framework** — hand-rolled `Composition.cs` per platform.
- **JSON via `System.Text.Json`** with `JsonCommentHandling.Skip` for JSONC compatibility.

## What this looks like day-to-day

Most days you'll edit:
- `AskMeFirst.Core/RuleEngine/*` when tweaking rule semantics
- `AskMeFirst.Core/BrowserManager/*` when adding browser support
- `AskMeFirst.Platforms.<OS>/*` when fixing an OS-specific quirk
- `samples/askmefirst.example.json` when adding a feature that needs config

That's it. ~20 files you'd touch regularly. The whole project stays under 10 KLOC for v1.