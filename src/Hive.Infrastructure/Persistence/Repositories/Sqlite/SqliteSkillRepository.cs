using Hive.Core.Entities;
using Hive.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Hive.Infrastructure.Persistence.Repositories.Sqlite;

public class SqliteSkillRepository : ISkillRepository
{
    private readonly HiveDbContext _context;

    public SqliteSkillRepository(HiveDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<Skill?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Skills.FindAsync([id], cancellationToken);
    }

    public async Task<Skill?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var normalizedName = name.Trim().ToLower();
        return await _context.Skills
            .FirstOrDefaultAsync(x => x.Name.ToLower() == normalizedName, cancellationToken);
    }

    public async Task<IReadOnlyList<Skill>> GetAllAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var query = _context.Skills.AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(x => x.IsActive);
        }

        // Join with categories to order by category sort order
        return await query
            .Join(_context.SkillCategories,
                skill => skill.SkillCategoryId,
                category => category.Id,
                (skill, category) => new { Skill = skill, Category = category })
            .OrderBy(x => x.Category.SortOrder)
            .ThenBy(x => x.Skill.Name)
            .Select(x => x.Skill)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Skill>> GetByCategoryIdAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        return await _context.Skills
            .Where(x => x.SkillCategoryId == categoryId && x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Skill> AddAsync(Skill skill, CancellationToken cancellationToken = default)
    {
        _context.Skills.Add(skill);
        await _context.SaveChangesAsync(cancellationToken);
        return skill;
    }

    public async Task UpdateAsync(Skill skill, CancellationToken cancellationToken = default)
    {
        _context.Skills.Update(skill);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Skills.FindAsync([id], cancellationToken);
        if (entity != null)
        {
            _context.Skills.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Skills.AnyAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<bool> NameExistsAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var normalizedName = name.Trim().ToLower();
        return await _context.Skills
            .AnyAsync(x => x.Name.ToLower() == normalizedName && (!excludeId.HasValue || x.Id != excludeId.Value), cancellationToken);
    }
}
