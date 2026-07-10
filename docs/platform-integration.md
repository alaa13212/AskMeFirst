# Platform Integration

How AskMeFirst registers itself as the default handler for `http://` and `https://` on each OS, plus how it discovers and launches browsers.

## Cross-platform registration strategy

Each OS has a slightly different notion of "default browser." We implement all three behind a common `IDefaultBrowserRegistrar` interface.

```csharp
public interface IDefaultBrowserRegistrar
{
    void RegisterAsDefault();
    void Unregister();
    bool IsCurrentDefault();
}
```

The `askmefirst install` CLI command calls `RegisterAsDefault()`. The `askmefirst uninstall` command calls `Unregister()`. A clean uninstall should restore the user's previous default (best effort — record it on first install).

All three OSes follow the same pattern: **register + one-time user prompt**. We register ourselves as a candidate, then the user confirms via the OS's standard mechanism. See [decisions-log.md](./decisions-log.md) for why we picked this over auto-claiming.

---

## Windows

### Registration

We register as a browser in `HKCU\Software\Clients\StartMenuInternet\AskMeFirst`:

- `StartMenuInternet\AskMeFirst\shell\open\command` = `"C:\path\to\askmefirst.exe" "%1"`
- `StartMenuInternet\AskMeFirst\Capabilities\StartMenu` and `URLAssociations` for `http` and `https`
- `StartMenuInternet\AskMeFirst\DefaultIcon` = path to our `.ico`
- `HKCU\Software\Clients\StartMenuInternet` default value = `AskMeFirst`

We do **not** write `UserChoice` keys. That's fragile and Microsoft actively works against non-browser apps doing it. See [decisions-log.md](./decisions-log.md).

Install flow:

1. User runs `askmefirst install`
2. We write the registry entries above
3. We display a one-time instruction: *"Open Settings → Default apps → Web browser → AskMeFirst."*
4. User clicks Default Apps, picks AskMeFirst
5. From now on, all `http`/`https` clicks route through us

### Browser discovery

- Primary: `HKLM\SOFTWARE\Clients\StartMenuInternet` (and `HKLM\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall` for installer-detected apps)
- Per-browser: read `<browser>\shell\open\command` to get the executable path
- Cache to `%LOCALAPPDATA%\AskMeFirst\browsers.json`

### Profile discovery (P2 — auto-discovered)

For Chromium-based browsers (Chrome, Edge, Brave, Arc, Vivaldi):

- Read `<User Data>\Local State` — JSON file with `profile.info_cache` containing each profile's metadata (name, gaia info, etc.)
- Profile directories: `<User Data>\<ProfileName>\` where `<ProfileName>` is `Default`, `Profile 1`, `Profile 2`, ...
- Default profile: marked in `Local State` under `profile.last_active`

For Firefox-based browsers:

- Parse `profiles.ini` from `%APPDATA%\Mozilla\Firefox\profiles.ini`
- Each `[ProfileN]` section has a `Name` and `Path` (relative to profile root)
- `Default=1` marks the default profile

Cache to `browsers.json` with TTL 24h.

### Running browser detection

- Enumerate processes via `Process.GetProcesses()` filtered by name (chrome, firefox, msedge, brave, opera, etc.)
- Get window title for each main window via `EnumWindows` + `GetWindowText` to determine active URL (best effort)
- More accurate (per-browser IPC like Chrome DevTools Protocol): defer to v2

### Launching

- Spawn browser with URL as arg via `Process.Start` with `UseShellExecute = false`
- For Chrome-based: append `--profile-directory=<name>`
- For Firefox: append `-P <name> -new-tab <url>`

### Focus existing instance

- For Chrome-based: launch with URL anyway; Chrome opens new tab in existing instance if one exists
- For Firefox: use `-new-tab <url>` against the running instance
- To bring window to front: `SetForegroundWindow` on the matching process's main HWND

---

## macOS

### Registration

Must be a `.app` bundle. Our build process produces:

```
AskMeFirst.app/
└── Contents/
    ├── Info.plist          ← declares URL handlers
    ├── MacOS/
    │   └── askmefirst      ← the native AOT binary
    └── Resources/
        └── icon.icns
```

`Info.plist` key entries:

```xml
<key>CFBundleURLTypes</key>
<array>
  <dict>
    <key>CFBundleURLName</key><string>AskMeFirst URL</string>
    <key>CFBundleURLSchemes</key>
    <array><string>http</string><string>https</string></array>
  </dict>
