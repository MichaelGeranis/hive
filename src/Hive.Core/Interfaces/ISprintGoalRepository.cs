using Hive.Core.Entities;

namespace Hive.Core.Interfaces;

/// <summary>
/// Repository interface for SprintGoal entity.
/// </summary>
public interface ISprintGoalRepository
{
    Task<SprintGoal?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SprintGoal?> GetByQuarterSprintAsync(Guid quarterId, Guid sprintId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SprintGoal>> GetByQuarterAsync(Guid quarterId, CancellationToken cancellationToken = default);
    Task<SprintGoal> AddAsync(SprintGoal sprintGoal, CancellationToken cancellationToken = default);
    Task UpdateAsync(SprintGoal sprintGoal, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
