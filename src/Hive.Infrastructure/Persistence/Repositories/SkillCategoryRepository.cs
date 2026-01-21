using Hive.Core.Entities;
using Hive.Core.Interfaces;

namespace Hive.Infrastructure.Persistence.Repositories;

/// <summary>
/// In-memory implementation of ISkillCategoryRepository.
/// </summary>
public class SkillCategoryRepository : ISkillCategoryRepository
{
    private readonly InMemoryDbContext _context;

    public SkillCategoryRepository(InMemoryDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task<SkillCategoryEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _context.SkillCategories.TryGetValue(id, out var entity);
        return Task.FromResult(entity);
    }

    public Task<SkillCategoryEntity?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var normalizedName = name.Trim();
        var entity = _context.SkillCategories.Values
            .FirstOrDefault(x => x.Name.Equals(normalizedName, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(entity);
    }

    public Task<IReadOnlyList<SkillCategoryEntity>> GetAllAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var query = _context.SkillCategories.Values.AsEnumerable();

        if (!includeInactive)
        {
            query = query.Where(x => x.IsActive);
        }

        var entities = query
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToList();

        return Task.FromResult<IReadOnlyList<SkillCategoryEntity>>(entities);
    }

    public Task<SkillCategoryEntity> AddAsync(SkillCategoryEntity category, CancellationToken cancellationToken = default)
    {
        if (!_context.SkillCategories.TryAdd(category.Id, category))
        {
            throw new InvalidOperationException($"Skill category with id '{category.Id}' already exists.");
        }
        return Task.FromResult(category);
    }

    public Task UpdateAsync(SkillCategoryEntity category, CancellationToken cancellationToken = default)
    {
        if (!_context.SkillCategories.ContainsKey(category.Id))
        {
            throw new InvalidOperationException($"Skill category with id '{category.Id}' not found.");
        }
        _context.SkillCategories[category.Id] = category;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _context.SkillCategories.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_context.SkillCategories.ContainsKey(id));
    }

    public Task<bool> NameExistsAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var normalizedName = name.Trim();
        var exists = _context.SkillCategories.Values
            .Any(x => x.Name.Equals(normalizedName, StringComparison.OrdinalIgnoreCase)
                      && (!excludeId.HasValue || x.Id != excludeId.Value));
        return Task.FromResult(exists);
    }

    public Task<bool> HasSkillsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var hasSkills = _context.Skills.Values.Any(s => s.SkillCategoryId == id);
        return Task.FromResult(hasSkills);
    }
}
