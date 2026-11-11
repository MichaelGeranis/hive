using Hive.Core.Entities;
using Hive.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Hive.Infrastructure.Persistence.Repositories.Sqlite;

public class SqliteRoadmapSkillRepository : IRoadmapSkillRepository
{
    private readonly HiveDbContext _context;

    public SqliteRoadmapSkillRepository(HiveDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<RoadmapSkill?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.RoadmapSkills.FindAsync([id], cancellationToken);
    }

    public async Task<IReadOnlyList<RoadmapSkill>> GetByRoadmapIdAsync(Guid roadmapId, CancellationToken cancellationToken = default)
    {
        return await Ordered(_context.RoadmapSkills.AsNoTracking().Where(x => x.RoadmapId == roadmapId))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RoadmapSkill>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await Ordered(_context.RoadmapSkills.AsNoTracking()).ToListAsync(cancellationToken);
    }

    public async Task<RoadmapSkill> AddAsync(RoadmapSkill skill, CancellationToken cancellationToken = default)
    {
        _context.RoadmapSkills.Add(skill);
        await _context.SaveChangesAsync(cancellationToken);
        return skill;
    }

    public async Task AddRangeAsync(IEnumerable<RoadmapSkill> skills, CancellationToken cancellationToken = default)
    {
        _context.RoadmapSkills.AddRange(skills);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(RoadmapSkill skill, CancellationToken cancellationToken = default)
    {
        _context.RoadmapSkills.UpdateUntracked(skill);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.RoadmapSkills.FindAsync([id], cancellationToken);
        if (entity != null)
        {
            _context.RoadmapSkills.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task DeleteByRoadmapIdAsync(Guid roadmapId, CancellationToken cancellationToken = default)
    {
        var skills = await _context.RoadmapSkills.Where(x => x.RoadmapId == roadmapId).ToListAsync(cancellationToken);
        _context.RoadmapSkills.RemoveRange(skills);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private static IQueryable<RoadmapSkill> Ordered(IQueryable<RoadmapSkill> skills) =>
        skills.OrderBy(x => x.SortOrder).ThenBy(x => EF.Functions.Collate(x.Title, "NOCASE"));
}
