using Hive.Core.Entities;
using Hive.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Hive.Infrastructure.Persistence.Repositories.Sqlite;

/// <summary>
/// SQLite implementation of IAppSettingsRepository.
/// </summary>
public class SqliteAppSettingsRepository : IAppSettingsRepository
{
    private readonly HiveDbContext _context;

    public SqliteAppSettingsRepository(HiveDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<AppSettings?> GetAsync(CancellationToken cancellationToken = default)
    {
        return await _context.AppSettings.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<AppSettings> AddAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        _context.AppSettings.Add(settings);
        await _context.SaveChangesAsync(cancellationToken);
        return settings;
    }

    public async Task UpdateAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        _context.AppSettings.Update(settings);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
