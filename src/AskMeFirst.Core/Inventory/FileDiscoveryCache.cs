using System.Text.Json;
using System.Text.Json.Serialization;
using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Launch;
using AskMeFirst.Core.Models;

namespace AskMeFirst.Core.Inventory;

public sealed class FileDiscoveryCache(
    string cacheFilePath,
    ILogger logger) : IDiscoveryCache
{
    public IReadOnlyList<Browser>? TryRead()
    {
        if (!File.Exists(cacheFilePath))
        {
            return null;
        }

        try
        {
            using FileStream stream = File.OpenRead(cacheFilePath);
            CachedInventory? cached = JsonSerializer.Deserialize(
                stream, DiscoveryCacheJsonContext.Default.CachedInventory);
            if (cached is null)
            {
                return null;
            }
            return cached.Browsers.Select(Materialize).ToList();
        }
        catch (Exception ex)
        {
            logger.LogWarn($"discovery-cache: read failed ({ex.Message}) — ignoring.");
            return null;
        }
    }

    public void Write(IReadOnlyList<Browser> browsers)
    {
        try
        {
            string? dir = Path.GetDirectoryName(cacheFilePath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            CachedInventory payload = new(DateTimeOffset.UtcNow, browsers.Select(Project).ToList());

            using FileStream stream = File.Create(cacheFilePath);
            JsonSerializer.Serialize(
                stream, payload, DiscoveryCacheJsonContext.Default.CachedInventory);
        }
        catch (Exception ex)
        {
            logger.LogWarn($"discovery-cache: write failed ({ex.Message}) — continuing without cache.");
        }
    }

    private static CachedBrowserDto Project(Browser browser) => new(
        Id: browser.Id,
        DisplayName: browser.DisplayName,
        ExecutablePath: CanonicalizePath(browser.ExecutablePath),
        IconName: browser.IconName,
        FlatpakAppId: browser.FlatpakAppId);

    private static Browser Materialize(CachedBrowserDto dto)
    {
        IBrowserLaunchStrategy strategy = BrowserLaunchStrategies.For(dto.Id);
        if (dto.FlatpakAppId is not null)
        {
            strategy = new FlatpakLaunchStrategy(dto.FlatpakAppId, strategy);
        }
        return new Browser
        {
            Id = dto.Id,
            DisplayName = dto.DisplayName,
            ExecutablePath = dto.ExecutablePath,
            IconName = dto.IconName,
            LaunchStrategy = strategy,
            FlatpakAppId = dto.FlatpakAppId,
        };
    }

    private static string CanonicalizePath(string path)
    {
        try
        {
            return Path.GetFullPath(path);
        }
        catch
        {
            return path;
        }
    }
}

[JsonSerializable(typeof(CachedInventory))]
[JsonSourceGenerationOptions(WriteIndented = false)]
internal partial class DiscoveryCacheJsonContext : JsonSerializerContext;
