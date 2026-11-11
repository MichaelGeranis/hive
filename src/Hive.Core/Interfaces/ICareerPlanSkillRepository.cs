using Hive.Core.Entities;

namespace Hive.Core.Interfaces;

/// <summary>
/// Repository interface for CareerPlanSkill entities.
/// </summary>
public interface ICareerPlanSkillRepository
{
    Task<CareerPlanSkill?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// A plan's skills, by sort order, then title.
    /// </summary>
    Task<IReadOnlyList<CareerPlanSkill>> GetByCareerPlanIdAsync(Guid careerPlanId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Every plan's skills, by sort order, then title.
    /// </summary>
    Task<IReadOnlyList<CareerPlanSkill>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<CareerPlanSkill> AddAsync(CareerPlanSkill skill, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<CareerPlanSkill> skills, CancellationToken cancellationToken = default);
    Task UpdateAsync(CareerPlanSkill skill, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeleteByCareerPlanIdAsync(Guid careerPlanId, CancellationToken cancellationToken = default);
}
