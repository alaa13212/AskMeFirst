# Handoff — 2026-06-29

> **First thing next session: read this file.**

## TL;DR

Phases 0–3 complete and the picker is **fully usable end-to-end on the user's actual machine**. This session was about getting Firefox 136+ Profile Groups and Chromium profile names right, then fixing the resulting avatar bug. Five sub-fixes, one revert, and a clean rewrite at the end:

1. **(Failed)** First tried to byte-scan Firefox's per-store SQLite like `FirefoxProfileAvatarReader` does. Too fragile — nested TEXT encoding inside SQLite cells rejected every real row.
2. **(Win)** Switched to `Microsoft.Data.Sqlite` 10.0.9 + `SQLitePCLRaw.bundle_e_sqlite3` 3.0.2 — AOT-compatible in .NET 10. Added ~1.5 MB to the binary, worth it for correctness.
3. **Firefox profile detection** now expands each `[ProfileN]` in `profiles.ini` into ALL selectable sub-profiles in its `<StoreID>.sqlite`. **Verified on this machine: 4 profiles across 2 groups** (Barrak=1 default, Work=3 — Profile 5, Test Profile 2, plus the auto-default Work).
4. **Profile name polish**:
   - Firefox: SQLite rows named `"Original profile"` get renamed to the INI group name (Work/Barrak) so users see meaningful labels. Custom names like "Profile 5", "Test Profile 2" pass through.
   - Chrome/Edge/Brave/Chromium: new `ChromiumProfileNames` parses `<User Data>/Local State` JSON's `profile.info_cache[<dir>].name`. Real names ("Ali Albarrak", "kgg.co", "Remal") instead of "Profile 6/7/8".
5. **Firefox avatar lookup rewrite**: replaced byte-scanning with a cross-store SQLite query (`SELECT avatar FROM Profiles WHERE path LIKE '%<dirTail>'`). Old code required the StoreID from `profiles.ini` first, which silently failed for SQLite-only profiles like "Profile 5" — which is exactly the one the user noticed missing its image.

**273/273 tests passing** (204 Core + 69 Picker; +20 since last handoff). AOT binary **18.50 MB** (was 17.00 MB; +1.5 MB SQLite native lib).

---

## Where we are

| Phase | Status | Notes |
|---|---|---|
| 0 — Bootstrap | ✅ Done | Build, test, AOT publish all working |
| 1 — MVP router | ✅ Done + polished | ICommand architecture, profile detection, browser-family launch strategies |
| 2 — Rule engine | ✅ Done + refactored | Rules + predicates + actions + source-app detection + tracking strip + profiles-first-class |
| 3 — Picker UI | ✅ Done | Two-line profile-first labels, full keyboard nav, one-click commit, window close on Done, source-app centering, PinnedProfileFilter wired |
| 4 — OS integration | 📋 Planned | Win/Mac/Linux registration + per-platform `ISourceAppWindowLocator` impls |
| 5 — Link processing | 📋 Planned | Async Unshortener (tracking strip already done) |
| 6 — Polish | 📋 Planned | Bench command, README, examples, config ↔ inventory mapping |

Full plan: [`docs/roadmap.md`](./roadmap.md).

---

## What's verified locally

- ✅ `dotnet build` — clean, 0 warnings, 0 errors
- ✅ `dotnet test` — **273/273 passing** in ~2 s
  - 204 in `AskMeFirst.Core.Tests` (+15 since last handoff: 5 FirefoxProfileStoreScanner, 7 ChromiumProfileNames, 6 FirefoxProfilesParser end-to-end, 1 integration assertion + 1 dump test)
  - 69 in `AskMeFirst.Picker.Tests` (+9 since last handoff: 7 FirefoxProfileAvatarReader, 2 real-data Firefox icon tests)
- ✅ `dotnet publish -p:PublishProfile=Aot -r win-x64` — produces `askmefirst.exe` **18.50 MB** (+1.5 MB for SQLite native lib)
- ✅ AOT binary `--version` and `--list` work end-to-end
- ✅ Verified on user's actual Firefox state: 4 profiles across 2 groups, correct names, avatars load for Barrak + Profile 5

**Live `--list` output:**

```
Discovered 3 browser(s):
  firefox      Mozilla Firefox          C:\Program Files\Mozilla Firefox\firefox.exe
      * Profiles\vc4ak1jq.Barrak-1706255686136 Barrak
        Profiles\kXwwp1SX.Profile 2 Profile 5
        Profiles\8j1IVuga.Profile 1 Test Profile 2
        Profiles\0m6kw70o.Work Work
  chrome       Google Chrome            C:\Program Files\Google\Chrome\Application\chrome.exe
        Profile 6            Ali Albarrak
        Profile 8            kgg.co
        Profile 7            Remal
  edge         Microsoft Edge           C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe
      * Default              Profile 1
```

