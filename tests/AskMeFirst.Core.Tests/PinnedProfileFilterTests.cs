using AskMeFirst.Core.Config;
using AskMeFirst.Core.Launch;
using AskMeFirst.Core.Models;
using AskMeFirst.Core.Profiles;
using AskMeFirst.Core.Routing;
using Xunit;

namespace AskMeFirst.Core.Tests;

public class PinnedProfileFilterTests
{
    [Fact]
    public void NoPins_ReturnsAllOptions()
    {
        Browser chrome = MakeBrowser("chrome", "Chrome");
        Browser firefox = MakeBrowser("firefox", "Firefox");
        PickerBrowserOption chromeOpt = new(chrome, null);
        PickerBrowserOption firefoxOpt = new(firefox, null);

        IReadOnlyList<PickerBrowserOption> result = PinnedProfileFilter.Filter(
            [chromeOpt, firefoxOpt],
            []);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void UnpinnedSpecs_AreTreatedAsNoPins()
    {
        Browser chrome = MakeBrowser("chrome", "Chrome");
        PickerBrowserOption chromeOpt = new(chrome, null);
        ProfileSpec spec = new() { Id = "chrome-profile", BrowserId = "chrome", Pinned = false };

        IReadOnlyList<PickerBrowserOption> result = PinnedProfileFilter.Filter(
            [chromeOpt],
            [spec]);

        Assert.Single(result);
    }

    [Fact]
    public void PinnedByName_KeepsMatchingOption()
    {
        Browser firefox = MakeBrowser("firefox", "Firefox");
        BrowserProfile workProfile = new(Name: "Work", DirectoryName: "work", IsDefault: false);
        BrowserProfile personalProfile = new(Name: "Personal", DirectoryName: "personal", IsDefault: false);
        PickerBrowserOption workOpt = new(firefox, workProfile);
        PickerBrowserOption personalOpt = new(firefox, personalProfile);

        ProfileSpec pinnedSpec = new()
        {
            Id = "firefox-work-profile",
            BrowserId = "firefox",
            Name = "Work",
            Pinned = true,
        };

        IReadOnlyList<PickerBrowserOption> result = PinnedProfileFilter.Filter(
            [workOpt, personalOpt],
            [pinnedSpec]);

        Assert.Single(result);
        Assert.Equal("Work", result[0].Profile!.Name);
    }

    [Fact]
    public void PinnedByDirectory_KeepsMatchingOption()
    {
        Browser firefox = MakeBrowser("firefox", "Firefox");
        BrowserProfile workProfile = new(Name: "Work", DirectoryName: "abc.work", IsDefault: false);
        PickerBrowserOption workOpt = new(firefox, workProfile);

        ProfileSpec pinnedSpec = new()
        {
            Id = "firefox-work-profile",
            BrowserId = "firefox",
            Directory = "abc.work",
            Pinned = true,
        };

        IReadOnlyList<PickerBrowserOption> result = PinnedProfileFilter.Filter(
            [workOpt],
            [pinnedSpec]);

        Assert.Single(result);
        Assert.Equal("abc.work", result[0].Profile!.DirectoryName);
    }

    [Fact]
    public void PinnedSpecForDifferentBrowser_DropsAllOptions()
    {
        Browser firefox = MakeBrowser("firefox", "Firefox");
        BrowserProfile workProfile = new(Name: "Work", DirectoryName: "work", IsDefault: false);
        PickerBrowserOption workOpt = new(firefox, workProfile);

        ProfileSpec pinnedSpec = new()
        {
            Id = "chrome-work-profile",
            BrowserId = "chrome",
            Name = "Work",
            Pinned = true,
        };

        IReadOnlyList<PickerBrowserOption> result = PinnedProfileFilter.Filter(
            [workOpt],
            [pinnedSpec]);

        Assert.Empty(result);
    }

    [Fact]
    public void NullProfileOptions_AreAlwaysKept_EvenWhenOtherProfilesPinned()
    {
        Browser chrome = MakeBrowser("chrome", "Chrome");
        Browser firefox = MakeBrowser("firefox", "Firefox");
        PickerBrowserOption chromeOpt = new(chrome, null);
        PickerBrowserOption firefoxOpt = new(firefox, null);

        ProfileSpec pinnedSpec = new()
        {
            Id = "firefox-work-profile",
            BrowserId = "firefox",
            Name = "Work",
            Pinned = true,
        };

        IReadOnlyList<PickerBrowserOption> result = PinnedProfileFilter.Filter(
            [chromeOpt, firefoxOpt],
            [pinnedSpec]);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, o => o.Browser.Id == "chrome");
        Assert.Contains(result, o => o.Browser.Id == "firefox");
    }

    [Fact]
    public void MultiplePinnedSpecs_KeepAllMatches()
    {
        Browser firefox = MakeBrowser("firefox", "Firefox");
        Browser chrome = MakeBrowser("chrome", "Chrome");
        BrowserProfile firefoxWork = new(Name: "Work", DirectoryName: "work", IsDefault: false);
        BrowserProfile chromePersonal = new(Name: "Personal", DirectoryName: "personal", IsDefault: false);
        PickerBrowserOption firefoxWorkOpt = new(firefox, firefoxWork);
        PickerBrowserOption chromePersonalOpt = new(chrome, chromePersonal);

        ProfileSpec pinnedFirefox = new()
        {
            Id = "firefox-work",
            BrowserId = "firefox",
            Name = "Work",
            Pinned = true,
        };
        ProfileSpec pinnedChrome = new()
        {
            Id = "chrome-personal",
            BrowserId = "chrome",
            Name = "Personal",
            Pinned = true,
        };

        IReadOnlyList<PickerBrowserOption> result = PinnedProfileFilter.Filter(
            [firefoxWorkOpt, chromePersonalOpt],
            [pinnedFirefox, pinnedChrome]);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void PinnedMatching_NameCaseInsensitive()
    {
        Browser firefox = MakeBrowser("firefox", "Firefox");
        BrowserProfile workProfile = new(Name: "work", DirectoryName: "work", IsDefault: false);
        PickerBrowserOption workOpt = new(firefox, workProfile);

        ProfileSpec pinnedSpec = new()
        {
            Id = "firefox-work-profile",
            BrowserId = "FIREFOX",
            Name = "WORK",
            Pinned = true,
        };

        IReadOnlyList<PickerBrowserOption> result = PinnedProfileFilter.Filter(
            [workOpt],
            [pinnedSpec]);

        Assert.Single(result);
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