using Hive.Core.Entities;
using Hive.Core.Interfaces;

namespace Hive.Infrastructure.Persistence.Repositories;

/// <summary>
/// In-memory implementation of IRoadmapSkillRepository.
/// </summary>
public class RoadmapSkillRepository : IRoadmapSkillRepository
{
    private readonly InMemoryDbContext _context;

    public RoadmapSkillRepository(InMemoryDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task<RoadmapSkill?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _context.RoadmapSkills.TryGetValue(id, out var entity);
        return Task.FromResult(entity);
    }

    public Task<IReadOnlyList<RoadmapSkill>> GetByRoadmapIdAsync(Guid roadmapId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Ordered(_context.RoadmapSkills.Values.Where(x => x.RoadmapId == roadmapId)));
    }

    public Task<IReadOnlyList<RoadmapSkill>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Ordered(_context.RoadmapSkills.Values));
    }

    public Task<RoadmapSkill> AddAsync(RoadmapSkill skill, CancellationToken cancellationToken = default)
    {
        if (!_context.RoadmapSkills.TryAdd(skill.Id, skill))
        {
            throw new InvalidOperationException($"RoadmapSkill with id '{skill.Id}' already exists.");
        }
        return Task.FromResult(skill);
    }

    public async Task AddRangeAsync(IEnumerable<RoadmapSkill> skills, CancellationToken cancellationToken = default)
    {
        foreach (var skill in skills)
        {
            await AddAsync(skill, cancellationToken);
        }
    }

    public Task UpdateAsync(RoadmapSkill skill, CancellationToken cancellationToken = default)
    {
        if (!_context.RoadmapSkills.ContainsKey(skill.Id))
        {
            throw new InvalidOperationException($"RoadmapSkill with id '{skill.Id}' not found.");
        }
        _context.RoadmapSkills[skill.Id] = skill;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _context.RoadmapSkills.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    public Task DeleteByRoadmapIdAsync(Guid roadmapId, CancellationToken cancellationToken = default)
    {
        foreach (var id in _context.RoadmapSkills.Values.Where(x => x.RoadmapId == roadmapId).Select(x => x.Id).ToList())
        {
            _context.RoadmapSkills.TryRemove(id, out _);
        }
        return Task.CompletedTask;
    }

    private static IReadOnlyList<RoadmapSkill> Ordered(IEnumerable<RoadmapSkill> skills) =>
        skills.OrderBy(x => x.SortOrder).ThenBy(x => x.Title, StringComparer.OrdinalIgnoreCase).ToList();
}
