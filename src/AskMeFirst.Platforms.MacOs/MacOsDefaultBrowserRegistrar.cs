using System.Diagnostics;
using System.Runtime.Versioning;
using AskMeFirst.Core.Abstractions;

namespace AskMeFirst.Platforms.MacOs;

[SupportedOSPlatform("osx")]
public sealed class MacOsDefaultBrowserRegistrar : IDefaultBrowserRegistrar
{
    private const string InstalledAppPath = "/Applications/AskMeFirst.app";
    private const string LsregisterPath = "/System/Library/Frameworks/CoreServices.framework/Versions/A/Frameworks/LaunchServices.framework/Versions/A/Support/lsregister";

    public async Task<RegistrationResult> RegisterAsync(CancellationToken ct = default)
    {
        try
        {
            string? sourceApp = LocateAppBundle();
            if (sourceApp is null)
            {
                return new RegistrationResult(
                    Success: false,
                    Message: "Could not locate AskMeFirst.app from the running binary.");
            }

            if (!string.Equals(sourceApp, InstalledAppPath, StringComparison.Ordinal))
            {
                await CopyDirectoryAsync(sourceApp, InstalledAppPath, ct);
            }

            await RunLsregisterAsync(InstalledAppPath, args: ["-f", InstalledAppPath], ct);

            return new RegistrationResult(
                Success: true,
                Message: "Registered as default browser candidate. " +
                         "Open System Settings → Desktop & Dock → Default web browser → AskMeFirst to finish setup.");
        }
        catch (Exception ex)
        {
            return new RegistrationResult(Success: false, Message: $"Registration failed: {ex.Message}");
        }
    }

    public async Task<RegistrationResult> UnregisterAsync(CancellationToken ct = default)
    {
        try
        {
            if (Directory.Exists(InstalledAppPath))
            {
                await RunLsregisterAsync(InstalledAppPath, args: ["-u", InstalledAppPath], ct);
                Directory.Delete(InstalledAppPath, recursive: true);
            }
            return new RegistrationResult(Success: true, Message: "Unregistered.");
        }
        catch (Exception ex)
        {
            return new RegistrationResult(Success: false, Message: $"Unregister failed: {ex.Message}");
        }
    }

    public bool TryOpenOsSettings()
    {
        try
        {
            using Process? p = Process.Start(new ProcessStartInfo
            {
                FileName = "open",
                ArgumentList = { "x-apple.systempreferences:com.apple.preference.general?DefaultWebBrowser" },
                UseShellExecute = false,
            });
            return p is not null;
        }
        catch
        {
            return false;
        }
    }

    private static string? LocateAppBundle()
    {
        string? path = Environment.ProcessPath;
        while (!string.IsNullOrEmpty(path))
        {
            string? parent = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(parent))
            {
                return null;
            }
            if (parent.EndsWith(".app", StringComparison.Ordinal))
            {
                return parent;
            }
            path = parent;
        }
        return null;
    }

    private static async Task CopyDirectoryAsync(string source, string dest, CancellationToken ct)
    {
        using Process p = new()
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "ditto",
                ArgumentList = { source, dest },
                UseShellExecute = false,
            },
        };
        if (!p.Start())
        {
            throw new InvalidOperationException("Could not invoke ditto to copy .app bundle.");
        }
        await p.WaitForExitAsync(ct);
        if (p.ExitCode != 0)
        {
            throw new InvalidOperationException($"ditto exited with code {p.ExitCode}.");
        }
    }

    private static async Task RunLsregisterAsync(string appPath, IReadOnlyList<string> args, CancellationToken ct)
    {
        using Process p = new()
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = LsregisterPath,
                UseShellExecute = false,
            },
        };
        foreach (string arg in args)
        {
            p.StartInfo.ArgumentList.Add(arg);
        }
        if (!p.Start())
        {
            throw new InvalidOperationException("Could not invoke lsregister.");
        }
        await p.WaitForExitAsync(ct);
    }
}