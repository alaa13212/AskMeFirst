# Language Decision — C# + .NET 10 LTS + Native AOT

**Recommendation: C# on .NET 10 LTS with Native AOT compilation.**

## Why this matters

The hardest constraint on this project is the **<3 s cold-start requirement**. Every time the user clicks a link, the OS spawns our process. We have to parse, decide, launch, and exit fast enough that it feels instant. This single requirement eliminates most language choices.

## The contenders

### C# on .NET 10 LTS + Native AOT ✅ chosen

| Aspect | Value |
|---|---|
| Cold start | **30–80 ms** (single-file Native AOT) |
| Warm start | **5–20 ms** |
| Binary size | ~10–15 MB self-contained |
| Distribution | Single `.exe` / single ELF binary, no runtime needed |
| Cross-platform GUI | Avalonia (very mature), MAUI (Win/Mac solid, Linux improving) |
| System integration | Full P/Invoke, registry, .desktop, .app bundles — all easy |
| Tooling | Visual Studio, Rider, VS Code — best in class for desktop apps |
| "Readability" | Mainstream; widely understood |

### Kotlin on JVM (HotSpot) — rejected

| Aspect | Value |
|---|---|
| Cold start | **400 ms – 2 s** ❌ (often fails the <3 s budget alone) |
| Distribution | Requires JRE on target machine ❌ |

JVM cold start is the **dealbreaker**. Even a tiny Kotlin CLI takes half a second to 2 seconds to JIT up on a fresh launch. After that first launch the JIT warms up and subsequent calls are faster, but link clicks happen at unpredictable intervals — we can't rely on warm state.

### Kotlin/Native — rejected

Kotlin/Native solves the startup problem but the desktop story is **significantly less mature** than .NET. Cross-platform desktop GUI support is thin; system integration (especially Windows registry, macOS app bundles) is rougher.

### C# wins where it counts

1. **Startup**: Native AOT is ~5–10× faster than a JVM CLI, and the tooling is mature.
2. **Distribution**: Single self-contained binary. No runtime install step for the user.
3. **System integration**: Full P/Invoke + registry + bundle tooling. Cleaner on every OS.
4. **GUI story**: Avalonia is a real, mature, cross-platform desktop GUI framework.
5. **Readability**: C# is mainstream. Easier to come back to in 6 months.

## Why .NET 10 and not .NET 9

.NET release cadence as of June 2026:

| Version | Type | Released | End of support |
|---|---|---|---|
| .NET 8 | LTS | Nov 2023 | Nov 2026 |
| .NET 9 | STS | Nov 2024 | **May 2026** (past) |
| **.NET 10** | **LTS** | **Nov 2025** | **Nov 2028** ✅ |
| .NET 11 | STS | Nov 2026 (preview) | May 2027 (predicted) |

For a brand-new project starting now, **.NET 10 LTS is the only sensible pick**. Long runway, current feature set, current AOT maturity.

## What we'd lose

- Kotlin's `null` safety ergonomics and data classes are lovely. C# has nullable reference types and records, both fine, but Kotlin's type system is a touch sharper.
- Kotlin Multiplatform shared-code story is nicer if we wanted iOS/Android later. We don't.
- If you specifically want to learn or deepen Kotlin, this isn't the right project for that goal.

## Verdict

**C# .NET 10 LTS + Native AOT.** Reasoning in one line: it's the only stack that hits the startup target cleanly, ships as a single binary, and has a mature cross-platform GUI story.

## Alternatives if you'd rather go Kotlin

If you have a strong reason to prefer Kotlin (learning, JVM background, etc.), the only viable path is **Kotlin/Native** with no GUI in v1 — purely CLI. We'd add GUI later when the tooling catches up. Expect a rougher ride on Windows registry work and macOS bundle creation.