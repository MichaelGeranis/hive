using Hive.Core.Entities;

namespace Hive.Core.Interfaces;

/// <summary>
/// Repository interface for Allocation entity.
/// </summary>
public interface IAllocationRepository
{
    Task<Allocation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Allocation>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Allocation>> GetByInitiativeAsync(Guid initiativeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Allocation>> GetByDirectReportAsync(Guid directReportId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Allocation>> GetBySprintAsync(Guid sprintId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Allocation>> GetByQuarterAsync(Guid quarterId, CancellationToken cancellationToken = default);
    Task<Allocation?> GetByInitiativeDirectReportSprintAsync(
        Guid initiativeId,
        Guid directReportId,
        Guid sprintId,
        CancellationToken cancellationToken = default);
    Task<Allocation> AddAsync(Allocation allocation, CancellationToken cancellationToken = default);
    Task UpdateAsync(Allocation allocation, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeleteByInitiativeAsync(Guid initiativeId, CancellationToken cancellationToken = default);
}