---

## Decisions recap (this session)

| # | Pick | Why |
|---|---|---|
| 67 | ~~Byte-scan Firefox per-store SQLite~~ **REVERTED** in this session | First attempt: same fragile pattern as `FirefoxProfileAvatarReader`. Turned out SQLite TEXT values nest inside cells with length-prefix varints — my naive `Profiles\<path>` scan was matching random substrings in Nimbus telemetry blobs and rejecting every real row. |
| 68 | **`Microsoft.Data.Sqlite` 10.0.9 + `SQLitePCLRaw.bundle_e_sqlite3` 3.0.2** for both profile store reads AND avatar reads | AOT-compatible in .NET 10. ~1.5 MB binary cost is worth correctness. Pinned bundle version explicitly to avoid transitive `SQLitePCLRaw.lib.e_sqlite3` 2.x with known CVE. |
| 69 | `BrowserProfile` gains optional `GroupId` + `GroupName` fields with default `null` | Existing positional constructions (`new BrowserProfile(name, dir, isDefault)`) keep compiling at all 32 call sites. GroupId = StoreID from `profiles.ini`. GroupName = the INI row's `Name` (the user-given group label like "Work" or "Barrak"). |
| 70 | Firefox parser **expands** each INI profile into ALL sub-profiles in its store, instead of just enriching the single INI entry | `profiles.ini` only lists the currently-selected sub-profile per group. To show all 4 profiles (1 Barrak + 3 Work), the parser must enumerate SQLite rows. |
| 71 | Firefox SQLite rows named `"Original profile"` (case-insensitive) get renamed to the INI group name | That's Firefox's placeholder for unmodified defaults. User-facing label should be the user-given name. Custom names (Profile 5, Test Profile 2) pass through unchanged. |
| 72 | New `ChromiumProfileNames.Read(userDataRoot)` for Chrome/Edge/Brave/Chromium | Reads `<User Data>/Local State` JSON's `profile.info_cache[<dir>].name`. All three platform detectors call it before emitting `BrowserProfile`s. Falls back to directory name if `Local State` missing/malformed. |
| 73 | Firefox avatar reader uses cross-store SQLite query, NOT StoreID-from-INI | Old code required looking up the profile's StoreID via `profiles.ini` first — failed for SQLite-only profiles like "Profile 5". New approach: enumerate `<groupsRoot>/*.sqlite`, run `SELECT avatar FROM Profiles WHERE path LIKE '%<dirTail>'` against each until found. Removed `FindFirefoxStoreId` helper entirely. |

---

## Files changed this session

### Source

```
src/AskMeFirst.Core/
├── AskMeFirst.Core.csproj              ← MODIFIED: +Microsoft.Data.Sqlite 10.0.9, +SQLitePCLRaw.bundle_e_sqlite3 3.0.2
├── Models/
│   └── BrowserProfile.cs               ← MODIFIED: +GroupId, +GroupName (nullable, defaulted)
└── Profiles/
    ├── FirefoxProfilesParser.cs        ← REWRITTEN: new Parse(ini, groupsRoot) overload; reads StoreID; expands SQLite rows; renames "Original profile" to group name
    ├── FirefoxProfileStoreScanner.cs   ← REWRITTEN: Microsoft.Data.Sqlite-based; was byte-scan
    └── ChromiumProfileNames.cs         ← NEW: parses Local State JSON for Chromium profile display names

src/AskMeFirst.Platforms.Windows/
├── FirefoxProfileAvatarReader.cs      ← REWRITTEN: Microsoft.Data.Sqlite-based; cross-store query; was byte-scan
└── WindowsIconProvider.cs              ← SIMPLIFIED: removed FindFirefoxStoreId helper

src/AskMeFirst.Platforms.MacOs/
└── MacOsBrowserProfileDetector.cs      ← MODIFIED: uses ChromiumProfileNames

src/AskMeFirst.Platforms.Linux/
└── LinuxBrowserProfileDetector.cs      ← MODIFIED: uses ChromiumProfileNames
```

### Tests

```
tests/AskMeFirst.Core.Tests/
├── AskMeFirst.Core.Tests.csproj       ← MODIFIED: +Microsoft.Data.Sqlite 10.0.9
├── FirefoxProfileStoreScannerTests.cs  ← REWRITTEN: 5 tests using real SQLite fixtures
├── FirefoxProfilesParserTests.cs       ← REWRITTEN: 7 tests including the "Original profile" rename
├── ChromiumProfileNamesTests.cs        ← NEW: 7 tests
└── RuleRouterPickerTests.cs            ← NEW: 7 tests for picker-as-catch-all on RuleRouter

tests/AskMeFirst.Picker.Tests/
├── AskMeFirst.Picker.Tests.csproj      ← MODIFIED: +Microsoft.Data.Sqlite 10.0.9
├── FirefoxProfileAvatarReaderTests.cs  ← NEW: 7 tests
└── WindowsIconProviderTests.cs         ← MODIFIED: replaced 1 obsolete test with 2 real-data Firefox icon tests (Barrak, Profile 5)
```

