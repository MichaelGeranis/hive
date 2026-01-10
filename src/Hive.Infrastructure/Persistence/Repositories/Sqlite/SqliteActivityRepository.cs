using Hive.Core.Entities;
using Hive.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Hive.Infrastructure.Persistence.Repositories.Sqlite;

/// <summary>
/// SQLite implementation of the Activity repository using Entity Framework Core.
/// </summary>
public class SqliteActivityRepository : IActivityRepository
{
    private readonly HiveDbContext _context;

    public SqliteActivityRepository(HiveDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<Activity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Activities.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<IReadOnlyList<Activity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Activities
            .AsNoTracking()
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Activity>> GetRecentAsync(int days, CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-days);
        return await _context.Activities
            .AsNoTracking()
            .Where(a => a.Timestamp >= cutoffDate)
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Activity>> GetByDateRangeAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        return await _context.Activities
            .AsNoTracking()
            .Where(a => a.Timestamp >= startDate && a.Timestamp <= endDate)
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Activity>> GetByEntityTypeAsync(
        EntityType entityType,
        CancellationToken cancellationToken = default)
    {
        return await _context.Activities
            .AsNoTracking()
            .Where(a => a.EntityType == entityType)
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync(cancellationToken);
    }

    public async Task<Activity> AddAsync(Activity activity, CancellationToken cancellationToken = default)
    {
        if (activity == null)
        {
            throw new ArgumentNullException(nameof(activity));
        }

        _context.Activities.Add(activity);
        await _context.SaveChangesAsync(cancellationToken);
        return activity;
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Activities.CountAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Activities.AnyAsync(a => a.Id == id, cancellationToken);
    }
}
