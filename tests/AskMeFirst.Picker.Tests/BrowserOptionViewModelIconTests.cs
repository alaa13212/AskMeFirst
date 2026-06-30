using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Launch;
using AskMeFirst.Core.Models;
using AskMeFirst.Core.Routing;
using AskMeFirst.Picker.ViewModels;
using Avalonia.Headless.XUnit;
using SkiaSharp;
using Xunit;

namespace AskMeFirst.Picker.Tests;

public class BrowserOptionViewModelIconTests
{
    [AvaloniaFact]
    public void PrimaryIcon_FallsBackToBrowserIcon_WhenNoProfilePic()
    {
        FakeIconProvider icons = new() { BrowserBytes = MakePng() };

        Browser browser = MakeBrowser("chrome", "Chrome");
        using BrowserOptionViewModel vm = new(browser, profile: null, hotkey: 1, icons);

        Assert.NotNull(vm.PrimaryIcon);
        Assert.False(vm.HasOverlay);
    }

    [AvaloniaFact]
    public void PrimaryIcon_UsesProfilePic_WhenAvailable()
    {
        byte[] profilePic = MakePng(r: 0xFF, g: 0x00, b: 0x00);
        byte[] browserIcon = MakePng(r: 0x00, g: 0xFF, b: 0x00);
        FakeIconProvider icons = new()
        {
            BrowserBytes = browserIcon,
            ProfileBytes = profilePic,
        };

        Browser browser = MakeBrowser("chrome", "Chrome");
        BrowserProfile profile = new(Name: "Work", DirectoryName: "Profile 1", IsDefault: false);
        using BrowserOptionViewModel vm = new(browser, profile, hotkey: 1, icons);

        Assert.NotNull(vm.PrimaryIcon);
        Assert.True(vm.HasOverlay);
        Assert.NotNull(vm.OverlayIcon);
    }

    [AvaloniaFact]
    public void PrimaryIcon_FallsBackToBrowser_WhenProfilePicMissing()
    {
        byte[] browserIcon = MakePng();
        FakeIconProvider icons = new()
        {
            BrowserBytes = browserIcon,
            ProfileBytes = null,
        };

        Browser browser = MakeBrowser("chrome", "Chrome");
        BrowserProfile profile = new(Name: "Work", DirectoryName: "Profile 1", IsDefault: false);
        using BrowserOptionViewModel vm = new(browser, profile, hotkey: 1, icons);

        Assert.NotNull(vm.PrimaryIcon);
        Assert.False(vm.HasOverlay);
        Assert.Null(vm.OverlayIcon);
    }

    [AvaloniaFact]
    public void PrimaryIcon_Null_WhenNeitherAvailable()
    {
        FakeIconProvider icons = new() { BrowserBytes = null, ProfileBytes = null };

        Browser browser = MakeBrowser("chrome", "Chrome");
        BrowserProfile profile = new(Name: "Work", DirectoryName: "Profile 1", IsDefault: false);
        using BrowserOptionViewModel vm = new(browser, profile, hotkey: 1, icons);

        Assert.Null(vm.PrimaryIcon);
        Assert.False(vm.HasOverlay);
        Assert.Null(vm.OverlayIcon);
    }

    private static byte[] MakePng(byte r = 0xFF, byte g = 0xFF, byte b = 0xFF)
    {
        using MemoryStream ms = new();
        using SKBitmap bmp = new(4, 4);
        using (SKCanvas canvas = new(bmp))
        {
            canvas.Clear(new SKColor(r, g, b));
        }
        using SKImage img = SKImage.FromBitmap(bmp);
        using SKData data = img.Encode(SKEncodedImageFormat.Png, 100);
        data.SaveTo(ms);
        return ms.ToArray();
    }

    private static Browser MakeBrowser(string id, string name) =>
        new()
        {
            Id = id,
            DisplayName = name,
            ExecutablePath = $"/{id}",
            LaunchStrategy = BrowserLaunchStrategies.For(id),
        };

    private sealed class FakeIconProvider : IIconProvider
    {
        public byte[]? BrowserBytes { get; set; }
        public byte[]? ProfileBytes { get; set; }

        public byte[]? GetBrowserIconPng(string browserId, string executablePath) => BrowserBytes;
        public byte[]? GetProfileIconPng(string browserId, BrowserProfile profile) => ProfileBytes;
    }
}