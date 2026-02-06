using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Entities;
using Hive.Core.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskStatus = Hive.Core.Entities.TaskStatus;

namespace Hive.Api.Controllers;

/// <summary>
/// API Controller for managing team tasks.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class TeamTasksController : ControllerBase
{
    private readonly ITeamTaskService _service;
    private readonly ILogger<TeamTasksController> _logger;

    public TeamTasksController(ITeamTaskService service, ILogger<TeamTasksController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Gets all tasks with pagination and optional filtering.
    /// </summary>
    /// <param name="pageNumber">Page number (1-based, default: 1).</param>
    /// <param name="pageSize">Items per page (default: 20, max: 100).</param>
    /// <param name="status">Optional status filter.</param>
    /// <param name="filter">Optional filter (all, overdue).</param>
    /// <param name="search">Optional search term.</param>
    /// <param name="label">Optional label filter.</param>
    /// <param name="sprint">Optional sprint filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated list of tasks ordered by Sprint desc, Priority desc, DueDate asc.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<TeamTaskDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<TeamTaskDto>>> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] TaskStatus? status = null,
        [FromQuery] string? filter = null,
        [FromQuery] string? search = null,
        [FromQuery] string? label = null,
        [FromQuery] string? sprint = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting tasks page {PageNumber} with size {PageSize}, status={Status}, filter={Filter}, search={Search}, label={Label}, sprint={Sprint}",
            pageNumber, pageSize, status, filter, search, label, sprint);

        var taskFilter = filter?.ToLowerInvariant() == "overdue" ? TaskFilter.Overdue : TaskFilter.All;

        var pagination = new TaskPaginationParams
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            Status = status,
            Filter = taskFilter,
            SearchTerm = search,
            Label = label,
            Sprint = sprint
        };

        var tasks = await _service.GetFilteredPagedAsync(pagination, cancellationToken);
        return Ok(tasks);
    }

    /// <summary>
    /// Gets a task by ID.
    /// </summary>
    /// <param name="id">The task ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The task if found.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TeamTaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TeamTaskDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting task with ID: {Id}", id);
        var task = await _service.GetByIdAsync(id, cancellationToken);

        if (task is null)
        {
            return NotFound(new { message = $"Task with ID '{id}' not found." });
        }

        return Ok(task);
    }

    /// <summary>
    /// Gets tasks by assignee.
    /// </summary>
    /// <param name="assigneeId">The assignee ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of tasks assigned to the specified person.</returns>
    [HttpGet("assignee/{assigneeId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<TeamTaskDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<TeamTaskDto>>> GetByAssignee(Guid assigneeId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting tasks for assignee: {AssigneeId}", assigneeId);
        var tasks = await _service.GetByAssigneeIdAsync(assigneeId, cancellationToken);
        return Ok(tasks);
    }

    /// <summary>
    /// Gets tasks by project.
    /// </summary>
    /// <param name="projectId">The project ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of tasks in the specified project.</returns>
    [HttpGet("project/{projectId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<TeamTaskDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<TeamTaskDto>>> GetByProject(Guid projectId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting tasks for project: {ProjectId}", projectId);
        var tasks = await _service.GetByProjectIdAsync(projectId, cancellationToken);
        return Ok(tasks);
    }

    /// <summary>
    /// Gets tasks by status.
    /// </summary>
    /// <param name="status">The task status.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of tasks with the specified status.</returns>
    [HttpGet("status/{status}")]
    [ProducesResponseType(typeof(IEnumerable<TeamTaskDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<TeamTaskDto>>> GetByStatus(TaskStatus status, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting tasks with status: {Status}", status);
        var tasks = await _service.GetByStatusAsync(status, cancellationToken);
        return Ok(tasks);
    }

    /// <summary>
    /// Gets tasks by priority.
    /// </summary>
    /// <param name="priority">The task priority.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of tasks with the specified priority.</returns>
    [HttpGet("priority/{priority}")]
    [ProducesResponseType(typeof(IEnumerable<TeamTaskDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<TeamTaskDto>>> GetByPriority(TaskPriority priority, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting tasks with priority: {Priority}", priority);
        var tasks = await _service.GetByPriorityAsync(priority, cancellationToken);
        return Ok(tasks);
    }

    /// <summary>
    /// Gets overdue tasks.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of overdue tasks.</returns>
    [HttpGet("overdue")]
    [ProducesResponseType(typeof(IEnumerable<TeamTaskDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<TeamTaskDto>>> GetOverdue(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting overdue tasks");
        var tasks = await _service.GetOverdueAsync(cancellationToken);
        return Ok(tasks);
    }

    /// <summary>
    /// Gets unassigned tasks.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of unassigned tasks.</returns>
    [HttpGet("unassigned")]
    [ProducesResponseType(typeof(IEnumerable<TeamTaskDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<TeamTaskDto>>> GetUnassigned(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting unassigned tasks");
        var tasks = await _service.GetUnassignedAsync(cancellationToken);
        return Ok(tasks);
    }

    /// <summary>
    /// Gets task summary statistics.
    /// </summary>
    /// <param name="projectId">Optional project ID to filter by.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task summary with counts by status.</returns>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(TaskSummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<TaskSummaryDto>> GetSummary([FromQuery] Guid? projectId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting task summary for project: {ProjectId}", projectId);
        var summary = await _service.GetSummaryAsync(projectId, cancellationToken);
        return Ok(summary);
    }

    /// <summary>
    /// Creates a new task.
    /// </summary>
    /// <param name="dto">The task data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created task.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(TeamTaskDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TeamTaskDto>> Create([FromBody] CreateTeamTaskDto dto, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating new task: {Title}", dto.Title);

        try
        {
            var created = await _service.CreateAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Updates an existing task.
    /// </summary>
    /// <param name="id">The task ID.</param>
    /// <param name="dto">The task data to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated task.</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(TeamTaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TeamTaskDto>> Update(Guid id, [FromBody] UpdateTeamTaskDto dto, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating task with ID: {Id}", id);
        try
        {
            var updated = await _service.UpdateAsync(id, dto, cancellationToken);
            return Ok(updated);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Duplicates an existing task.
    /// </summary>
    /// <param name="id">The task ID to duplicate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The duplicated task.</returns>
    [HttpPost("{id:guid}/duplicate")]
    [ProducesResponseType(typeof(TeamTaskDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TeamTaskDto>> Duplicate(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Duplicating task with ID: {Id}", id);
        try
        {
            var duplicated = await _service.DuplicateAsync(id, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = duplicated.Id }, duplicated);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Overrides specific fields on a task, pinning them so Jira re-import preserves these values.
    /// </summary>
    /// <param name="id">The task ID.</param>
    /// <param name="dto">The fields to override.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated task.</returns>
    [HttpPatch("{id:guid}/overrides")]
    [ProducesResponseType(typeof(TeamTaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TeamTaskDto>> OverrideFields(Guid id, [FromBody] OverrideTeamTaskFieldsDto dto, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Overriding fields on task {Id}", id);

        try
        {
            var updated = await _service.OverrideFieldsAsync(id, dto, cancellationToken);
            return Ok(updated);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Clears overrides on specific fields, allowing Jira re-import to update them again.
    /// </summary>
    /// <param name="id">The task ID.</param>
    /// <param name="dto">The fields to clear overrides for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated task.</returns>
    [HttpPost("{id:guid}/clear-overrides")]
    [ProducesResponseType(typeof(TeamTaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TeamTaskDto>> ClearOverrides(Guid id, [FromBody] ClearTeamTaskOverridesDto dto, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Clearing overrides on task {Id} for fields: {Fields}", id, string.Join(", ", dto.Fields));

        try
        {
            var updated = await _service.ClearOverridesAsync(id, dto, cancellationToken);
            return Ok(updated);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Deletes a task.
    /// </summary>
    /// <param name="id">The task ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting task with ID: {Id}", id);

        try
        {
            await _service.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Deletes multiple tasks.
    /// </summary>
    /// <param name="ids">The list of task IDs to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpPost("bulk-delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteMany([FromBody] IEnumerable<Guid> ids, CancellationToken cancellationToken)
    {
        var idsList = ids.ToList();
        _logger.LogInformation("Bulk deleting {Count} tasks", idsList.Count);

        if (idsList.Count == 0)
        {
            return BadRequest(new { message = "No task IDs provided." });
        }

        try
        {
            await _service.DeleteManyAsync(idsList, cancellationToken);
            return NoContent();
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
