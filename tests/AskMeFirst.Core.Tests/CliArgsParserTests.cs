using AskMeFirst;
using AskMeFirst.Core.Models;
using Xunit;

namespace AskMeFirst.Core.Tests;

public class CliArgsParserTests
{
    [Theory]
    [InlineData("https://example.com", "https://example.com/", null, false)]
    [InlineData("http://example.com/x", "http://example.com/x", null, false)]
    [InlineData("https://example.com", "https://example.com/", "chrome", false)]
    [InlineData("https://example.com", "https://example.com/", null, true)]
    public void Parse_ValidArgs(string input, string expectedUrl, string? expectedBrowser, bool expectedVerbose)
    {
        List<string> args = [input];
        if (expectedBrowser is not null)
        {
            args.Add("--browser");
            args.Add(expectedBrowser);
        }
        if (expectedVerbose)
        {
            args.Add("--verbose");
        }

        CliArgs parsed = CliArgsParser.Parse(args.ToArray());

        Assert.Equal(new Uri(expectedUrl), parsed.Url);
        Assert.Equal(expectedBrowser, parsed.BrowserId);
        Assert.Equal(expectedVerbose, parsed.Verbose);
    }

    [Fact]
    public void Parse_BrowserShortFlag_Works()
    {
        CliArgs parsed = CliArgsParser.Parse(["https://example.com", "-b", "firefox"]);
        Assert.Equal("firefox", parsed.BrowserId);
    }

    [Fact]
    public void Parse_NoUrl_Throws()
    {
        Assert.Throws<CliArgsException>(() => CliArgsParser.Parse([]));
    }

    [Fact]
    public void Parse_BrowserFlagWithoutValue_Throws()
    {
        Assert.Throws<CliArgsException>(() => CliArgsParser.Parse(["https://example.com", "--browser"]));
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("ftp://example.com")]
    [InlineData("javascript:alert(1)")]
    public void Parse_NonHttpUrl_Throws(string url)
    {
        Assert.Throws<CliArgsException>(() => CliArgsParser.Parse([url]));
    }

    [Fact]
    public void Parse_UnknownFlag_Throws()
    {
        Assert.Throws<CliArgsException>(() => CliArgsParser.Parse(["--nope", "https://example.com"]));
    }
}
