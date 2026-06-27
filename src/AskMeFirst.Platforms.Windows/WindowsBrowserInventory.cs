using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Models;
using Microsoft.Win32;

namespace AskMeFirst.Platforms.Windows;

public sealed class WindowsBrowserInventory : IBrowserInventory
{
    private const string START_MENU_INTERNET_KEY =
        @"SOFTWARE\WOW6432Node\Clients\StartMenuInternet";

    public IReadOnlyList<Browser> Discover()
    {
        List<Browser> browsers = [];

        if (!OperatingSystem.IsWindows())
        {
            return browsers;
        }

        using RegistryKey? rootKey = Registry.LocalMachine.OpenSubKey(START_MENU_INTERNET_KEY);
        if (rootKey is null)
        {
            return browsers;
        }

        foreach (string browserName in rootKey.GetSubKeyNames())
        {
            Browser? browser = ReadBrowser(browserName);
            if (browser is not null)
            {
                browsers.Add(browser);
            }
        }
        return browsers;
    }

    public Browser? FindById(string id)
    {
        return Discover().FirstOrDefault(b =>
            string.Equals(b.Id, id, StringComparison.OrdinalIgnoreCase));
    }

    private static Browser? ReadBrowser(string registryName)
    {
        if (!OperatingSystem.IsWindows())
        {
            return null;
        }

        using RegistryKey? browserKey = Registry.LocalMachine.OpenSubKey(
            $@"{START_MENU_INTERNET_KEY}\{registryName}\shell\open\command");
        if (browserKey is null)
        {
            return null;
        }

        object? commandValue = browserKey.GetValue(null);
        if (commandValue is not string rawCommand || string.IsNullOrWhiteSpace(rawCommand))
        {
            return null;
        }

        string executable = ParseExecutable(rawCommand);
        if (string.IsNullOrWhiteSpace(executable) || !File.Exists(executable))
        {
            return null;
        }

        string id = NormalizeId(registryName);
        string displayName = NormalizeDisplayName(registryName);
        return new Browser(id, displayName, executable);
    }

    private static string ParseExecutable(string rawCommand)
    {
        string trimmed = rawCommand.Trim();
        if (trimmed.StartsWith('"'))
        {
            int closingQuote = trimmed.IndexOf('"', 1);
            return closingQuote > 0 ? trimmed[1..closingQuote] : trimmed[1..];
        }
        int firstSpace = trimmed.IndexOf(' ');
        return firstSpace > 0 ? trimmed[..firstSpace] : trimmed;
    }

    private static string NormalizeId(string registryName)
    {
        string lowered = registryName.ToLowerInvariant();
        return lowered switch
        {
            "google chrome" => "chrome",
            "firefox" or "mozilla firefox" => "firefox",
            "microsoft edge" => "edge",
            "brave" or "brave browser" => "brave",
            "opera" => "opera",
            "vivaldi" => "vivaldi",
            _ => lowered.Replace(' ', '-'),
        };
    }

    private static string NormalizeDisplayName(string registryName)
    {
        return registryName switch
        {
            "Google Chrome" => "Google Chrome",
            "Firefox" or "Mozilla Firefox" => "Mozilla Firefox",
            "Microsoft Edge" => "Microsoft Edge",
            "Brave" or "Brave Browser" => "Brave",
            "Opera" => "Opera",
            "Vivaldi" => "Vivaldi",
            _ => registryName,
        };
    }
}
