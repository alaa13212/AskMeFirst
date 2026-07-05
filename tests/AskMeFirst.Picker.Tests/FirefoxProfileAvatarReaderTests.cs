#if WINDOWS
using AskMeFirst.Core.Profiles;
using Microsoft.Data.Sqlite;
using Xunit;

namespace AskMeFirst.Picker.Tests;

public class FirefoxProfileAvatarReaderTests
{
    private static readonly byte[] PngBytes =
    [
        0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D,
    ];

    private static string CreateGroupRootWithStore(
        string root, string storeId, (string Path, string Avatar)[] rows)
    {
        Directory.CreateDirectory(root);
        Directory.CreateDirectory(Path.Combine(root, "avatars"));

        string sqlitePath = Path.Combine(root, $"{storeId}.sqlite");
        using SqliteConnection conn = new($"Data Source={sqlitePath}");
        conn.Open();

        using (SqliteCommand createCmd = conn.CreateCommand())
        {
            createCmd.CommandText = """
                CREATE TABLE Profiles (
                    id INTEGER NOT NULL,
                    path TEXT NOT NULL,
                    name TEXT,
                    avatar TEXT,
                    themeId TEXT,
                    themeFg TEXT,
                    themeBg TEXT,
                    PRIMARY KEY(id)
                )
                """;
            createCmd.ExecuteNonQuery();
        }

        foreach ((string Path, string Avatar) row in rows)
        {
            using SqliteCommand insertCmd = conn.CreateCommand();
            insertCmd.CommandText = "INSERT INTO Profiles (path, avatar) VALUES ($p, $a)";

            SqliteParameter p = insertCmd.CreateParameter();
            p.ParameterName = "$p";
            p.Value = row.Path;
            insertCmd.Parameters.Add(p);

            SqliteParameter a = insertCmd.CreateParameter();
            a.ParameterName = "$a";
            a.Value = row.Avatar;
            insertCmd.Parameters.Add(a);

            insertCmd.ExecuteNonQuery();
        }

        return root;
    }

    private static void WriteAvatarPng(string groupsRoot, string avatarUuid)
    {
        string path = Path.Combine(groupsRoot, "avatars", avatarUuid);
        File.WriteAllBytes(path, PngBytes);
    }

    private static void Cleanup(string dir)
    {
        try { Directory.Delete(dir, recursive: true); } catch { }
    }

    [Fact]
    public void ReadAvatarPng_MissingRoot_ReturnsNull()
    {
        byte[]? result = FirefoxProfileAvatarReader.ReadAvatarPng(@"C:\does\not\exist", "any");
        Assert.Null(result);
    }

    [Fact]
    public void ReadAvatarPng_NoSqliteFiles_ReturnsNull()
    {
        string root = Path.Combine(Path.GetTempPath(), $"firefox-avatars-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            byte[]? result = FirefoxProfileAvatarReader.ReadAvatarPng(root, "any");
            Assert.Null(result);
        }
        finally
        {
            Cleanup(root);
        }
    }

    [Fact]
    public void ReadAvatarPng_ProfileNotInStore_ReturnsNull()
    {
        string root = CreateGroupRootWithStore(
            Path.Combine(Path.GetTempPath(), $"firefox-avatars-{Guid.NewGuid():N}"),
            "store1",
            [("Profiles/abc.work", "uuid-aaaa")]);

        try
        {
            byte[]? result = FirefoxProfileAvatarReader.ReadAvatarPng(root, "missing.profile");
            Assert.Null(result);
        }
        finally
        {
            Cleanup(root);
        }
    }

    [Fact]
    public void ReadAvatarPng_PresetAvatarName_ReturnsNull()
    {
        string root = CreateGroupRootWithStore(
            Path.Combine(Path.GetTempPath(), $"firefox-avatars-{Guid.NewGuid():N}"),
            "store1",
            [("Profiles/abc.work", "star")]);

        try
        {
            byte[]? result = FirefoxProfileAvatarReader.ReadAvatarPng(root, "abc.work");
            Assert.Null(result);
        }
        finally
        {
            Cleanup(root);
        }
    }

    [Fact]
    public void ReadAvatarPng_AvatarUuidNotOnDisk_ReturnsNull()
    {
        string root = CreateGroupRootWithStore(
            Path.Combine(Path.GetTempPath(), $"firefox-avatars-{Guid.NewGuid():N}"),
            "store1",
            [("Profiles/abc.work", "uuid-aaaa")]);

        try
        {
            byte[]? result = FirefoxProfileAvatarReader.ReadAvatarPng(root, "abc.work");
            Assert.Null(result);
        }
        finally
        {
            Cleanup(root);
        }
    }

    [Fact]
    public void ReadAvatarPng_ValidAvatar_ReturnsPngBytes()
    {
        string avatarUuid = "2b112981-eb68-4fef-9747-1aea97654174";
        string root = CreateGroupRootWithStore(
            Path.Combine(Path.GetTempPath(), $"firefox-avatars-{Guid.NewGuid():N}"),
            "store1",
            [("Profiles/abc.work", avatarUuid)]);
        WriteAvatarPng(root, avatarUuid);

        try
        {
            byte[]? result = FirefoxProfileAvatarReader.ReadAvatarPng(root, "abc.work");
            Assert.NotNull(result);
            Assert.Equal(PngBytes, result);
        }
        finally
        {
            Cleanup(root);
        }
    }
}
#endif
