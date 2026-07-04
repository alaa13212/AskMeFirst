using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Launch;
using AskMeFirst.Core.Models;
using AskMeFirst.Core.Profiles;
using Xunit;

namespace AskMeFirst.Core.Tests;

public class LaunchStrategyTests
{
    private static readonly Uri SampleUrl = new("https://example.com/");

    [Fact]
    public void Chromium_NoProfile_ReturnsUrlOnly()
    {
        string[] args = ChromiumLaunchStrategy.Instance.BuildArguments(SampleUrl, profile: null);
        Assert.Equal(["https://example.com/"], args);
    }

    [Fact]
    public void Chromium_WithProfile_UsesDirectoryNameWithFlag()
    {
        BrowserProfile profile = new("Default", "Default", IsDefault: true);
        string[] args = ChromiumLaunchStrategy.Instance.BuildArguments(SampleUrl, profile);
        Assert.Equal(["--profile-directory=Default", "https://example.com/"], args);
    }

    [Fact]
    public void Chromium_FlagsBeforeUrl()
    {
        BrowserProfile profile = new("Default", "Default", IsDefault: true);
        string[] args = ChromiumLaunchStrategy.Instance.BuildArguments(SampleUrl, profile, newWindow: true);
        Assert.Equal(["--new-window", "--profile-directory=Default", "https://example.com/"], args);
    }

    [Fact]
    public void Flatpak_Chromium_WrapsArgsInRun()
    {
        BrowserProfile profile = new("Default", "Default", IsDefault: true);
        FlatpakLaunchStrategy strategy = new(
            "com.opera.opera-gx",
            ChromiumLaunchStrategy.Instance);
        string[] args = strategy.BuildArguments(SampleUrl, profile);
        Assert.Equal(["run", "com.opera.opera-gx", "--profile-directory=Default", "https://example.com/"], args);
    }

    [Fact]
    public void Flatpak_Firefox_WrapsArgsInRun()
    {
        string profilesRoot = FirefoxProfilesRoot.Get();
        BrowserProfile profile = new("default", "abc.default", IsDefault: true);
        FlatpakLaunchStrategy strategy = new(
            "org.mozilla.firefox",
            FirefoxLaunchStrategy.Instance);
        string[] args = strategy.BuildArguments(SampleUrl, profile);
        Assert.Equal(["run", "org.mozilla.firefox", "-profile", Path.Combine(profilesRoot, "abc.default"), "https://example.com/"], args);
    }

    [Fact]
    public void Chromium_WithProfile_UsesDirectoryNameNotName()
    {
        BrowserProfile profile = new("Work", "Profile 7", IsDefault: false);
        string[] args = ChromiumLaunchStrategy.Instance.BuildArguments(SampleUrl, profile);
        Assert.Contains("--profile-directory=Profile 7", args);
        Assert.DoesNotContain("Profile 7", args.Where(a => !a.StartsWith("--", StringComparison.Ordinal)));
    }

    [Fact]
    public void Firefox_NoProfile_ReturnsUrlOnly()
    {
        string[] args = FirefoxLaunchStrategy.Instance.BuildArguments(SampleUrl, profile: null);
        Assert.Equal(["https://example.com/"], args);
    }

    [Fact]
    public void Firefox_WithProfile_UsesAbsoluteProfilePath()
    {
        BrowserProfile profile = new("default-release", "Profiles/vc4ak1jq.default-release", IsDefault: true);
        string[] args = FirefoxLaunchStrategy.Instance.BuildArguments(SampleUrl, profile);
        string expectedPath = Path.Combine(FirefoxProfilesRoot.Get(), "vc4ak1jq.default-release");
        Assert.Equal(["-profile", expectedPath, "https://example.com/"], args);
    }

    [Fact]
    public void Firefox_WithProfile_HandlesBareTail()
    {
        BrowserProfile profile = new("legacy", "abc.default", IsDefault: false);
        string[] args = FirefoxLaunchStrategy.Instance.BuildArguments(SampleUrl, profile);
        string expectedPath = Path.Combine(FirefoxProfilesRoot.Get(), "abc.default");
        Assert.Equal(["-profile", expectedPath, "https://example.com/"], args);
    }

    [Fact]
    public void Firefox_WithProfile_PreservesAbsolutePath()
    {
        string absolute = Path.Combine(FirefoxProfilesRoot.Get(), "abc.Work");
        BrowserProfile profile = new("Work", absolute, IsDefault: false);
        string[] args = FirefoxLaunchStrategy.Instance.BuildArguments(SampleUrl, profile);
        Assert.Equal(["-profile", absolute, "https://example.com/"], args);
    }

    [Fact]
    public void Firefox_GroupChild_UsesPathSoFirefoxCanResolve()
    {
        BrowserProfile groupChild = new(
            Name: "Work",
            DirectoryName: "Profiles/0m6kw70o.Work",
            IsDefault: false);
        string[] args = FirefoxLaunchStrategy.Instance.BuildArguments(SampleUrl, groupChild);
        Assert.DoesNotContain("-P", args);
        Assert.Contains("-profile", args);
        string expectedPath = Path.Combine(FirefoxProfilesRoot.Get(), "0m6kw70o.Work");
        Assert.Contains(expectedPath, args);
    }

    [Fact]
    public void Default_NoProfile_ReturnsUrlOnly()
    {
        string[] args = DefaultLaunchStrategy.Instance.BuildArguments(SampleUrl, profile: null);
        Assert.Equal(["https://example.com/"], args);
    }

    [Fact]
    public void Default_WithProfile_ReturnsUrlOnly()
    {
        BrowserProfile profile = new("Default", "Default", IsDefault: true);
        string[] args = DefaultLaunchStrategy.Instance.BuildArguments(SampleUrl, profile);
        Assert.Equal(["https://example.com/"], args);
    }

    [Theory]
    [InlineData("chrome", typeof(ChromiumLaunchStrategy))]
    [InlineData("chromium", typeof(ChromiumLaunchStrategy))]
    [InlineData("edge", typeof(ChromiumLaunchStrategy))]
    [InlineData("brave", typeof(ChromiumLaunchStrategy))]
    [InlineData("opera", typeof(ChromiumLaunchStrategy))]
    [InlineData("opera-gx", typeof(ChromiumLaunchStrategy))]
    [InlineData("vivaldi", typeof(ChromiumLaunchStrategy))]
    [InlineData("arc", typeof(ChromiumLaunchStrategy))]
    [InlineData("firefox", typeof(FirefoxLaunchStrategy))]
    [InlineData("lynx", typeof(DefaultLaunchStrategy))]
    [InlineData("wget", typeof(DefaultLaunchStrategy))]
    public void Factory_PicksCorrectStrategy(string browserId, Type expectedType)
    {
        object strategy = BrowserLaunchStrategies.For(browserId);
        Assert.IsType(expectedType, strategy);
    }

    [Theory]
    [InlineData("chrome")]
    [InlineData("FIREFOX")]
    [InlineData("Edge")]
    public void Factory_IsCaseInsensitive(string browserId)
    {
        IBrowserLaunchStrategy a = BrowserLaunchStrategies.For(browserId);
        IBrowserLaunchStrategy b = BrowserLaunchStrategies.For(browserId.ToLowerInvariant());
        Assert.Equal(a.GetType(), b.GetType());
    }
}