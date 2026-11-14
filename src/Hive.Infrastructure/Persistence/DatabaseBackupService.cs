using System.Globalization;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace Hive.Infrastructure.Persistence;

/// <summary>
/// Automatic backups of the SQLite database: a whole copy of it, so every table and every
/// field, next to the database as <c>hive.db.backup.yyyy-MM-dd_HH-mm-ss</c>. Taken when the
/// backend starts and every hour while it runs (<see cref="DatabaseBackupScheduler"/>).
/// </summary>
/// <remarks>
/// A backup is written with <c>VACUUM INTO</c>, SQLite's own consistent copy: it reads one
/// snapshot while the app goes on writing, includes what is still in the write-ahead log, and
/// writes a compacted file. It goes to a temporary name first, so a crash never leaves half a
/// backup. Nothing is written when nothing changed since the last backup.
/// </remarks>
public class DatabaseBackupService
{
    /// <summary>The newest backups, all kept: a day of hourly copies.</summary>
    public const int RecentBackupsToKeep = 24;

    /// <summary>Before those, the newest backup of each day, for this many days.</summary>
    public const int DailyBackupsToKeep = 30;

    public const string TimestampFormat = "yyyy-MM-dd_HH-mm-ss";

    private readonly ILogger<DatabaseBackupService> _logger;
    private readonly TimeProvider _time;
    private readonly object _gate = new();
    private readonly string? _databaseKey;

    /// <param name="databaseKey">The database's key, when it is encrypted: backups are then encrypted with it too</param>
    public DatabaseBackupService(ILogger<DatabaseBackupService> logger, TimeProvider? time = null, string? databaseKey = null)
    {
        _logger = logger;
        _time = time ?? TimeProvider.System;
        _databaseKey = databaseKey;
    }

    /// <summary>
    /// Backs the database up, unless nothing changed since the last backup.
    /// </summary>
    /// <param name="databasePath">Path to the database file to back up</param>
    /// <returns>True when the database is backed up, by this call or an earlier one; false on failure</returns>
    public bool CreateBackup(string databasePath)
    {
        // The startup backup and the hourly one never run at once
        lock (_gate)
        {
            try
            {
                if (!File.Exists(databasePath))
                {
                    _logger.LogWarning("Database file not found at {DatabasePath}. Skipping backup.", databasePath);
                    return false;
                }

                if (IsUnchangedSinceLastBackup(databasePath))
                {
                    _logger.LogInformation("Database unchanged since the last backup; no new backup needed");
                    return true;
                }

                // Compared with the database's own file times, so it is the real clock, not _time
                var snapshotAt = DateTime.UtcNow;
                var backupPath = $"{databasePath}.backup.{_time.GetLocalNow().ToString(TimestampFormat, CultureInfo.InvariantCulture)}";
                if (File.Exists(backupPath))
                {
                    return true;
                }

                _logger.LogInformation("Creating database backup: {BackupPath}", backupPath);
                var partialPath = $"{databasePath}.backing-up";
                if (File.Exists(partialPath))
                {
                    File.Delete(partialPath);
                }

                VacuumInto(databasePath, partialPath);
                File.Move(partialPath, backupPath);
                // The backup holds the database as it was when the copy began
                File.SetLastWriteTimeUtc(backupPath, snapshotAt);

                _logger.LogInformation("Database backup created successfully: {BackupPath}", backupPath);

                CleanupOldBackups(databasePath);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create database backup for {DatabasePath}", databasePath);
                return false;
            }
        }
    }

    /// <summary>
    /// Where the backups go, how many there are and when the newest was taken.
    /// </summary>
    public (string Folder, int Count, DateTime? LastBackupAt) GetStatus(string databasePath)
    {
        var backups = ListBackups(databasePath);
        return (
            Path.GetDirectoryName(Path.GetFullPath(databasePath)) ?? string.Empty,
            backups.Count,
            backups.Count > 0 ? backups[0].LastWriteTimeUtc : null);
    }

    // With a key, VACUUM INTO writes the copy encrypted with the same key
    private void VacuumInto(string databasePath, string targetPath)
    {
        var connectionString = DatabaseEncryption.WithKey(
            new SqliteConnectionStringBuilder { DataSource = databasePath, Pooling = false }.ToString(), _databaseKey);
        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "VACUUM INTO $target;";
        command.Parameters.AddWithValue("$target", targetPath);
        command.ExecuteNonQuery();
    }

    // Unchanged when neither the database nor its write-ahead log was written after the newest
    // backup's snapshot
    private static bool IsUnchangedSinceLastBackup(string databasePath)
    {
        var newest = ListBackups(databasePath).FirstOrDefault();
        if (newest is null)
        {
            return false;
        }

        var lastWrite = File.GetLastWriteTimeUtc(databasePath);
        var wal = $"{databasePath}-wal";
        if (File.Exists(wal) && File.GetLastWriteTimeUtc(wal) > lastWrite)
        {
            lastWrite = File.GetLastWriteTimeUtc(wal);
        }

        return lastWrite <= newest.LastWriteTimeUtc;
    }

    /// <summary>
    /// The database's backups, newest first.
    /// </summary>
    private static List<FileInfo> ListBackups(string databasePath)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(databasePath));
        var databaseFileName = Path.GetFileName(databasePath);
        if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
        {
            return [];
        }

        // The file name carries the timestamp, so it sorts by age
        return Directory.GetFiles(directory, $"{databaseFileName}.backup.*")
            .Select(f => new FileInfo(f))
            .OrderByDescending(f => f.Name, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// Keeps the newest <see cref="RecentBackupsToKeep"/> backups, then the newest backup of
    /// each earlier day for <see cref="DailyBackupsToKeep"/> days. A file whose name carries no
    /// readable timestamp is never deleted.
    /// </summary>
    private void CleanupOldBackups(string databasePath)
    {
        try
        {
            var prefix = $"{Path.GetFileName(databasePath)}.backup.";
            var older = ListBackups(databasePath)
                .Skip(RecentBackupsToKeep)
                .Select(file => (File: file, Taken: ParseTimestamp(file.Name, prefix)))
                .Where(backup => backup.Taken.HasValue)
                .ToList();

            var dailyKept = older
                .GroupBy(backup => backup.Taken!.Value.Date)
                .OrderByDescending(day => day.Key)
                .Take(DailyBackupsToKeep)
                .Select(day => day.First().File.FullName)
                .ToHashSet();

            var deleted = 0;
            foreach (var (file, _) in older.Where(backup => !dailyKept.Contains(backup.File.FullName)))
            {
                _logger.LogInformation("Deleting old backup: {BackupPath}", file.FullName);
                file.Delete();
                deleted++;
            }

            if (deleted > 0)
            {
                _logger.LogInformation(
                    "Cleaned up {Count} old backup(s). Keeping the newest {Recent} and one a day for {Days} days.",
                    deleted, RecentBackupsToKeep, DailyBackupsToKeep);
            }
        }
        catch (Exception ex)
        {
            // A failed clean-up never fails the backup, nor the start of the app
            _logger.LogWarning(ex, "Failed to cleanup old backups for {DatabasePath}", databasePath);
        }
    }

    private static DateTime? ParseTimestamp(string fileName, string prefix) =>
        fileName.StartsWith(prefix, StringComparison.Ordinal)
        && DateTime.TryParseExact(fileName[prefix.Length..], TimestampFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var taken)
            ? taken
            : null;
}
