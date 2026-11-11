using Hive.Core.Entities;
using Hive.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Hive.Infrastructure.Persistence.Repositories.Sqlite;

/// <summary>
/// SQLite repository for the cached retro summary.
/// </summary>
public class SqliteRetroSummaryRepository : IRetroSummaryRepository
{
    private readonly HiveDbContext _context;

    public SqliteRetroSummaryRepository(HiveDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<RetroSummary?> GetAsync(CancellationToken cancellationToken = default)
    {
        return await _context.RetroSummaries
            .AsNoTracking()
            .OrderByDescending(s => s.AnalyzedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RetroSummary>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.RetroSummaries
            .AsNoTracking()
            .OrderByDescending(s => s.AnalyzedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<RetroSummary> AddAsync(RetroSummary summary, CancellationToken cancellationToken = default)
    {
        await _context.RetroSummaries.AddAsync(summary, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return summary;
    }

    public async Task UpdateAsync(RetroSummary summary, CancellationToken cancellationToken = default)
    {
        _context.RetroSummaries.UpdateUntracked(summary);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
