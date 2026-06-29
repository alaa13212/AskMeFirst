using AskMeFirst.Core.Launch;
using AskMeFirst.Core.Models;
using AskMeFirst.Core.Routing;
using Xunit;

namespace AskMeFirst.Core.Tests;

public class PickerOptionsTests
{
    [Fact]
    public void Build_NoProfilesDetected_ReturnsSingleNullProfileOption()
    {
        Browser chrome = MakeBrowser("chrome", "Chrome");
        FakeProfileDetector profiles = new();

        IReadOnlyList<PickerBrowserOption> result = PickerOptions.Build([chrome], profiles);

        Assert.Single(result);
        Assert.Null(result[0].Profile);
        Assert.Equal("chrome", result[0].Browser.Id);
    }

    [Fact]
    public void Build_WithProfiles_ReturnsOneOptionPerProfile()
    {
        Browser chrome = MakeBrowser("chrome", "Chrome");
        BrowserProfile work = new(Name: "Work", DirectoryName: "work", IsDefault: false);
        BrowserProfile personal = new(Name: "Personal", DirectoryName: "personal", IsDefault: true);
        FakeProfileDetector profiles = new()
        {
            Profiles = { ["chrome"] = [work, personal] },
        };

        IReadOnlyList<PickerBrowserOption> result = PickerOptions.Build([chrome], profiles);

        Assert.Equal(2, result.Count);
        Assert.All(result, o => Assert.Equal("chrome", o.Browser.Id));
        Assert.Contains(result, o => o.Profile?.Name == "Work");
        Assert.Contains(result, o => o.Profile?.Name == "Personal");
    }

    [Fact]
    public void Build_WithProfiles_PropagatesProfileToBrowser()
    {
        Browser chrome = MakeBrowser("chrome", "Chrome");
        BrowserProfile work = new(Name: "Work", DirectoryName: "work", IsDefault: false);
        FakeProfileDetector profiles = new()
        {
            Profiles = { ["chrome"] = [work] },
        };

        IReadOnlyList<PickerBrowserOption> result = PickerOptions.Build([chrome], profiles);

        Assert.NotNull(result[0].Browser.Profile);
        Assert.Equal("Work", result[0].Browser.Profile!.Name);
    }

    [Fact]
    public void Build_MultipleBrowsers_GroupsByBrowser()
    {
        Browser chrome = MakeBrowser("chrome", "Chrome");
        Browser firefox = MakeBrowser("firefox", "Firefox");
        BrowserProfile chromeWork = new(Name: "Work", DirectoryName: "work", IsDefault: true);
        BrowserProfile firefoxDefault = new(Name: "default-release", DirectoryName: "default-release", IsDefault: true);
        FakeProfileDetector profiles = new()
        {
            Profiles =
            {
                ["chrome"] = [chromeWork],
                ["firefox"] = [firefoxDefault],
            },
        };

        IReadOnlyList<PickerBrowserOption> result = PickerOptions.Build([chrome, firefox], profiles);

        Assert.Equal(2, result.Count);
        Assert.Equal("chrome", result[0].Browser.Id);
        Assert.Equal("firefox", result[1].Browser.Id);
    }

    [Fact]
    public void Build_MixedProfilesAndNoProfiles_PreservesAll()
    {
        Browser chrome = MakeBrowser("chrome", "Chrome");
        Browser firefox = MakeBrowser("firefox", "Firefox");
        BrowserProfile firefoxWork = new(Name: "Work", DirectoryName: "work", IsDefault: true);
        FakeProfileDetector profiles = new()
        {
            Profiles = { ["firefox"] = [firefoxWork] },
        };

        IReadOnlyList<PickerBrowserOption> result = PickerOptions.Build([chrome, firefox], profiles);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, o => o.Browser.Id == "chrome" && o.Profile is null);
        Assert.Contains(result, o => o.Browser.Id == "firefox" && o.Profile?.Name == "Work");
    }

    [Fact]
    public void Build_EmptyBrowsers_ReturnsEmpty()
    {
        FakeProfileDetector profiles = new();

        IReadOnlyList<PickerBrowserOption> result = PickerOptions.Build([], profiles);

        Assert.Empty(result);
    }

    private static Browser MakeBrowser(string id, string name) =>
        new()
        {
            Id = id,
            DisplayName = name,
            ExecutablePath = $"/{id}",
            LaunchStrategy = BrowserLaunchStrategies.For(id),
        };
}