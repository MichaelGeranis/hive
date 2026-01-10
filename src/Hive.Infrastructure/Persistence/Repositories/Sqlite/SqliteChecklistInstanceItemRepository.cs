using Hive.Core.Entities;
using Hive.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Hive.Infrastructure.Persistence.Repositories.Sqlite;

public class SqliteChecklistInstanceItemRepository : IChecklistInstanceItemRepository
{
    private readonly HiveDbContext _context;

    public SqliteChecklistInstanceItemRepository(HiveDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<ChecklistInstanceItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ChecklistInstanceItems.FindAsync([id], cancellationToken);
    }

    public async Task<IReadOnlyList<ChecklistInstanceItem>> GetByInstanceIdAsync(Guid instanceId, CancellationToken cancellationToken = default)
    {
        return await _context.ChecklistInstanceItems
            .Where(x => x.InstanceId == instanceId)
            .OrderBy(x => x.SortOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ChecklistInstanceItem>> GetPendingByInstanceIdAsync(Guid instanceId, CancellationToken cancellationToken = default)
    {
        return await _context.ChecklistInstanceItems
            .Where(x => x.InstanceId == instanceId
                        && (x.Status == ChecklistItemStatus.Pending || x.Status == ChecklistItemStatus.InProgress))
            .OrderBy(x => x.SortOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ChecklistInstanceItem>> GetOverdueAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _context.ChecklistInstanceItems
            .Where(x => x.DueDate.HasValue
                        && x.DueDate < now
                        && x.Status != ChecklistItemStatus.Completed
                        && x.Status != ChecklistItemStatus.Skipped
                        && x.Status != ChecklistItemStatus.NotApplicable)
            .OrderBy(x => x.DueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<ChecklistInstanceItem> AddAsync(ChecklistInstanceItem item, CancellationToken cancellationToken = default)
    {
        _context.ChecklistInstanceItems.Add(item);
        await _context.SaveChangesAsync(cancellationToken);
        return item;
    }

    public async Task AddRangeAsync(IEnumerable<ChecklistInstanceItem> items, CancellationToken cancellationToken = default)
    {
        _context.ChecklistInstanceItems.AddRange(items);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(ChecklistInstanceItem item, CancellationToken cancellationToken = default)
    {
        _context.ChecklistInstanceItems.Update(item);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.ChecklistInstanceItems.FindAsync([id], cancellationToken);
        if (entity != null)
        {
            _context.ChecklistInstanceItems.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task DeleteByInstanceIdAsync(Guid instanceId, CancellationToken cancellationToken = default)
    {
        var items = await _context.ChecklistInstanceItems
            .Where(x => x.InstanceId == instanceId)
            .ToListAsync(cancellationToken);

        if (items.Count > 0)
        {
            _context.ChecklistInstanceItems.RemoveRange(items);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ChecklistInstanceItems.AnyAsync(x => x.Id == id, cancellationToken);
    }
}
