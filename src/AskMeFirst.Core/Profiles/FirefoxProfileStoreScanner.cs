using Microsoft.Data.Sqlite;

namespace AskMeFirst.Core.Profiles;

public sealed record FirefoxProfileStoreEntry(string? Name, string Path);

public static class FirefoxProfileStoreScanner
{
    public static IReadOnlyList<FirefoxProfileStoreEntry> Read(string sqlitePath)
    {
        try
        {
            using SqliteConnection conn = new($"Data Source={sqlitePath};Mode=ReadOnly");
            conn.Open();

            if (!TableExists(conn, "profiles"))
            {
                return [];
            }

            if (!ColumnExists(conn, "profiles", "name"))
            {
                return [];
            }

            List<FirefoxProfileStoreEntry> entries = [];
            using SqliteCommand cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT name, path FROM profiles";
            using SqliteDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                string? name = reader.IsDBNull(0) ? null : reader.GetString(0);
                string path = reader.IsDBNull(1) ? "" : reader.GetString(1);
                if (!string.IsNullOrEmpty(path))
                {
                    entries.Add(new FirefoxProfileStoreEntry(name, path));
                }
            }
            return entries;
        }
        catch
        {
            return [];
        }
    }

    private static bool TableExists(SqliteConnection conn, string tableName)
    {
        using SqliteCommand cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table'";
        using SqliteDataReader reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            if (reader.GetString(0).Equals(tableName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    private static bool ColumnExists(SqliteConnection conn, string tableName, string columnName)
    {
        using SqliteCommand cmd = conn.CreateCommand();
        cmd.CommandText = $"PRAGMA table_info(\"{tableName}\")";
        using SqliteDataReader reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            if (reader.GetString(1).Equals(columnName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }
}
