using Hive.Core.Entities;
using Hive.Core.Interfaces;

namespace Hive.Infrastructure.Persistence.Repositories;

public class ChecklistTemplateItemRepository : IChecklistTemplateItemRepository
{
    private readonly InMemoryDbContext _context;

    public ChecklistTemplateItemRepository(InMemoryDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task<ChecklistTemplateItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _context.ChecklistTemplateItems.TryGetValue(id, out var entity);
        return Task.FromResult(entity);
    }

    public Task<IReadOnlyList<ChecklistTemplateItem>> GetByTemplateIdAsync(Guid templateId, CancellationToken cancellationToken = default)
    {
        var entities = _context.ChecklistTemplateItems.Values
            .Where(x => x.TemplateId == templateId)
            .OrderBy(x => x.SortOrder)
            .ToList();

        return Task.FromResult<IReadOnlyList<ChecklistTemplateItem>>(entities);
    }

    public Task<ChecklistTemplateItem> AddAsync(ChecklistTemplateItem item, CancellationToken cancellationToken = default)
    {
        if (!_context.ChecklistTemplateItems.TryAdd(item.Id, item))
        {
            throw new InvalidOperationException($"ChecklistTemplateItem with id '{item.Id}' already exists.");
        }
        return Task.FromResult(item);
    }

    public Task UpdateAsync(ChecklistTemplateItem item, CancellationToken cancellationToken = default)
    {
        if (!_context.ChecklistTemplateItems.ContainsKey(item.Id))
        {
            throw new InvalidOperationException($"ChecklistTemplateItem with id '{item.Id}' not found.");
        }
        _context.ChecklistTemplateItems[item.Id] = item;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _context.ChecklistTemplateItems.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    public Task DeleteByTemplateIdAsync(Guid templateId, CancellationToken cancellationToken = default)
    {
        var itemIds = _context.ChecklistTemplateItems.Values
            .Where(x => x.TemplateId == templateId)
            .Select(x => x.Id)
            .ToList();

        foreach (var id in itemIds)
        {
            _context.ChecklistTemplateItems.TryRemove(id, out _);
        }

        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_context.ChecklistTemplateItems.ContainsKey(id));
    }

    public Task<int> GetNextSortOrderAsync(Guid templateId, CancellationToken cancellationToken = default)
    {
        var maxSortOrder = _context.ChecklistTemplateItems.Values
            .Where(x => x.TemplateId == templateId)
            .Select(x => x.SortOrder)
            .DefaultIfEmpty(-1)
            .Max();

        return Task.FromResult(maxSortOrder + 1);
    }
}
