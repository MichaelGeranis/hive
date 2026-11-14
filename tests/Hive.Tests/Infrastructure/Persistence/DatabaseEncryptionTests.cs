using System.Text;
using Hive.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;

namespace Hive.Tests.Infrastructure.Persistence;

public class DatabaseEncryptionTests : IDisposable
{
    private const string Key = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";
    private const string Secret = "Worried about the reorg, wants to move teams";

    private readonly string _directory = Path.Combine(Path.GetTempPath(), "HiveEncryptionTests", Guid.NewGuid().ToString());

    public DatabaseEncryptionTests()
    {
        Directory.CreateDirectory(_directory);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        try
        {
            Directory.Delete(_directory, recursive: true);
        }
        catch (IOException)
        {
            // Ignore cleanup errors
        }

        GC.SuppressFinalize(this);
    }

    [Theory]
    [InlineData(Key, true)]
    [InlineData("0123", false)]
    [InlineData("zz23456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef", false)]
    [InlineData("0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcde'", false)]
    [InlineData(null, false)]
    public void IsValidKey_OnlyAccepts64HexCharacters(string? key, bool expected)
    {
        DatabaseEncryption.IsValidKey(key).Should().Be(expected);
    }

    [Fact]
    public void WithKey_InvalidKey_Throws()
    {
        var act = () => DatabaseEncryption.WithKey("Data Source=x.db", "not-a-key");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void WithKey_NoKey_LeavesConnectionStringUnchanged()
    {
        DatabaseEncryption.WithKey("Data Source=x.db", null).Should().Be("Data Source=x.db");
    }

    [Fact]
    public void EncryptPlaintextFiles_PlainDatabaseAndBackups_EncryptsAllAndKeepsTheirData()
    {
        var database = CreatePlainDatabase("hive.db", wal: true);
        var backup = CreatePlainDatabase("hive.db.backup.2026-09-29_10-00-00", wal: false);

        var count = DatabaseEncryption.EncryptPlaintextFiles(database, Key, NullLogger.Instance);

        count.Should().Be(2);
        foreach (var path in new[] { database, backup })
        {
            DatabaseEncryption.IsPlaintext(path).Should().BeFalse();
            File.ReadAllBytes(path).AsSpan().IndexOf(Encoding.UTF8.GetBytes(Secret)).Should().Be(-1);
            ReadNote(path, Key).Should().Be(Secret);
        }
    }

    [Fact]
    public void EncryptPlaintextFiles_WalDatabase_StaysInWalMode()
    {
        var database = CreatePlainDatabase("hive.db", wal: true);

        DatabaseEncryption.EncryptPlaintextFiles(database, Key, NullLogger.Instance);

        using var connection = Open(database, Key);
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA journal_mode;";
        command.ExecuteScalar().Should().Be("wal");
    }

    [Fact]
    public void EncryptPlaintextFiles_AlreadyEncrypted_DoesNothing()
    {
        var database = CreatePlainDatabase("hive.db", wal: false);
        DatabaseEncryption.EncryptPlaintextFiles(database, Key, NullLogger.Instance);

        var count = DatabaseEncryption.EncryptPlaintextFiles(database, Key, NullLogger.Instance);

        count.Should().Be(0);
        ReadNote(database, Key).Should().Be(Secret);
    }

    [Fact]
    public void EncryptedDatabase_OpenedWithoutKey_CannotBeRead()
    {
        var database = CreatePlainDatabase("hive.db", wal: false);
        DatabaseEncryption.EncryptPlaintextFiles(database, Key, NullLogger.Instance);

        var act = () => ReadNote(database, key: null);

        act.Should().Throw<SqliteException>();
    }

    [Fact]
    public void CreateBackup_WithKey_WritesAnEncryptedBackup()
    {
        var database = CreatePlainDatabase("hive.db", wal: false);
        DatabaseEncryption.EncryptPlaintextFiles(database, Key, NullLogger.Instance);
        var service = new DatabaseBackupService(NullLogger<DatabaseBackupService>.Instance, databaseKey: Key);

        service.CreateBackup(database).Should().BeTrue();

        var backup = Directory.GetFiles(_directory, "hive.db.backup.*").Single();
        DatabaseEncryption.IsPlaintext(backup).Should().BeFalse();
        ReadNote(backup, Key).Should().Be(Secret);
    }

    private string CreatePlainDatabase(string name, bool wal)
    {
        var path = Path.Combine(_directory, name);
        using var connection = Open(path, key: null);
        using var command = connection.CreateCommand();
        command.CommandText = (wal ? "PRAGMA journal_mode=WAL;" : "")
            + "CREATE TABLE notes (body TEXT); INSERT INTO notes VALUES ($body);";
        command.Parameters.AddWithValue("$body", Secret);
        command.ExecuteNonQuery();
        DatabaseEncryption.IsPlaintext(path).Should().BeTrue();
        return path;
    }

    private static string? ReadNote(string path, string? key)
    {
        using var connection = Open(path, key);
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT body FROM notes;";
        return command.ExecuteScalar() as string;
    }

    private static SqliteConnection Open(string path, string? key)
    {
        var connection = new SqliteConnection(
            DatabaseEncryption.WithKey(new SqliteConnectionStringBuilder { DataSource = path, Pooling = false }.ToString(), key));
        connection.Open();
        return connection;
    }
}
