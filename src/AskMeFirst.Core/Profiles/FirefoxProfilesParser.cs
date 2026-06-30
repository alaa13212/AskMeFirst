using System.Text.RegularExpressions;
using AskMeFirst.Core.Models;
using AskMeFirst.Core.Paths;

namespace AskMeFirst.Core.Profiles;

public static partial class FirefoxProfilesParser
{
    public static IReadOnlyList<BrowserProfile> Parse(string iniPath)
    {
        return Parse(iniPath, groupsRoot: null);
    }

    public static IReadOnlyList<BrowserProfile> Parse(string iniPath, string? groupsRoot)
    {
        IReadOnlyList<IniRow> iniRows = ReadIni(iniPath);
        if (iniRows.Count == 0)
        {
            return [];
        }

        HashSet<string> seenTails = new(StringComparer.OrdinalIgnoreCase);
        List<BrowserProfile> result = [];

        foreach (IniRow row in iniRows)
        {
            foreach (BrowserProfile profile in Expand(row, groupsRoot))
            {
                if (seenTails.Add(PathTail.Segment(profile.DirectoryName)))
                {
                    result.Add(profile);
                }
            }
        }

        return Sort(result);
    }

    private static IEnumerable<BrowserProfile> Expand(IniRow row, string? groupsRoot)
    {
        bool hasStore = !string.IsNullOrEmpty(row.StoreId);

        if (row.Path is null)
        {
            yield break;
        }

        if (!hasStore)
        {
            yield return new BrowserProfile(
                Name: row.Name ?? row.Path,
                DirectoryName: row.Path,
                IsDefault: row.IsDefault,
                GroupId: null,
                GroupName: null);
            yield break;
        }

        IReadOnlyList<FirefoxProfileStoreEntry> storeEntries = ReadStoreEntries(row.StoreId!, groupsRoot);
        if (storeEntries.Count == 0)
        {
            yield return new BrowserProfile(
                Name: row.Name ?? row.Path,
                DirectoryName: row.Path,
                IsDefault: row.IsDefault,
                GroupId: row.StoreId,
                GroupName: row.Name);
            yield break;
        }

        foreach (FirefoxProfileStoreEntry entry in storeEntries)
        {
            yield return new BrowserProfile(
                Name: ResolveEntryName(entry, row),
                DirectoryName: entry.Path,
                IsDefault: IsPathMatch(entry.Path, row.Path) && row.IsDefault,
                GroupId: row.StoreId,
                GroupName: row.Name);
        }
    }

    private static IReadOnlyList<FirefoxProfileStoreEntry> ReadStoreEntries(string storeId, string? groupsRoot)
    {
        string sqlitePath = Path.Combine(groupsRoot ?? "", $"{storeId}.sqlite");
        return File.Exists(sqlitePath)
            ? FirefoxProfileStoreScanner.Read(sqlitePath)
            : [];
    }

    private static List<BrowserProfile> Sort(IEnumerable<BrowserProfile> profiles) =>
        profiles
            .OrderBy(p => p.IsDefault ? 0 : 1)
            .ThenBy(p => p.GroupName ?? p.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static IReadOnlyList<IniRow> ReadIni(string iniPath)
    {
        if (!File.Exists(iniPath))
        {
            return [];
        }

        Dictionary<int, IniRow> rows = [];
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
                    rows[currentSection] = new IniRow(null, null, false, null);
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
            IniRow row = rows[currentSection];

            rows[currentSection] = key switch
            {
                "Name" => row with { Name = value },
                "Path" => row with { Path = value },
                "Default" => row with { IsDefault = value == "1" },
                "StoreID" => row with { StoreId = value },
                _ => row,
            };
        }

        return [.. rows.Values];
    }

    private static bool IsPathMatch(string a, string b)
    {
        return string.Equals(PathTail.Segment(a), PathTail.Segment(b), StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveEntryName(FirefoxProfileStoreEntry entry, IniRow row)
    {
        bool isFirefoxDefaultName = !string.IsNullOrWhiteSpace(entry.Name)
            && string.Equals(entry.Name.Trim(), "Original profile", StringComparison.OrdinalIgnoreCase);

        if (!isFirefoxDefaultName && !string.IsNullOrWhiteSpace(entry.Name))
        {
            return entry.Name!;
        }

        return row.Name ?? PathTail.Segment(entry.Path);
    }

    private sealed record IniRow(string? Name, string? Path, bool IsDefault, string? StoreId);

    [GeneratedRegex(@"^Profile(\d+)$")]
    private static partial Regex SectionRegex();
}
