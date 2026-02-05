using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hive.Api.Controllers;

/// <summary>
/// API Controller for managing meeting notes and action items.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class MeetingNotesController : ControllerBase
{
    private readonly IMeetingNoteService _service;
    private readonly ILogger<MeetingNotesController> _logger;

    public MeetingNotesController(IMeetingNoteService service, ILogger<MeetingNotesController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Gets a meeting note by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(MeetingNoteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MeetingNoteDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting meeting note with ID: {Id}", id);
        var note = await _service.GetByIdAsync(id, cancellationToken);

        if (note is null)
        {
            return NotFound(new { message = $"Meeting note with ID '{id}' not found." });
        }

        return Ok(note);
    }

    /// <summary>
    /// Gets all notes for a meeting.
    /// </summary>
    [HttpGet("by-meeting/{meetingId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<MeetingNoteDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<MeetingNoteDto>>> GetByMeeting(
        Guid meetingId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting notes for meeting: {MeetingId}", meetingId);
        var notes = await _service.GetByMeetingIdAsync(meetingId, cancellationToken);
        return Ok(notes);
    }

    /// <summary>
    /// Gets all action items, optionally filtered by direct report.
    /// </summary>
    [HttpGet("action-items")]
    [ProducesResponseType(typeof(IEnumerable<MeetingNoteDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<MeetingNoteDto>>> GetActionItems(
        [FromQuery] Guid? directReportId = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting action items (directReportId: {DirectReportId})", directReportId);
        var notes = await _service.GetActionItemsAsync(directReportId, cancellationToken);
        return Ok(notes);
    }

    /// <summary>
    /// Gets all open (not completed/cancelled) action items.
    /// </summary>
    [HttpGet("action-items/open")]
    [ProducesResponseType(typeof(IEnumerable<MeetingNoteDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<MeetingNoteDto>>> GetOpenActionItems(
        [FromQuery] Guid? directReportId = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting open action items (directReportId: {DirectReportId})", directReportId);
        var notes = await _service.GetOpenActionItemsAsync(directReportId, cancellationToken);
        return Ok(notes);
    }

    /// <summary>
    /// Gets all overdue action items.
    /// </summary>
    [HttpGet("action-items/overdue")]
    [ProducesResponseType(typeof(IEnumerable<MeetingNoteDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<MeetingNoteDto>>> GetOverdueActionItems(
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting overdue action items");
        var notes = await _service.GetOverdueActionItemsAsync(cancellationToken);
        return Ok(notes);
    }

    /// <summary>
    /// Creates a new meeting note.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(MeetingNoteDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MeetingNoteDto>> Create(
        [FromBody] CreateMeetingNoteDto dto,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating note for meeting: {MeetingId}", dto.MeetingId);

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
    /// Updates a meeting note.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(MeetingNoteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MeetingNoteDto>> Update(
        Guid id,
        [FromBody] UpdateMeetingNoteDto dto,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating note: {Id}", id);

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
    /// Updates the status of an action item.
    /// </summary>
    [HttpPut("{id:guid}/action-status")]
    [ProducesResponseType(typeof(MeetingNoteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MeetingNoteDto>> UpdateActionStatus(
        Guid id,
        [FromBody] UpdateActionStatusDto dto,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating action status for note: {Id}", id);

        try
        {
            var updated = await _service.UpdateActionStatusAsync(id, dto, cancellationToken);
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
    /// Marks an action item as completed.
    /// </summary>
    [HttpPost("{id:guid}/complete")]
    [ProducesResponseType(typeof(MeetingNoteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MeetingNoteDto>> CompleteAction(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Completing action item: {Id}", id);

        try
        {
            var updated = await _service.CompleteActionAsync(id, cancellationToken);
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
    /// Deletes a meeting note.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting note: {Id}", id);

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
