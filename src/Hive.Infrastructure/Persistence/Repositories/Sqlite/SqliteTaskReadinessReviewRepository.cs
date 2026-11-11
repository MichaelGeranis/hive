using Hive.Core.Entities;
using Hive.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Hive.Infrastructure.Persistence.Repositories.Sqlite;

/// <summary>
/// SQLite repository for the cached AI reviews of tasks.
/// </summary>
public class SqliteTaskReadinessReviewRepository : ITaskReadinessReviewRepository
{
    private readonly HiveDbContext _context;

    public SqliteTaskReadinessReviewRepository(HiveDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<TaskReadinessReview?> GetByTaskIdAsync(Guid teamTaskId, CancellationToken cancellationToken = default)
    {
        return await _context.TaskReadinessReviews
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.TeamTaskId == teamTaskId, cancellationToken);
    }

    public async Task<IReadOnlyList<TaskReadinessReview>> GetByTaskIdsAsync(IReadOnlyCollection<Guid> teamTaskIds, CancellationToken cancellationToken = default)
    {
        if (teamTaskIds.Count == 0)
        {
            return [];
        }

        var ids = teamTaskIds.ToList();
        return await _context.TaskReadinessReviews
            .AsNoTracking()
            .Where(r => ids.Contains(r.TeamTaskId))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TaskReadinessReview>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.TaskReadinessReviews
            .AsNoTracking()
            .OrderByDescending(r => r.AnalyzedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<TaskReadinessReview> AddAsync(TaskReadinessReview review, CancellationToken cancellationToken = default)
    {
        await _context.TaskReadinessReviews.AddAsync(review, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return review;
    }

    public async Task UpdateAsync(TaskReadinessReview review, CancellationToken cancellationToken = default)
    {
        _context.TaskReadinessReviews.UpdateUntracked(review);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteForMissingTasksAsync(IReadOnlyCollection<Guid> existingTaskIds, CancellationToken cancellationToken = default)
    {
        var existing = existingTaskIds.ToHashSet();
        var orphans = (await _context.TaskReadinessReviews.ToListAsync(cancellationToken))
            .Where(r => !existing.Contains(r.TeamTaskId))
            .ToList();

        if (orphans.Count > 0)
        {
            _context.TaskReadinessReviews.RemoveRange(orphans);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
