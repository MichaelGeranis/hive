using Hive.Core.Entities;
using Hive.Core.Interfaces;
using TaskStatus = Hive.Core.Entities.TaskStatus;

namespace Hive.Infrastructure.Persistence.Repositories;

/// <summary>
/// In-memory implementation of ITeamTaskRepository.
/// </summary>
public class TeamTaskRepository : ITeamTaskRepository
{
    private readonly InMemoryDbContext _context;

    public TeamTaskRepository(InMemoryDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task<TeamTask?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _context.TeamTasks.TryGetValue(id, out var entity);
        return Task.FromResult(entity);
    }

    public Task<IReadOnlyList<TeamTask>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = _context.TeamTasks.Values
            .OrderByDescending(x => x.Priority)
            .ThenBy(x => x.DueDate)
            .ThenBy(x => x.CreatedAt)
            .ToList();
        return Task.FromResult<IReadOnlyList<TeamTask>>(entities);
    }

    public Task<(IReadOnlyList<TeamTask> Items, int TotalCount)> GetAllPagedAsync(int skip, int take, CancellationToken cancellationToken = default)
    {
        var query = _context.TeamTasks.Values
            .OrderByDescending(x => x.Sprint)
            .ThenByDescending(x => x.Priority)
            .ThenBy(x => x.DueDate)
            .ThenBy(x => x.CreatedAt);

        var totalCount = _context.TeamTasks.Count;
        var items = query.Skip(skip).Take(take).ToList();

        return Task.FromResult<(IReadOnlyList<TeamTask>, int)>((items, totalCount));
    }

    public Task<(IReadOnlyList<TeamTask> Items, int TotalCount, int TotalStoryPoints)> GetFilteredPagedAsync(
        int skip,
        int take,
        TeamTaskFilter filter,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<TeamTask> query = _context.TeamTasks.Values;

        // Apply parent task exclusion
        if (filter.ExcludeTaskTitles != null)
        {
            var excludeSet = new HashSet<string>(filter.ExcludeTaskTitles, StringComparer.OrdinalIgnoreCase);
            query = query.Where(x => !excludeSet.Contains(x.Title));
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(x => x.Status == filter.Status.Value);
        }

        if (filter.Type.HasValue)
        {
            query = query.Where(x => x.Type == filter.Type.Value);
        }

        if (filter.Overdue)
        {
            query = query.Where(x => x.IsOverdue());
        }

        if (filter.StaleBefore.HasValue)
        {
            var cutoff = filter.StaleBefore.Value;
            query = query.Where(x => TeamTask.InFlightStatuses.Contains(x.Status) && x.StatusChangedAt <= cutoff);
        }

        if (filter.AssigneeId.HasValue)
        {
            query = query.Where(x => x.AssigneeId == filter.AssigneeId.Value);
        }
        else if (filter.Unassigned)
        {
            query = query.Where(x => !x.AssigneeId.HasValue);
        }

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.ToLowerInvariant();
            query = query.Where(x =>
                x.Title.ToLowerInvariant().Contains(term) ||
                (x.Description?.ToLowerInvariant().Contains(term) ?? false) ||
                (x.Labels?.ToLowerInvariant().Contains(term) ?? false) ||
                (x.Sprint?.ToLowerInvariant().Contains(term) ?? false) ||
                (x.Tags?.ToLowerInvariant().Contains(term) ?? false));
        }

        if (!string.IsNullOrWhiteSpace(filter.Label))
        {
            query = query.Where(x => x.HasLabel(filter.Label));
        }

        if (!string.IsNullOrWhiteSpace(filter.Component))
        {
            query = query.Where(x => x.HasComponent(filter.Component));
        }

        if (filter.Ids != null)
        {
            var ids = filter.Ids.ToHashSet();
            query = query.Where(x => ids.Contains(x.Id));
        }

        if (filter.UnattributedAmong != null)
        {
            var products = filter.UnattributedAmong;
            query = query.Where(x => !products.Any(p => p.IsNamedIn(x.Components)));
        }

        // Apply sprint filter
        if (!string.IsNullOrWhiteSpace(filter.Sprint))
        {
            // Any of the task's sprints, so work carried over is listed in each sprint it was in
            query = query.Where(x => x.IsInSprint(filter.Sprint));
        }

        // Order and paginate
        var orderedQuery = query
            .OrderByDescending(x => x.Sprint)
            .ThenByDescending(x => x.Priority)
            .ThenBy(x => x.DueDate)
            .ThenBy(x => x.CreatedAt);

        // Materialize filtered list for aggregates
        var filteredList = orderedQuery.ToList();
        var totalCount = filteredList.Count;
        var totalStoryPoints = filteredList.Sum(x => x.StoryPoints ?? 0);
        var items = filteredList.Skip(skip).Take(take).ToList();

        return Task.FromResult<(IReadOnlyList<TeamTask>, int, int)>((items, totalCount, totalStoryPoints));
    }

    public Task<IReadOnlyList<TeamTask>> GetByAssigneeIdAsync(Guid assigneeId, CancellationToken cancellationToken = default)
    {
        var entities = _context.TeamTasks.Values
            .Where(x => x.AssigneeId == assigneeId)
            .OrderByDescending(x => x.Priority)
            .ThenBy(x => x.DueDate)
            .ToList();
        return Task.FromResult<IReadOnlyList<TeamTask>>(entities);
    }

    public Task<IReadOnlyList<TeamTask>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var entities = _context.TeamTasks.Values
            .Where(x => x.ProjectId == projectId)
            .OrderByDescending(x => x.Priority)
            .ThenBy(x => x.DueDate)
            .ToList();
        return Task.FromResult<IReadOnlyList<TeamTask>>(entities);
    }

    public Task<IReadOnlyList<TeamTask>> GetByParentIdAsync(Guid parentId, CancellationToken cancellationToken = default)
    {
        var entities = _context.TeamTasks.Values
            .Where(x => x.ParentId == parentId)
            .OrderByDescending(x => x.Priority)
            .ThenBy(x => x.DueDate)
            .ToList();
        return Task.FromResult<IReadOnlyList<TeamTask>>(entities);
    }

    public Task<IReadOnlyList<TeamTask>> GetByStatusAsync(TaskStatus status, CancellationToken cancellationToken = default)
    {
        var entities = _context.TeamTasks.Values
            .Where(x => x.Status == status)
            .OrderByDescending(x => x.Priority)
            .ThenBy(x => x.DueDate)
            .ToList();
        return Task.FromResult<IReadOnlyList<TeamTask>>(entities);
    }

    public Task<IReadOnlyList<TeamTask>> GetByPriorityAsync(TaskPriority priority, CancellationToken cancellationToken = default)
    {
        var entities = _context.TeamTasks.Values
            .Where(x => x.Priority == priority)
            .OrderBy(x => x.DueDate)
            .ThenBy(x => x.CreatedAt)
            .ToList();
        return Task.FromResult<IReadOnlyList<TeamTask>>(entities);
    }

    public Task<IReadOnlyList<TeamTask>> GetOverdueAsync(CancellationToken cancellationToken = default)
    {
        var entities = _context.TeamTasks.Values
            .Where(x => x.IsOverdue())
            .OrderBy(x => x.DueDate)
            .ThenByDescending(x => x.Priority)
            .ToList();
        return Task.FromResult<IReadOnlyList<TeamTask>>(entities);
    }

    public Task<IReadOnlyList<TeamTask>> GetUnassignedAsync(CancellationToken cancellationToken = default)
    {
        var entities = _context.TeamTasks.Values
            .Where(x => !x.AssigneeId.HasValue && x.Status != TaskStatus.Done && x.Status != TaskStatus.Cancelled)
            .OrderByDescending(x => x.Priority)
            .ThenBy(x => x.DueDate)
            .ToList();
        return Task.FromResult<IReadOnlyList<TeamTask>>(entities);
    }

    public Task<TeamTask> AddAsync(TeamTask task, CancellationToken cancellationToken = default)
    {
        if (!_context.TeamTasks.TryAdd(task.Id, task))
        {
            throw new InvalidOperationException($"Task with id '{task.Id}' already exists.");
        }
        return Task.FromResult(task);
    }

    public Task UpdateAsync(TeamTask task, CancellationToken cancellationToken = default)
    {
        if (!_context.TeamTasks.ContainsKey(task.Id))
        {
            throw new InvalidOperationException($"Task with id '{task.Id}' not found.");
        }
        _context.TeamTasks[task.Id] = task;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _context.TeamTasks.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    public Task DeleteManyAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        foreach (var id in ids)
        {
            _context.TeamTasks.TryRemove(id, out _);
        }
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_context.TeamTasks.ContainsKey(id));
    }
}
