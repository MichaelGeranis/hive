using Hive.Core.Entities;
using Hive.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Hive.Infrastructure.Persistence.Repositories.Sqlite;

/// <summary>
/// SQLite/EF Core implementation of IDirectReportRepository.
/// </summary>
public class SqliteDirectReportRepository : IDirectReportRepository
{
    private readonly HiveDbContext _context;

    public SqliteDirectReportRepository(HiveDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<DirectReport?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.DirectReports.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<DirectReport?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        return await _context.DirectReports
            .FirstOrDefaultAsync(x => x.Email == normalizedEmail, cancellationToken);
    }

    public async Task<IReadOnlyList<DirectReport>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.DirectReports
            .OrderBy(x => x.LastName)
            .ThenBy(x => x.FirstName)
            .ToListAsync(cancellationToken);
    }

    public async Task<DirectReport> AddAsync(DirectReport directReport, CancellationToken cancellationToken = default)
    {
        _context.DirectReports.Add(directReport);
        await _context.SaveChangesAsync(cancellationToken);
        return directReport;
    }

    public async Task UpdateAsync(DirectReport directReport, CancellationToken cancellationToken = default)
    {
        _context.DirectReports.Update(directReport);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.DirectReports.FindAsync(new object[] { id }, cancellationToken);
        if (entity != null)
        {
            _context.DirectReports.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.DirectReports.AnyAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<bool> EmailExistsAsync(string email, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        return await _context.DirectReports
            .AnyAsync(x => x.Email == normalizedEmail && (!excludeId.HasValue || x.Id != excludeId.Value), cancellationToken);
    }
}
