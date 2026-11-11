using Hive.Core.Entities;

namespace Hive.Core.Interfaces;

/// <summary>
/// Repository interface for the cached AI reviews of tasks. A task has at most one.
/// </summary>
public interface ITaskReadinessReviewRepository
{
    Task<TaskReadinessReview?> GetByTaskIdAsync(Guid teamTaskId, CancellationToken cancellationToken = default);

    /// <summary>
    /// The reviews of the given tasks; tasks without one are left out.
    /// </summary>
    Task<IReadOnlyList<TaskReadinessReview>> GetByTaskIdsAsync(IReadOnlyCollection<Guid> teamTaskIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Every review, for the backup.
    /// </summary>
    Task<IReadOnlyList<TaskReadinessReview>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<TaskReadinessReview> AddAsync(TaskReadinessReview review, CancellationToken cancellationToken = default);

    Task UpdateAsync(TaskReadinessReview review, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes the reviews of tasks that no longer exist.
    /// </summary>
    Task DeleteForMissingTasksAsync(IReadOnlyCollection<Guid> existingTaskIds, CancellationToken cancellationToken = default);
}
