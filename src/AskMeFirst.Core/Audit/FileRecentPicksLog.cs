using System.Text.Json;
using AskMeFirst.Core.Abstractions;

namespace AskMeFirst.Core.Audit;

public sealed class FileRecentPicksLog(string configPath, ILogger logger) : IRecentPicksLog
{
    private const string FileName = "recent-picks.jsonl";

    public void Append(RecentPickEntry entry)
    {
        string? dir = Path.GetDirectoryName(configPath);
        if (string.IsNullOrEmpty(dir))
        {
            return;
        }
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, FileName);

        string json = JsonSerializer.Serialize(entry, RecentPickJsonContext.Default.RecentPickEntry);
        try
        {
            File.AppendAllText(path, json + Environment.NewLine);
        }
        catch (Exception ex)
        {
            logger.LogWarn($"Failed to write recent-picks log: {ex.Message}");
        }
    }
}

[System.Text.Json.Serialization.JsonSerializable(typeof(RecentPickEntry))]
internal partial class RecentPickJsonContext : System.Text.Json.Serialization.JsonSerializerContext;