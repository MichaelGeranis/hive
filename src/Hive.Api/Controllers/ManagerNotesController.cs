using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Entities;
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
    /// Gets all notes with pagination and optional filtering.
    /// </summary>
    /// <param name="pageNumber">Page number (1-based, default: 1).</param>
    /// <param name="pageSize">Items per page (default: 20, max: 100).</param>
    /// <param name="filter">Status filter: all, pending, completed, overdue (default: all).</param>
    /// <param name="search">Search term to filter by title, content, or tags.</param>
    /// <param name="tag">Tag to filter by.</param>
    /// <param name="folderId">Folder to list. Omit to list notes from every folder.</param>
    /// <param name="sort">Ordering: recent (pinned first, latest edit first) or priority (default).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated list of notes in the requested order.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ManagerNoteDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ManagerNoteDto>>> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? filter = null,
        [FromQuery] string? search = null,
        [FromQuery] string? tag = null,
        [FromQuery] Guid? folderId = null,
        [FromQuery] string? sort = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting notes page {PageNumber} with size {PageSize}, filter: {Filter}, search: {Search}, tag: {Tag}, folder: {FolderId}",
            pageNumber, pageSize, filter, search, tag, folderId);

        // Parse filter string to enum
        var noteFilter = filter?.ToLowerInvariant() switch
        {
            "pending" => NoteFilter.Pending,
            "completed" => NoteFilter.Completed,
            "overdue" => NoteFilter.Overdue,
            _ => NoteFilter.All
        };

        var noteSort = sort?.ToLowerInvariant() switch
        {
            "recent" => NoteSortOrder.Recent,
            _ => NoteSortOrder.Priority
        };

        var pagination = new NotePaginationParams
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            Filter = noteFilter,
            SearchTerm = search,
            Tag = tag,
            FolderId = folderId,
            Sort = noteSort
        };

        var notes = await _service.GetFilteredPagedAsync(pagination, cancellationToken);
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
    /// Creates a blank note, ready to be written into.
    /// </summary>
    [HttpPost("blank")]
    [ProducesResponseType(typeof(ManagerNoteDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<ManagerNoteDto>> CreateBlank(
        [FromBody] CreateBlankNoteDto? dto,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating blank note in folder: {FolderId}", dto?.FolderId);
        var note = await _service.CreateBlankAsync(dto ?? new CreateBlankNoteDto(), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = note.Id }, note);
    }

    /// <summary>
    /// Saves the body of a note. The title is derived from the first line of the content.
    /// </summary>
    [HttpPut("{id:guid}/content")]
    [ProducesResponseType(typeof(ManagerNoteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ManagerNoteDto>> UpdateContent(
        Guid id,
        [FromBody] UpdateNoteContentDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var note = await _service.UpdateContentAsync(id, dto, cancellationToken);
            return Ok(note);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Files a note under a folder, or at the root when no folder is given.
    /// </summary>
    [HttpPost("{id:guid}/move")]
    [ProducesResponseType(typeof(ManagerNoteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ManagerNoteDto>> Move(
        Guid id,
        [FromBody] MoveNoteDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Moving note {Id} to folder {FolderId}", id, dto.FolderId);
            var note = await _service.MoveToFolderAsync(id, dto, cancellationToken);
            return Ok(note);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Pins or unpins a note.
    /// </summary>
    [HttpPost("{id:guid}/pin")]
    [ProducesResponseType(typeof(ManagerNoteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ManagerNoteDto>> TogglePin(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Toggling pin for note: {Id}", id);
            var note = await _service.TogglePinAsync(id, cancellationToken);
            return Ok(note);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
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
