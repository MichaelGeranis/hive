using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hive.Api.Controllers;

/// <summary>
/// API Controller for managing personal manager notes/TODOs.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class ManagerNotesController : ControllerBase
{
    private readonly IManagerNoteService _service;
    private readonly ILogger<ManagerNotesController> _logger;

    public ManagerNotesController(IManagerNoteService service, ILogger<ManagerNotesController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Gets all notes.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ManagerNoteDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ManagerNoteDto>>> GetAll(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting all manager notes");
        var notes = await _service.GetAllAsync(cancellationToken);
        return Ok(notes);
    }

    /// <summary>
    /// Searches notes by text and/or tag.
    /// </summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(IEnumerable<ManagerNoteDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ManagerNoteDto>>> Search(
        [FromQuery] string? q,
        [FromQuery] string? tag,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Searching manager notes with term: {Term}, tag: {Tag}", q, tag);
        var notes = await _service.SearchAsync(q, tag, cancellationToken);
        return Ok(notes);
    }

    /// <summary>
    /// Gets all unique tags.
    /// </summary>
    [HttpGet("tags")]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<string>>> GetTags(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting all tags");
        var tags = await _service.GetAllTagsAsync(cancellationToken);
        return Ok(tags);
    }

    /// <summary>
    /// Gets notes by tag.
    /// </summary>
    [HttpGet("by-tag/{tag}")]
    [ProducesResponseType(typeof(IEnumerable<ManagerNoteDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ManagerNoteDto>>> GetByTag(string tag, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting manager notes with tag: {Tag}", tag);
        var notes = await _service.GetByTagAsync(tag, cancellationToken);
        return Ok(notes);
    }

    /// <summary>
    /// Gets pending (incomplete) notes.
    /// </summary>
    [HttpGet("pending")]
    [ProducesResponseType(typeof(IEnumerable<ManagerNoteDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ManagerNoteDto>>> GetPending(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting pending manager notes");
        var notes = await _service.GetPendingAsync(cancellationToken);
        return Ok(notes);
    }

    /// <summary>
    /// Gets completed notes.
    /// </summary>
    [HttpGet("completed")]
    [ProducesResponseType(typeof(IEnumerable<ManagerNoteDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ManagerNoteDto>>> GetCompleted(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting completed manager notes");
        var notes = await _service.GetCompletedAsync(cancellationToken);
        return Ok(notes);
    }

    /// <summary>
    /// Gets overdue notes.
    /// </summary>
    [HttpGet("overdue")]
    [ProducesResponseType(typeof(IEnumerable<ManagerNoteDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ManagerNoteDto>>> GetOverdue(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting overdue manager notes");
        var notes = await _service.GetOverdueAsync(cancellationToken);
        return Ok(notes);
    }

    /// <summary>
    /// Gets a note by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ManagerNoteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ManagerNoteDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting manager note with ID: {Id}", id);
        var note = await _service.GetByIdAsync(id, cancellationToken);

        if (note is null)
        {
            return NotFound(new { message = $"Manager note with ID '{id}' not found." });
        }

        return Ok(note);
    }

    /// <summary>
    /// Creates a new note.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ManagerNoteDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ManagerNoteDto>> Create(
        [FromBody] CreateManagerNoteDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Creating manager note: {Title}", dto.Title);
            var note = await _service.CreateAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = note.Id }, note);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Updates an existing note.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ManagerNoteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ManagerNoteDto>> Update(
        Guid id,
        [FromBody] UpdateManagerNoteDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Updating manager note: {Id}", id);
            var note = await _service.UpdateAsync(id, dto, cancellationToken);
            return Ok(note);
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
    /// Toggles the completion status of a note.
    /// </summary>
    [HttpPost("{id:guid}/toggle")]
    [ProducesResponseType(typeof(ManagerNoteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ManagerNoteDto>> ToggleComplete(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Toggling completion for manager note: {Id}", id);
            var note = await _service.ToggleCompleteAsync(id, cancellationToken);
            return Ok(note);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Deletes a note.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Deleting manager note: {Id}", id);
            await _service.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
