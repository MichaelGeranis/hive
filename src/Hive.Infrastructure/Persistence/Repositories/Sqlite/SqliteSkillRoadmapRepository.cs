using Hive.Core.Entities;
using Hive.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Hive.Infrastructure.Persistence.Repositories.Sqlite;

public class SqliteSkillRoadmapRepository : ISkillRoadmapRepository
{
    private readonly HiveDbContext _context;

    public SqliteSkillRoadmapRepository(HiveDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<SkillRoadmap?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.SkillRoadmaps.FindAsync([id], cancellationToken);
    }

    public async Task<IReadOnlyList<SkillRoadmap>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SkillRoadmaps
            .AsNoTracking()
            .OrderBy(x => EF.Functions.Collate(x.Name, "NOCASE"))
            .ToListAsync(cancellationToken);
    }

    public async Task<SkillRoadmap> AddAsync(SkillRoadmap roadmap, CancellationToken cancellationToken = default)
    {
        _context.SkillRoadmaps.Add(roadmap);
        await _context.SaveChangesAsync(cancellationToken);
        return roadmap;
    }

    public async Task UpdateAsync(SkillRoadmap roadmap, CancellationToken cancellationToken = default)
    {
        _context.SkillRoadmaps.UpdateUntracked(roadmap);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.SkillRoadmaps.FindAsync([id], cancellationToken);
        if (entity != null)
        {
            _context.SkillRoadmaps.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> NameExistsAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var lowered = name.Trim().ToLower();
        return await _context.SkillRoadmaps
            .AnyAsync(x => x.Name.ToLower() == lowered && (!excludeId.HasValue || x.Id != excludeId.Value), cancellationToken);
    }
}
