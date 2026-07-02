using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Launch;
using AskMeFirst.Core.Models;
using Xunit;

namespace AskMeFirst.Core.Tests;

public class NewWindowLaunchTests
{
    private static readonly Uri SampleUrl = new("https://example.com/");

    [Fact]
    public void Chromium_NewWindowTrue_PrependsFlagBeforeUrl()
    {
        string[] args = ChromiumLaunchStrategy.Instance.BuildArguments(SampleUrl, profile: null, newWindow: true);
        Assert.Equal(["--new-window", "https://example.com/"], args);
    }

    [Fact]
    public void Chromium_NewWindowTrue_WithProfile_KeepsProfileFlag()
    {
        BrowserProfile profile = new("Work", "Profile 7", IsDefault: false);
        string[] args = ChromiumLaunchStrategy.Instance.BuildArguments(SampleUrl, profile, newWindow: true);
        Assert.Equal(["--new-window", "https://example.com/", "--profile-directory=Profile 7"], args);
    }

    [Fact]
    public void Chromium_NewWindowDefaultFalse_OmitsFlag()
    {
        string[] args = ChromiumLaunchStrategy.Instance.BuildArguments(SampleUrl, profile: null);
        Assert.DoesNotContain("--new-window", args);
    }

    [Fact]
    public void Firefox_NewWindowTrue_PrependsFlagBeforeProfile()
    {
        BrowserProfile profile = new("Work", "Profiles/abc.Work", IsDefault: false);
        string[] args = FirefoxLaunchStrategy.Instance.BuildArguments(SampleUrl, profile, newWindow: true);
        Assert.Equal("-new-window", args[0]);
        Assert.Contains("-profile", args);
        Assert.Equal("https://example.com/", args[^1]);
    }

    [Fact]
    public void Firefox_NewWindowTrue_NoProfile_AppendsUrlAfterFlag()
    {
        string[] args = FirefoxLaunchStrategy.Instance.BuildArguments(SampleUrl, profile: null, newWindow: true);
        Assert.Equal(["-new-window", "https://example.com/"], args);
    }

    [Fact]
    public void Firefox_NewWindowDefaultFalse_OmitsFlag()
    {
        string[] args = FirefoxLaunchStrategy.Instance.BuildArguments(SampleUrl, profile: null);
        Assert.DoesNotContain("-new-window", args);
    }

    [Fact]
    public void Default_NewWindowTrue_IgnoresFlag()
    {
        string[] args = DefaultLaunchStrategy.Instance.BuildArguments(SampleUrl, profile: null, newWindow: true);
        Assert.Equal(["https://example.com/"], args);
    }
}