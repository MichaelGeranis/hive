using Hive.Application.DTOs;

namespace Hive.Application.Interfaces;

/// <summary>
/// Service interface defining use cases for PerformanceReview management.
/// </summary>
public interface IPerformanceReviewService
{
    Task<PerformanceReviewDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PerformanceReviewDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PerformanceReviewDto>> GetByDirectReportIdAsync(Guid directReportId, CancellationToken cancellationToken = default);
    Task<PerformanceReviewDto> CreateAsync(CreatePerformanceReviewDto dto, CancellationToken cancellationToken = default);
    Task<PerformanceReviewDto> UpdateContentAsync(Guid id, UpdatePerformanceReviewContentDto dto, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
