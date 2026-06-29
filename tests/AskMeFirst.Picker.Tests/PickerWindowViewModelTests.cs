using AskMeFirst.Core.Launch;
using AskMeFirst.Core.Models;
using AskMeFirst.Core.Routing;
using AskMeFirst.Picker.ViewModels;
using Xunit;

namespace AskMeFirst.Picker.Tests;

public class PickerWindowViewModelTests
{
    [Fact]
    public void Constructor_PopulatesDisplayUrlFromRequest()
    {
        PickerRequest request = MakeRequest("https://example.com/path", sourceApp: null);
        TestLogger logger = new();
        PickerWindowViewModel vm = new(request, logger);

        Assert.Equal("https://example.com/path", vm.DisplayUrl);
    }

    [Fact]
    public void Constructor_PopulatesBrowserOptionsFromRequest()
    {
        Browser chrome = new() { Id = "chrome", DisplayName = "Chrome", ExecutablePath = "/chrome", LaunchStrategy = BrowserLaunchStrategies.For("chrome") };
        Browser firefox = new() { Id = "firefox", DisplayName = "Firefox", ExecutablePath = "/firefox", LaunchStrategy = BrowserLaunchStrategies.For("firefox") };
        PickerRequest request = MakeRequest("https://example.com", sourceApp: null, browsers: [chrome, firefox]);
        TestLogger logger = new();
        PickerWindowViewModel vm = new(request, logger);

        Assert.Equal(2, vm.BrowserOptions.Count);
        Assert.Equal("Chrome", vm.BrowserOptions[0].PrimaryLabel);
        Assert.Equal("Firefox", vm.BrowserOptions[1].PrimaryLabel);
    }

    [Fact]
    public void Constructor_SetsFirstBrowserAsHotkey1()
    {
        Browser chrome = new() { Id = "chrome", DisplayName = "Chrome", ExecutablePath = "/chrome", LaunchStrategy = BrowserLaunchStrategies.For("chrome") };
        PickerRequest request = MakeRequest("https://example.com", sourceApp: null, browsers: [chrome]);
        PickerWindowViewModel vm = new(request, new TestLogger());

        Assert.Equal("1", vm.BrowserOptions[0].HotkeyLabel);
    }

    [Fact]
    public void Constructor_NoSourceApp_HidesSourceAppLabel()
    {
        PickerRequest request = MakeRequest("https://example.com", sourceApp: null);
        PickerWindowViewModel vm = new(request, new TestLogger());

        Assert.Equal("", vm.SourceAppLabel);
        Assert.False(vm.IsSourceAppLabelVisible);
    }

    [Fact]
    public void Constructor_WithSourceApp_ShowsSourceAppLabel()
    {
        PickerRequest request = MakeRequest("https://example.com", sourceApp: "slack");
        PickerWindowViewModel vm = new(request, new TestLogger());

        Assert.Equal("From slack", vm.SourceAppLabel);
        Assert.True(vm.IsSourceAppLabelVisible);
    }

    [Fact]
    public void Constructor_BuildsRememberOptions_AllFiveWhenSourceAndHost()
    {
        PickerRequest request = MakeRequest("https://company.atlassian.net/wiki", sourceApp: "slack");
        PickerWindowViewModel vm = new(request, new TestLogger());

        Assert.Equal(5, vm.RememberOptions.Count);
        Assert.True(vm.RememberOptions[0].IsAvailable);
        Assert.True(vm.RememberOptions[1].IsAvailable);
        Assert.True(vm.RememberOptions[2].IsAvailable);
        Assert.True(vm.RememberOptions[3].IsAvailable);
        Assert.True(vm.RememberOptions[4].IsAvailable);
    }

    [Fact]
    public void Constructor_NoSourceApp_DisablesSourceBasedOptions()
    {
        PickerRequest request = MakeRequest("https://example.com", sourceApp: null);
        PickerWindowViewModel vm = new(request, new TestLogger());

        Assert.Equal(5, vm.RememberOptions.Count);
        Assert.True(vm.RememberOptions[0].IsAvailable);
        Assert.True(vm.RememberOptions[1].IsAvailable);
        Assert.True(vm.RememberOptions[2].IsAvailable);
        Assert.False(vm.RememberOptions[3].IsAvailable);
        Assert.False(vm.RememberOptions[4].IsAvailable);
    }