### Docs

```
docs/HANDOFF.md                         ← THIS FILE
```

---

## Architecture highlights

### Firefox profile expansion

```csharp
public static IReadOnlyList<BrowserProfile> Parse(string iniPath, string? groupsRoot)
{
    IReadOnlyList<IniRow> iniRows = ReadIni(iniPath);
    if (iniRows.Count == 0) return [];

    List<BrowserProfile> result = [];
    HashSet<string> emittedTail = new(StringComparer.OrdinalIgnoreCase);

    foreach (IniRow row in iniRows)
    {
        if (row.Path is null || string.IsNullOrEmpty(row.StoreId))
        {
            if (row.Path is not null) EmitIniOnly(row, result, emittedTail);
            continue;
        }

        string sqlitePath = Path.Combine(groupsRoot ?? "", $"{row.StoreId}.sqlite");
        if (!File.Exists(sqlitePath))
        {
            EmitIniOnly(row, result, emittedTail);
            continue;
        }

        IReadOnlyList<FirefoxProfileStoreEntry> storeEntries =
            FirefoxProfileStoreScanner.Read(sqlitePath);

        foreach (FirefoxProfileStoreEntry entry in storeEntries)
        {
            string tail = ExtractTailSegment(entry.Path);
            if (!emittedTail.Add(tail)) continue;

            string name = ResolveEntryName(entry, row);
            bool isDefault = IsPathMatch(entry.Path, row.Path) && row.IsDefault;

            result.Add(new BrowserProfile(
                Name: name,
                DirectoryName: entry.Path,
                IsDefault: isDefault,
                GroupId: row.StoreId,
                GroupName: row.Name));
        }
    }

    return result
        .OrderBy(p => p.IsDefault ? 0 : 1)
        .ThenBy(p => p.GroupName ?? p.Name, StringComparer.OrdinalIgnoreCase)
        .ThenBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
        .ToList();
}

private static string ResolveEntryName(FirefoxProfileStoreEntry entry, IniRow row)
{
    bool isFirefoxDefaultName = !string.IsNullOrWhiteSpace(entry.Name)
        && string.Equals(entry.Name.Trim(), "Original profile", StringComparison.OrdinalIgnoreCase);

    if (!isFirefoxDefaultName && !string.IsNullOrWhiteSpace(entry.Name))
        return entry.Name!;

    return row.Name ?? ExtractTailSegment(entry.Path);
}
```

### Firefox avatar lookup (cross-store)

```csharp
public static byte[]? ReadAvatarPng(string groupsRoot, string profileDirTail)
{
    if (string.IsNullOrEmpty(groupsRoot) || !Directory.Exists(groupsRoot))
        return null;

    string? avatarId = FindAvatarId(groupsRoot, profileDirTail);
    if (string.IsNullOrEmpty(avatarId) || !IsUuid(avatarId))
        return null;

    string avatarPath = Path.Combine(groupsRoot, "avatars", avatarId);
    if (!File.Exists(avatarPath)) return null;

    byte[] bytes = File.ReadAllBytes(avatarPath);
    return IsPng(bytes) ? bytes : null;
}

private static string? FindAvatarId(string groupsRoot, string profileDirTail)
{
    foreach (string sqlitePath in Directory.EnumerateFiles(groupsRoot, "*.sqlite"))
    {
        string? result = QueryAvatarId(sqlitePath, profileDirTail);
        if (!string.IsNullOrEmpty(result)) return result;
    }
    return null;
}

private static string? QueryAvatarId(string sqlitePath, string profileDirTail)
{
    using SqliteConnection conn = new($"Data Source={sqlitePath};Mode=ReadOnly");
    conn.Open();
    if (!TableExists(conn, "Profiles")) return null;

    using SqliteCommand cmd = conn.CreateCommand();
    cmd.CommandText = "SELECT avatar FROM Profiles WHERE path LIKE $pattern";
    SqliteParameter p = cmd.CreateParameter();
    p.ParameterName = "$pattern";
    p.Value = "%" + profileDirTail;
    cmd.Parameters.Add(p);

    return cmd.ExecuteScalar() as string;
}
```

### Chromium profile names

