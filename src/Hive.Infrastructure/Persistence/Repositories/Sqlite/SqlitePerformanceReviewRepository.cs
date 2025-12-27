using Hive.Core.Entities;
using Hive.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Hive.Infrastructure.Persistence.Repositories.Sqlite;

public class SqlitePerformanceReviewRepository : IPerformanceReviewRepository
{
    private readonly HiveDbContext _context;

    public SqlitePerformanceReviewRepository(HiveDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<PerformanceReview?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.PerformanceReviews.FindAsync([id], cancellationToken);
    }

    public async Task<IReadOnlyList<PerformanceReview>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.PerformanceReviews
            .OrderByDescending(x => x.ReviewDate)
            .ThenBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PerformanceReview>> GetByDirectReportIdAsync(Guid directReportId, CancellationToken cancellationToken = default)
    {
        return await _context.PerformanceReviews
            .Where(x => x.DirectReportId == directReportId)
            .OrderByDescending(x => x.ReviewDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PerformanceReview>> GetByStatusAsync(ReviewStatus status, CancellationToken cancellationToken = default)
    {
        return await _context.PerformanceReviews
            .Where(x => x.Status == status)
            .OrderByDescending(x => x.ReviewDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<PerformanceReview> AddAsync(PerformanceReview review, CancellationToken cancellationToken = default)
    {
        _context.PerformanceReviews.Add(review);
        await _context.SaveChangesAsync(cancellationToken);
        return review;
    }

    public async Task UpdateAsync(PerformanceReview review, CancellationToken cancellationToken = default)
    {
        _context.PerformanceReviews.Update(review);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.PerformanceReviews.FindAsync([id], cancellationToken);
        if (entity != null)
        {
            _context.PerformanceReviews.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.PerformanceReviews.AnyAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<bool> HasReviewForPeriodAsync(Guid directReportId, string reviewPeriod, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var normalizedPeriod = reviewPeriod.Trim().ToLower();
        return await _context.PerformanceReviews
            .AnyAsync(x => x.DirectReportId == directReportId
                        && x.ReviewPeriod.ToLower() == normalizedPeriod
                        && (!excludeId.HasValue || x.Id != excludeId.Value), cancellationToken);
    }
}
