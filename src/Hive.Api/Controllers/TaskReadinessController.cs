using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hive.Api.Controllers;

/// <summary>
/// API Controller for whether the upcoming tasks are ready to build.
/// </summary>
[ApiController]
[Route("api/task-readiness")]
[Authorize]
[Produces("application/json")]
public class TaskReadinessController : ControllerBase
{
    private readonly ITaskReadinessService _service;
    private readonly ILogger<TaskReadinessController> _logger;

    public TaskReadinessController(ITaskReadinessService service, ILogger<TaskReadinessController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Gets the upcoming tasks with their facts and AI reviews. Asks no model.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(TaskReadinessOverviewDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<TaskReadinessOverviewDto>> Get(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting task readiness");
        return Ok(await _service.GetOverviewAsync(cancellationToken));
    }

    /// <summary>
    /// Reviews the next upcoming task that has no up-to-date review.
    /// </summary>
    /// <returns>The overview after the review; a failed review carries its error.</returns>
    [HttpPost("review-next")]
    [ProducesResponseType(typeof(TaskReadinessOverviewDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<TaskReadinessOverviewDto>> ReviewNext(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Reviewing the next upcoming task");
        return Ok(await _service.ReviewNextAsync(cancellationToken));
    }

    /// <summary>
    /// Reviews one task again.
    /// </summary>
    /// <returns>The overview after the review; a failed review carries its error.</returns>
    [HttpPost("tasks/{id:guid}/review")]
    [ProducesResponseType(typeof(TaskReadinessOverviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskReadinessOverviewDto>> ReviewTask(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Reviewing task {TaskId}", id);
        var overview = await _service.ReviewTaskAsync(id, cancellationToken);
        return overview is null ? NotFound(new { message = "Task not found" }) : Ok(overview);
    }
}
