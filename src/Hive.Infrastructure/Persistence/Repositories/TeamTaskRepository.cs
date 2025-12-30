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

    public Task<IReadOnlyList<TeamTask>> GetByMatchingLabelsAsync(IEnumerable<string> labels, CancellationToken cancellationToken = default)
    {
        var labelSet = labels
            .Select(l => l.Trim().ToLowerInvariant())
            .Where(l => !string.IsNullOrEmpty(l))
            .ToHashSet();

        if (labelSet.Count == 0)
        {
            return Task.FromResult<IReadOnlyList<TeamTask>>(new List<TeamTask>());
        }

        var entities = _context.TeamTasks.Values
            .Where(x => !string.IsNullOrEmpty(x.Labels) &&
                        x.Labels.Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(l => l.Trim().ToLowerInvariant())
                            .Any(taskLabel => labelSet.Contains(taskLabel)))
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
