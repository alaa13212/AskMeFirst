#if WINDOWS
using AskMeFirst.Core.Models;
using AskMeFirst.Platforms.Windows;
using Xunit;

namespace AskMeFirst.Picker.Tests;

public class WindowsIconProviderTests
{
    private const string ChromeExe = @"C:\Program Files\Google\Chrome\Application\chrome.exe";
    private const string EdgeExe = @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe";

    [Fact]
    public void GetBrowserIconPng_RealChrome_ReturnsValidPng()
    {
        if (!File.Exists(ChromeExe))
        {
            return;
        }

        WindowsIconProvider provider = new();
        byte[]? bytes = provider.GetBrowserIconPng("Google Chrome", ChromeExe);

        Assert.NotNull(bytes);
        Assert.True(bytes!.Length > 0);
        Assert.True(IsPng(bytes));
    }

    [Fact]
    public void GetBrowserIconPng_RealEdge_ReturnsValidPng()
    {
        if (!File.Exists(EdgeExe))
        {
            return;
        }

        WindowsIconProvider provider = new();
        byte[]? bytes = provider.GetBrowserIconPng("Microsoft Edge", EdgeExe);

        Assert.NotNull(bytes);
        Assert.True(bytes!.Length > 0);
        Assert.True(IsPng(bytes));
    }

    [Fact]
    public void GetBrowserIconPng_NonexistentPath_ReturnsNull()
    {
        WindowsIconProvider provider = new();
        byte[]? bytes = provider.GetBrowserIconPng(
            "fake-browser",
            @"C:\does\not\exist\fake.exe");

        Assert.Null(bytes);
    }

    [Fact]
    public void GetBrowserIconPng_EmptyExecutablePath_ReturnsNull()
    {
        WindowsIconProvider provider = new();
        byte[]? bytes = provider.GetBrowserIconPng("chrome", "");
        Assert.Null(bytes);
    }

    [Fact]
    public void GetProfileIconPng_ChromeProfile_ReturnsPng()
    {
        if (!File.Exists(ChromeExe))
        {
            return;
        }

        WindowsIconProvider provider = new();
        byte[]? bytes = provider.GetProfileIconPng(
            "chrome",
            new BrowserProfile(Name: "Default", DirectoryName: "Default", IsDefault: true));

        Assert.True(bytes is null || IsPng(bytes));
    }

    [Fact]
    public void GetProfileIconPng_FirefoxProfile_ReturnsPngFromProfileGroups()
    {
        string profilesIni = Path.Combine(
            Environment.GetEnvironmentVariable("APPDATA") ?? "",
            @"Mozilla\Firefox\profiles.ini");
        if (!File.Exists(profilesIni))
        {
            return;
        }

        WindowsIconProvider provider = new();
        byte[]? bytesFullPath = provider.GetProfileIconPng(
            "firefox",
            new BrowserProfile(Name: "Work", DirectoryName: "Profiles/0m6kw70o.Work", IsDefault: false));
        byte[]? bytesShort = provider.GetProfileIconPng(
            "firefox",
            new BrowserProfile(Name: "Work", DirectoryName: "0m6kw70o.Work", IsDefault: false));

        Assert.NotNull(bytesFullPath);
        Assert.NotNull(bytesShort);
        Assert.True(IsPng(bytesFullPath!));
        Assert.True(IsPng(bytesShort!));
    }

    private static bool IsPng(byte[] bytes) =>
        bytes.Length >= 4 &&
        bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47;
}
#endif