using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text;
using AskMeFirst.Core.Abstractions;

namespace AskMeFirst.Platforms.Linux;

[SupportedOSPlatform("linux")]
[SupportedOSPlatform("freebsd")]
public sealed class LinuxDefaultBrowserRegistrar : IDefaultBrowserRegistrar
{
    private const string DesktopFileName = "askmefirst.desktop";
    private const string MimeHttp = "x-scheme-handler/http";
    private const string MimeHttps = "x-scheme-handler/https";

    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    public async Task<RegistrationResult> RegisterAsync(CancellationToken ct = default)
    {
        try
        {
            string exePath = Environment.ProcessPath
                ?? throw new InvalidOperationException("Cannot determine running executable path.");

            string desktopDir = GetApplicationsDirectory();
            Directory.CreateDirectory(desktopDir);
            string desktopPath = Path.Combine(desktopDir, DesktopFileName);

            string desktopContents = BuildDesktopFile(exePath);
            await File.WriteAllTextAsync(desktopPath, desktopContents, Utf8NoBom, ct);

            await RunProcessAsync("xdg-mime", ["default", DesktopFileName, MimeHttp], ct);
            await RunProcessAsync("xdg-mime", ["default", DesktopFileName, MimeHttps], ct);
            await RunProcessAsync("update-desktop-database", [desktopDir], ct);

            return new RegistrationResult(
                Success: true,
                Message: "Registered as default browser. " +
                         "askmefirst now handles http:// and https:// links on this system.");
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
            string desktopDir = GetApplicationsDirectory();
            string desktopPath = Path.Combine(desktopDir, DesktopFileName);

            await RunProcessAsync("xdg-mime", ["unset-default", MimeHttp], ct);
            await RunProcessAsync("xdg-mime", ["unset-default", MimeHttps], ct);

            if (File.Exists(desktopPath))
            {
                File.Delete(desktopPath);
            }

            await RunProcessAsync("update-desktop-database", [desktopDir], ct);

            return new RegistrationResult(Success: true, Message: "Unregistered.");
        }
        catch (Exception ex)
        {
            return new RegistrationResult(Success: false, Message: $"Unregister failed: {ex.Message}");
        }
    }

    private static string GetApplicationsDirectory()
    {
        string home = Environment.GetEnvironmentVariable("HOME")
            ?? throw new InvalidOperationException("HOME environment variable is not set.");
        return Path.Combine(home, ".local", "share", "applications");
    }

    internal static string BuildDesktopFile(string exePath)
    {
        StringBuilder sb = new();
        sb.AppendLine("[Desktop Entry]");
        sb.AppendLine("Type=Application");
        sb.AppendLine("Name=AskMeFirst");
        sb.AppendLine("Comment=Smart browser router");
        sb.Append("Exec=").Append(exePath).AppendLine(" %u");
        sb.AppendLine("Icon=askmefirst");
        sb.AppendLine("Terminal=false");
        sb.AppendLine("Categories=Network;WebBrowser;");
        sb.AppendLine("MimeType=x-scheme-handler/http;x-scheme-handler/https;");
        sb.AppendLine("StartupNotify=true");
        return sb.ToString();
    }

    private static async Task RunProcessAsync(string fileName, IReadOnlyList<string> args, CancellationToken ct)
    {
        using Process p = new()
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                UseShellExecute = false,
            },
        };
        foreach (string arg in args)
        {
            p.StartInfo.ArgumentList.Add(arg);
        }
        if (!p.Start())
        {
            throw new InvalidOperationException($"Could not invoke {fileName}.");
        }
        await p.WaitForExitAsync(ct);
    }
}