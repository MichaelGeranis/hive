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
    Task<(IReadOnlyList<TeamTask> Items, int TotalCount)> GetAllPagedAsync(int skip, int take, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<TeamTask> Items, int TotalCount, int TotalStoryPoints)> GetFilteredPagedAsync(
        int skip,
        int take,
        TeamTaskFilter filter,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TeamTask>> GetByAssigneeIdAsync(Guid assigneeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TeamTask>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TeamTask>> GetByParentIdAsync(Guid parentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TeamTask>> GetByStatusAsync(TaskStatus status, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TeamTask>> GetByPriorityAsync(TaskPriority priority, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TeamTask>> GetOverdueAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TeamTask>> GetUnassignedAsync(CancellationToken cancellationToken = default);
    Task<TeamTask> AddAsync(TeamTask task, CancellationToken cancellationToken = default);
    Task UpdateAsync(TeamTask task, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeleteManyAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}

/// <summary>
/// What a filtered task query keeps. Every criterion that is set must match.
/// </summary>
public record TeamTaskFilter
{
    public TaskStatus? Status { get; init; }
    public TaskType? Type { get; init; }

    /// <summary>
    /// Open tasks past their due date (<see cref="TeamTask.IsOverdue"/>).
    /// </summary>
    public bool Overdue { get; init; }

    /// <summary>
    /// In-flight tasks whose status has not changed since this moment (<see cref="TeamTask.IsStale"/>).
    /// </summary>
    public DateTime? StaleBefore { get; init; }

    public Guid? AssigneeId { get; init; }

    /// <summary>
    /// Only tasks nobody is assigned to. Ignored when <see cref="AssigneeId"/> is set.
    /// </summary>
    public bool Unassigned { get; init; }

    public string? SearchTerm { get; init; }
    public string? Label { get; init; }
    public string? Component { get; init; }

    /// <summary>
    /// Only tasks whose components name none of these products (<see cref="Product.IsNamedIn"/>):
    /// the tasks not attributed to a product. Not applied when null.
    /// </summary>
    public IReadOnlyCollection<Product>? UnattributedAmong { get; init; }

    /// <summary>
    /// Only these tasks. Not applied when null.
    /// </summary>
    public IReadOnlyCollection<Guid>? Ids { get; init; }

    public string? Sprint { get; init; }
    public IEnumerable<string>? ExcludeTaskTitles { get; init; }
}
