using Hive.Core.Entities;
using Hive.Core.Interfaces;

namespace Hive.Infrastructure.Persistence.Repositories;

public class ChecklistInstanceItemRepository : IChecklistInstanceItemRepository
{
    private readonly InMemoryDbContext _context;

    public ChecklistInstanceItemRepository(InMemoryDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task<ChecklistInstanceItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _context.ChecklistInstanceItems.TryGetValue(id, out var entity);
        return Task.FromResult(entity);
    }

    public Task<IReadOnlyList<ChecklistInstanceItem>> GetByInstanceIdAsync(Guid instanceId, CancellationToken cancellationToken = default)
    {
        var entities = _context.ChecklistInstanceItems.Values
            .Where(x => x.InstanceId == instanceId)
            .OrderBy(x => x.SortOrder)
            .ToList();

        return Task.FromResult<IReadOnlyList<ChecklistInstanceItem>>(entities);
    }

    public Task<IReadOnlyList<ChecklistInstanceItem>> GetPendingByInstanceIdAsync(Guid instanceId, CancellationToken cancellationToken = default)
    {
        var entities = _context.ChecklistInstanceItems.Values
            .Where(x => x.InstanceId == instanceId
                        && (x.Status == ChecklistItemStatus.Pending || x.Status == ChecklistItemStatus.InProgress))
            .OrderBy(x => x.SortOrder)
            .ToList();

        return Task.FromResult<IReadOnlyList<ChecklistInstanceItem>>(entities);
    }

    public Task<IReadOnlyList<ChecklistInstanceItem>> GetOverdueAsync(CancellationToken cancellationToken = default)
    {
        var entities = _context.ChecklistInstanceItems.Values
            .Where(x => x.IsOverdue())
            .OrderBy(x => x.DueDate)
            .ToList();

        return Task.FromResult<IReadOnlyList<ChecklistInstanceItem>>(entities);
    }

    public Task<ChecklistInstanceItem> AddAsync(ChecklistInstanceItem item, CancellationToken cancellationToken = default)
    {
        if (!_context.ChecklistInstanceItems.TryAdd(item.Id, item))
        {
            throw new InvalidOperationException($"ChecklistInstanceItem with id '{item.Id}' already exists.");
        }
        return Task.FromResult(item);
    }

    public Task AddRangeAsync(IEnumerable<ChecklistInstanceItem> items, CancellationToken cancellationToken = default)
    {
        foreach (var item in items)
        {
            if (!_context.ChecklistInstanceItems.TryAdd(item.Id, item))
            {
                throw new InvalidOperationException($"ChecklistInstanceItem with id '{item.Id}' already exists.");
            }
        }
        return Task.CompletedTask;
    }

    public Task UpdateAsync(ChecklistInstanceItem item, CancellationToken cancellationToken = default)
    {
        if (!_context.ChecklistInstanceItems.ContainsKey(item.Id))
        {
            throw new InvalidOperationException($"ChecklistInstanceItem with id '{item.Id}' not found.");
        }
        _context.ChecklistInstanceItems[item.Id] = item;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _context.ChecklistInstanceItems.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    public Task DeleteByInstanceIdAsync(Guid instanceId, CancellationToken cancellationToken = default)
    {
        var itemIds = _context.ChecklistInstanceItems.Values
            .Where(x => x.InstanceId == instanceId)
            .Select(x => x.Id)
            .ToList();

        foreach (var id in itemIds)
        {
            _context.ChecklistInstanceItems.TryRemove(id, out _);
        }

        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_context.ChecklistInstanceItems.ContainsKey(id));
    }
}
