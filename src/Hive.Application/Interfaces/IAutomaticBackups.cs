using Hive.Application.DTOs;

namespace Hive.Application.Interfaces;

/// <summary>
/// The automatic backups of the database: whether they run, where they go and when the last
/// one was taken. Implemented in Infrastructure, which takes them.
/// </summary>
public interface IAutomaticBackups
{
    AutomaticBackupStatusDto GetStatus();
}
