using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Entities;
using Hive.Core.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hive.Api.Controllers;

/// <summary>
/// API Controller for managing one-on-one meetings.
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
    /// Gets all one-on-one meetings.
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
    /// Gets upcoming meetings within specified days.
    /// </summary>
    [HttpGet("upcoming")]
    [ProducesResponseType(typeof(IEnumerable<OneOnOneMeetingDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<OneOnOneMeetingDto>>> GetUpcoming(
        [FromQuery] int days = 7,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting upcoming meetings for next {Days} days", days);
        var meetings = await _service.GetUpcomingAsync(days, cancellationToken);
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
    /// Gets the next scheduled meeting for a direct report.
    /// </summary>
    [HttpGet("next/{directReportId:guid}")]
    [ProducesResponseType(typeof(OneOnOneMeetingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OneOnOneMeetingDto>> GetNextMeeting(
        Guid directReportId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting next meeting for direct report: {DirectReportId}", directReportId);
        var meeting = await _service.GetNextMeetingAsync(directReportId, cancellationToken);

        if (meeting is null)
        {
            return NotFound(new { message = "No upcoming meeting found for this direct report." });
        }

        return Ok(meeting);
    }

    /// <summary>
    /// Gets meetings by status.
    /// </summary>
    [HttpGet("by-status/{status}")]
    [ProducesResponseType(typeof(IEnumerable<OneOnOneMeetingDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<OneOnOneMeetingDto>>> GetByStatus(
        MeetingStatus status,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting meetings with status: {Status}", status);
        var meetings = await _service.GetByStatusAsync(status, cancellationToken);
        return Ok(meetings);
    }

    /// <summary>
    /// Creates a new one-on-one meeting.
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
    /// Updates a meeting.
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
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Marks a meeting as completed.
    /// </summary>
    [HttpPost("{id:guid}/complete")]
    [ProducesResponseType(typeof(OneOnOneMeetingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OneOnOneMeetingDto>> Complete(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Completing meeting: {Id}", id);

        try
        {
            var updated = await _service.CompleteAsync(id, cancellationToken);
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
    /// Cancels a meeting.
    /// </summary>
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(OneOnOneMeetingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OneOnOneMeetingDto>> Cancel(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Cancelling meeting: {Id}", id);

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
    /// Reschedules a meeting.
    /// </summary>
    [HttpPost("{id:guid}/reschedule")]
    [ProducesResponseType(typeof(OneOnOneMeetingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OneOnOneMeetingDto>> Reschedule(
        Guid id,
        [FromBody] RescheduleMeetingDto dto,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Rescheduling meeting: {Id}", id);

        try
        {
            var updated = await _service.RescheduleAsync(id, dto, cancellationToken);
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
    /// Deletes a meeting.
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
