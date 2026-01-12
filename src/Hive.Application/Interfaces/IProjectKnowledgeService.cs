using Hive.Application.DTOs;

namespace Hive.Application.Interfaces;

/// <summary>
/// Service interface for ProjectKnowledge management.
/// </summary>
public interface IProjectKnowledgeService
{
    Task<ProjectKnowledgeDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProjectKnowledgeDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProjectKnowledgeDto>> GetByDirectReportIdAsync(Guid directReportId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProjectKnowledgeDto>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<ProjectKnowledgeMatrixDto> GetMatrixAsync(CancellationToken cancellationToken = default);
    Task<ProjectKnowledgeDto> CreateOrUpdateAsync(CreateOrUpdateProjectKnowledgeDto dto, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
