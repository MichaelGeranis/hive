using Hive.Core.Entities;
using Hive.Core.Interfaces;

namespace Hive.Infrastructure.Persistence.Repositories;

/// <summary>
/// In-memory implementation of IKnowledgePointRepository.
/// </summary>
public class KnowledgePointRepository : IKnowledgePointRepository
{
    private readonly InMemoryDbContext _context;

    public KnowledgePointRepository(InMemoryDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task<KnowledgePoint?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _context.KnowledgePoints.TryGetValue(id, out var entity);
        return Task.FromResult(entity);
    }

    public Task<KnowledgePoint?> GetByDirectReportAndProjectAsync(Guid directReportId, Guid projectId, CancellationToken cancellationToken = default)
    {
        var entity = _context.KnowledgePoints.Values
            .FirstOrDefault(x => x.DirectReportId == directReportId && x.ProjectId == projectId);
        return Task.FromResult(entity);
    }

    public Task<IReadOnlyList<KnowledgePoint>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = _context.KnowledgePoints.Values
            .OrderBy(x => x.ProjectId)
            .ThenBy(x => x.DirectReportId)
            .ToList();

        return Task.FromResult<IReadOnlyList<KnowledgePoint>>(entities);
    }

    public Task<IReadOnlyList<KnowledgePoint>> GetByDirectReportIdAsync(Guid directReportId, CancellationToken cancellationToken = default)
    {
        var entities = _context.KnowledgePoints.Values
            .Where(x => x.DirectReportId == directReportId)
            .OrderBy(x => x.ProjectId)
            .ToList();

        return Task.FromResult<IReadOnlyList<KnowledgePoint>>(entities);
    }

    public Task<IReadOnlyList<KnowledgePoint>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var entities = _context.KnowledgePoints.Values
            .Where(x => x.ProjectId == projectId)
            .OrderBy(x => x.DirectReportId)
            .ToList();

        return Task.FromResult<IReadOnlyList<KnowledgePoint>>(entities);
    }

    public Task<KnowledgePoint> AddAsync(KnowledgePoint knowledgePoint, CancellationToken cancellationToken = default)
    {
        if (!_context.KnowledgePoints.TryAdd(knowledgePoint.Id, knowledgePoint))
        {
            throw new InvalidOperationException($"KnowledgePoint with id '{knowledgePoint.Id}' already exists.");
        }
        return Task.FromResult(knowledgePoint);
    }

    public Task UpdateAsync(KnowledgePoint knowledgePoint, CancellationToken cancellationToken = default)
    {
        if (!_context.KnowledgePoints.ContainsKey(knowledgePoint.Id))
        {
            throw new InvalidOperationException($"KnowledgePoint with id '{knowledgePoint.Id}' not found.");
        }
        _context.KnowledgePoints[knowledgePoint.Id] = knowledgePoint;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _context.KnowledgePoints.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_context.KnowledgePoints.ContainsKey(id));
    }

    public Task<bool> ExistsForDirectReportAndProjectAsync(Guid directReportId, Guid projectId, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var exists = _context.KnowledgePoints.Values
            .Any(x => x.DirectReportId == directReportId
                      && x.ProjectId == projectId
                      && (!excludeId.HasValue || x.Id != excludeId.Value));
        return Task.FromResult(exists);
    }
}