```csharp
public static IReadOnlyDictionary<string, string> Read(string userDataRoot)
{
    Dictionary<string, string> result = [];
    string localState = Path.Combine(userDataRoot, "Local State");
    if (!File.Exists(localState)) return result;

    using FileStream fs = new(localState, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
    using JsonDocument doc = JsonDocument.Parse(fs);
    if (!doc.RootElement.TryGetProperty("profile", out JsonElement profile)) return result;
    if (!profile.TryGetProperty("info_cache", out JsonElement cache)) return result;

    foreach (JsonProperty entry in cache.EnumerateObject())
    {
        if (entry.Value.TryGetProperty("name", out JsonElement nameElement)
            && nameElement.ValueKind == JsonValueKind.String)
        {
            string? displayName = nameElement.GetString();
            if (!string.IsNullOrWhiteSpace(displayName))
                result[entry.Name] = displayName!;
        }
    }

    return result;
}
```

---

## Bugs caught this session

1. **Firefox picker showed only 2 profiles, expected 4 across 2 groups** — `profiles.ini` only references the currently-selected sub-profile per group. First fix: byte-scan SQLite (broke). Final fix: switch to `Microsoft.Data.Sqlite`, parser now expands each INI row into all sub-profiles.

2. **Firefox "Original profile" placeholder shown to users** — SQLite rows for unmodified defaults have `name="Original profile"`. Renamed to the INI group name ("Work", "Barrak") so users see meaningful labels.

3. **Chromium profiles shown as "Profile 6/7/8"** — Chrome/Edge store display names in `<User Data>/Local State`, not in the directory name. Added `ChromiumProfileNames` reader.

4. **Firefox "Profile 5" had no avatar** — Old avatar reader required the profile's StoreID from `profiles.ini`, but "Profile 5" isn't in `profiles.ini` (only the INI's currently-selected sub-profile is). Replaced with cross-store SQLite query — now finds avatars for any profile in any store.

5. **`Microsoft.Data.Sqlite 9.0.0` pulled in vulnerable `SQLitePCLRaw.lib.e_sqlite3` 2.1.10** (CVE GHSA-2m69-gcr7-jv3q) — explicitly pinned `SQLitePCLRaw.bundle_e_sqlite3` to 3.0.2 (latest, no known CVEs).

---

## Style rules (READ THESE — non-obvious)

User-level preferences, also in `~/.mavis/memory/user.md`:

> **Comments describe WHAT, never WHY/HOW.** No plan-phase references, no requirement-doc references, no decision-history, no trivial info. Comment on symbols (classes / methods), not on instructions inside methods. Default to no comment.

> **NEVER use `var`** — always use explicit types (`csharp_style_var_elsewhere = false:error`).

> **Prefer braces** even on single-line `if` statements.

> **One type per file** — split classes/records into their own `.cs` when adding to existing files.

> **No `IsDefault` (or similar) flags on interface types.** Use explicit registry methods (e.g. `RegisterDefault`) to enforce single-default invariants at the call site, not by inspection.

> **Pattern recognition over hand-rolling.** When the user pushes back with "which design pattern would you use", think about Strategy / Pipeline / Result-type / Discriminated-Union combinations, not monolithic refactor shapes.

When editing any file, prune comments that violate these.

---

## Toolchain notes

- .NET 10 LTS
- AOT publish target: `dotnet publish -c Release -r win-x64 -p:PublishProfile=Aot`
- Native packages pulled in: `Microsoft.Data.Sqlite` 10.0.9, `SQLitePCLRaw.bundle_e_sqlite3` 3.0.2, `Avalonia.*` 11.2.2, `SkiaSharp` 2.88.9
- Binary size: 18.50 MB (up from 17.00 MB — +1.5 MB SQLite native)
- SQLite tests use real file-backed `SqliteConnection` (not in-memory `:memory:`) because `Mode=ReadOnly` doesn't work on shared memory DBs

---

## Things to know about the user

- C# or Kotlin background — comfortable with both
- Cares about "understanding all the code" — no magic, no heavy frameworks
- Working style: detailed Q&A interview per design decision, one question at a time
- Code-style rules (comment / var / braces / one-class-per-file / no-interface-flags) apply project-wide
- Pattern-recognition preference: when reviewing refactor shapes, the user will push back with "which design pattern(s) would you use" — answer with a mix (Strategy + Pipeline + Result type), not a monolithic shape
- Session preference: don't commit until asked. Single big commit is acceptable for handoff; multi-commit logical groups also welcome.

---

## How to pick up

1. Read this file (`docs/HANDOFF.md`)
2. Skim `docs/decisions-log.md` for context on locked choices (now 73 decisions)
3. Glance at `docs/roadmap.md` for the phase plan
4. Look at the comment rules in memory (`mavis memory show`)
5. Pick a phase: **Phase 4 (OS integration + per-platform source-app-window-locator)** or **Phase 5 (Unshortener)** — Phase 3 polish (avatars verified, recent-picks display) is also a good next step
