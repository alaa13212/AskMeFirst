using AskMeFirst.Commands;
using Xunit;

namespace AskMeFirst.Core.Tests;

public class RouteCommandTests
{
    [Theory]
    [InlineData("https://example.com", "https://example.com/", null, null, false)]
    [InlineData("http://example.com/x", "http://example.com/x", null, null, false)]
    [InlineData("https://example.com", "https://example.com/", "chrome", null, false)]
    [InlineData("https://example.com", "https://example.com/", null, null, true)]
    [InlineData("https://example.com", "https://example.com/", "chrome", "Work", false)]
    public void ParseArgs_ValidInput(
        string input,
        string expectedUrl,
        string? expectedBrowser,
        string? expectedProfile,
        bool expectedVerbose)
    {
        List<string> args = [input];
        if (expectedBrowser is not null)
        {
            args.Add("--browser");
            args.Add(expectedBrowser);
        }
        if (expectedProfile is not null)
        {
            args.Add("--profile");
            args.Add(expectedProfile);
        }
        if (expectedVerbose)
        {
            args.Add("--verbose");
        }

        RouteArgs parsed = RouteCommand.ParseArgs(args.ToArray());

        Assert.Equal(new Uri(expectedUrl), parsed.Url);
        Assert.Equal(expectedBrowser, parsed.BrowserId);
        Assert.Equal(expectedProfile, parsed.ProfileName);
        Assert.Equal(expectedVerbose, parsed.Verbose);
    }

    [Fact]
    public void ParseArgs_BrowserShortFlag_Works()
    {
        RouteArgs parsed = RouteCommand.ParseArgs(["https://example.com", "-b", "firefox"]);
        Assert.Equal("firefox", parsed.BrowserId);
    }

    [Fact]
    public void ParseArgs_ProfileShortFlag_Works()
    {
        RouteArgs parsed = RouteCommand.ParseArgs(["https://example.com", "-p", "Work"]);
        Assert.Equal("Work", parsed.ProfileName);
    }

    [Fact]
    public void ParseArgs_NoUrl_Throws()
    {
        Assert.Throws<CliArgsException>(() => RouteCommand.ParseArgs([]));
    }

    [Fact]
    public void ParseArgs_BrowserFlagWithoutValue_Throws()
    {
        Assert.Throws<CliArgsException>(() =>
            RouteCommand.ParseArgs(["https://example.com", "--browser"]));
    }

    [Fact]
    public void ParseArgs_ProfileFlagWithoutValue_Throws()
    {
        Assert.Throws<CliArgsException>(() =>
            RouteCommand.ParseArgs(["https://example.com", "--profile"]));
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("ftp://example.com")]
    [InlineData("javascript:alert(1)")]
    public void ParseArgs_NonHttpUrl_Throws(string url)
    {
        Assert.Throws<CliArgsException>(() => RouteCommand.ParseArgs([url]));
    }

    [Fact]
    public void ParseArgs_UnknownFlag_Throws()
    {
        Assert.Throws<CliArgsException>(() =>
            RouteCommand.ParseArgs(["--nope", "https://example.com"]));
    }
}