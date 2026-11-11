using Hive.Application.DTOs;

namespace Hive.Application.Interfaces;

/// <summary>
/// Whether the upcoming tasks are ready to build: facts checked by Hive, and an AI review of
/// each task, made one at a time so no request waits for the whole sprint.
/// </summary>
public interface ITaskReadinessService
{
    /// <summary>
    /// The upcoming tasks with their facts and reviews. Asks no model.
    /// </summary>
    Task<TaskReadinessOverviewDto> GetOverviewAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Reviews the first upcoming task with no up-to-date review, then returns the overview.
    /// A model failure never throws: the overview carries the error.
    /// </summary>
    Task<TaskReadinessOverviewDto> ReviewNextAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Reviews one task again, whatever its review says. Null when there is no such task.
    /// A model failure never throws: the overview carries the error.
    /// </summary>
    Task<TaskReadinessOverviewDto?> ReviewTaskAsync(Guid taskId, CancellationToken cancellationToken = default);
}
