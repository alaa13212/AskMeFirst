using AskMeFirst.Core.Profiles;
using Microsoft.Data.Sqlite;
using Xunit;

namespace AskMeFirst.Core.Tests;

public class FirefoxProfileStoreScannerTests
{
    [Fact]
    public void Read_MissingFile_ReturnsEmpty()
    {
        IReadOnlyList<FirefoxProfileStoreEntry> result = FirefoxProfileStoreScanner.Read(@"C:\does\not\exist.sqlite");
        Assert.Empty(result);
    }

    [Fact]
    public void Read_ValidStore_ReturnsEntries()
    {
        string dbPath = Path.Combine(Path.GetTempPath(), $"ffscan-{Guid.NewGuid():N}.sqlite");
        try
        {
            CreateStore(dbPath, [
                ("Person 1", @"C:\Users\Ali\AppData\Local\Mozilla\Firefox\Profiles\abc.default"),
                ("Work", @"C:\Users\Ali\AppData\Local\Mozilla\Firefox\Profiles\def.work"),
            ]);

            IReadOnlyList<FirefoxProfileStoreEntry> result = FirefoxProfileStoreScanner.Read(dbPath);

            Assert.Equal(2, result.Count);
            Assert.Equal("Person 1", result[0].Name);
            Assert.Contains("abc.default", result[0].Path);
            Assert.Equal("Work", result[1].Name);
            Assert.Contains("def.work", result[1].Path);
        }
        finally
        {
            try { File.Delete(dbPath); } catch { }
        }
    }

    [Fact]
    public void Read_EmptyName_IsIncluded()
    {
        string dbPath = Path.Combine(Path.GetTempPath(), $"ffscan-{Guid.NewGuid():N}.sqlite");
        try
        {
            CreateStore(dbPath, [
                (null, @"C:\Users\Ali\AppData\Local\Mozilla\Firefox\Profiles\abc.default"),
            ]);

            IReadOnlyList<FirefoxProfileStoreEntry> result = FirefoxProfileStoreScanner.Read(dbPath);

            Assert.Single(result);
            Assert.Null(result[0].Name);
        }
        finally
        {
            try { File.Delete(dbPath); } catch { }
        }
    }

    [Fact]
    public void Read_EmptyPath_IsExcluded()
    {
        string dbPath = Path.Combine(Path.GetTempPath(), $"ffscan-{Guid.NewGuid():N}.sqlite");
        try
        {
            CreateStore(dbPath, [
                ("Has path", @"C:\path\exists"),
                ("No path", ""),
            ]);

            IReadOnlyList<FirefoxProfileStoreEntry> result = FirefoxProfileStoreScanner.Read(dbPath);

            Assert.Single(result);
            Assert.Equal("Has path", result[0].Name);
        }
        finally
        {
            try { File.Delete(dbPath); } catch { }
        }
    }

    private static void CreateStore(string dbPath, (string? Name, string Path)[] entries)
    {
        using SqliteConnection conn = new($"Data Source={dbPath}");
        conn.Open();
        using SqliteCommand create = conn.CreateCommand();
        create.CommandText = "CREATE TABLE profiles (name TEXT, path TEXT)";
        create.ExecuteNonQuery();
        foreach ((string? Name, string Path) entry in entries)
        {
            using SqliteCommand insert = conn.CreateCommand();
            insert.CommandText = "INSERT INTO profiles (name, path) VALUES ($name, $path)";
            insert.Parameters.AddWithValue("$name", entry.Name ?? (object)DBNull.Value);
            insert.Parameters.AddWithValue("$path", entry.Path);
            insert.ExecuteNonQuery();
        }
    }
}
