using Hive.Core.Entities;
using TaskStatus = Hive.Core.Entities.TaskStatus;

namespace Hive.Core.Interfaces;

/// <summary>
/// Repository interface for TeamTask entity.
/// </summary>
public interface ITeamTaskRepository
{
    Task<TeamTask?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TeamTask>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TeamTask>> GetByAssigneeIdAsync(Guid assigneeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TeamTask>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TeamTask>> GetByStatusAsync(TaskStatus status, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TeamTask>> GetByPriorityAsync(TaskPriority priority, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TeamTask>> GetOverdueAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TeamTask>> GetUnassignedAsync(CancellationToken cancellationToken = default);
    Task<TeamTask> AddAsync(TeamTask task, CancellationToken cancellationToken = default);
    Task UpdateAsync(TeamTask task, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}
