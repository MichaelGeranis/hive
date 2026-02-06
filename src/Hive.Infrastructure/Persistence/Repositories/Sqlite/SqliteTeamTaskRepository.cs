using Hive.Core.Entities;
using Hive.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using TaskStatus = Hive.Core.Entities.TaskStatus;

namespace Hive.Infrastructure.Persistence.Repositories.Sqlite;

public class SqliteTeamTaskRepository : ITeamTaskRepository
{
    private readonly HiveDbContext _context;

    public SqliteTeamTaskRepository(HiveDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<TeamTask?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.TeamTasks.FindAsync([id], cancellationToken);
    }

    public async Task<IReadOnlyList<TeamTask>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.TeamTasks
            .OrderByDescending(x => x.Priority)
            .ThenBy(x => x.DueDate)
            .ThenBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<TeamTask> Items, int TotalCount)> GetAllPagedAsync(int skip, int take, CancellationToken cancellationToken = default)
    {
        var query = _context.TeamTasks
            .OrderByDescending(x => x.Sprint)
            .ThenByDescending(x => x.Priority)
            .ThenBy(x => x.DueDate)
            .ThenBy(x => x.CreatedAt);

        var totalCount = await _context.TeamTasks.CountAsync(cancellationToken);
        var items = await query.Skip(skip).Take(take).ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<(IReadOnlyList<TeamTask> Items, int TotalCount, int TotalStoryPoints, int TotalEstimatedHours, int TotalTimeSpentMinutes)> GetFilteredPagedAsync(
        int skip,
        int take,
        TaskStatus? status = null,
        bool? overdue = null,
        string? searchTerm = null,
        string? label = null,
        string? sprint = null,
        IEnumerable<string>? excludeTaskTitles = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<TeamTask> query = _context.TeamTasks;

        // Apply parent task exclusion
        if (excludeTaskTitles != null)
        {
            var excludeList = excludeTaskTitles.ToList();
            if (excludeList.Count > 0)
            {
                query = query.Where(x => !excludeList.Contains(x.Title));
            }
        }

        // Apply status filter
        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        // Apply overdue filter
        if (overdue == true)
        {
            var now = DateTime.UtcNow;
            query = query.Where(x => x.DueDate.HasValue
                                     && x.DueDate < now
                                     && x.Status != TaskStatus.Done
                                     && x.Status != TaskStatus.Cancelled);
        }

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLowerInvariant();
            query = query.Where(x =>
                x.Title.ToLower().Contains(term) ||
                (x.Description != null && x.Description.ToLower().Contains(term)) ||
                (x.Labels != null && x.Labels.ToLower().Contains(term)) ||
                (x.Sprint != null && x.Sprint.ToLower().Contains(term)) ||
                (x.Tags != null && x.Tags.ToLower().Contains(term)));
        }

        // Apply sprint filter
        if (!string.IsNullOrWhiteSpace(sprint))
        {
            query = query.Where(x => x.Sprint != null && x.Sprint.Trim() == sprint.Trim());
        }

        // Apply label filter and compute aggregates
        List<TeamTask> filteredList;
        if (!string.IsNullOrWhiteSpace(label))
        {
            var labelLower = label.Trim().ToLowerInvariant();
            // Fetch and filter in memory for comma-separated label matching
            var allItems = await query
                .OrderByDescending(x => x.Sprint)
                .ThenByDescending(x => x.Priority)
                .ThenBy(x => x.DueDate)
                .ThenBy(x => x.CreatedAt)
                .ToListAsync(cancellationToken);

            filteredList = allItems
                .Where(x => !string.IsNullOrEmpty(x.Labels) &&
                            x.Labels.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                .Select(l => l.Trim().ToLowerInvariant())
                                .Contains(labelLower))
                .ToList();
        }
        else
        {
            filteredList = await query
                .OrderByDescending(x => x.Sprint)
                .ThenByDescending(x => x.Priority)
                .ThenBy(x => x.DueDate)
                .ThenBy(x => x.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        var totalCount = filteredList.Count;
        var totalStoryPoints = filteredList.Sum(x => x.StoryPoints ?? 0);
        var totalEstimatedHours = filteredList.Sum(x => x.EstimatedHours ?? 0);
        var totalTimeSpentMinutes = filteredList.Sum(x => x.TimeSpentMinutes ?? 0);
        var items = filteredList.Skip(skip).Take(take).ToList();

        return (items, totalCount, totalStoryPoints, totalEstimatedHours, totalTimeSpentMinutes);
    }

    public async Task<IReadOnlyList<TeamTask>> GetByAssigneeIdAsync(Guid assigneeId, CancellationToken cancellationToken = default)
    {
        return await _context.TeamTasks
            .Where(x => x.AssigneeId == assigneeId)
            .OrderByDescending(x => x.Priority)
            .ThenBy(x => x.DueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TeamTask>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        return await _context.TeamTasks
            .Where(x => x.ProjectId == projectId)
            .OrderByDescending(x => x.Priority)
            .ThenBy(x => x.DueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TeamTask>> GetByParentIdAsync(Guid parentId, CancellationToken cancellationToken = default)
    {
        return await _context.TeamTasks
            .Where(x => x.ParentId == parentId)
            .OrderByDescending(x => x.Priority)
            .ThenBy(x => x.DueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TeamTask>> GetByMatchingLabelsAsync(IEnumerable<string> labels, CancellationToken cancellationToken = default)
    {
        var labelSet = labels
            .Select(l => l.Trim().ToLowerInvariant())
            .Where(l => !string.IsNullOrEmpty(l))
            .ToHashSet();

        if (labelSet.Count == 0)
        {
            return new List<TeamTask>();
        }

        // Fetch tasks with labels and filter in memory for comma-separated matching
        var tasksWithLabels = await _context.TeamTasks
            .Where(x => x.Labels != null && x.Labels != "")
            .ToListAsync(cancellationToken);

        return tasksWithLabels
            .Where(x => x.Labels.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Trim().ToLowerInvariant())
                .Any(taskLabel => labelSet.Contains(taskLabel)))
            .OrderByDescending(x => x.Priority)
            .ThenBy(x => x.DueDate)
            .ToList();
    }

    public async Task<IReadOnlyList<TeamTask>> GetByStatusAsync(TaskStatus status, CancellationToken cancellationToken = default)
    {
        return await _context.TeamTasks
            .Where(x => x.Status == status)
            .OrderByDescending(x => x.Priority)
            .ThenBy(x => x.DueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TeamTask>> GetByPriorityAsync(TaskPriority priority, CancellationToken cancellationToken = default)
    {
        return await _context.TeamTasks
            .Where(x => x.Priority == priority)
            .OrderBy(x => x.DueDate)
            .ThenBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TeamTask>> GetOverdueAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _context.TeamTasks
            .Where(x => x.DueDate.HasValue
                        && x.DueDate < now
                        && x.Status != TaskStatus.Done
                        && x.Status != TaskStatus.Cancelled)
            .OrderBy(x => x.DueDate)
            .ThenByDescending(x => x.Priority)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TeamTask>> GetUnassignedAsync(CancellationToken cancellationToken = default)
    {
        return await _context.TeamTasks
            .Where(x => !x.AssigneeId.HasValue && x.Status != TaskStatus.Done && x.Status != TaskStatus.Cancelled)
            .OrderByDescending(x => x.Priority)
            .ThenBy(x => x.DueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<TeamTask> AddAsync(TeamTask task, CancellationToken cancellationToken = default)
    {
        _context.TeamTasks.Add(task);
        await _context.SaveChangesAsync(cancellationToken);
        return task;
    }

    public async Task UpdateAsync(TeamTask task, CancellationToken cancellationToken = default)
    {
        _context.TeamTasks.Update(task);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.TeamTasks.FindAsync([id], cancellationToken);
        if (entity != null)
        {
            _context.TeamTasks.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task DeleteManyAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var idsList = ids.ToList();
        var entities = await _context.TeamTasks
            .Where(x => idsList.Contains(x.Id))
            .ToListAsync(cancellationToken);

        if (entities.Count > 0)
        {
            _context.TeamTasks.RemoveRange(entities);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.TeamTasks.AnyAsync(x => x.Id == id, cancellationToken);
    }
}
