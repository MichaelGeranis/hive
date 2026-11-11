using Hive.Core.Entities;
using Hive.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Hive.Infrastructure.Persistence.Repositories.Sqlite;

public class SqliteCareerPlanSkillRepository : ICareerPlanSkillRepository
{
    private readonly HiveDbContext _context;

    public SqliteCareerPlanSkillRepository(HiveDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<CareerPlanSkill?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.CareerPlanSkills.FindAsync([id], cancellationToken);
    }

    public async Task<IReadOnlyList<CareerPlanSkill>> GetByCareerPlanIdAsync(Guid careerPlanId, CancellationToken cancellationToken = default)
    {
        return await Ordered(_context.CareerPlanSkills.AsNoTracking().Where(x => x.CareerPlanId == careerPlanId))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CareerPlanSkill>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await Ordered(_context.CareerPlanSkills.AsNoTracking()).ToListAsync(cancellationToken);
    }

    public async Task<CareerPlanSkill> AddAsync(CareerPlanSkill skill, CancellationToken cancellationToken = default)
    {
        _context.CareerPlanSkills.Add(skill);
        await _context.SaveChangesAsync(cancellationToken);
        return skill;
    }

    public async Task AddRangeAsync(IEnumerable<CareerPlanSkill> skills, CancellationToken cancellationToken = default)
    {
        _context.CareerPlanSkills.AddRange(skills);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(CareerPlanSkill skill, CancellationToken cancellationToken = default)
    {
        _context.CareerPlanSkills.UpdateUntracked(skill);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.CareerPlanSkills.FindAsync([id], cancellationToken);
        if (entity != null)
        {
            _context.CareerPlanSkills.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task DeleteByCareerPlanIdAsync(Guid careerPlanId, CancellationToken cancellationToken = default)
    {
        var skills = await _context.CareerPlanSkills.Where(x => x.CareerPlanId == careerPlanId).ToListAsync(cancellationToken);
        _context.CareerPlanSkills.RemoveRange(skills);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private static IQueryable<CareerPlanSkill> Ordered(IQueryable<CareerPlanSkill> skills) =>
        skills.OrderBy(x => x.SortOrder).ThenBy(x => EF.Functions.Collate(x.Title, "NOCASE"));
}
