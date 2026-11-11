using Hive.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace Hive.Tests.Infrastructure.Persistence;

public class DatabaseBackupSchedulerTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "HiveBackupSchedulerTests", Guid.NewGuid().ToString());

    public DatabaseBackupSchedulerTests()
    {
        Directory.CreateDirectory(_directory);
    }

    [Fact]
    public void Interval_IsAnHour()
    {
        DatabaseBackupScheduler.Interval.Should().Be(TimeSpan.FromHours(1));
    }

    [Fact]
    public async Task Running_BacksTheDatabaseUpOnEveryTick_AndStopsWithTheBackend()
    {
        // Arrange — a real database, and a scheduler ticking every 50 ms rather than every hour
        var dbPath = Path.Combine(_directory, "hive.db");
        using (var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = dbPath, Pooling = false }.ToString()))
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "CREATE TABLE Notes (Title TEXT);";
            command.ExecuteNonQuery();
        }
        File.SetLastWriteTimeUtc(dbPath, DateTime.UtcNow.AddSeconds(-5));
        var backups = new DatabaseBackupService(new Mock<ILogger<DatabaseBackupService>>().Object);
        using var scheduler = new DatabaseBackupScheduler(
            backups, dbPath, new Mock<ILogger<DatabaseBackupScheduler>>().Object, TimeSpan.FromMilliseconds(50));

        // Act
        await scheduler.StartAsync(CancellationToken.None);
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (Directory.GetFiles(_directory, "hive.db.backup.*").Length == 0 && DateTime.UtcNow < deadline)
        {
            await Task.Delay(25);
        }
        await scheduler.StopAsync(CancellationToken.None);

        // Assert
        Directory.GetFiles(_directory, "hive.db.backup.*").Should().ContainSingle(
            "the first tick backs it up, and the database has not changed since");
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_directory, recursive: true);
        }
        catch
        {
            // Ignore cleanup errors
        }
        GC.SuppressFinalize(this);
    }
}
