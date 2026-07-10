using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Inventory;
using AskMeFirst.Core.Models;
using Xunit;

namespace AskMeFirst.Core.Tests;

public class CachingBrowserInventoryTests
{
    [Fact]
    public void Discover_CacheMiss_CallsInnerAndWrites()
    {
        FakeInventory inner = new();
        inner.Browsers.Add(TestBrowser.Make("chrome", "Chrome", "/usr/bin/google-chrome"));
        RecordingCache cache = new();
        CachingBrowserInventory sut = new(inner, cache);

        IReadOnlyList<Browser> result = sut.Discover();

        Assert.Equal(1, inner.DiscoverCount);
        Assert.Equal(1, cache.WriteCount);
        Assert.Single(result);
        Assert.Equal("chrome", result[0].Id);
    }

    [Fact]
    public void Discover_CacheHit_DoesNotCallInner()
    {
        FakeInventory inner = new();
        RecordingCache cache = new();
        cache.Snapshot =
        [
            TestBrowser.Make("firefox", "Firefox", "/usr/bin/firefox"),
        ];
        CachingBrowserInventory sut = new(inner, cache);

        IReadOnlyList<Browser> result = sut.Discover();

        Assert.Equal(0, inner.DiscoverCount);
        Assert.Equal(0, cache.WriteCount);
        Assert.Single(result);
        Assert.Equal("firefox", result[0].Id);
    }

    [Fact]
    public void Discover_CachedAcrossCalls()
    {
        FakeInventory inner = new();
        inner.Browsers.Add(TestBrowser.Make("chrome", "Chrome", "/usr/bin/google-chrome"));
        RecordingCache cache = new();
        CachingBrowserInventory sut = new(inner, cache);

        sut.Discover();
        sut.Discover();
        sut.Discover();

        Assert.Equal(1, inner.DiscoverCount);
        Assert.Equal(1, cache.WriteCount);
    }

    [Fact]
    public void FindById_CaseInsensitive_AfterCacheWarmed()
    {
        FakeInventory inner = new();
        inner.Browsers.Add(TestBrowser.Make("chrome", "Chrome", "/usr/bin/google-chrome"));
        RecordingCache cache = new();
        CachingBrowserInventory sut = new(inner, cache);

        Browser? result = sut.FindById("CHROME");

        Assert.NotNull(result);
        Assert.Equal("chrome", result.Id);
    }

    private sealed class RecordingCache : IDiscoveryCache
    {
        public IReadOnlyList<Browser>? Snapshot { get; set; }

        public int WriteCount { get; private set; }

        public DateTimeOffset? LastGenerated => Snapshot is null ? null : DateTimeOffset.UtcNow;

        public IReadOnlyList<Browser>? TryRead() => Snapshot;

        public void Write(IReadOnlyList<Browser> browsers)
        {
            Snapshot = browsers;
            WriteCount++;
        }
    }
}
