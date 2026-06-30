using Microsoft.Data.Sqlite;

namespace AskMeFirst.Core.Data;

public static class SqliteTable
{
    public static bool Exists(SqliteConnection connection, string tableName)
    {
        return GetActualName(connection, tableName) is not null;
    }

    public static string? GetActualName(SqliteConnection connection, string tableName)
    {
        using SqliteCommand cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table'";
        using SqliteDataReader reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            if (reader.GetString(0).Equals(tableName, StringComparison.OrdinalIgnoreCase))
            {
                return reader.GetString(0);
            }
        }
        return null;
    }
}
