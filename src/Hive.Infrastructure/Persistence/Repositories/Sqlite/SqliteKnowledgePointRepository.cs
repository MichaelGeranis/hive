using Hive.Core.Entities;
using Hive.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Hive.Infrastructure.Persistence.Repositories.Sqlite;

public class SqliteKnowledgePointRepository : IKnowledgePointRepository
{
    private readonly HiveDbContext _context;

    public SqliteKnowledgePointRepository(HiveDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<KnowledgePoint?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.KnowledgePoints.FindAsync([id], cancellationToken);
    }

    public async Task<KnowledgePoint?> GetByDirectReportAndProjectAsync(Guid directReportId, Guid projectId, CancellationToken cancellationToken = default)
    {
        return await _context.KnowledgePoints
            .FirstOrDefaultAsync(x => x.DirectReportId == directReportId && x.ProjectId == projectId, cancellationToken);
    }

    public async Task<IReadOnlyList<KnowledgePoint>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.KnowledgePoints
            .OrderBy(x => x.ProjectId)
            .ThenBy(x => x.DirectReportId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<KnowledgePoint>> GetByDirectReportIdAsync(Guid directReportId, CancellationToken cancellationToken = default)
    {
        return await _context.KnowledgePoints
            .Where(x => x.DirectReportId == directReportId)
            .OrderBy(x => x.ProjectId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<KnowledgePoint>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        return await _context.KnowledgePoints
            .Where(x => x.ProjectId == projectId)
            .OrderBy(x => x.DirectReportId)
            .ToListAsync(cancellationToken);
    }

    public async Task<KnowledgePoint> AddAsync(KnowledgePoint knowledgePoint, CancellationToken cancellationToken = default)
    {
        _context.KnowledgePoints.Add(knowledgePoint);
        await _context.SaveChangesAsync(cancellationToken);
        return knowledgePoint;
    }

    public async Task UpdateAsync(KnowledgePoint knowledgePoint, CancellationToken cancellationToken = default)
    {
        _context.KnowledgePoints.Update(knowledgePoint);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.KnowledgePoints.FindAsync([id], cancellationToken);
        if (entity != null)
        {
            _context.KnowledgePoints.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.KnowledgePoints.AnyAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsForDirectReportAndProjectAsync(Guid directReportId, Guid projectId, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        return await _context.KnowledgePoints
            .AnyAsync(x => x.DirectReportId == directReportId
                        && x.ProjectId == projectId
                        && (!excludeId.HasValue || x.Id != excludeId.Value), cancellationToken);
    }
}
