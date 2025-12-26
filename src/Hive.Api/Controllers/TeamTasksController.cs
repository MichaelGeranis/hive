using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Entities;
using Hive.Core.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
    /// Gets all tasks.
    /// </summary>
    /// <returns>List of all tasks.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<TeamTaskDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<TeamTaskDto>>> GetAll(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting all tasks");
        var tasks = await _service.GetAllAsync(cancellationToken);
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
    /// <param name="dto">The updated task data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated task.</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(TeamTaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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
    /// Assigns a task to a team member.
    /// </summary>
    /// <param name="id">The task ID.</param>
    /// <param name="dto">The assignment data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated task.</returns>
    [HttpPost("{id:guid}/assign")]
    [ProducesResponseType(typeof(TeamTaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TeamTaskDto>> Assign(Guid id, [FromBody] AssignTaskDto dto, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Assigning task {Id} to {AssigneeId}", id, dto.AssigneeId);

        try
        {
            var updated = await _service.AssignAsync(id, dto, cancellationToken);
            return Ok(updated);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Moves a task to backlog.
    /// </summary>
    /// <param name="id">The task ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated task.</returns>
    [HttpPost("{id:guid}/backlog")]
    [ProducesResponseType(typeof(TeamTaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TeamTaskDto>> MoveToBacklog(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Moving task {Id} to backlog", id);

        try
        {
            var updated = await _service.MoveToBacklogAsync(id, cancellationToken);
            return Ok(updated);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Moves a task to todo.
    /// </summary>
    /// <param name="id">The task ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated task.</returns>
    [HttpPost("{id:guid}/todo")]
    [ProducesResponseType(typeof(TeamTaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TeamTaskDto>> MoveToTodo(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Moving task {Id} to todo", id);

        try
        {
            var updated = await _service.MoveToTodoAsync(id, cancellationToken);
            return Ok(updated);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Starts work on a task.
    /// </summary>
    /// <param name="id">The task ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated task.</returns>
    [HttpPost("{id:guid}/start")]
    [ProducesResponseType(typeof(TeamTaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TeamTaskDto>> Start(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting task {Id}", id);

        try
        {
            var updated = await _service.StartAsync(id, cancellationToken);
            return Ok(updated);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Moves a task to review.
    /// </summary>
    /// <param name="id">The task ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated task.</returns>
    [HttpPost("{id:guid}/review")]
    [ProducesResponseType(typeof(TeamTaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TeamTaskDto>> MoveToReview(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Moving task {Id} to review", id);

        try
        {
            var updated = await _service.MoveToReviewAsync(id, cancellationToken);
            return Ok(updated);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Completes a task.
    /// </summary>
    /// <param name="id">The task ID.</param>
    /// <param name="dto">Optional completion data with actual hours.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated task.</returns>
    [HttpPost("{id:guid}/complete")]
    [ProducesResponseType(typeof(TeamTaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TeamTaskDto>> Complete(Guid id, [FromBody] CompleteTaskDto? dto, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Completing task {Id}", id);

        try
        {
            var updated = await _service.CompleteAsync(id, dto, cancellationToken);
            return Ok(updated);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Cancels a task.
    /// </summary>
    /// <param name="id">The task ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated task.</returns>
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(TeamTaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TeamTaskDto>> Cancel(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Cancelling task {Id}", id);

        try
        {
            var updated = await _service.CancelAsync(id, cancellationToken);
            return Ok(updated);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Reopens a completed or cancelled task.
    /// </summary>
    /// <param name="id">The task ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated task.</returns>
    [HttpPost("{id:guid}/reopen")]
    [ProducesResponseType(typeof(TeamTaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TeamTaskDto>> Reopen(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Reopening task {Id}", id);

        try
        {
            var updated = await _service.ReopenAsync(id, cancellationToken);
            return Ok(updated);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Logs hours worked on a task.
    /// </summary>
    /// <param name="id">The task ID.</param>
    /// <param name="dto">The hours to log.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated task.</returns>
    [HttpPost("{id:guid}/log-hours")]
    [ProducesResponseType(typeof(TeamTaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TeamTaskDto>> LogHours(Guid id, [FromBody] LogHoursDto dto, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Logging {Hours} hours on task {Id}", dto.Hours, id);

        try
        {
            var updated = await _service.LogHoursAsync(id, dto, cancellationToken);
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
}
