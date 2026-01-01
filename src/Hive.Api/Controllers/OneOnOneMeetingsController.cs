using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hive.Api.Controllers;

/// <summary>
/// API Controller for managing one-on-one meeting records.
/// Simplified for note tracking - no scheduling workflow.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class OneOnOneMeetingsController : ControllerBase
{
    private readonly IOneOnOneMeetingService _service;
    private readonly ILogger<OneOnOneMeetingsController> _logger;

    public OneOnOneMeetingsController(IOneOnOneMeetingService service, ILogger<OneOnOneMeetingsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Gets all one-on-one meetings ordered by date descending.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<OneOnOneMeetingDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<OneOnOneMeetingDto>>> GetAll(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting all one-on-one meetings");
        var meetings = await _service.GetAllAsync(cancellationToken);
        return Ok(meetings);
    }

    /// <summary>
    /// Gets a meeting by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OneOnOneMeetingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OneOnOneMeetingDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting meeting with ID: {Id}", id);
        var meeting = await _service.GetByIdAsync(id, cancellationToken);

        if (meeting is null)
        {
            return NotFound(new { message = $"Meeting with ID '{id}' not found." });
        }

        return Ok(meeting);
    }

    /// <summary>
    /// Gets full meeting details including all notes.
    /// </summary>
    [HttpGet("{id:guid}/details")]
    [ProducesResponseType(typeof(OneOnOneMeetingDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OneOnOneMeetingDetailsDto>> GetDetails(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting meeting details for ID: {Id}", id);
        var details = await _service.GetDetailsAsync(id, cancellationToken);

        if (details is null)
        {
            return NotFound(new { message = $"Meeting with ID '{id}' not found." });
        }

        return Ok(details);
    }

    /// <summary>
    /// Gets all meetings for a specific direct report.
    /// </summary>
    [HttpGet("by-direct-report/{directReportId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<OneOnOneMeetingDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<OneOnOneMeetingDto>>> GetByDirectReport(
        Guid directReportId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting meetings for direct report: {DirectReportId}", directReportId);
        var meetings = await _service.GetByDirectReportIdAsync(directReportId, cancellationToken);
        return Ok(meetings);
    }

    /// <summary>
    /// Creates a new one-on-one meeting record.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(OneOnOneMeetingDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OneOnOneMeetingDto>> Create(
        [FromBody] CreateOneOnOneMeetingDto dto,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating meeting for direct report: {DirectReportId}", dto.DirectReportId);

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
    /// Updates a meeting record.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(OneOnOneMeetingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OneOnOneMeetingDto>> Update(
        Guid id,
        [FromBody] UpdateOneOnOneMeetingDto dto,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating meeting: {Id}", id);

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
    /// Deletes a meeting record.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting meeting: {Id}", id);

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