    [Fact]
    public void Constructor_DefaultsSelectedBrowserIndexToZero()
    {
        Browser chrome = new() { Id = "chrome", DisplayName = "Chrome", ExecutablePath = "/chrome", LaunchStrategy = BrowserLaunchStrategies.For("chrome") };
        Browser firefox = new() { Id = "firefox", DisplayName = "Firefox", ExecutablePath = "/firefox", LaunchStrategy = BrowserLaunchStrategies.For("firefox") };
        PickerRequest request = MakeRequest("https://example.com", sourceApp: null, browsers: [chrome, firefox]);
        PickerWindowViewModel vm = new(request, new TestLogger());

        Assert.Equal(0, vm.SelectedBrowserIndex);
    }

    [Fact]
    public void CommitCommand_CanExecute_ReadyAfterConstruction()
    {
        PickerRequest request = MakeRequest("https://example.com", sourceApp: null);
        PickerWindowViewModel vm = new(request, new TestLogger());

        Assert.True(vm.CommitCommand.CanExecute(null));
    }

    [Fact]
    public void Constructor_SelectsFirstRememberOption()
    {
        PickerRequest request = MakeRequest("https://example.com", sourceApp: null);
        PickerWindowViewModel vm = new(request, new TestLogger());

        Assert.True(vm.RememberOptions[0].IsSelected);
        Assert.False(vm.RememberOptions[1].IsSelected);
    }

    [Fact]
    public void Commit_WithFirstBrowser_SetsLaunchedResult()
    {
        Browser chrome = new() { Id = "chrome", DisplayName = "Chrome", ExecutablePath = "/chrome", LaunchStrategy = BrowserLaunchStrategies.For("chrome") };
        PickerRequest request = MakeRequest("https://example.com", sourceApp: null, browsers: [chrome]);
        PickerWindowViewModel vm = new(request, new TestLogger());

        vm.CommitCommand.Execute(null);

        Launched launched = Assert.IsType<Launched>(vm.Result);
        Assert.Equal("chrome", launched.Browser.Id);
        Assert.Equal(new Uri("https://example.com"), launched.Url);
    }

    [Fact]
    public void Commit_WithSelectedSecondBrowser_UsesThatBrowser()
    {
        Browser chrome = new() { Id = "chrome", DisplayName = "Chrome", ExecutablePath = "/chrome", LaunchStrategy = BrowserLaunchStrategies.For("chrome") };
        Browser firefox = new() { Id = "firefox", DisplayName = "Firefox", ExecutablePath = "/firefox", LaunchStrategy = BrowserLaunchStrategies.For("firefox") };
        PickerRequest request = MakeRequest("https://example.com", sourceApp: null, browsers: [chrome, firefox]);
        PickerWindowViewModel vm = new(request, new TestLogger());

        vm.SelectedBrowserIndex = 1;
        vm.CommitCommand.Execute(null);

        Launched launched = Assert.IsType<Launched>(vm.Result);
        Assert.Equal("firefox", launched.Browser.Id);
    }

    [Fact]
    public void Cancel_SetsCancelledResult()
    {
        PickerRequest request = MakeRequest("https://example.com", sourceApp: null);
        PickerWindowViewModel vm = new(request, new TestLogger());

        vm.CancelCommand.Execute(null);

        Assert.IsType<Cancelled>(vm.Result);
    }

    private static PickerRequest MakeRequest(string url, string? sourceApp, IReadOnlyList<Browser>? browsers = null) =>
        new(
            OriginalUrl: new Uri(url),
            SourceApp: sourceApp,
            UnshortenTask: null,
            AvailableBrowsers: (browsers ?? []).Select(b => new PickerBrowserOption(b, b.Profile)).ToList());

    private sealed class TestLogger : Core.Abstractions.ILogger
    {
        public void LogInfo(string message) { }
        public void LogWarn(string message) { }
        public void LogError(string message) { }
    }
}