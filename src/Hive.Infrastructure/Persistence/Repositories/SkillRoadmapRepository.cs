using Hive.Core.Entities;
using Hive.Core.Interfaces;

namespace Hive.Infrastructure.Persistence.Repositories;

/// <summary>
/// In-memory implementation of ISkillRoadmapRepository.
/// </summary>
public class SkillRoadmapRepository : ISkillRoadmapRepository
{
    private readonly InMemoryDbContext _context;

    public SkillRoadmapRepository(InMemoryDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task<SkillRoadmap?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _context.SkillRoadmaps.TryGetValue(id, out var entity);
        return Task.FromResult(entity);
    }

    public Task<IReadOnlyList<SkillRoadmap>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = _context.SkillRoadmaps.Values
            .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        return Task.FromResult<IReadOnlyList<SkillRoadmap>>(entities);
    }

    public Task<SkillRoadmap> AddAsync(SkillRoadmap roadmap, CancellationToken cancellationToken = default)
    {
        if (!_context.SkillRoadmaps.TryAdd(roadmap.Id, roadmap))
        {
            throw new InvalidOperationException($"SkillRoadmap with id '{roadmap.Id}' already exists.");
        }
        return Task.FromResult(roadmap);
    }

    public Task UpdateAsync(SkillRoadmap roadmap, CancellationToken cancellationToken = default)
    {
        if (!_context.SkillRoadmaps.ContainsKey(roadmap.Id))
        {
            throw new InvalidOperationException($"SkillRoadmap with id '{roadmap.Id}' not found.");
        }
        _context.SkillRoadmaps[roadmap.Id] = roadmap;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _context.SkillRoadmaps.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    public Task<bool> NameExistsAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var trimmed = name.Trim();
        var exists = _context.SkillRoadmaps.Values
            .Any(x => x.Name.Equals(trimmed, StringComparison.OrdinalIgnoreCase)
                      && (!excludeId.HasValue || x.Id != excludeId.Value));
        return Task.FromResult(exists);
    }
}
