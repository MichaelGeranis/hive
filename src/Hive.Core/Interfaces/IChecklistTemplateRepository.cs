using Hive.Core.Entities;

namespace Hive.Core.Interfaces;

public interface IChecklistTemplateRepository
{
    Task<ChecklistTemplate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ChecklistTemplate>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ChecklistTemplate>> GetByTypeAsync(ChecklistType type, bool includeInactive = false, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ChecklistTemplate>> GetActiveAsync(ChecklistType? type = null, CancellationToken cancellationToken = default);
    Task<ChecklistTemplate> AddAsync(ChecklistTemplate template, CancellationToken cancellationToken = default);
    Task UpdateAsync(ChecklistTemplate template, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> NameExistsAsync(string name, ChecklistType type, Guid? excludeId = null, CancellationToken cancellationToken = default);
}
