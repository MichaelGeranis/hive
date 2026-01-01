using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hive.Api.Controllers;

/// <summary>
/// API Controller for managing sprints.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class SprintsController : ControllerBase
{
    private readonly ISprintService _service;
    private readonly ILogger<SprintsController> _logger;

    public SprintsController(ISprintService service, ILogger<SprintsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Gets all sprints.
    /// </summary>
    /// <returns>List of all sprints.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<SprintDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<SprintDto>>> GetAll(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting all sprints");
        var sprints = await _service.GetAllAsync(cancellationToken);
        return Ok(sprints);
    }

    /// <summary>
    /// Gets a sprint by ID.
    /// </summary>
    /// <param name="id">The sprint ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The sprint if found.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SprintDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SprintDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting sprint with ID: {Id}", id);
        var sprint = await _service.GetByIdAsync(id, cancellationToken);

        if (sprint is null)
        {
            return NotFound(new { message = $"Sprint with ID '{id}' not found." });
        }

        return Ok(sprint);
    }

    /// <summary>
    /// Gets a sprint by name.
    /// </summary>
    /// <param name="name">The sprint name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The sprint if found.</returns>
    [HttpGet("by-name/{name}")]
    [ProducesResponseType(typeof(SprintDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SprintDto>> GetByName(string name, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting sprint with name: {Name}", name);
        var sprint = await _service.GetByNameAsync(name, cancellationToken);

        if (sprint is null)
        {
            return NotFound(new { message = $"Sprint with name '{name}' not found." });
        }

        return Ok(sprint);
    }

    /// <summary>
    /// Gets sprints by team name.
    /// </summary>
    /// <param name="teamName">The team name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of sprints for the team.</returns>
    [HttpGet("team/{teamName}")]
    [ProducesResponseType(typeof(IEnumerable<SprintDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<SprintDto>>> GetByTeam(string teamName, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting sprints for team: {TeamName}", teamName);
        var sprints = await _service.GetByTeamAsync(teamName, cancellationToken);
        return Ok(sprints);
    }

    /// <summary>
    /// Gets sprints by year.
    /// </summary>
    /// <param name="year">The year.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of sprints for the year.</returns>
    [HttpGet("year/{year:int}")]
    [ProducesResponseType(typeof(IEnumerable<SprintDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<SprintDto>>> GetByYear(int year, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting sprints for year: {Year}", year);
        var sprints = await _service.GetByYearAsync(year, cancellationToken);
        return Ok(sprints);
    }

    /// <summary>
    /// Gets sprints by year and quarter.
    /// </summary>
    /// <param name="year">The year.</param>
    /// <param name="quarter">The quarter (1-4).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of sprints for the year and quarter.</returns>
    [HttpGet("year/{year:int}/quarter/{quarter:int}")]
    [ProducesResponseType(typeof(IEnumerable<SprintDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<SprintDto>>> GetByYearQuarter(int year, int quarter, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting sprints for year: {Year}, quarter: {Quarter}", year, quarter);
        var sprints = await _service.GetByYearQuarterAsync(year, quarter, cancellationToken);
        return Ok(sprints);
    }

    /// <summary>
    /// Creates a new sprint.
    /// </summary>
    /// <param name="dto">The sprint data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created sprint.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(SprintDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SprintDto>> Create([FromBody] CreateSprintDto dto, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating new sprint: {Name}", dto.Name);

        try
        {
            var created = await _service.CreateAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (ConflictException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Deletes a sprint.
    /// </summary>
    /// <param name="id">The sprint ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting sprint with ID: {Id}", id);

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
