using Hive.Core.Entities;

namespace Hive.Core.Interfaces;

/// <summary>
/// Repository interface for Leave entity.
/// </summary>
public interface ILeaveRepository
{
    Task<Leave?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Leave>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Leave>> GetByDirectReportIdAsync(Guid directReportId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Leave>> GetByStatusAsync(LeaveStatus status, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Leave>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Leave>> GetByMonthAsync(int year, int month, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Leave>> GetUpcomingAsync(int days = 30, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Leave>> GetOverlappingLeavesAsync(Guid directReportId, DateTime startDate, DateTime endDate, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task<Leave> AddAsync(Leave leave, CancellationToken cancellationToken = default);
    Task UpdateAsync(Leave leave, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}
