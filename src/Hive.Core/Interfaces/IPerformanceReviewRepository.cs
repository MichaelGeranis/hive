using Hive.Core.Entities;

namespace Hive.Core.Interfaces;

/// <summary>
/// Repository interface for PerformanceReview entity.
/// </summary>
public interface IPerformanceReviewRepository
{
    Task<PerformanceReview?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PerformanceReview>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PerformanceReview>> GetByDirectReportIdAsync(Guid directReportId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PerformanceReview>> GetByStatusAsync(ReviewStatus status, CancellationToken cancellationToken = default);
    Task<PerformanceReview> AddAsync(PerformanceReview review, CancellationToken cancellationToken = default);
    Task UpdateAsync(PerformanceReview review, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> HasReviewForPeriodAsync(Guid directReportId, string reviewPeriod, Guid? excludeId = null, CancellationToken cancellationToken = default);
}
