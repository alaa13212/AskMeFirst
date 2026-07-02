using System.Text.RegularExpressions;
using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Launch;
using AskMeFirst.Core.Models;
using AskMeFirst.Core.Paths;
using Microsoft.Win32;

namespace AskMeFirst.Platforms.Windows;

public sealed partial class WindowsBrowserInventory : IBrowserInventory
{
    private const string StartMenuInternetKey = @"SOFTWARE\WOW6432Node\Clients\StartMenuInternet";

    private static readonly Dictionary<string, string> IdOverrides =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["google chrome"] = "chrome",
            ["firefox"] = "firefox",
            ["mozilla firefox"] = "firefox",
            ["microsoft edge"] = "edge",
            ["brave browser"] = "brave",
        };

    private static readonly Dictionary<string, string> DisplayOverrides =
        new(StringComparer.Ordinal)
        {
            ["Google Chrome"] = "Google Chrome",
            ["Firefox"] = "Mozilla Firefox",
            ["Mozilla Firefox"] = "Mozilla Firefox",
            ["Microsoft Edge"] = "Microsoft Edge",
            ["Brave Browser"] = "Brave",
        };

    private static readonly Regex FirefoxHashSuffix = FirefoxHashSuffixRegex();

    public IReadOnlyList<Browser> Discover()
    {
        List<Browser> browsers = [];

        if (!OperatingSystem.IsWindows())
        {
            return browsers;
        }

        using RegistryKey? rootKey = Registry.LocalMachine.OpenSubKey(StartMenuInternetKey);
        if (rootKey is null)
        {
            return browsers;
        }

        foreach (string browserName in rootKey.GetSubKeyNames())
        {
            Browser? browser = ReadBrowser(browserName);
            if (browser is not null && !SelfExecutable.IsSelf(browser.ExecutablePath))
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
            $@"{StartMenuInternetKey}\{registryName}\shell\open\command");

        object? commandValue = browserKey?.GetValue(null);
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
        IBrowserLaunchStrategy launchStrategy = BrowserLaunchStrategies.For(id);
        return new Browser
        {
            Id = id,
            DisplayName = displayName,
            ExecutablePath = executable,
            LaunchStrategy = launchStrategy,
        };
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
        string baseName = StripFirefoxHash(registryName);

        return IdOverrides.TryGetValue(baseName, out string? mapped)
            ? mapped
            : baseName.ToLowerInvariant().Replace(' ', '-');
    }

    private static string NormalizeDisplayName(string registryName)
    {
        string baseName = StripFirefoxHash(registryName);

        return DisplayOverrides.GetValueOrDefault(baseName, baseName);
    }

    private static string StripFirefoxHash(string registryName)
    {
        return FirefoxHashSuffix.Replace(registryName, "");
    }

    [GeneratedRegex(@"-[\dA-F]{16}$", RegexOptions.IgnoreCase)]
    private static partial Regex FirefoxHashSuffixRegex();
}
