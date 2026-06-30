using System.Text.Json;

namespace AskMeFirst.Core.Profiles;

public static class ChromiumProfileNames
{
    public static IReadOnlyDictionary<string, string> Read(string profileRootDir)
    {
        if (!Directory.Exists(profileRootDir))
        {
            return new Dictionary<string, string>();
        }

        string localStatePath = Path.Combine(profileRootDir, "Local State");
        if (!File.Exists(localStatePath))
        {
            return new Dictionary<string, string>();
        }

        string raw;
        try
        {
            raw = File.ReadAllText(localStatePath);
        }
        catch
        {
            return new Dictionary<string, string>();
        }

        if (string.IsNullOrWhiteSpace(raw))
        {
            return new Dictionary<string, string>();
        }

        try
        {
            using JsonDocument doc = JsonDocument.Parse(raw);
            JsonElement? profileRoot = doc.RootElement.TryGetProperty("profile", out JsonElement p) ? p : null;
            JsonElement? infoCache = profileRoot?.TryGetProperty("info_cache", out JsonElement ic) == true ? ic : null;
            if (infoCache is null || infoCache.Value.ValueKind != JsonValueKind.Object)
            {
                return new Dictionary<string, string>();
            }

            Dictionary<string, string> result = new();
            foreach (JsonProperty prop in infoCache.Value.EnumerateObject())
            {
                if (prop.Value.TryGetProperty("name", out JsonElement nameEl)
                    && nameEl.ValueKind == JsonValueKind.String
                    && !string.IsNullOrWhiteSpace(nameEl.GetString()))
                {
                    result[prop.Name] = nameEl.GetString()!;
                }
            }
            return result;
        }
        catch (JsonException)
        {
            return new Dictionary<string, string>();
        }
    }
}
