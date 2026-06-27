using System.Text.RegularExpressions;
using AskMeFirst.Core.Models;

namespace AskMeFirst.Core.Profiles;

public static partial class FirefoxProfilesParser
{
    public static IReadOnlyList<BrowserProfile> Parse(string iniPath)
    {
        if (!File.Exists(iniPath))
        {
            return [];
        }

        Dictionary<int, RawRow> rows = [];
        int currentSection = -1;

        foreach (string rawLine in File.ReadAllLines(iniPath))
        {
            string line = rawLine.Trim();
            if (line.StartsWith('[') && line.EndsWith(']'))
            {
                string section = line[1..^1];
                Match m = SectionRegex().Match(section);
                if (m.Success && int.TryParse(m.Groups[1].Value, out int n))
                {
                    currentSection = n;
                    rows[currentSection] = new RawRow(null, null, false);
                }
                else
                {
                    currentSection = -1;
                }
                continue;
            }

            if (currentSection < 0)
            {
                continue;
            }

            int eq = line.IndexOf('=');
            if (eq <= 0)
            {
                continue;
            }

            string key = line[..eq].Trim();
            string value = line[(eq + 1)..].Trim();
            RawRow row = rows[currentSection];

            rows[currentSection] = key switch
            {
                "Name" => row with { Name = value },
                "Path" => row with { Path = value },
                "Default" => row with { IsDefault = value == "1" },
                _ => row,
            };
        }

        return rows.Values
            .Where(r => r.Path is not null)
            .Select(r => new BrowserProfile(
                Name: r.Name ?? r.Path!,
                DirectoryName: r.Path!,
                IsDefault: r.IsDefault))
            .OrderBy(p => p.IsDefault ? 0 : 1)
            .ThenBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private sealed record RawRow(string? Name, string? Path, bool IsDefault);

    [GeneratedRegex(@"^Profile(\d+)$")]
    private static partial Regex SectionRegex();
}