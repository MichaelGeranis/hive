using Hive.Core.Entities;
using Hive.Core.Interfaces;

namespace Hive.Infrastructure.Persistence.Repositories;

/// <summary>
/// In-memory implementation of ISkillRepository.
/// </summary>
public class SkillRepository : ISkillRepository
{
    private readonly InMemoryDbContext _context;

    public SkillRepository(InMemoryDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task<Skill?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _context.Skills.TryGetValue(id, out var entity);
        return Task.FromResult(entity);
    }

    public Task<Skill?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var normalizedName = name.Trim();
        var entity = _context.Skills.Values
            .FirstOrDefault(x => x.Name.Equals(normalizedName, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(entity);
    }

    public Task<IReadOnlyList<Skill>> GetAllAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var query = _context.Skills.Values.AsEnumerable();

        if (!includeInactive)
        {
            query = query.Where(x => x.IsActive);
        }

        var entities = query
            .OrderBy(x => x.Category)
            .ThenBy(x => x.Name)
            .ToList();

        return Task.FromResult<IReadOnlyList<Skill>>(entities);
    }

    public Task<IReadOnlyList<Skill>> GetByCategoryAsync(SkillCategory category, CancellationToken cancellationToken = default)
    {
        var entities = _context.Skills.Values
            .Where(x => x.Category == category && x.IsActive)
            .OrderBy(x => x.Name)
            .ToList();

        return Task.FromResult<IReadOnlyList<Skill>>(entities);
    }

    public Task<Skill> AddAsync(Skill skill, CancellationToken cancellationToken = default)
    {
        if (!_context.Skills.TryAdd(skill.Id, skill))
        {
            throw new InvalidOperationException($"Skill with id '{skill.Id}' already exists.");
        }
        return Task.FromResult(skill);
    }

    public Task UpdateAsync(Skill skill, CancellationToken cancellationToken = default)
    {
        if (!_context.Skills.ContainsKey(skill.Id))
        {
            throw new InvalidOperationException($"Skill with id '{skill.Id}' not found.");
        }
        _context.Skills[skill.Id] = skill;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _context.Skills.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_context.Skills.ContainsKey(id));
    }

    public Task<bool> NameExistsAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var normalizedName = name.Trim();
        var exists = _context.Skills.Values
            .Any(x => x.Name.Equals(normalizedName, StringComparison.OrdinalIgnoreCase)
                      && (!excludeId.HasValue || x.Id != excludeId.Value));
        return Task.FromResult(exists);
    }
}