</array>
<key>LSHandlerRank</key><string>Owner</string>
<key>LSApplicationCategoryType</key><string>public.app-category.utilities</string>
```

**v1: unsigned.** Gatekeeper warns on first launch (right-click → Open to override). Signed + notarized is deferred to Phase 9.

Install flow:

1. User runs `askmefirst install`
2. We move/place `AskMeFirst.app` into `/Applications` (or `~/Applications`)
3. We launch the bundle once to register with Launch Services
4. We prompt: *"System Settings → Desktop & Dock → Default web browser → AskMeFirst."*
5. User picks AskMeFirst in System Settings
6. All `http`/`https` clicks route through us

### Browser discovery

- Scan `/Applications/*.app` and `~/Applications/*.app`
- For each `.app`, read `Contents/Info.plist`, look for `CFBundleURLTypes` containing `http`/`https`, or known browser bundle IDs (com.google.Chrome, org.mozilla.firefox, com.microsoft.edgemac, com.brave.Browser, etc.)
- Cache to `~/Library/Application Support/AskMeFirst/browsers.json`

### Profile discovery (P2 — auto-discovered)

For Chromium-based (Chrome, Edge, Brave, Arc):

- `~/Library/Application Support/<Browser>/Local State`
- `profile.info_cache` has each profile's metadata

For Firefox:

- `~/Library/Application Support/Firefox/profiles.ini`

### Running browser detection

- `NSWorkspace.shared.runningApplications` (via minimal ObjC bindings)
- For active URL: AppleScript bridge to ask each browser — defer to v2

### Launching

- `Process.Start("open", "-a \"Browser Name\" \"https://...\"")` for any bundle
- For Chromium-based: append `--profile-directory=<name>`
- For Firefox: append `-P <name> -new-tab <url>`

---

## Linux

### Registration

1. Generate a `.desktop` file:

```ini
[Desktop Entry]
Type=Application
Name=AskMeFirst
Comment=Smart browser router
Exec=askmefirst %u
Icon=askmefirst
Terminal=false
Categories=Network;WebBrowser;
MimeType=x-scheme-handler/http;x-scheme-handler/https;
StartupNotify=true
```

2. Install to `~/.local/share/applications/askmefirst.desktop` (user install, default)
3. Set as default:

```bash
xdg-mime default askmefirst.desktop x-scheme-handler/http
xdg-mime default askmefirst.desktop x-scheme-handler/https
update-desktop-database ~/.local/share/applications/
```

`xdg-mime` is part of `xdg-utils`, universal on Linux desktops.

A `--system` flag (Phase 9) installs to `/usr/share/applications/` system-wide (requires root).

### Browser discovery

- Parse `.desktop` files from:
  - `/usr/share/applications/`
  - `/usr/local/share/applications/`
  - `~/.local/share/applications/`
  - `/var/lib/flatpak/exports/share/applications/` (Flatpak)
  - `/var/lib/snapd/desktop/applications/` (Snap)
- Filter by `Categories` containing `WebBrowser`, or `MimeType` containing `x-scheme-handler/http`
- Extract `Exec` field, strip field codes (`%u`, `%U`, `%F`, etc.)
- Cache to `~/.config/AskMeFirst/browsers.json`

### Profile discovery (P2 — auto-discovered)

For Chromium-based:

- `~/.config/<Browser>/Local State`
- `profile.info_cache` for profile metadata

For Firefox:

- `~/.mozilla/firefox/profiles.ini`

### Running browser detection

- D-Bus: query each browser's remote-debugging interface (only available when launched with `--remote-debugging-port`)
- Fallback: `ps aux` for known browser process names
- X11: `xdotool search --name` for window titles
- **Wayland: most compositors don't expose window lists.** v1 uses process detection only on Wayland.

### Launching

- Parse the browser's `.desktop` `Exec` line, execute with URL substituted in
- For Chromium-based: append `--profile-directory=<name>`
- For Firefox: append `-P <name> -new-tab <url>`

---

## Common types

### Browser record

```csharp
public record BrowserRecord(
    string Id,                  // stable id (user-defined or auto-generated)
    string DisplayName,
    string ExecutablePath,
    string BundleId,            // macOS bundle id, "" on Win/Linux
    string DesktopFilePath,     // Linux only, "" elsewhere
    IReadOnlyList<BrowserProfile> Profiles,
    string[] CommandLineArgs    // extra args always passed
);

public record BrowserProfile(
    string Id,                  // "Default", "Profile 1", "work", etc.
    string DisplayName,
    bool IsDefault
);
```

User maps browsers to URLs via URL rules and pinned profiles in config. No tags indirection.

---

## Testing

Each platform layer has integration tests that require a real machine — no OS mocking. Tests are tagged so CI can skip platform tests for other platforms.

- Windows: GitHub Actions `windows-latest`
- macOS: GitHub Actions `macos-latest` (Apple Silicon)
- Linux: GitHub Actions `ubuntu-latest`

For manual cross-platform testing, we maintain a small VM image checklist in `docs/manual-testing.md` (to write later).