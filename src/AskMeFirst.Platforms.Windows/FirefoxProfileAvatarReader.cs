using Microsoft.Data.Sqlite;

namespace AskMeFirst.Platforms.Windows;

public static class FirefoxProfileAvatarReader
{
    private static readonly byte[] PngMagic = [0x89, 0x50, 0x4E, 0x47];

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

        if (!IsUuid(avatarId))
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

        if (!IsPng(bytes))
        {
            return null;
        }

        return bytes;
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

            if (!TableExists(conn, "Profiles"))
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

    private static bool TableExists(SqliteConnection conn, string tableName)
    {
        using SqliteCommand cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT 1 FROM sqlite_master WHERE type='table' AND name=$name LIMIT 1";
        SqliteParameter param = cmd.CreateParameter();
        param.ParameterName = "$name";
        param.Value = tableName;
        cmd.Parameters.Add(param);
        object? result = cmd.ExecuteScalar();
        return result is not null;
    }

    private static bool IsUuid(string s)
    {
        if (s.Length != 36)
        {
            return false;
        }
        for (int i = 0; i < 36; i++)
        {
            byte b = (byte)s[i];
            bool isHex = (b >= (byte)'0' && b <= (byte)'9') || (b >= (byte)'a' && b <= (byte)'f');
            bool isDash = b == (byte)'-';
            bool dashPos = i == 8 || i == 13 || i == 18 || i == 23;
            if (dashPos)
            {
                if (!isDash)
                {
                    return false;
                }
            }
            else
            {
                if (!isHex)
                {
                    return false;
                }
            }
        }
        return true;
    }

    private static bool IsPng(byte[] bytes)
    {
        if (bytes.Length < PngMagic.Length)
        {
            return false;
        }
        for (int i = 0; i < PngMagic.Length; i++)
        {
            if (bytes[i] != PngMagic[i])
            {
                return false;
            }
        }
        return true;
    }
}
