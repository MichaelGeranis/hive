using Hive.Core.Entities;

namespace Hive.Core.Interfaces;

/// <summary>
/// Repository interface for CareerPlan entities.
/// </summary>
public interface ICareerPlanRepository
{
    Task<CareerPlan?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Every plan, newest first.
    /// </summary>
    Task<IReadOnlyList<CareerPlan>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// The direct report's plan; a direct report has at most one.
    /// </summary>
    Task<CareerPlan?> GetByDirectReportIdAsync(Guid directReportId, CancellationToken cancellationToken = default);

    Task<CareerPlan> AddAsync(CareerPlan plan, CancellationToken cancellationToken = default);
    Task UpdateAsync(CareerPlan plan, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Whether the direct report already has a plan.
    /// </summary>
    Task<bool> ExistsForDirectReportAsync(Guid directReportId, CancellationToken cancellationToken = default);
}
