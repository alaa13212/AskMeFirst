using AskMeFirst.Core.Config;
using AskMeFirst.Core.Routing;
using Xunit;

namespace AskMeFirst.Core.Tests;

public class ConfigShortenerDomainListTests
{
    [Fact]
    public void EmptyConfig_StillKnowsBuiltInShorteners()
    {
        ConfigShortenerDomainList sut = new(new AppConfig());

        Assert.True(sut.IsKnown("t.co"));
        Assert.True(sut.IsKnown("bit.ly"));
        Assert.True(sut.IsKnown("tinyurl.com"));
        Assert.False(sut.IsKnown("example.com"));
    }

    [Fact]
    public void HostMatchingIsCaseInsensitive()
    {
        ConfigShortenerDomainList sut = new(new AppConfig());

        Assert.True(sut.IsKnown("T.CO"));
        Assert.True(sut.IsKnown("Bit.Ly"));
    }

    [Fact]
    public void ExtraDomainsExtendBuiltIn()
    {
        AppConfig config = new() { UnshortenDomainsExtra = ["company.short"] };
        ConfigShortenerDomainList sut = new(config);

        Assert.True(sut.IsKnown("t.co"));
        Assert.True(sut.IsKnown("company.short"));
    }

    [Fact]
    public void OverrideReplacesBuiltIn()
    {
        AppConfig config = new()
        {
            UnshortenDomainsExtra = ["only-this.example"],
            UnshortenDomainsOverride = true,
        };
        ConfigShortenerDomainList sut = new(config);

        Assert.False(sut.IsKnown("t.co"));
        Assert.True(sut.IsKnown("only-this.example"));
    }

    [Fact]
    public void OverrideWithEmptyExtraList_RejectsAllShorteners()
    {
        AppConfig config = new() { UnshortenDomainsOverride = true };
        ConfigShortenerDomainList sut = new(config);

        Assert.False(sut.IsKnown("t.co"));
        Assert.False(sut.IsKnown("bit.ly"));
    }

    [Fact]
    public void EmptyHost_ReturnsFalse()
    {
        ConfigShortenerDomainList sut = new(new AppConfig());

        Assert.False(sut.IsKnown(""));
    }
}