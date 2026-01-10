using Hive.Core.Entities;
using Hive.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Hive.Infrastructure.Persistence.Repositories.Sqlite;

public class SqliteChecklistInstanceRepository : IChecklistInstanceRepository
{
    private readonly HiveDbContext _context;

    public SqliteChecklistInstanceRepository(HiveDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<ChecklistInstance?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ChecklistInstances.FindAsync([id], cancellationToken);
    }

    public async Task<IReadOnlyList<ChecklistInstance>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ChecklistInstances
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ChecklistInstance>> GetByTypeAsync(ChecklistType type, CancellationToken cancellationToken = default)
    {
        return await _context.ChecklistInstances
            .Where(x => x.Type == type)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ChecklistInstance>> GetByTemplateIdAsync(Guid templateId, CancellationToken cancellationToken = default)
    {
        return await _context.ChecklistInstances
            .Where(x => x.TemplateId == templateId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ChecklistInstance>> GetActiveAsync(ChecklistType? type = null, CancellationToken cancellationToken = default)
    {
        var query = _context.ChecklistInstances
            .Where(x => x.Status == ChecklistInstanceStatus.NotStarted
                        || x.Status == ChecklistInstanceStatus.InProgress);

        if (type.HasValue)
        {
            query = query.Where(x => x.Type == type.Value);
        }

        return await query
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<ChecklistInstance> AddAsync(ChecklistInstance instance, CancellationToken cancellationToken = default)
    {
        _context.ChecklistInstances.Add(instance);
        await _context.SaveChangesAsync(cancellationToken);
        return instance;
    }

    public async Task UpdateAsync(ChecklistInstance instance, CancellationToken cancellationToken = default)
    {
        _context.ChecklistInstances.Update(instance);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.ChecklistInstances.FindAsync([id], cancellationToken);
        if (entity != null)
        {
            _context.ChecklistInstances.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ChecklistInstances.AnyAsync(x => x.Id == id, cancellationToken);
    }
}
