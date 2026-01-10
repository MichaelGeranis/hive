using Hive.Core.Entities;
using Hive.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Hive.Infrastructure.Persistence.Repositories.Sqlite;

public class SqliteChecklistTemplateItemRepository : IChecklistTemplateItemRepository
{
    private readonly HiveDbContext _context;

    public SqliteChecklistTemplateItemRepository(HiveDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<ChecklistTemplateItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ChecklistTemplateItems.FindAsync([id], cancellationToken);
    }

    public async Task<IReadOnlyList<ChecklistTemplateItem>> GetByTemplateIdAsync(Guid templateId, CancellationToken cancellationToken = default)
    {
        return await _context.ChecklistTemplateItems
            .Where(x => x.TemplateId == templateId)
            .OrderBy(x => x.SortOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task<ChecklistTemplateItem> AddAsync(ChecklistTemplateItem item, CancellationToken cancellationToken = default)
    {
        _context.ChecklistTemplateItems.Add(item);
        await _context.SaveChangesAsync(cancellationToken);
        return item;
    }

    public async Task UpdateAsync(ChecklistTemplateItem item, CancellationToken cancellationToken = default)
    {
        _context.ChecklistTemplateItems.Update(item);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.ChecklistTemplateItems.FindAsync([id], cancellationToken);
        if (entity != null)
        {
            _context.ChecklistTemplateItems.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task DeleteByTemplateIdAsync(Guid templateId, CancellationToken cancellationToken = default)
    {
        var items = await _context.ChecklistTemplateItems
            .Where(x => x.TemplateId == templateId)
            .ToListAsync(cancellationToken);

        if (items.Count > 0)
        {
            _context.ChecklistTemplateItems.RemoveRange(items);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ChecklistTemplateItems.AnyAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<int> GetNextSortOrderAsync(Guid templateId, CancellationToken cancellationToken = default)
    {
        var maxSortOrder = await _context.ChecklistTemplateItems
            .Where(x => x.TemplateId == templateId)
            .Select(x => (int?)x.SortOrder)
            .MaxAsync(cancellationToken);

        return (maxSortOrder ?? -1) + 1;
    }
}
