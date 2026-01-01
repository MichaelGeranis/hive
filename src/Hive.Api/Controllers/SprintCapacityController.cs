using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hive.Api.Controllers;

/// <summary>
/// API Controller for managing sprint capacity.
/// </summary>
[ApiController]
[Route("api/sprint-capacity")]
[Authorize]
[Produces("application/json")]
public class SprintCapacityController : ControllerBase
{
    private readonly ISprintCapacityService _service;
    private readonly ILogger<SprintCapacityController> _logger;

    public SprintCapacityController(ISprintCapacityService service, ILogger<SprintCapacityController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Gets all sprint capacities.
    /// </summary>
    /// <returns>List of all sprint capacities.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<SprintCapacityDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<SprintCapacityDto>>> GetAll(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting all sprint capacities");
        var capacities = await _service.GetAllAsync(cancellationToken);
        return Ok(capacities);
    }

    /// <summary>
    /// Gets sprint capacity by ID.
    /// </summary>
    /// <param name="id">The sprint capacity ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The sprint capacity if found.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SprintCapacityDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SprintCapacityDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting sprint capacity with ID: {Id}", id);
        var capacity = await _service.GetByIdAsync(id, cancellationToken);

        if (capacity is null)
        {
            return NotFound(new { message = $"Sprint capacity with ID '{id}' not found." });
        }

        return Ok(capacity);
    }

    /// <summary>
    /// Gets sprint capacity by sprint ID.
    /// </summary>
    /// <param name="sprintId">The sprint ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The sprint capacity if found.</returns>
    [HttpGet("sprint/{sprintId:guid}")]
    [ProducesResponseType(typeof(SprintCapacityDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SprintCapacityDto>> GetBySprintId(Guid sprintId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting sprint capacity for sprint: {SprintId}", sprintId);
        var capacity = await _service.GetBySprintIdAsync(sprintId, cancellationToken);

        if (capacity is null)
        {
            return NotFound(new { message = $"Sprint capacity for sprint '{sprintId}' not found." });
        }

        return Ok(capacity);
    }

    /// <summary>
    /// Creates or updates sprint capacity.
    /// </summary>
    /// <param name="dto">The sprint capacity data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created or updated sprint capacity.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(SprintCapacityDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SprintCapacityDto>> CreateOrUpdate([FromBody] CreateSprintCapacityDto dto, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating or updating sprint capacity for sprint: {SprintId}", dto.SprintId);

        try
        {
            var capacity = await _service.CreateOrUpdateAsync(dto, cancellationToken);
            return Ok(capacity);
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
    /// Deletes sprint capacity.
    /// </summary>
    /// <param name="id">The sprint capacity ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting sprint capacity with ID: {Id}", id);

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
