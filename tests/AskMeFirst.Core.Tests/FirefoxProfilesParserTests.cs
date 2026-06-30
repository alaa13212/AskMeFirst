using AskMeFirst.Core.Models;
using AskMeFirst.Core.Profiles;
using Xunit;

namespace AskMeFirst.Core.Tests;

public class FirefoxProfilesParserTests
{
    [Fact]
    public void Parse_MissingIni_ReturnsEmpty()
    {
        IReadOnlyList<BrowserProfile> result = FirefoxProfilesParser.Parse(@"C:\does\not\exist.ini");
        Assert.Empty(result);
    }

    [Fact]
    public void Parse_EmptyIni_ReturnsEmpty()
    {
        string iniPath = WriteIni("");

        try
        {
            IReadOnlyList<BrowserProfile> result = FirefoxProfilesParser.Parse(iniPath);
            Assert.Empty(result);
        }
        finally
        {
            File.Delete(iniPath);
        }
    }

    [Fact]
    public void Parse_SingleProfile_ReturnsProfile()
    {
        string iniPath = WriteIni("""
            [Profile0]
            Name=Default
            Path=C:\Users\Ali\AppData\Local\Mozilla\Firefox\Profiles\abc.default
            Default=1
            """);

        try
        {
            IReadOnlyList<BrowserProfile> result = FirefoxProfilesParser.Parse(iniPath);

            Assert.Single(result);
            Assert.Equal("Default", result[0].Name);
            Assert.Contains("abc.default", result[0].DirectoryName);
            Assert.True(result[0].IsDefault);
            Assert.Null(result[0].GroupId);
        }
        finally
        {
            File.Delete(iniPath);
        }
    }

    [Fact]
    public void Parse_ProfileWithoutStoreId_IsIncluded()
    {
        string iniPath = WriteIni("""
            [Profile0]
            Name=OldStyle
            Path=C:\Firefox\profiles\old
            Default=0
            """);

        try
        {
            IReadOnlyList<BrowserProfile> result = FirefoxProfilesParser.Parse(iniPath);

            Assert.Single(result);
            Assert.Equal("OldStyle", result[0].Name);
            Assert.Null(result[0].GroupId);
        }
        finally
        {
            File.Delete(iniPath);
        }
    }

    [Fact]
    public void Parse_StoreIdWithoutSqlite_UsesIniName()
    {
        string iniPath = WriteIni("""
            [Profile0]
            Name=Groups Profile
            Path=C:\Users\Ali\AppData\Local\Mozilla\Firefox\Profiles\abc123.default
            Default=1
            StoreID=abc123
            """);

        try
        {
            IReadOnlyList<BrowserProfile> result = FirefoxProfilesParser.Parse(iniPath, groupsRoot: null);

            Assert.Single(result);
            Assert.Equal("Groups Profile", result[0].Name);
            Assert.Equal("abc123", result[0].GroupId);
            Assert.Equal("Groups Profile", result[0].GroupName);
        }
        finally
        {
            File.Delete(iniPath);
        }
    }

    [Fact]
    public void Parse_DefaultProfile_SortsFirst()
    {
        string iniPath = WriteIni("""
            [Profile0]
            Name=Beta
            Path=C:\Firefox\beta
            Default=0

            [Profile1]
            Name=Default
            Path=C:\Firefox\default
            Default=1
            """);

        try
        {
            IReadOnlyList<BrowserProfile> result = FirefoxProfilesParser.Parse(iniPath);

            Assert.Equal(2, result.Count);
            Assert.True(result[0].IsDefault);
            Assert.Equal("Default", result[0].Name);
        }
        finally
        {
            File.Delete(iniPath);
        }
    }

    [Fact]
    public void Parse_RelativePath_IsStoredAsIs()
    {
        string iniPath = WriteIni("""
            [Profile0]
            Name=Relative
            Path=Profiles/abc.default
            """);

        try
        {
            IReadOnlyList<BrowserProfile> result = FirefoxProfilesParser.Parse(iniPath);

            Assert.Single(result);
            Assert.Equal("Profiles/abc.default", result[0].DirectoryName);
        }
        finally
        {
            File.Delete(iniPath);
        }
    }

    private static string WriteIni(string content)
    {
        string path = Path.Combine(Path.GetTempPath(), $"ff-profiles-{Guid.NewGuid():N}.ini");
        File.WriteAllText(path, content);
        return path;
    }
}
