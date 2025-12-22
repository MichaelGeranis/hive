using Hive.Application.DTOs;

namespace Hive.Application.Interfaces;

/// <summary>
/// Service interface for SkillAssessment management.
/// </summary>
public interface ISkillAssessmentService
{
    Task<SkillAssessmentDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SkillAssessmentDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SkillAssessmentDto>> GetByDirectReportIdAsync(Guid directReportId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SkillAssessmentDto>> GetBySkillIdAsync(Guid skillId, CancellationToken cancellationToken = default);
    Task<SkillAssessmentDto> CreateAsync(CreateSkillAssessmentDto dto, CancellationToken cancellationToken = default);
    Task<SkillAssessmentDto> UpdateAsync(Guid id, UpdateSkillAssessmentDto dto, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SkillAssessmentDto>> BulkAssessAsync(BulkSkillAssessmentDto dto, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SkillMatrixDto> GetSkillMatrixAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SkillAssessmentDto>> GetSkillGapsAsync(CancellationToken cancellationToken = default);
}
