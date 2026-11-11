using Hive.Core.Entities;
using Hive.Core.Interfaces;

namespace Hive.Infrastructure.Persistence.Repositories;

/// <summary>
/// In-memory implementation of ICareerPlanSkillRepository.
/// </summary>
public class CareerPlanSkillRepository : ICareerPlanSkillRepository
{
    private readonly InMemoryDbContext _context;

    public CareerPlanSkillRepository(InMemoryDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task<CareerPlanSkill?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _context.CareerPlanSkills.TryGetValue(id, out var entity);
        return Task.FromResult(entity);
    }

    public Task<IReadOnlyList<CareerPlanSkill>> GetByCareerPlanIdAsync(Guid careerPlanId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Ordered(_context.CareerPlanSkills.Values.Where(x => x.CareerPlanId == careerPlanId)));
    }

    public Task<IReadOnlyList<CareerPlanSkill>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Ordered(_context.CareerPlanSkills.Values));
    }

    public Task<CareerPlanSkill> AddAsync(CareerPlanSkill skill, CancellationToken cancellationToken = default)
    {
        if (!_context.CareerPlanSkills.TryAdd(skill.Id, skill))
        {
            throw new InvalidOperationException($"CareerPlanSkill with id '{skill.Id}' already exists.");
        }
        return Task.FromResult(skill);
    }

    public async Task AddRangeAsync(IEnumerable<CareerPlanSkill> skills, CancellationToken cancellationToken = default)
    {
        foreach (var skill in skills)
        {
            await AddAsync(skill, cancellationToken);
        }
    }

    public Task UpdateAsync(CareerPlanSkill skill, CancellationToken cancellationToken = default)
    {
        if (!_context.CareerPlanSkills.ContainsKey(skill.Id))
        {
            throw new InvalidOperationException($"CareerPlanSkill with id '{skill.Id}' not found.");
        }
        _context.CareerPlanSkills[skill.Id] = skill;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _context.CareerPlanSkills.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    public Task DeleteByCareerPlanIdAsync(Guid careerPlanId, CancellationToken cancellationToken = default)
    {
        foreach (var id in _context.CareerPlanSkills.Values.Where(x => x.CareerPlanId == careerPlanId).Select(x => x.Id).ToList())
        {
            _context.CareerPlanSkills.TryRemove(id, out _);
        }
        return Task.CompletedTask;
    }

    private static IReadOnlyList<CareerPlanSkill> Ordered(IEnumerable<CareerPlanSkill> skills) =>
        skills.OrderBy(x => x.SortOrder).ThenBy(x => x.Title, StringComparer.OrdinalIgnoreCase).ToList();
}
