using Hive.Application.DTOs;
using Hive.Application.Interfaces;

namespace Hive.Infrastructure.Persistence;

/// <summary>
/// The automatic backups of the SQLite database, read from the backup files beside it.
/// </summary>
public sealed class DatabaseBackupStatus(DatabaseBackupService backups, string databasePath) : IAutomaticBackups
{
    public AutomaticBackupStatusDto GetStatus()
    {
        var (folder, count, lastBackupAt) = backups.GetStatus(databasePath);
        return new AutomaticBackupStatusDto
        {
            Enabled = true,
            Folder = folder,
            Count = count,
            LastBackupAt = lastBackupAt,
            IntervalHours = (int)DatabaseBackupScheduler.Interval.TotalHours
        };
    }
}
