using Hive.Core.Entities;

namespace Hive.Core.Interfaces;

public interface IChecklistTemplateItemRepository
{
    Task<ChecklistTemplateItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ChecklistTemplateItem>> GetByTemplateIdAsync(Guid templateId, CancellationToken cancellationToken = default);
    Task<ChecklistTemplateItem> AddAsync(ChecklistTemplateItem item, CancellationToken cancellationToken = default);
    Task UpdateAsync(ChecklistTemplateItem item, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeleteByTemplateIdAsync(Guid templateId, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<int> GetNextSortOrderAsync(Guid templateId, CancellationToken cancellationToken = default);
}
