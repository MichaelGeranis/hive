using Hive.Core.Entities;

namespace Hive.Core.Interfaces;

/// <summary>
/// Repository interface for managing Activity entities.
/// Provides read-only access to activity logs with no update or delete operations (immutable).
/// </summary>
public interface IActivityRepository
{
    /// <summary>
    /// Gets an activity by its unique identifier.
    /// </summary>
    /// <param name="id">The activity ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The activity if found; otherwise, null.</returns>
    Task<Activity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all activities in the system, ordered by timestamp descending.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A read-only list of all activities.</returns>
    Task<IReadOnlyList<Activity>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets activities from the last N days, ordered by timestamp descending.
    /// </summary>
    /// <param name="days">Number of days to look back.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A read-only list of recent activities.</returns>
    Task<IReadOnlyList<Activity>> GetRecentAsync(int days, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets activities within a specific date range, ordered by timestamp descending.
    /// </summary>
    /// <param name="startDate">The start date of the range.</param>
    /// <param name="endDate">The end date of the range.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A read-only list of activities in the date range.</returns>
    Task<IReadOnlyList<Activity>> GetByDateRangeAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets activities for a specific entity type, ordered by timestamp descending.
    /// </summary>
    /// <param name="entityType">The type of entity to filter by.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A read-only list of activities for the specified entity type.</returns>
    Task<IReadOnlyList<Activity>> GetByEntityTypeAsync(
        EntityType entityType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new activity to the repository.
    /// </summary>
    /// <param name="activity">The activity to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The added activity.</returns>
    Task<Activity> AddAsync(Activity activity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total count of activities in the system.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The total count of activities.</returns>
    Task<int> CountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an activity exists with the specified ID.
    /// </summary>
    /// <param name="id">The activity ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the activity exists; otherwise, false.</returns>
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches activities with pagination support.
    /// </summary>
    /// <param name="searchTerm">Optional search term to filter by entity name or description.</param>
    /// <param name="skip">Number of items to skip.</param>
    /// <param name="take">Number of items to take.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple containing the list of activities and total count.</returns>
    Task<(IReadOnlyList<Activity> Items, int TotalCount)> SearchAsync(
        string? searchTerm,
        int skip,
        int take,
        CancellationToken cancellationToken = default);
}
