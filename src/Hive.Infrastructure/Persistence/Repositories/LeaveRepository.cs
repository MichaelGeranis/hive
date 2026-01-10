using Hive.Core.Entities;
using Hive.Core.Interfaces;

namespace Hive.Infrastructure.Persistence.Repositories;

/// <summary>
/// In-memory implementation of ILeaveRepository.
/// </summary>
public class LeaveRepository : ILeaveRepository
{
    private readonly InMemoryDbContext _context;

    public LeaveRepository(InMemoryDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task<Leave?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _context.Leaves.TryGetValue(id, out var leave);
        return Task.FromResult(leave);
    }

    public Task<IReadOnlyList<Leave>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var leaves = _context.Leaves.Values
            .OrderByDescending(l => l.StartDate)
            .ToList();
        return Task.FromResult<IReadOnlyList<Leave>>(leaves);
    }

    public Task<IReadOnlyList<Leave>> GetByDirectReportIdAsync(Guid directReportId, CancellationToken cancellationToken = default)
    {
        var leaves = _context.Leaves.Values
            .Where(l => l.DirectReportId == directReportId)
            .OrderByDescending(l => l.StartDate)
            .ToList();
        return Task.FromResult<IReadOnlyList<Leave>>(leaves);
    }

    public Task<IReadOnlyList<Leave>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var leaves = _context.Leaves.Values
            .Where(l => l.OverlapsWith(startDate, endDate))
            .OrderByDescending(l => l.StartDate)
            .ToList();
        return Task.FromResult<IReadOnlyList<Leave>>(leaves);
    }

    public Task<IReadOnlyList<Leave>> GetByMonthAsync(int year, int month, CancellationToken cancellationToken = default)
    {
        var startOfMonth = new DateTime(year, month, 1);
        var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

        var leaves = _context.Leaves.Values
            .Where(l => l.OverlapsWith(startOfMonth, endOfMonth))
            .OrderByDescending(l => l.StartDate)
            .ToList();
        return Task.FromResult<IReadOnlyList<Leave>>(leaves);
    }

    public Task<IReadOnlyList<Leave>> GetUpcomingAsync(int days = 30, CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        var endDate = today.AddDays(days);

        var leaves = _context.Leaves.Values
            .Where(l => l.StartDate >= today && l.StartDate <= endDate)
            .OrderBy(l => l.StartDate)
            .ToList();
        return Task.FromResult<IReadOnlyList<Leave>>(leaves);
    }

    public Task<IReadOnlyList<Leave>> GetOverlappingLeavesAsync(
        Guid directReportId,
        DateTime startDate,
        DateTime endDate,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        var leaves = _context.Leaves.Values
            .Where(l => l.DirectReportId == directReportId
                && l.OverlapsWith(startDate, endDate)
                && (excludeId == null || l.Id != excludeId))
            .ToList();
        return Task.FromResult<IReadOnlyList<Leave>>(leaves);
    }

    public Task<Leave> AddAsync(Leave leave, CancellationToken cancellationToken = default)
    {
        _context.Leaves.TryAdd(leave.Id, leave);
        return Task.FromResult(leave);
    }

    public Task UpdateAsync(Leave leave, CancellationToken cancellationToken = default)
    {
        _context.Leaves[leave.Id] = leave;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _context.Leaves.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_context.Leaves.ContainsKey(id));
    }
}
