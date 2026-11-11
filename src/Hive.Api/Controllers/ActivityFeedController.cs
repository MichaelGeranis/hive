using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hive.Api.Controllers;

/// <summary>
/// API controller for managing activity feed/logs.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class ActivityFeedController : ControllerBase
{
    private readonly IActivityService _activityService;
    private readonly ILogger<ActivityFeedController> _logger;

    public ActivityFeedController(IActivityService activityService, ILogger<ActivityFeedController> logger)
    {
        _activityService = activityService ?? throw new ArgumentNullException(nameof(activityService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets an activity by its unique identifier.
    /// </summary>
    /// <param name="id">The activity ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The activity DTO if found.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ActivityDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ActivityDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting activity with ID: {Id}", id);
        var activity = await _activityService.GetByIdAsync(id, cancellationToken);

        if (activity is null)
        {
            return NotFound(new { message = $"Activity with ID '{id}' not found." });
        }

        return Ok(activity);
    }

    /// <summary>
    /// Gets activities with pagination and optional search.
    /// </summary>
    /// <param name="pageNumber">Page number (default: 1).</param>
    /// <param name="pageSize">Page size (default: 50, max: 100).</param>
    /// <param name="search">Optional search term to filter by entity name or description.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paginated list of activity DTOs.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ActivityDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ActivityDto>>> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting activities with pagination. Page: {PageNumber}, Size: {PageSize}, Search: {Search}",
            pageNumber, pageSize, search);

        var pagination = new ActivityPaginationParams
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            SearchTerm = search
        };

        var result = await _activityService.SearchAsync(pagination, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets activities from the last N days, ordered by timestamp descending.
    /// </summary>
    /// <param name="days">Number of days to look back (default: 7).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of recent activity DTOs.</returns>
    [HttpGet("recent")]
    [ProducesResponseType(typeof(IReadOnlyList<ActivityDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<ActivityDto>>> GetRecent(
        [FromQuery] int days = 7,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting activities from the last {Days} days", days);

        if (days <= 0)
        {
            return BadRequest(new { message = "Days must be greater than zero." });
        }

        try
        {
            var activities = await _activityService.GetRecentAsync(days, cancellationToken);
            return Ok(activities);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Gets activities for a specific entity type, ordered by timestamp descending.
    /// </summary>
    /// <param name="type">The entity type (Review, Task, Leave).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of activity DTOs for the specified entity type.</returns>
    [HttpGet("by-entity-type/{type}")]
    [ProducesResponseType(typeof(IReadOnlyList<ActivityDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<ActivityDto>>> GetByEntityType(
        string type,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting activities for entity type: {Type}", type);

        if (!Enum.TryParse<EntityType>(type, true, out var entityType))
        {
            return BadRequest(new { message = $"Invalid entity type: {type}. Valid types are: Review, Task, Leave." });
        }

        var activities = await _activityService.GetByEntityTypeAsync(entityType, cancellationToken);
        return Ok(activities);
    }
}
