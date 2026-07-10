using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Inventory;
using AskMeFirst.Core.Launch;
using AskMeFirst.Core.Models;
using Xunit;

namespace AskMeFirst.Core.Tests;

public class FileDiscoveryCacheTests : IDisposable
{
    private readonly string tempDir;

    public FileDiscoveryCacheTests()
    {
        tempDir = Path.Combine(Path.GetTempPath(), "askmefirst-cache-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        try
        {
            Directory.Delete(tempDir, recursive: true);
        }
        catch
        {
        }
    }

    [Fact]
    public void TryRead_AbsentFile_ReturnsNull()
    {
        FileDiscoveryCache sut = new(Path.Combine(tempDir, "nope.json"), new FakeLogger());

        IReadOnlyList<Browser>? result = sut.TryRead();

        Assert.Null(result);
    }

    [Fact]
    public void Write_ThenTryRead_RoundtripsBrowsers()
    {
        string path = Path.Combine(tempDir, "cache.json");
        FileDiscoveryCache sut = new(path, new FakeLogger());

        IReadOnlyList<Browser> input =
        [
            TestBrowser.Make("chrome", "Google Chrome", "/usr/bin/google-chrome"),
            TestBrowser.Make("firefox", "Firefox", "/usr/bin/firefox"),
        ];
        sut.Write(input);

        IReadOnlyList<Browser>? result = sut.TryRead();

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("chrome", result[0].Id);
        Assert.Equal("Google Chrome", result[0].DisplayName);
        Assert.Equal(Path.GetFullPath("/usr/bin/google-chrome"), result[0].ExecutablePath);
        Assert.NotNull(result[0].LaunchStrategy);
        Assert.Equal("firefox", result[1].Id);
    }

    [Fact]
    public void TryRead_CorruptJson_ReturnsNull()
    {
        string path = Path.Combine(tempDir, "cache.json");
        File.WriteAllText(path, "{ this is not valid json");
        FileDiscoveryCache sut = new(path, new FakeLogger());

        IReadOnlyList<Browser>? result = sut.TryRead();

        Assert.Null(result);
    }

    [Fact]
    public void Write_EmptyList_RoundtripsEmpty()
    {
        string path = Path.Combine(tempDir, "cache.json");
        FileDiscoveryCache sut = new(path, new FakeLogger());

        sut.Write([]);

        IReadOnlyList<Browser>? result = sut.TryRead();

        Assert.NotNull(result);
        Assert.Empty(result);
    }
}
