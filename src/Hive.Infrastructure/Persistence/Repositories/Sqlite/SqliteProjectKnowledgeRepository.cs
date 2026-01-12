using Hive.Core.Entities;
using Hive.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Hive.Infrastructure.Persistence.Repositories.Sqlite;

public class SqliteProjectKnowledgeRepository : IProjectKnowledgeRepository
{
    private readonly HiveDbContext _context;

    public SqliteProjectKnowledgeRepository(HiveDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<ProjectKnowledge?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ProjectKnowledge.FindAsync([id], cancellationToken);
    }

    public async Task<ProjectKnowledge?> GetByDirectReportAndProjectAsync(Guid directReportId, Guid projectId, CancellationToken cancellationToken = default)
    {
        return await _context.ProjectKnowledge
            .FirstOrDefaultAsync(x => x.DirectReportId == directReportId && x.ProjectId == projectId, cancellationToken);
    }

    public async Task<IReadOnlyList<ProjectKnowledge>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ProjectKnowledge
            .OrderBy(x => x.ProjectId)
            .ThenBy(x => x.DirectReportId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProjectKnowledge>> GetByDirectReportIdAsync(Guid directReportId, CancellationToken cancellationToken = default)
    {
        return await _context.ProjectKnowledge
            .Where(x => x.DirectReportId == directReportId)
            .OrderBy(x => x.ProjectId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProjectKnowledge>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        return await _context.ProjectKnowledge
            .Where(x => x.ProjectId == projectId)
            .OrderBy(x => x.DirectReportId)
            .ToListAsync(cancellationToken);
    }

    public async Task<ProjectKnowledge> AddAsync(ProjectKnowledge knowledge, CancellationToken cancellationToken = default)
    {
        _context.ProjectKnowledge.Add(knowledge);
        await _context.SaveChangesAsync(cancellationToken);
        return knowledge;
    }

    public async Task UpdateAsync(ProjectKnowledge knowledge, CancellationToken cancellationToken = default)
    {
        _context.ProjectKnowledge.Update(knowledge);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.ProjectKnowledge.FindAsync([id], cancellationToken);
        if (entity != null)
        {
            _context.ProjectKnowledge.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ProjectKnowledge.AnyAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsForDirectReportAndProjectAsync(Guid directReportId, Guid projectId, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        return await _context.ProjectKnowledge
            .AnyAsync(x => x.DirectReportId == directReportId
                        && x.ProjectId == projectId
                        && (!excludeId.HasValue || x.Id != excludeId.Value), cancellationToken);
    }
}
