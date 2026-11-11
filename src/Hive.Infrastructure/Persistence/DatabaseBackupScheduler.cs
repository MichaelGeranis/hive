using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Hive.Infrastructure.Persistence;

/// <summary>
/// Backs the database up every <see cref="Interval"/> while the backend runs. The first
/// backup is the one <c>DatabaseInitializer</c> takes at startup, before the migrations.
/// </summary>
public sealed class DatabaseBackupScheduler : BackgroundService
{
    public static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    private readonly DatabaseBackupService _backups;
    private readonly string _databasePath;
    private readonly TimeSpan _interval;
    private readonly ILogger<DatabaseBackupScheduler> _logger;

    public DatabaseBackupScheduler(
        DatabaseBackupService backups,
        string databasePath,
        ILogger<DatabaseBackupScheduler> logger,
        TimeSpan? interval = null)
    {
        _backups = backups;
        _databasePath = databasePath;
        _logger = logger;
        _interval = interval ?? Interval;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_interval);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                // Off the timer's thread: a backup reads the whole database
                await Task.Run(() => _backups.CreateBackup(_databasePath), stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // The backend is stopping
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hourly database backups stopped");
        }
    }
}
