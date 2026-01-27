using Hive.Core.Entities;
using Hive.Core.Interfaces;

namespace Hive.Infrastructure.Persistence.Repositories;

/// <summary>
/// In-memory implementation of the Activity repository using ConcurrentDictionary.
/// </summary>
public class ActivityRepository : IActivityRepository
{
    private readonly InMemoryDbContext _context;

    public ActivityRepository(InMemoryDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task<Activity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _context.Activities.TryGetValue(id, out var activity);
        return Task.FromResult(activity);
    }

    public Task<IReadOnlyList<Activity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var activities = _context.Activities.Values
            .OrderByDescending(a => a.Timestamp)
            .ToList();
        return Task.FromResult<IReadOnlyList<Activity>>(activities);
    }

    public Task<IReadOnlyList<Activity>> GetRecentAsync(int days, CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-days);
        var activities = _context.Activities.Values
            .Where(a => a.Timestamp >= cutoffDate)
            .OrderByDescending(a => a.Timestamp)
            .ToList();
        return Task.FromResult<IReadOnlyList<Activity>>(activities);
    }

    public Task<IReadOnlyList<Activity>> GetByDateRangeAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        var activities = _context.Activities.Values
            .Where(a => a.Timestamp >= startDate && a.Timestamp <= endDate)
            .OrderByDescending(a => a.Timestamp)
            .ToList();
        return Task.FromResult<IReadOnlyList<Activity>>(activities);
    }

    public Task<IReadOnlyList<Activity>> GetByEntityTypeAsync(
        EntityType entityType,
        CancellationToken cancellationToken = default)
    {
        var activities = _context.Activities.Values
            .Where(a => a.EntityType == entityType)
            .OrderByDescending(a => a.Timestamp)
            .ToList();
        return Task.FromResult<IReadOnlyList<Activity>>(activities);
    }

    public Task<Activity> AddAsync(Activity activity, CancellationToken cancellationToken = default)
    {
        if (activity == null)
        {
            throw new ArgumentNullException(nameof(activity));
        }

        _context.Activities.TryAdd(activity.Id, activity);
        return Task.FromResult(activity);
    }

    public Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_context.Activities.Count);
    }

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_context.Activities.ContainsKey(id));
    }

    public Task<(IReadOnlyList<Activity> Items, int TotalCount)> SearchAsync(
        string? searchTerm,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Activities.Values.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLowerInvariant();
            query = query.Where(a =>
                a.EntityName.ToLowerInvariant().Contains(term) ||
                a.Description.ToLowerInvariant().Contains(term));
        }

        var orderedQuery = query.OrderByDescending(a => a.Timestamp);
        var totalCount = orderedQuery.Count();
        var items = orderedQuery.Skip(skip).Take(take).ToList();

        return Task.FromResult<(IReadOnlyList<Activity>, int)>((items, totalCount));
    }
}
