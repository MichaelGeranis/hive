using Hive.Core.Entities;

namespace Hive.Core.Interfaces;

/// <summary>
/// Repository interface for SprintCapacity entity.
/// </summary>
public interface ISprintCapacityRepository
{
    Task<SprintCapacity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SprintCapacity?> GetBySprintIdAsync(Guid sprintId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SprintCapacity>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SprintCapacity>> GetBySprintIdsAsync(IEnumerable<Guid> sprintIds, CancellationToken cancellationToken = default);
    Task<SprintCapacity> AddAsync(SprintCapacity capacity, CancellationToken cancellationToken = default);
    Task UpdateAsync(SprintCapacity capacity, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsBySprintIdAsync(Guid sprintId, CancellationToken cancellationToken = default);
}
