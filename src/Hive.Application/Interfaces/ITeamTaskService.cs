using Hive.Application.DTOs;
using Hive.Core.Entities;
using TaskStatus = Hive.Core.Entities.TaskStatus;

namespace Hive.Application.Interfaces;

/// <summary>
/// Service interface for TeamTask management.
/// </summary>
public interface ITeamTaskService
{
    Task<TeamTaskDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TeamTaskDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<TeamTaskDto>> GetAllPagedAsync(PaginationParams pagination, CancellationToken cancellationToken = default);
    Task<PagedResult<TeamTaskDto>> GetFilteredPagedAsync(TaskPaginationParams pagination, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TeamTaskDto>> GetByAssigneeIdAsync(Guid assigneeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TeamTaskDto>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TeamTaskDto>> GetByStatusAsync(TaskStatus status, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TeamTaskDto>> GetByPriorityAsync(TaskPriority priority, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TeamTaskDto>> GetOverdueAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TeamTaskDto>> GetUnassignedAsync(CancellationToken cancellationToken = default);
    Task<TaskSummaryDto> GetSummaryAsync(Guid? projectId = null, CancellationToken cancellationToken = default);
    Task<TeamTaskDto> CreateAsync(CreateTeamTaskDto dto, CancellationToken cancellationToken = default);
    Task<TeamTaskDto> UpdateAsync(Guid id, UpdateTeamTaskDto dto, CancellationToken cancellationToken = default);
    Task<TeamTaskDto> AssignAsync(Guid id, AssignTaskDto dto, CancellationToken cancellationToken = default);
    Task<TeamTaskDto> MoveToBacklogAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TeamTaskDto> MoveToTodoAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TeamTaskDto> StartAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TeamTaskDto> MoveToReviewAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TeamTaskDto> CompleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TeamTaskDto> CancelAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TeamTaskDto> ReopenAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TeamTaskDto> OverrideFieldsAsync(Guid id, OverrideTeamTaskFieldsDto dto, CancellationToken cancellationToken = default);
    Task<TeamTaskDto> ClearOverridesAsync(Guid id, ClearTeamTaskOverridesDto dto, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeleteManyAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
}
