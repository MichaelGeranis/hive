using Hive.Core.Entities;

namespace Hive.Core.Interfaces;

/// <summary>
/// Repository interface for KnowledgePoint entities.
/// </summary>
public interface IKnowledgePointRepository
{
    Task<KnowledgePoint?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<KnowledgePoint?> GetByDirectReportAndProjectAsync(Guid directReportId, Guid projectId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<KnowledgePoint>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<KnowledgePoint>> GetByDirectReportIdAsync(Guid directReportId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<KnowledgePoint>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);

    Task<KnowledgePoint> AddAsync(KnowledgePoint knowledgePoint, CancellationToken cancellationToken = default);

    Task UpdateAsync(KnowledgePoint knowledgePoint, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> ExistsForDirectReportAndProjectAsync(Guid directReportId, Guid projectId, Guid? excludeId = null, CancellationToken cancellationToken = default);
}
