using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Launch;
using AskMeFirst.Core.Models;
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
        Assert.Equal(["https://example.com/", "--profile-directory=Default"], args);
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
    public void Firefox_WithProfile_UsesNameWithDashPFlag()
    {
        BrowserProfile profile = new("default-release", "Profiles/vc4ak1jq.default-release", IsDefault: true);
        string[] args = FirefoxLaunchStrategy.Instance.BuildArguments(SampleUrl, profile);
        Assert.Equal(["-P", "default-release", "https://example.com/"], args);
    }

    [Fact]
    public void Firefox_WithProfile_UsesNameNotDirectoryPath()
    {
        BrowserProfile profile = new("Work", "Profiles/0m6kw70o.Work", IsDefault: false);
        string[] args = FirefoxLaunchStrategy.Instance.BuildArguments(SampleUrl, profile);
        Assert.Contains("Work", args);
        Assert.DoesNotContain("Profiles/0m6kw70o.Work", args);
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