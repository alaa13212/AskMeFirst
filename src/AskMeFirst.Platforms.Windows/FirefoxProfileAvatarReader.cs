using AskMeFirst.Core.Data;
using Microsoft.Data.Sqlite;

namespace AskMeFirst.Platforms.Windows;

public static class FirefoxProfileAvatarReader
{
    public static byte[]? ReadAvatarPng(string groupsRoot, string profileDirTail)
    {
        if (string.IsNullOrEmpty(groupsRoot) || !Directory.Exists(groupsRoot))
        {
            return null;
        }

        string? avatarId = FindAvatarId(groupsRoot, profileDirTail);
        if (string.IsNullOrEmpty(avatarId))
        {
            return null;
        }

        if (!Guid.TryParseExact(avatarId, "D", out _))
        {
            return null;
        }

        string avatarPath = Path.Combine(groupsRoot, "avatars", avatarId);
        if (!File.Exists(avatarPath))
        {
            return null;
        }

        byte[] bytes;
        try
        {
            bytes = File.ReadAllBytes(avatarPath);
        }
        catch
        {
            return null;
        }

        return PngSignature.Matches(bytes) ? bytes : null;
    }

    private static string? FindAvatarId(string groupsRoot, string profileDirTail)
    {
        foreach (string sqlitePath in Directory.EnumerateFiles(groupsRoot, "*.sqlite"))
        {
            string? result = QueryAvatarId(sqlitePath, profileDirTail);
            if (!string.IsNullOrEmpty(result))
            {
                return result;
            }
        }
        return null;
    }

    private static string? QueryAvatarId(string sqlitePath, string profileDirTail)
    {
        try
        {
            using SqliteConnection conn = new($"Data Source={sqlitePath};Mode=ReadOnly");
            conn.Open();

            if (!SqliteTable.Exists(conn, "Profiles"))
            {
                return null;
            }

            using SqliteCommand cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT avatar FROM Profiles WHERE path LIKE $pattern";
            SqliteParameter p = cmd.CreateParameter();
            p.ParameterName = "$pattern";
            p.Value = "%" + profileDirTail;
            cmd.Parameters.Add(p);

            object? result = cmd.ExecuteScalar();
            if (result is string s && !string.IsNullOrWhiteSpace(s))
            {
                return s;
            }
        }
        catch
        {
        }
        return null;
    }
}
