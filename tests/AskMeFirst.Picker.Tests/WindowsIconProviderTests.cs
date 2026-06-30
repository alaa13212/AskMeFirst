#if WINDOWS
using AskMeFirst.Core.Data;
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
        Assert.True(PngSignature.Matches(bytes));
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
        Assert.True(PngSignature.Matches(bytes));
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

        Assert.True(bytes is null || PngSignature.Matches(bytes));
    }

    [Fact]
    public void GetProfileIconPng_FirefoxProfile_Barrak_ReturnsPng()
    {
        string groupsRoot = Path.Combine(
            Environment.GetEnvironmentVariable("APPDATA") ?? "",
            @"Mozilla\Firefox\Profile Groups");
        if (!Directory.Exists(groupsRoot))
        {
            return;
        }

        WindowsIconProvider provider = new();
        byte[]? bytes = provider.GetProfileIconPng(
            "firefox",
            new BrowserProfile(Name: "Barrak", DirectoryName: "Profiles/vc4ak1jq.Barrak-1706255686136", IsDefault: true));

        Assert.NotNull(bytes);
        Assert.True(PngSignature.Matches(bytes!));
    }

    [Fact]
    public void GetProfileIconPng_FirefoxProfile_Profile5_ReturnsPng()
    {
        string groupsRoot = Path.Combine(
            Environment.GetEnvironmentVariable("APPDATA") ?? "",
            @"Mozilla\Firefox\Profile Groups");
        if (!Directory.Exists(groupsRoot))
        {
            return;
        }

        WindowsIconProvider provider = new();
        byte[]? bytes = provider.GetProfileIconPng(
            "firefox",
            new BrowserProfile(Name: "Profile 5", DirectoryName: "Profiles/kXwwp1SX.Profile 2", IsDefault: false));

        Assert.NotNull(bytes);
        Assert.True(PngSignature.Matches(bytes!));
    }

}
#endif
