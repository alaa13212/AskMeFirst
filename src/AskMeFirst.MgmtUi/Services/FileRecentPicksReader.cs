using System.Text.Json;
using AskMeFirst.Core.Audit;

namespace AskMeFirst.MgmtUi.Services;

public sealed class FileRecentPicksReader : IRecentPicksReader
{
    private const string FileName = "recent-picks.jsonl";

    private readonly string _configPath;
    private readonly object _gate = new();

    public FileRecentPicksReader(string configPath)
    {
        _configPath = configPath;
    }

    public IReadOnlyList<RecentPickEntry> ReadAll()
    {
        lock (_gate)
        {
            string? dir = Path.GetDirectoryName(_configPath);
            if (string.IsNullOrEmpty(dir))
            {
                return [];
            }
            string path = Path.Combine(dir, FileName);
            if (!File.Exists(path))
            {
                return [];
            }

            List<RecentPickEntry> entries = [];
            try
            {
                foreach (string line in File.ReadAllLines(path))
                {
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }
                    RecentPickEntry? entry = JsonSerializer.Deserialize(
                        line,
                        RecentPicksJsonContext.Default.RecentPickEntry);
                    if (entry is not null)
                    {
                        entries.Add(entry);
                    }
                }
            }
            catch (JsonException)
            {
            }
            return entries;
        }
    }
}

[System.Text.Json.Serialization.JsonSerializable(typeof(RecentPickEntry))]
internal sealed partial class RecentPicksJsonContext : System.Text.Json.Serialization.JsonSerializerContext;
