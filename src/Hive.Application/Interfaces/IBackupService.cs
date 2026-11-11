using Hive.Application.DTOs;

namespace Hive.Application.Interfaces;

/// <summary>
/// Service interface for backup and restore operations.
/// </summary>
public interface IBackupService
{
    Task<BackupDto> ExportAsync(CancellationToken cancellationToken = default);
    Task<RestoreResultDto> ImportAsync(BackupDto backup, bool clearExisting = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// The automatic backups of the database file: where, how many, and the last one.
    /// </summary>
    AutomaticBackupStatusDto GetAutomaticBackupStatus();
}
