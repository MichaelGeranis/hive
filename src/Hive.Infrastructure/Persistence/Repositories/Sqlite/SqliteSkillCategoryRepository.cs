using Hive.Core.Entities;
using Hive.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Hive.Infrastructure.Persistence.Repositories.Sqlite;

public class SqliteSkillCategoryRepository : ISkillCategoryRepository
{
    private readonly HiveDbContext _context;

    public SqliteSkillCategoryRepository(HiveDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<SkillCategoryEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.SkillCategories.FindAsync([id], cancellationToken);
    }

    public async Task<SkillCategoryEntity?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var normalizedName = name.Trim().ToLower();
        return await _context.SkillCategories
            .FirstOrDefaultAsync(x => x.Name.ToLower() == normalizedName, cancellationToken);
    }

    public async Task<IReadOnlyList<SkillCategoryEntity>> GetAllAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var query = _context.SkillCategories.AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(x => x.IsActive);
        }

        return await query
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<SkillCategoryEntity> AddAsync(SkillCategoryEntity category, CancellationToken cancellationToken = default)
    {
        _context.SkillCategories.Add(category);
        await _context.SaveChangesAsync(cancellationToken);
        return category;
    }

    public async Task UpdateAsync(SkillCategoryEntity category, CancellationToken cancellationToken = default)
    {
        _context.SkillCategories.Update(category);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.SkillCategories.FindAsync([id], cancellationToken);
        if (entity != null)
        {
            _context.SkillCategories.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.SkillCategories.AnyAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<bool> NameExistsAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var normalizedName = name.Trim().ToLower();
        return await _context.SkillCategories
            .AnyAsync(x => x.Name.ToLower() == normalizedName && (!excludeId.HasValue || x.Id != excludeId.Value), cancellationToken);
    }

    public async Task<bool> HasSkillsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Skills.AnyAsync(s => s.SkillCategoryId == id, cancellationToken);
    }
}
