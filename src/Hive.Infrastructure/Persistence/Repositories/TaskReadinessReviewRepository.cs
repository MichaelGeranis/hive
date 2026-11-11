using Hive.Core.Entities;
using Hive.Core.Interfaces;

namespace Hive.Infrastructure.Persistence.Repositories;

/// <summary>
/// In-memory repository for the cached AI reviews of tasks.
/// </summary>
public class TaskReadinessReviewRepository : ITaskReadinessReviewRepository
{
    private readonly InMemoryDbContext _context;

    public TaskReadinessReviewRepository(InMemoryDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task<TaskReadinessReview?> GetByTaskIdAsync(Guid teamTaskId, CancellationToken cancellationToken = default)
    {
        var review = _context.TaskReadinessReviews.Values.FirstOrDefault(r => r.TeamTaskId == teamTaskId);
        return Task.FromResult(review);
    }

    public Task<IReadOnlyList<TaskReadinessReview>> GetByTaskIdsAsync(IReadOnlyCollection<Guid> teamTaskIds, CancellationToken cancellationToken = default)
    {
        var ids = teamTaskIds.ToHashSet();
        var reviews = _context.TaskReadinessReviews.Values.Where(r => ids.Contains(r.TeamTaskId)).ToList();
        return Task.FromResult<IReadOnlyList<TaskReadinessReview>>(reviews);
    }

    public Task<IReadOnlyList<TaskReadinessReview>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var reviews = _context.TaskReadinessReviews.Values.OrderByDescending(r => r.AnalyzedAt).ToList();
        return Task.FromResult<IReadOnlyList<TaskReadinessReview>>(reviews);
    }

    public Task<TaskReadinessReview> AddAsync(TaskReadinessReview review, CancellationToken cancellationToken = default)
    {
        if (_context.TaskReadinessReviews.Values.Any(r => r.TeamTaskId == review.TeamTaskId))
        {
            throw new InvalidOperationException($"Task {review.TeamTaskId} already has a review.");
        }

        if (!_context.TaskReadinessReviews.TryAdd(review.Id, review))
        {
            throw new InvalidOperationException($"Task review with ID {review.Id} already exists.");
        }
        return Task.FromResult(review);
    }

    public Task UpdateAsync(TaskReadinessReview review, CancellationToken cancellationToken = default)
    {
        _context.TaskReadinessReviews[review.Id] = review;
        return Task.CompletedTask;
    }

    public Task DeleteForMissingTasksAsync(IReadOnlyCollection<Guid> existingTaskIds, CancellationToken cancellationToken = default)
    {
        var existing = existingTaskIds.ToHashSet();
        foreach (var review in _context.TaskReadinessReviews.Values.Where(r => !existing.Contains(r.TeamTaskId)).ToList())
        {
            _context.TaskReadinessReviews.TryRemove(review.Id, out _);
        }

        return Task.CompletedTask;
    }
}
