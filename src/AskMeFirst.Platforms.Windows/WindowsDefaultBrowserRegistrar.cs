using System.Runtime.Versioning;
using AskMeFirst.Core.Abstractions;
using Microsoft.Win32;

namespace AskMeFirst.Platforms.Windows;

[SupportedOSPlatform("windows")]
public sealed class WindowsDefaultBrowserRegistrar : IDefaultBrowserRegistrar
{
    private const string AppId = "AskMeFirst";
    private const string BaseKey = @"Software\Clients\StartMenuInternet";

    public Task<RegistrationResult> RegisterAsync(CancellationToken ct = default)
    {
        try
        {
            string exePath = Environment.ProcessPath
                ?? throw new InvalidOperationException("Cannot determine running executable path.");

            using RegistryKey askme = Registry.CurrentUser.CreateSubKey($@"{BaseKey}\{AppId}");
            askme.SetValue("", AppId);

            using RegistryKey icon = askme.CreateSubKey("DefaultIcon");
            icon.SetValue("", $"{exePath},0");

            using RegistryKey shellOpenCmd = askme.CreateSubKey(@"shell\open\command");
            shellOpenCmd.SetValue("", $"\"{exePath}\" \"%1\"");

            using RegistryKey caps = askme.CreateSubKey("Capabilities");
            caps.SetValue("ApplicationName", "AskMeFirst");
            caps.SetValue("ApplicationIcon", $"{exePath},0");

            using RegistryKey startMenu = caps.CreateSubKey("StartMenu");
            startMenu.SetValue("", "AskMeFirst");

            using RegistryKey urlAssoc = caps.CreateSubKey("URLAssociations");
            urlAssoc.SetValue("http", AppId);
            urlAssoc.SetValue("https", AppId);

            using RegistryKey? clients = Registry.CurrentUser.OpenSubKey(BaseKey, writable: true);
            clients?.SetValue("", AppId);

            return Task.FromResult(new RegistrationResult(
                Success: true,
                Message: "Registered as default browser candidate. " +
                         "Open Settings → Apps → Default apps → Web browser → AskMeFirst to finish setup."));
        }
        catch (Exception ex)
        {
            return Task.FromResult(new RegistrationResult(
                Success: false,
                Message: $"Registration failed: {ex.Message}"));
        }
    }

    public Task<RegistrationResult> UnregisterAsync(CancellationToken ct = default)
    {
        try
        {
            Registry.CurrentUser.DeleteSubKeyTree($@"{BaseKey}\{AppId}", throwOnMissingSubKey: false);

            using RegistryKey? clients = Registry.CurrentUser.OpenSubKey(BaseKey, writable: true);
            if (clients?.GetValue("") as string == AppId)
            {
                clients.SetValue("", "");
            }
            return Task.FromResult(new RegistrationResult(
                Success: true,
                Message: "Unregistered."));
        }
        catch (Exception ex)
        {
            return Task.FromResult(new RegistrationResult(
                Success: false,
                Message: $"Unregister failed: {ex.Message}"));
        }
    }
}