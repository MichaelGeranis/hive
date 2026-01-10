using Hive.Core.Entities;

namespace Hive.Core.Interfaces;

public interface IChecklistInstanceItemRepository
{
    Task<ChecklistInstanceItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ChecklistInstanceItem>> GetByInstanceIdAsync(Guid instanceId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ChecklistInstanceItem>> GetPendingByInstanceIdAsync(Guid instanceId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ChecklistInstanceItem>> GetOverdueAsync(CancellationToken cancellationToken = default);
    Task<ChecklistInstanceItem> AddAsync(ChecklistInstanceItem item, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<ChecklistInstanceItem> items, CancellationToken cancellationToken = default);
    Task UpdateAsync(ChecklistInstanceItem item, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeleteByInstanceIdAsync(Guid instanceId, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}
