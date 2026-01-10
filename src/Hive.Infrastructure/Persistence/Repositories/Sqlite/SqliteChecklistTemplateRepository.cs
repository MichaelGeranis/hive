using Hive.Core.Entities;
using Hive.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Hive.Infrastructure.Persistence.Repositories.Sqlite;

public class SqliteChecklistTemplateRepository : IChecklistTemplateRepository
{
    private readonly HiveDbContext _context;

    public SqliteChecklistTemplateRepository(HiveDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<ChecklistTemplate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ChecklistTemplates.FindAsync([id], cancellationToken);
    }

    public async Task<IReadOnlyList<ChecklistTemplate>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ChecklistTemplates
            .OrderBy(x => x.Type)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ChecklistTemplate>> GetByTypeAsync(ChecklistType type, bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var query = _context.ChecklistTemplates.Where(x => x.Type == type);

        if (!includeInactive)
        {
            query = query.Where(x => x.IsActive);
        }

        return await query
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ChecklistTemplate>> GetActiveAsync(ChecklistType? type = null, CancellationToken cancellationToken = default)
    {
        var query = _context.ChecklistTemplates.Where(x => x.IsActive);

        if (type.HasValue)
        {
            query = query.Where(x => x.Type == type.Value);
        }

        return await query
            .OrderBy(x => x.Type)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<ChecklistTemplate> AddAsync(ChecklistTemplate template, CancellationToken cancellationToken = default)
    {
        _context.ChecklistTemplates.Add(template);
        await _context.SaveChangesAsync(cancellationToken);
        return template;
    }

    public async Task UpdateAsync(ChecklistTemplate template, CancellationToken cancellationToken = default)
    {
        _context.ChecklistTemplates.Update(template);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.ChecklistTemplates.FindAsync([id], cancellationToken);
        if (entity != null)
        {
            _context.ChecklistTemplates.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ChecklistTemplates.AnyAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<bool> NameExistsAsync(string name, ChecklistType type, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.ChecklistTemplates
            .Where(x => x.Name.ToLower() == name.ToLower() && x.Type == type);

        if (excludeId.HasValue)
        {
            query = query.Where(x => x.Id != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }
}
