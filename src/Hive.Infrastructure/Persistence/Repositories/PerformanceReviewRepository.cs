using Hive.Core.Entities;
using Hive.Core.Interfaces;

namespace Hive.Infrastructure.Persistence.Repositories;

/// <summary>
/// In-memory implementation of IPerformanceReviewRepository.
/// </summary>
public class PerformanceReviewRepository : IPerformanceReviewRepository
{
    private readonly InMemoryDbContext _context;

    public PerformanceReviewRepository(InMemoryDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task<PerformanceReview?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _context.PerformanceReviews.TryGetValue(id, out var entity);
        return Task.FromResult(entity);
    }

    public Task<IReadOnlyList<PerformanceReview>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = _context.PerformanceReviews.Values
            .OrderByDescending(x => x.ReviewDate)
            .ThenBy(x => x.CreatedAt)
            .ToList();
        return Task.FromResult<IReadOnlyList<PerformanceReview>>(entities);
    }

    public Task<IReadOnlyList<PerformanceReview>> GetByDirectReportIdAsync(Guid directReportId, CancellationToken cancellationToken = default)
    {
        var entities = _context.PerformanceReviews.Values
            .Where(x => x.DirectReportId == directReportId)
            .OrderByDescending(x => x.ReviewDate)
            .ToList();
        return Task.FromResult<IReadOnlyList<PerformanceReview>>(entities);
    }

    public Task<IReadOnlyList<PerformanceReview>> GetByStatusAsync(ReviewStatus status, CancellationToken cancellationToken = default)
    {
        var entities = _context.PerformanceReviews.Values
            .Where(x => x.Status == status)
            .OrderByDescending(x => x.ReviewDate)
            .ToList();
        return Task.FromResult<IReadOnlyList<PerformanceReview>>(entities);
    }

    public Task<PerformanceReview> AddAsync(PerformanceReview review, CancellationToken cancellationToken = default)
    {
        if (!_context.PerformanceReviews.TryAdd(review.Id, review))
        {
            throw new InvalidOperationException($"PerformanceReview with id '{review.Id}' already exists.");
        }
        return Task.FromResult(review);
    }

    public Task UpdateAsync(PerformanceReview review, CancellationToken cancellationToken = default)
    {
        if (!_context.PerformanceReviews.ContainsKey(review.Id))
        {
            throw new InvalidOperationException($"PerformanceReview with id '{review.Id}' not found.");
        }
        _context.PerformanceReviews[review.Id] = review;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _context.PerformanceReviews.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_context.PerformanceReviews.ContainsKey(id));
    }

    public Task<bool> HasReviewForPeriodAsync(Guid directReportId, string reviewPeriod, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var normalizedPeriod = reviewPeriod.Trim();
        var exists = _context.PerformanceReviews.Values
            .Any(x => x.DirectReportId == directReportId
                      && x.ReviewPeriod.Equals(normalizedPeriod, StringComparison.OrdinalIgnoreCase)
                      && (!excludeId.HasValue || x.Id != excludeId.Value));
        return Task.FromResult(exists);
    }
}
