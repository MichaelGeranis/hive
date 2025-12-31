using Hive.Core.Entities;
using Hive.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Hive.Infrastructure.Persistence.Repositories.Sqlite;

/// <summary>
/// SQLite/EF Core implementation of ILeaveRepository.
/// </summary>
public class SqliteLeaveRepository : ILeaveRepository
{
    private readonly HiveDbContext _context;

    public SqliteLeaveRepository(HiveDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<Leave?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Leaves.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<IReadOnlyList<Leave>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Leaves
            .OrderByDescending(l => l.StartDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Leave>> GetByDirectReportIdAsync(Guid directReportId, CancellationToken cancellationToken = default)
    {
        return await _context.Leaves
            .Where(l => l.DirectReportId == directReportId)
            .OrderByDescending(l => l.StartDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Leave>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _context.Leaves
            .Where(l => l.StartDate <= endDate && l.EndDate >= startDate)
            .OrderByDescending(l => l.StartDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Leave>> GetByMonthAsync(int year, int month, CancellationToken cancellationToken = default)
    {
        var startOfMonth = new DateTime(year, month, 1);
        var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

        return await _context.Leaves
            .Where(l => l.StartDate <= endOfMonth && l.EndDate >= startOfMonth)
            .OrderByDescending(l => l.StartDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Leave>> GetUpcomingAsync(int days = 30, CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        var endDate = today.AddDays(days);

        return await _context.Leaves
            .Where(l => l.StartDate >= today && l.StartDate <= endDate)
            .OrderBy(l => l.StartDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Leave>> GetOverlappingLeavesAsync(
        Guid directReportId,
        DateTime startDate,
        DateTime endDate,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Leaves
            .Where(l => l.DirectReportId == directReportId
                && l.StartDate <= endDate
                && l.EndDate >= startDate);

        if (excludeId.HasValue)
        {
            query = query.Where(l => l.Id != excludeId.Value);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<Leave> AddAsync(Leave leave, CancellationToken cancellationToken = default)
    {
        _context.Leaves.Add(leave);
        await _context.SaveChangesAsync(cancellationToken);
        return leave;
    }

    public async Task UpdateAsync(Leave leave, CancellationToken cancellationToken = default)
    {
        _context.Leaves.Update(leave);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Leaves.FindAsync(new object[] { id }, cancellationToken);
        if (entity != null)
        {
            _context.Leaves.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Leaves.AnyAsync(l => l.Id == id, cancellationToken);
    }
}
