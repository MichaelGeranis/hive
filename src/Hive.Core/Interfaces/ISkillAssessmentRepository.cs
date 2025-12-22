using Hive.Core.Entities;

namespace Hive.Core.Interfaces;

/// <summary>
/// Repository interface for SkillAssessment entity.
/// </summary>
public interface ISkillAssessmentRepository
{
    Task<SkillAssessment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SkillAssessment?> GetByDirectReportAndSkillAsync(Guid directReportId, Guid skillId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SkillAssessment>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SkillAssessment>> GetByDirectReportIdAsync(Guid directReportId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SkillAssessment>> GetBySkillIdAsync(Guid skillId, CancellationToken cancellationToken = default);
    Task<SkillAssessment> AddAsync(SkillAssessment assessment, CancellationToken cancellationToken = default);
    Task UpdateAsync(SkillAssessment assessment, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> AssessmentExistsAsync(Guid directReportId, Guid skillId, Guid? excludeId = null, CancellationToken cancellationToken = default);
}
