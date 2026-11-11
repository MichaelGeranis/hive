using Hive.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace Hive.Tests.Infrastructure.Persistence;

public class DatabaseBackupServiceTests : IDisposable
{
    private readonly Mock<ILogger<DatabaseBackupService>> _loggerMock;
    private readonly Clock _clock = new(new DateTimeOffset(2026, 9, 29, 10, 0, 0, TimeSpan.Zero));
    private readonly DatabaseBackupService _service;
    private readonly string _tempDirectory;

    public DatabaseBackupServiceTests()
    {
        _loggerMock = new Mock<ILogger<DatabaseBackupService>>();
        _service = new DatabaseBackupService(_loggerMock.Object, _clock);
        _tempDirectory = Path.Combine(Path.GetTempPath(), "HiveBackupTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void CreateBackup_WithNonExistentDatabase_ReturnsFalseAndLogsWarning()
    {
        var result = _service.CreateBackup(Path.Combine(_tempDirectory, "nonexistent.db"));

        result.Should().BeFalse();
        VerifyLogged(LogLevel.Warning, "Database file not found");
    }

    [Fact]
    public void CreateBackup_WithExistingDatabase_CopiesItsData()
    {
        var dbPath = CreateDatabase(rows: ["first", "second"]);

        var result = _service.CreateBackup(dbPath);

        result.Should().BeTrue();
        ReadTitles(SingleBackup()).Should().Equal("first", "second");
        VerifyLogged(LogLevel.Information, "Database backup created successfully");
    }

    [Fact]
    public void CreateBackup_NamesTheBackupAfterWhenItWasTaken()
    {
        var dbPath = CreateDatabase();

        _service.CreateBackup(dbPath);

        Path.GetFileName(SingleBackup()).Should().Be("test.db.backup.2026-09-29_10-00-00");
    }

    [Fact]
    public void CreateBackup_WhenNothingChanged_TakesNoNewBackup()
    {
        var dbPath = CreateDatabase();
        _service.CreateBackup(dbPath);

        _clock.Advance(TimeSpan.FromHours(1));
        var result = _service.CreateBackup(dbPath);

        result.Should().BeTrue();
        Backups().Should().ContainSingle();
        VerifyLogged(LogLevel.Information, "unchanged since the last backup");
    }

    [Fact]
    public void CreateBackup_WhenTheDatabaseChanged_TakesANewBackup()
    {
        var dbPath = CreateDatabase(rows: ["first"]);
        _service.CreateBackup(dbPath);

        AddRow(dbPath, "second");
        _clock.Advance(TimeSpan.FromHours(1));
        _service.CreateBackup(dbPath);

        var backups = Backups();
        backups.Should().HaveCount(2);
        ReadTitles(backups[0]).Should().Equal("first", "second");
    }

    [Fact]
    public void CreateBackup_WithChangesStillInTheWriteAheadLog_BacksThemUp()
    {
        // Arrange — a WAL database whose writer is still open, as when the backend is killed:
        // the row is only in the -wal file, not yet in the database file
        var dbPath = Path.Combine(_tempDirectory, "wal.db");
        using var writer = Open(dbPath);
        Execute(writer, "PRAGMA journal_mode = WAL;");
        Execute(writer, "CREATE TABLE Notes (Title TEXT);");
        Execute(writer, "INSERT INTO Notes VALUES ('latest');");
        File.Exists($"{dbPath}-wal").Should().BeTrue();

        // Act
        var result = _service.CreateBackup(dbPath);

        // Assert
        result.Should().BeTrue();
        ReadTitles(Directory.GetFiles(_tempDirectory, "wal.db.backup.*").Single()).Should().Equal("latest");
    }

    [Fact]
    public void CreateBackup_WhileAWriteIsInProgress_TakesTheLastCommittedState()
    {
        // Arrange — a transaction the app has not committed yet
        var dbPath = CreateDatabase(rows: ["committed"], wal: true);
        using var writer = Open(dbPath);
        using var transaction = writer.BeginTransaction();
        Execute(writer, "INSERT INTO Notes VALUES ('uncommitted');", transaction);

        // Act
        var result = _service.CreateBackup(dbPath);

        // Assert — a consistent snapshot: the half-done write is not in it
        result.Should().BeTrue();
        ReadTitles(SingleBackup()).Should().Equal("committed");
    }

    [Fact]
    public void CreateBackup_LeavesNoPartialFileBehind()
    {
        var dbPath = CreateDatabase();

        _service.CreateBackup(dbPath);

        File.Exists($"{dbPath}.backing-up").Should().BeFalse();
    }

    [Fact]
    public void CreateBackup_OfAFileThatIsNotADatabase_ReturnsFalseAndLogsError()
    {
        var path = Path.Combine(_tempDirectory, "notes.txt");
        File.WriteAllText(path, "not a database");

        var result = _service.CreateBackup(path);

        result.Should().BeFalse();
        VerifyLogged(LogLevel.Error, "Failed to create database backup");
        Directory.GetFiles(_tempDirectory, "notes.txt.backup.*").Should().BeEmpty();
    }

    [Fact]
    public void CreateBackup_WithReadOnlyDatabase_CreatesBackup()
    {
        var dbPath = CreateDatabase(rows: ["kept"]);
        var file = new FileInfo(dbPath) { IsReadOnly = true };

        try
        {
            _service.CreateBackup(dbPath).Should().BeTrue();
            ReadTitles(SingleBackup()).Should().Equal("kept");
        }
        finally
        {
            file.IsReadOnly = false;
        }
    }

    [Fact]
    public void CreateBackup_WithEmptyDatabase_CreatesBackup()
    {
        var dbPath = Path.Combine(_tempDirectory, "test.db");
        File.WriteAllText(dbPath, string.Empty);

        _service.CreateBackup(dbPath).Should().BeTrue();

        Backups().Should().ContainSingle();
    }

    [Fact]
    public void CreateBackup_WithSpecialCharactersInPath_HandlesCorrectly()
    {
        var specialDir = Path.Combine(_tempDirectory, "special (dir) [test] 'quote'");
        Directory.CreateDirectory(specialDir);
        var dbPath = CreateDatabase(directory: specialDir, rows: ["x"]);

        _service.CreateBackup(dbPath).Should().BeTrue();

        ReadTitles(Directory.GetFiles(specialDir, "*.backup.*").Single()).Should().Equal("x");
    }

    [Fact]
    public void CreateBackup_KeepsADayOfBackupsThenOneADayForAMonth()
    {
        // Arrange — 30 hourly backups over the last day and a bit, then 3 a day for 40 days
        var dbPath = CreateDatabase();
        var now = _clock.GetLocalNow().DateTime;
        for (var hours = 1; hours <= 30; hours++)
        {
            SeedBackup(dbPath, now.AddHours(-hours));
        }
        for (var days = 2; days <= 41; days++)
        {
            foreach (var hour in new[] { 9, 13, 17 })
            {
                SeedBackup(dbPath, now.Date.AddDays(-days).AddHours(hour));
            }
        }

        // Act — a new backup, and the clean-up after it
        _service.CreateBackup(dbPath);

        // Assert
        var kept = Backups().Select(Taken).ToList();
        kept.Take(DatabaseBackupService.RecentBackupsToKeep).Should().Equal(
            new[] { now }.Concat(Enumerable.Range(1, 23).Select(hours => now.AddHours(-hours))));
        var daily = kept.Skip(DatabaseBackupService.RecentBackupsToKeep).ToList();
        daily.Should().HaveCount(DatabaseBackupService.DailyBackupsToKeep);
        daily.Select(taken => taken.Date).Should().OnlyHaveUniqueItems();
        daily.Should().OnlyContain(taken => taken.Hour == 17 || taken.Date >= now.Date.AddDays(-1),
            "the newest backup of each day is the one kept");
    }

    [Fact]
    public void CreateBackup_NeverDeletesAFileWithoutATimestamp()
    {
        var dbPath = CreateDatabase();
        for (var hours = 1; hours <= 30; hours++)
        {
            SeedBackup(dbPath, _clock.GetLocalNow().DateTime.AddHours(-hours));
        }
        var mine = $"{dbPath}.backup.before-upgrade";
        File.WriteAllText(mine, "kept by hand");

        _service.CreateBackup(dbPath);

        File.Exists(mine).Should().BeTrue();
    }

    [Fact]
    public void GetStatus_SaysWhereTheBackupsAreHowManyAndTheLastOne()
    {
        var dbPath = CreateDatabase();
        var before = _service.GetStatus(dbPath);
        _service.CreateBackup(dbPath);

        var after = _service.GetStatus(dbPath);

        before.Count.Should().Be(0);
        before.LastBackupAt.Should().BeNull();
        after.Folder.Should().Be(Path.GetFullPath(_tempDirectory));
        after.Count.Should().Be(1);
        after.LastBackupAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    #region Helper Methods

    private string CreateDatabase(string? directory = null, string[]? rows = null, bool wal = false)
    {
        var dbPath = Path.Combine(directory ?? _tempDirectory, "test.db");
        using (var connection = Open(dbPath))
        {
            if (wal) Execute(connection, "PRAGMA journal_mode = WAL;");
            Execute(connection, "CREATE TABLE Notes (Title TEXT);");
            foreach (var row in rows ?? [])
            {
                Execute(connection, $"INSERT INTO Notes VALUES ('{row}');");
            }
        }

        // A little in the past, so a backup taken now is clearly newer than the last write
        File.SetLastWriteTimeUtc(dbPath, DateTime.UtcNow.AddSeconds(-5));
        return dbPath;
    }

    private static void AddRow(string dbPath, string title)
    {
        using (var connection = Open(dbPath))
        {
            Execute(connection, $"INSERT INTO Notes VALUES ('{title}');");
        }
        File.SetLastWriteTimeUtc(dbPath, DateTime.UtcNow.AddSeconds(1));
    }

    private static void SeedBackup(string dbPath, DateTime taken)
    {
        var path = $"{dbPath}.backup.{taken.ToString(DatabaseBackupService.TimestampFormat)}";
        File.WriteAllText(path, "old backup");
        File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddDays(-60));
    }

    private static DateTime Taken(string backupPath) =>
        DateTime.ParseExact(Path.GetFileName(backupPath)["test.db.backup.".Length..], DatabaseBackupService.TimestampFormat, null);

    private List<string> Backups() =>
        Directory.GetFiles(_tempDirectory, "test.db.backup.*").OrderByDescending(Path.GetFileName, StringComparer.Ordinal).ToList();

    private string SingleBackup() => Backups().Single();

    private static SqliteConnection Open(string path)
    {
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path, Pooling = false }.ToString());
        connection.Open();
        return connection;
    }

    private static List<string> ReadTitles(string path)
    {
        using var connection = Open(path);
        using var query = connection.CreateCommand();
        query.CommandText = "SELECT Title FROM Notes ORDER BY rowid;";
        using var reader = query.ExecuteReader();
        var titles = new List<string>();
        while (reader.Read()) titles.Add(reader.GetString(0));
        return titles;
    }

    private static void Execute(SqliteConnection connection, string sql, SqliteTransaction? transaction = null)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    private void VerifyLogged(LogLevel level, string text) =>
        _loggerMock.Verify(
            x => x.Log(
                level,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains(text)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);

    // A clock the tests move by hand; local time is UTC so file names are predictable
    private sealed class Clock(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset _now = now;
        public override DateTimeOffset GetUtcNow() => _now;
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
        public void Advance(TimeSpan by) => _now += by;
    }

    #endregion
}
