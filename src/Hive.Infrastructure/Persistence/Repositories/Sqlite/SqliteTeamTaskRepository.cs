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
            .AsNoTracking()
            .OrderByDescending(x => x.Priority)
            .ThenBy(x => x.DueDate)
            .ThenBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<TeamTask> Items, int TotalCount)> GetAllPagedAsync(int skip, int take, CancellationToken cancellationToken = default)
    {
        var query = _context.TeamTasks
            .AsNoTracking()
            .OrderByDescending(x => x.Sprint)
            .ThenByDescending(x => x.Priority)
            .ThenBy(x => x.DueDate)
            .ThenBy(x => x.CreatedAt);

        var totalCount = await _context.TeamTasks.CountAsync(cancellationToken);
        var items = await query.Skip(skip).Take(take).ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<(IReadOnlyList<TeamTask> Items, int TotalCount, int TotalStoryPoints)> GetFilteredPagedAsync(
        int skip,
        int take,
        TeamTaskFilter filter,
        CancellationToken cancellationToken = default)
    {
        IQueryable<TeamTask> query = _context.TeamTasks.AsNoTracking();

        // Apply parent task exclusion
        if (filter.ExcludeTaskTitles != null)
        {
            var excludeList = filter.ExcludeTaskTitles.ToList();
            if (excludeList.Count > 0)
            {
                query = query.Where(x => !excludeList.Contains(x.Title));
            }
        }

        if (filter.Ids != null)
        {
            var ids = filter.Ids.ToList();
            query = query.Where(x => ids.Contains(x.Id));
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
            var now = DateTime.UtcNow;
            query = query.Where(x => x.DueDate.HasValue
                                     && x.DueDate < now
                                     && x.Status != TaskStatus.Done
                                     && x.Status != TaskStatus.Cancelled);
        }

        if (filter.StaleBefore.HasValue)
        {
            // TeamTask.IsStale in SQL
            var cutoff = filter.StaleBefore.Value;
            var inFlight = TeamTask.InFlightStatuses.ToList();
            query = query.Where(x => inFlight.Contains(x.Status) && x.StatusChangedAt <= cutoff);
        }

        if (filter.AssigneeId.HasValue)
        {
            query = query.Where(x => x.AssigneeId == filter.AssigneeId.Value);
        }
        else if (filter.Unassigned)
        {
            query = query.Where(x => x.AssigneeId == null);
        }

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.ToLowerInvariant();
            query = query.Where(x =>
                x.Title.ToLower().Contains(term) ||
                (x.Description != null && x.Description.ToLower().Contains(term)) ||
                (x.Labels != null && x.Labels.ToLower().Contains(term)) ||
                (x.Sprint != null && x.Sprint.ToLower().Contains(term)) ||
                (x.Tags != null && x.Tags.ToLower().Contains(term)));
        }

        // Apply sprint filter
        if (!string.IsNullOrWhiteSpace(filter.Sprint))
        {
            // Any of the task's sprints, so work carried over is listed in each sprint it was in.
            // TeamTask.IsInSprint in SQL: the comma-separated list, wrapped in commas, contains
            // the name wrapped in commas, ignoring case and spaces.
            var needle = "," + filter.Sprint.Replace(" ", string.Empty).ToLower() + ",";
            query = query.Where(x => x.Sprint != null &&
                ("," + x.Sprint.Replace(" ", string.Empty).ToLower() + ",").Contains(needle));
        }

        IEnumerable<TeamTask> filteredItems = await query
            .OrderByDescending(x => x.Sprint)
            .ThenByDescending(x => x.Priority)
            .ThenBy(x => x.DueDate)
            .ThenBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        // Labels and components are comma-separated lists, matched in memory
        if (!string.IsNullOrWhiteSpace(filter.Label))
        {
            filteredItems = filteredItems.Where(x => x.HasLabel(filter.Label));
        }

        if (!string.IsNullOrWhiteSpace(filter.Component))
        {
            filteredItems = filteredItems.Where(x => x.HasComponent(filter.Component));
        }

        if (filter.UnattributedAmong != null)
        {
            var products = filter.UnattributedAmong;
            filteredItems = filteredItems.Where(x => !products.Any(p => p.IsNamedIn(x.Components)));
        }

        var filteredList = filteredItems.ToList();
        var totalCount = filteredList.Count;
        var totalStoryPoints = filteredList.Sum(x => x.StoryPoints ?? 0);
        var items = filteredList.Skip(skip).Take(take).ToList();

        return (items, totalCount, totalStoryPoints);
    }

    public async Task<IReadOnlyList<TeamTask>> GetByAssigneeIdAsync(Guid assigneeId, CancellationToken cancellationToken = default)
    {
        return await _context.TeamTasks
            .AsNoTracking()
            .Where(x => x.AssigneeId == assigneeId)
            .OrderByDescending(x => x.Priority)
            .ThenBy(x => x.DueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TeamTask>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        return await _context.TeamTasks
            .AsNoTracking()
            .Where(x => x.ProjectId == projectId)
            .OrderByDescending(x => x.Priority)
            .ThenBy(x => x.DueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TeamTask>> GetByParentIdAsync(Guid parentId, CancellationToken cancellationToken = default)
    {
        return await _context.TeamTasks
            .AsNoTracking()
            .Where(x => x.ParentId == parentId)
            .OrderByDescending(x => x.Priority)
            .ThenBy(x => x.DueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TeamTask>> GetByStatusAsync(TaskStatus status, CancellationToken cancellationToken = default)
    {
        return await _context.TeamTasks
            .AsNoTracking()
            .Where(x => x.Status == status)
            .OrderByDescending(x => x.Priority)
            .ThenBy(x => x.DueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TeamTask>> GetByPriorityAsync(TaskPriority priority, CancellationToken cancellationToken = default)
    {
        return await _context.TeamTasks
            .AsNoTracking()
            .Where(x => x.Priority == priority)
            .OrderBy(x => x.DueDate)
            .ThenBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TeamTask>> GetOverdueAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _context.TeamTasks
            .AsNoTracking()
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
            .AsNoTracking()
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
        _context.TeamTasks.UpdateUntracked(task);
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
