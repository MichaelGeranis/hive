using Hive.Application.DTOs;
using Hive.Core.Entities;

namespace Hive.Application.Interfaces;

/// <summary>
/// Service interface for managing activity logs.
/// </summary>
public interface IActivityService
{
    /// <summary>
    /// Gets an activity by its unique identifier.
    /// </summary>
    /// <param name="id">The activity ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The activity DTO if found; otherwise, null.</returns>
    Task<ActivityDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all activities in the system, ordered by timestamp descending.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A read-only list of all activity DTOs.</returns>
    Task<IReadOnlyList<ActivityDto>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets activities from the last N days, ordered by timestamp descending.
    /// </summary>
    /// <param name="days">Number of days to look back (default: 7).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A read-only list of recent activity DTOs.</returns>
    Task<IReadOnlyList<ActivityDto>> GetRecentAsync(int days = 7, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets activities for a specific entity type, ordered by timestamp descending.
    /// </summary>
    /// <param name="entityType">The type of entity to filter by.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A read-only list of activity DTOs for the specified entity type.</returns>
    Task<IReadOnlyList<ActivityDto>> GetByEntityTypeAsync(EntityType entityType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs a new activity in the system.
    /// This method is called internally by other services to record actions.
    /// </summary>
    /// <param name="activityType">The type of activity.</param>
    /// <param name="entityType">The type of entity.</param>
    /// <param name="entityId">The ID of the affected entity.</param>
    /// <param name="entityName">The name of the entity for display.</param>
    /// <param name="description">The human-readable description of the activity.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created activity DTO.</returns>
    Task<ActivityDto> LogActivityAsync(
        ActivityType activityType,
        EntityType entityType,
        Guid entityId,
        string entityName,
        string description,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches activities with pagination support.
    /// </summary>
    /// <param name="pagination">Pagination and search parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paged result of activity DTOs.</returns>
    Task<PagedResult<ActivityDto>> SearchAsync(
        ActivityPaginationParams pagination,
        CancellationToken cancellationToken = default);
}
