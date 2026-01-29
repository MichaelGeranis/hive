using Hive.Application.DTOs;

namespace Hive.Application.Interfaces;

/// <summary>
/// Service interface for KnowledgePoint management.
/// </summary>
public interface IKnowledgePointService
{
    Task<KnowledgePointDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<KnowledgePointDto?> GetByDirectReportAndProjectAsync(Guid directReportId, Guid projectId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<KnowledgePointDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<KnowledgePointDto>> GetByDirectReportIdAsync(Guid directReportId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<KnowledgePointDto>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<KnowledgePointDto> CreateOrUpdateAsync(CreateOrUpdateKnowledgePointDto dto, CancellationToken cancellationToken = default);
    Task<KnowledgePointDto> AddPointsAsync(AddKnowledgePointsDto dto, CancellationToken cancellationToken = default);
    Task ResetPointsAsync(Guid directReportId, Guid projectId, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<int> CalculateAutomaticPointsAsync(Guid directReportId, Guid projectId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<KnowledgeLevelSuggestionDto>> GetLevelIncreaseSuggestionsAsync(CancellationToken cancellationToken = default);
    Task<ProjectKnowledgeMatrixWithPointsDto> GetMatrixWithPointsAsync(CancellationToken cancellationToken = default);
}
