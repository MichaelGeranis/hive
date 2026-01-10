using Hive.Core.Entities;

namespace Hive.Core.Interfaces;

public interface IChecklistInstanceRepository
{
    Task<ChecklistInstance?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ChecklistInstance>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ChecklistInstance>> GetByTypeAsync(ChecklistType type, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ChecklistInstance>> GetByTemplateIdAsync(Guid templateId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ChecklistInstance>> GetActiveAsync(ChecklistType? type = null, CancellationToken cancellationToken = default);
    Task<ChecklistInstance> AddAsync(ChecklistInstance instance, CancellationToken cancellationToken = default);
    Task UpdateAsync(ChecklistInstance instance, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}
