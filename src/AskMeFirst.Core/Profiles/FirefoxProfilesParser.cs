using System.Text.RegularExpressions;
using AskMeFirst.Core.Models;

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

        Dictionary<string, IniRow> groupNameByStore = [];
        foreach (IniRow row in iniRows)
        {
            if (!string.IsNullOrEmpty(row.StoreId) && !groupNameByStore.ContainsKey(row.StoreId!))
            {
                groupNameByStore[row.StoreId!] = row;
            }
        }

        List<BrowserProfile> result = [];
        HashSet<string> emittedTail = new(StringComparer.OrdinalIgnoreCase);

        foreach (IniRow row in iniRows)
        {
            if (row.Path is null || string.IsNullOrEmpty(row.StoreId))
            {
                if (row.Path is not null)
                {
                    string tail = ExtractTailSegment(row.Path);
                    if (emittedTail.Add(tail))
                    {
                        result.Add(new BrowserProfile(
                            Name: row.Name ?? row.Path,
                            DirectoryName: row.Path,
                            IsDefault: row.IsDefault,
                            GroupId: null,
                            GroupName: null));
                    }
                }
                continue;
            }

            string sqlitePath = Path.Combine(groupsRoot ?? "", $"{row.StoreId}.sqlite");
            if (!File.Exists(sqlitePath))
            {
                string tail = ExtractTailSegment(row.Path);
                if (emittedTail.Add(tail))
                {
                    result.Add(new BrowserProfile(
                        Name: row.Name ?? row.Path,
                        DirectoryName: row.Path,
                        IsDefault: row.IsDefault,
                        GroupId: row.StoreId,
                        GroupName: row.Name));
                }
                continue;
            }

            IReadOnlyList<FirefoxProfileStoreEntry> storeEntries = FirefoxProfileStoreScanner.Read(sqlitePath);
            if (storeEntries.Count == 0)
            {
                string tail = ExtractTailSegment(row.Path);
                if (emittedTail.Add(tail))
                {
                    result.Add(new BrowserProfile(
                        Name: row.Name ?? row.Path,
                        DirectoryName: row.Path,
                        IsDefault: row.IsDefault,
                        GroupId: row.StoreId,
                        GroupName: row.Name));
                }
                continue;
            }

            foreach (FirefoxProfileStoreEntry entry in storeEntries)
            {
                string tail = ExtractTailSegment(entry.Path);
                if (!emittedTail.Add(tail))
                {
                    continue;
                }

                string name = ResolveEntryName(entry, row);

                bool isDefault = IsPathMatch(entry.Path, row.Path) && row.IsDefault;

                result.Add(new BrowserProfile(
                    Name: name,
                    DirectoryName: entry.Path,
                    IsDefault: isDefault,
                    GroupId: row.StoreId,
                    GroupName: row.Name));
            }
        }

        return result
            .OrderBy(p => p.IsDefault ? 0 : 1)
            .ThenBy(p => p.GroupName ?? p.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

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

    private static string ExtractTailSegment(string path)
    {
        string normalized = path.Replace('/', '\\');
        int lastSlash = normalized.LastIndexOf('\\');
        return lastSlash >= 0 ? normalized[(lastSlash + 1)..] : normalized;
    }

    private static bool IsPathMatch(string a, string b)
    {
        return string.Equals(ExtractTailSegment(a), ExtractTailSegment(b), StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveEntryName(FirefoxProfileStoreEntry entry, IniRow row)
    {
        bool isFirefoxDefaultName = !string.IsNullOrWhiteSpace(entry.Name)
            && string.Equals(entry.Name.Trim(), "Original profile", StringComparison.OrdinalIgnoreCase);

        if (!isFirefoxDefaultName && !string.IsNullOrWhiteSpace(entry.Name))
        {
            return entry.Name!;
        }

        return row.Name ?? ExtractTailSegment(entry.Path);
    }

    private sealed record IniRow(string? Name, string? Path, bool IsDefault, string? StoreId);

    [GeneratedRegex(@"^Profile(\d+)$")]
    private static partial Regex SectionRegex();
}
