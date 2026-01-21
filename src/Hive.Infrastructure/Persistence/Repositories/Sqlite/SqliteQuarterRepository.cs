using Hive.Core.Entities;
using Hive.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Hive.Infrastructure.Persistence.Repositories.Sqlite;

public class SqliteQuarterRepository : IQuarterRepository
{
    private readonly HiveDbContext _context;

    public SqliteQuarterRepository(HiveDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<Quarter?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Quarters.FindAsync([id], cancellationToken);
    }

    public async Task<Quarter?> GetByYearQuarterAsync(int year, int quarterNumber, CancellationToken cancellationToken = default)
    {
        return await _context.Quarters
            .FirstOrDefaultAsync(x => x.Year == year && x.QuarterNumber == quarterNumber, cancellationToken);
    }

    public async Task<IReadOnlyList<Quarter>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Quarters
            .OrderByDescending(x => x.Year)
            .ThenByDescending(x => x.QuarterNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Quarter>> GetByYearAsync(int year, CancellationToken cancellationToken = default)
    {
        return await _context.Quarters
            .Where(x => x.Year == year)
            .OrderBy(x => x.QuarterNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<Quarter?> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Quarters
            .FirstOrDefaultAsync(x => x.Status == QuarterStatus.Active, cancellationToken);
    }

    public async Task<Quarter> AddAsync(Quarter quarter, CancellationToken cancellationToken = default)
    {
        _context.Quarters.Add(quarter);
        await _context.SaveChangesAsync(cancellationToken);
        return quarter;
    }

    public async Task UpdateAsync(Quarter quarter, CancellationToken cancellationToken = default)
    {
        _context.Quarters.Update(quarter);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Quarters.FindAsync([id], cancellationToken);
        if (entity != null)
        {
            _context.Quarters.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> ExistsAsync(int year, int quarterNumber, CancellationToken cancellationToken = default)
    {
        return await _context.Quarters
            .AnyAsync(x => x.Year == year && x.QuarterNumber == quarterNumber, cancellationToken);
    }
}
