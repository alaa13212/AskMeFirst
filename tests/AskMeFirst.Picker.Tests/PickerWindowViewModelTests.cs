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
        PickerRequest request = MakeRequest("https://example.com/path");
        TestLogger logger = new();
        PickerWindowViewModel vm = new(request, logger);

        Assert.Equal("https://example.com/path", vm.DisplayUrl);
    }

    [Fact]
    public void Constructor_PopulatesBrowserOptionsFromRequest()
    {
        Browser chrome = MakeBrowser("chrome", "Chrome", "/chrome");
        Browser firefox = MakeBrowser("firefox", "Firefox", "/firefox");
        PickerRequest request = MakeRequest("https://example.com", browsers: [chrome, firefox]);
        TestLogger logger = new();
        PickerWindowViewModel vm = new(request, logger);

        Assert.Equal(2, vm.BrowserOptions.Count);
        Assert.Equal("Chrome", vm.BrowserOptions[0].PrimaryLabel);
        Assert.Equal("Firefox", vm.BrowserOptions[1].PrimaryLabel);
    }

    [Fact]
    public void Constructor_SetsFirstBrowserAsHotkey1()
    {
        Browser chrome = MakeBrowser("chrome", "Chrome", "/chrome");
        PickerRequest request = MakeRequest("https://example.com", browsers: [chrome]);
        PickerWindowViewModel vm = new(request, new TestLogger());

        Assert.Equal("1", vm.BrowserOptions[0].HotkeyLabel);
    }

    [Fact]
    public void Constructor_BuildsRememberOptions_ForHostOnly()
    {
        PickerRequest request = MakeRequest("https://company.atlassian.net/wiki");
        PickerWindowViewModel vm = new(request, new TestLogger());

        Assert.Equal(3, vm.RememberOptions.Count);
        Assert.Equal(RememberKind.Once, vm.RememberOptions[0].Kind);
        Assert.Equal(RememberKind.AlwaysExactHost, vm.RememberOptions[1].Kind);
        Assert.Equal(RememberKind.AlwaysWildcardHost, vm.RememberOptions[2].Kind);
        Assert.All(vm.RememberOptions, option => Assert.True(option.IsAvailable));
    }

    [Fact]
    public void Constructor_NoHost_DisablesHostRememberOptions()
    {
        PickerRequest request = MakeRequest("file:///tmp/example.html");
        PickerWindowViewModel vm = new(request, new TestLogger());

        Assert.Equal(3, vm.RememberOptions.Count);
        Assert.True(vm.RememberOptions[0].IsAvailable);
        Assert.False(vm.RememberOptions[1].IsAvailable);
        Assert.False(vm.RememberOptions[2].IsAvailable);
    }

    [Fact]
    public void Constructor_DefaultsSelectedBrowserIndexToZero()
    {
        Browser chrome = MakeBrowser("chrome", "Chrome", "/chrome");
        Browser firefox = MakeBrowser("firefox", "Firefox", "/firefox");
        PickerRequest request = MakeRequest("https://example.com", browsers: [chrome, firefox]);
        PickerWindowViewModel vm = new(request, new TestLogger());

        Assert.Equal(0, vm.SelectedBrowserIndex);
    }

    [Fact]
    public void CommitCommand_CanExecute_ReadyAfterConstruction()
    {
        PickerRequest request = MakeRequest("https://example.com");
        PickerWindowViewModel vm = new(request, new TestLogger());

        Assert.True(vm.CommitCommand.CanExecute(null));
    }

    [Fact]
    public void Constructor_SelectsFirstRememberOption()
    {
        PickerRequest request = MakeRequest("https://example.com");
        PickerWindowViewModel vm = new(request, new TestLogger());

        Assert.True(vm.RememberOptions[0].IsSelected);
        Assert.False(vm.RememberOptions[1].IsSelected);
    }

    [Fact]
    public void Commit_WithFirstBrowser_SetsLaunchedResult()
    {
        Browser chrome = MakeBrowser("chrome", "Chrome", "/chrome");
        PickerRequest request = MakeRequest("https://example.com", browsers: [chrome]);
        PickerWindowViewModel vm = new(request, new TestLogger());

        vm.CommitCommand.Execute(null);

        Launched launched = Assert.IsType<Launched>(vm.Result);
        Assert.Equal("chrome", launched.Browser.Id);
        Assert.Equal(new Uri("https://example.com"), launched.Url);
    }

    [Fact]
    public void Commit_WithSelectedSecondBrowser_UsesThatBrowser()
    {
        Browser chrome = MakeBrowser("chrome", "Chrome", "/chrome");
        Browser firefox = MakeBrowser("firefox", "Firefox", "/firefox");
        PickerRequest request = MakeRequest("https://example.com", browsers: [chrome, firefox]);
        PickerWindowViewModel vm = new(request, new TestLogger());

        vm.SelectedBrowserIndex = 1;
        vm.CommitCommand.Execute(null);

        Launched launched = Assert.IsType<Launched>(vm.Result);
        Assert.Equal("firefox", launched.Browser.Id);
    }

    [Fact]
    public void Cancel_SetsCancelledResult()
    {
        PickerRequest request = MakeRequest("https://example.com");
        PickerWindowViewModel vm = new(request, new TestLogger());

        vm.CancelCommand.Execute(null);

        Assert.IsType<Cancelled>(vm.Result);
    }

    [Fact]
    public async Task Constructor_WithCompletedUnshortenTask_UpdatesDisplayUrl()
    {
        Browser chrome = MakeBrowser("chrome", "Chrome", "/chrome");
        PickerRequest request = MakeRequest("https://t.co/abc", browsers: [chrome], unshortenTask: Task.FromResult<string?>("https://example.com/article"));
        PickerWindowViewModel vm = new(request, new TestLogger());
        await Task.Yield();

        Assert.Equal("https://example.com/article", vm.DisplayUrl);
    }

    [Fact]
    public async Task Commit_WithResolvedUrl_LaunchesResolved()
    {
        Browser chrome = MakeBrowser("chrome", "Chrome", "/chrome");
        PickerRequest request = MakeRequest("https://t.co/abc", browsers: [chrome], unshortenTask: Task.FromResult<string?>("https://example.com/article"));
        PickerWindowViewModel vm = new(request, new TestLogger());
        await Task.Yield();

        vm.CommitCommand.Execute(null);

        Launched launched = Assert.IsType<Launched>(vm.Result);
        Assert.Equal(new Uri("https://example.com/article"), launched.Url);
    }

    [Fact]
    public async Task Commit_WithNullUnshortenResult_LaunchesOriginalUrl()
    {
        Browser chrome = MakeBrowser("chrome", "Chrome", "/chrome");
        PickerRequest request = MakeRequest("https://t.co/abc", browsers: [chrome], unshortenTask: Task.FromResult<string?>(null));
        PickerWindowViewModel vm = new(request, new TestLogger());
        await Task.Yield();

        vm.CommitCommand.Execute(null);

        Launched launched = Assert.IsType<Launched>(vm.Result);
        Assert.Equal(new Uri("https://t.co/abc"), launched.Url);
    }

    [Fact]
    public async Task Constructor_WithNullUnshortenResult_DisplayUrlStaysOriginal()
    {
        Browser chrome = MakeBrowser("chrome", "Chrome", "/chrome");
        PickerRequest request = MakeRequest("https://t.co/abc", browsers: [chrome], unshortenTask: Task.FromResult<string?>(null));
        PickerWindowViewModel vm = new(request, new TestLogger());
        await Task.Yield();

        Assert.Equal("https://t.co/abc", vm.DisplayUrl);
    }

    private static Browser MakeBrowser(string id, string displayName, string executablePath)
    {
        return new Browser
        {
            Id = id,
            DisplayName = displayName,
            ExecutablePath = executablePath,
            LaunchStrategy = BrowserLaunchStrategies.For(id),
        };
    }

    private static PickerRequest MakeRequest(string url, IReadOnlyList<Browser>? browsers = null, Task<string?>? unshortenTask = null) =>
        new(
            OriginalUrl: new Uri(url),
            UnshortenTask: unshortenTask,
            AvailableBrowsers: (browsers ?? []).Select(b => new PickerBrowserOption(b, b.Profile)).ToList());

    private sealed class TestLogger : Core.Abstractions.ILogger
    {
        public void LogInfo(string message) { }
        public void LogWarn(string message) { }
        public void LogError(string message) { }
    }
}
