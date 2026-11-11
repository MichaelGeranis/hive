using Hive.Application.DTOs;
using Hive.Application.Interfaces;

namespace Hive.Infrastructure.Persistence;

/// <summary>
/// The in-memory database has no file to back up, and so no automatic backups.
/// </summary>
public sealed class NoDatabaseBackups : IAutomaticBackups
{
    public AutomaticBackupStatusDto GetStatus() => new() { Enabled = false };
}
