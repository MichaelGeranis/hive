using Hive.Core.Entities;

namespace Hive.Core.Interfaces;

/// <summary>
/// Repository interface for ProjectKnowledge entities.
/// </summary>
public interface IProjectKnowledgeRepository
{
    Task<ProjectKnowledge?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ProjectKnowledge?> GetByDirectReportAndProjectAsync(Guid directReportId, Guid projectId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProjectKnowledge>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProjectKnowledge>> GetByDirectReportIdAsync(Guid directReportId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProjectKnowledge>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);

    Task<ProjectKnowledge> AddAsync(ProjectKnowledge knowledge, CancellationToken cancellationToken = default);

    Task UpdateAsync(ProjectKnowledge knowledge, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> ExistsForDirectReportAndProjectAsync(Guid directReportId, Guid projectId, Guid? excludeId = null, CancellationToken cancellationToken = default);
}
