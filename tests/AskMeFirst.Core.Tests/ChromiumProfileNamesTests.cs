using AskMeFirst.Core.Profiles;
using Xunit;

namespace AskMeFirst.Core.Tests;

public class ChromiumProfileNamesTests
{
    private static string WriteLocalState(string dir, string json)
    {
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "Local State"), json);
        return dir;
    }

    private static void Cleanup(string dir)
    {
        try { Directory.Delete(dir, recursive: true); } catch { }
    }

    [Fact]
    public void Read_MissingDirectory_ReturnsEmpty()
    {
        IReadOnlyDictionary<string, string> result = ChromiumProfileNames.Read(@"C:\does\not\exist");
        Assert.Empty(result);
    }

    [Fact]
    public void Read_MissingLocalState_ReturnsEmpty()
    {
        string dir = Path.Combine(Path.GetTempPath(), $"chrom-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        try
        {
            IReadOnlyDictionary<string, string> result = ChromiumProfileNames.Read(dir);
            Assert.Empty(result);
        }
        finally
        {
            Cleanup(dir);
        }
    }

    [Fact]
    public void Read_ValidLocalState_ReturnsProfileNames()
    {
        string dir = Path.Combine(Path.GetTempPath(), $"chrom-{Guid.NewGuid():N}");
        WriteLocalState(dir, """
            {
              "profile": {
                "info_cache": {
                  "Default": { "name": "Person 1" },
                  "Profile 6": { "name": "Work" },
                  "Profile 7": { "name": "Personal" }
                }
              }
            }
            """);

        try
        {
            IReadOnlyDictionary<string, string> result = ChromiumProfileNames.Read(dir);

            Assert.Equal(3, result.Count);
            Assert.Equal("Person 1", result["Default"]);
            Assert.Equal("Work", result["Profile 6"]);
            Assert.Equal("Personal", result["Profile 7"]);
        }
        finally
        {
            Cleanup(dir);
        }
    }

    [Fact]
    public void Read_NonStringName_IsSkipped()
    {
        string dir = Path.Combine(Path.GetTempPath(), $"chrom-{Guid.NewGuid():N}");
        WriteLocalState(dir, """
            {
              "profile": {
                "info_cache": {
                  "Default": { "name": "Person 1" },
                  "Profile 6": { "name": 42 }
                }
              }
            }
            """);

        try
        {
            IReadOnlyDictionary<string, string> result = ChromiumProfileNames.Read(dir);

            Assert.Single(result);
            Assert.Equal("Person 1", result["Default"]);
        }
        finally
        {
            Cleanup(dir);
        }
    }

    [Fact]
    public void Read_MalformedJson_ReturnsEmpty()
    {
        string dir = Path.Combine(Path.GetTempPath(), $"chrom-{Guid.NewGuid():N}");
        WriteLocalState(dir, "{not valid json");

        try
        {
            IReadOnlyDictionary<string, string> result = ChromiumProfileNames.Read(dir);
            Assert.Empty(result);
        }
        finally
        {
            Cleanup(dir);
        }
    }

    [Fact]
    public void Read_NoProfileSection_ReturnsEmpty()
    {
        string dir = Path.Combine(Path.GetTempPath(), $"chrom-{Guid.NewGuid():N}");
        WriteLocalState(dir, """{ "other": { "thing": 1 } }""");

        try
        {
            IReadOnlyDictionary<string, string> result = ChromiumProfileNames.Read(dir);
            Assert.Empty(result);
        }
        finally
        {
            Cleanup(dir);
        }
    }
}
