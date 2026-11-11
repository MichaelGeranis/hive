using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hive.Api.Controllers;

/// <summary>
/// API Controller for the folders that organise manager notes.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class NoteFoldersController : ControllerBase
{
    private readonly INoteFolderService _service;
    private readonly ILogger<NoteFoldersController> _logger;

    public NoteFoldersController(INoteFolderService service, ILogger<NoteFoldersController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Gets every folder, each with the number of notes filed directly in it.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<NoteFolderDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<NoteFolderDto>>> GetAll(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting all note folders");
        var folders = await _service.GetAllAsync(cancellationToken);
        return Ok(folders);
    }

    /// <summary>
    /// Gets a folder by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(NoteFolderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NoteFolderDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting note folder with ID: {Id}", id);
        var folder = await _service.GetByIdAsync(id, cancellationToken);

        if (folder is null)
        {
            return NotFound(new { message = $"Note folder with ID '{id}' not found." });
        }

        return Ok(folder);
    }

    /// <summary>
    /// Creates a folder.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(NoteFolderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NoteFolderDto>> Create(
        [FromBody] CreateNoteFolderDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Creating note folder: {Name}", dto.Name);
            var folder = await _service.CreateAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = folder.Id }, folder);
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
    /// Renames, moves or reorders a folder.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(NoteFolderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NoteFolderDto>> Update(
        Guid id,
        [FromBody] UpdateNoteFolderDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Updating note folder: {Id}", id);
            var folder = await _service.UpdateAsync(id, dto, cancellationToken);
            return Ok(folder);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Deletes a folder. Its notes and sub-folders move up to the folder that contained it.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Deleting note folder: {Id}", id);
            await _service.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
