using Microsoft.Extensions.Logging;

namespace Hive.Infrastructure.Persistence;

/// <summary>
/// Service for creating and managing SQLite database backups.
/// </summary>
public class DatabaseBackupService
{
    private readonly ILogger<DatabaseBackupService> _logger;
    private const int MaxBackupsToKeep = 10;

    public DatabaseBackupService(ILogger<DatabaseBackupService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Creates a timestamped backup of the database file.
    /// </summary>
    /// <param name="databasePath">Path to the database file to backup</param>
    /// <returns>True if backup was successful, false otherwise</returns>
    public bool CreateBackup(string databasePath)
    {
        try
        {
            if (!File.Exists(databasePath))
            {
                _logger.LogWarning("Database file not found at {DatabasePath}. Skipping backup.", databasePath);
                return false;
            }

            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            var backupPath = $"{databasePath}.backup.{timestamp}";

            _logger.LogInformation("Creating database backup: {BackupPath}", backupPath);

            // Copy the database file
            File.Copy(databasePath, backupPath, overwrite: false);

            _logger.LogInformation("Database backup created successfully: {BackupPath}", backupPath);

            // Clean up old backups
            CleanupOldBackups(databasePath);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create database backup for {DatabasePath}", databasePath);
            return false;
        }
    }

    /// <summary>
    /// Removes old backup files, keeping only the most recent ones.
    /// </summary>
    /// <param name="databasePath">Path to the database file</param>
    private void CleanupOldBackups(string databasePath)
    {
        try
        {
            var directory = Path.GetDirectoryName(databasePath);
            var databaseFileName = Path.GetFileName(databasePath);

            if (string.IsNullOrEmpty(directory) || string.IsNullOrEmpty(databaseFileName))
            {
                return;
            }

            // Find all backup files for this database
            var backupPattern = $"{databaseFileName}.backup.*";
            var backupFiles = Directory.GetFiles(directory, backupPattern)
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.Name) // Sort by filename (contains timestamp yyyy-MM-dd_HH-mm-ss)
                .ToList();

            // Keep only the most recent backups
            var backupsToDelete = backupFiles.Skip(MaxBackupsToKeep).ToList();

            foreach (var backup in backupsToDelete)
            {
                _logger.LogInformation("Deleting old backup: {BackupPath}", backup.FullName);
                backup.Delete();
            }

            if (backupsToDelete.Any())
            {
                _logger.LogInformation("Cleaned up {Count} old backup(s). Keeping {KeepCount} most recent.",
                    backupsToDelete.Count, MaxBackupsToKeep);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cleanup old backups for {DatabasePath}", databasePath);
            // Don't throw - cleanup failure shouldn't prevent app from starting
        }
    }
}
