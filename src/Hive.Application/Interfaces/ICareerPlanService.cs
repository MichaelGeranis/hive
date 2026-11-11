using Hive.Application.DTOs;

namespace Hive.Application.Interfaces;

/// <summary>
/// Career plans: one per direct report, recording which skills they have, which not yet and
/// which they are working on, read against the career ladder.
/// </summary>
public interface ICareerPlanService
{
    /// <summary>
    /// Every plan, by direct report name.
    /// </summary>
    Task<IReadOnlyList<CareerPlanSummaryDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<CareerPlanDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CareerPlanDto?> GetByDirectReportIdAsync(Guid directReportId, CancellationToken cancellationToken = default);

    /// <summary>
    /// What needs the manager, for the dashboard: who has no plan, and which plans have nothing
    /// in focus or have gone quiet.
    /// </summary>
    Task<CareerOverviewDto> GetOverviewAsync(CancellationToken cancellationToken = default);

    Task<CareerPlanDto> CreateAsync(CreateCareerPlanDto dto, CancellationToken cancellationToken = default);
    Task<CareerPlanDto> UpdateAsync(Guid id, UpdateCareerPlanDto dto, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<SkillLevelChangeDto> SetSkillLevelAsync(Guid planId, Guid skillId, SetSkillLevelDto dto, CancellationToken cancellationToken = default);
    Task<CareerPlanDto> SetSkillFocusAsync(Guid planId, Guid skillId, SetSkillFocusDto dto, CancellationToken cancellationToken = default);
    Task<CareerPlanDto> AddSkillAsync(Guid planId, AddCareerPlanSkillDto dto, CancellationToken cancellationToken = default);
    Task<CareerPlanDto> DeleteSkillAsync(Guid planId, Guid skillId, CancellationToken cancellationToken = default);
}
