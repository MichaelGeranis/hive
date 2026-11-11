using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hive.Api.Controllers;

/// <summary>
/// API Controller for pulling data from Jira on demand: everything at once, or one part.
/// </summary>
[ApiController]
[Route("api/jira-sync")]
[Authorize]
[Produces("application/json")]
public class JiraSyncController : ControllerBase
{
    private readonly IJiraSyncService _service;
    private readonly ILogger<JiraSyncController> _logger;

    public JiraSyncController(IJiraSyncService service, ILogger<JiraSyncController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Runs every Jira sync: sprint dates, products, parents and tasks. A part that is not configured, or
    /// that Jira refuses, is reported in its place without stopping the rest.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Each part's result or error.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(JiraSyncAllResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<JiraSyncAllResultDto>> SyncAll(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Syncing everything from Jira");
        return Ok(await _service.SyncAllAsync(cancellationToken));
    }

    /// <summary>
    /// Makes Hive's products match the components of the configured Jira space.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Products created, updated and removed, and components that could not be applied.</returns>
    [HttpPost("products")]
    [ProducesResponseType(typeof(JiraProductSyncResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<JiraProductSyncResultDto>> SyncProducts(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Syncing products from Jira");

        try
        {
            return Ok(await _service.SyncProductsAsync(cancellationToken));
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (JiraApiException ex)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Makes Hive's parents match the parent issues (epics and anything with sub-tasks) of the
    /// configured Jira space.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Parents created and updated, and issues that could not be applied.</returns>
    [HttpPost("parents")]
    [ProducesResponseType(typeof(JiraParentSyncResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<JiraParentSyncResultDto>> SyncParents(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Syncing parents from Jira");

        try
        {
            return Ok(await _service.SyncParentsAsync(cancellationToken));
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (JiraApiException ex)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Makes Hive's tasks match the work items in the team's sprints in the configured Jira
    /// space, after syncing parents.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Tasks created, updated and removed, and the parent sync that ran first.</returns>
    [HttpPost("tasks")]
    [ProducesResponseType(typeof(JiraTaskSyncResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<JiraTaskSyncResultDto>> SyncTasks(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Syncing tasks from Jira");

        try
        {
            return Ok(await _service.SyncTasksAsync(cancellationToken));
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (JiraApiException ex)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Copies sprint start and end dates from the configured Jira board onto Hive's sprints.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>What changed, and which sprints could not be matched.</returns>
    [HttpPost("sprint-dates")]
    [ProducesResponseType(typeof(JiraSprintSyncResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<JiraSprintSyncResultDto>> SyncSprintDates(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Syncing sprint dates from Jira");

        try
        {
            var result = await _service.SyncSprintDatesAsync(cancellationToken);
            return Ok(result);
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (JiraApiException ex)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { message = ex.Message });
        }
    }
}
