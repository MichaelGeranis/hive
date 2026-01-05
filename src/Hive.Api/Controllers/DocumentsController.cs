using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hive.Api.Controllers;

/// <summary>
/// API Controller for managing documents.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _service;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(IDocumentService service, ILogger<DocumentsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Gets all documents.
    /// </summary>
    /// <returns>List of all documents.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<DocumentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<DocumentDto>>> GetAll(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting all documents");
        var documents = await _service.GetAllAsync(cancellationToken);
        return Ok(documents);
    }

    /// <summary>
    /// Gets documents by tags.
    /// </summary>
    /// <param name="tags">Comma-separated list of tags.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of documents matching the tags.</returns>
    [HttpGet("by-tags")]
    [ProducesResponseType(typeof(IEnumerable<DocumentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<DocumentDto>>> GetByTags([FromQuery] string tags, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting documents by tags: {Tags}", tags);
        var documents = await _service.GetByTagsAsync(tags, cancellationToken);
        return Ok(documents);
    }

    /// <summary>
    /// Gets a document by ID.
    /// </summary>
    /// <param name="id">The document ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The document if found.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(DocumentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting document by ID: {Id}", id);
        var document = await _service.GetByIdAsync(id, cancellationToken);
        if (document is null)
        {
            return NotFound();
        }
        return Ok(document);
    }

    /// <summary>
    /// Creates a new document.
    /// </summary>
    /// <param name="dto">The document data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created document.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(DocumentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DocumentDto>> Create(CreateDocumentDto dto, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating new document: {Title}", dto.Title);
        try
        {
            var document = await _service.CreateAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = document.Id }, document);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Updates an existing document.
    /// </summary>
    /// <param name="id">The document ID.</param>
    /// <param name="dto">The updated document data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated document.</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(DocumentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DocumentDto>> Update(Guid id, UpdateDocumentDto dto, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating document {Id}: {Title}", id, dto.Title);
        try
        {
            var document = await _service.UpdateAsync(id, dto, cancellationToken);
            return Ok(document);
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Deletes a document.
    /// </summary>
    /// <param name="id">The document ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content if successful.</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting document {Id}", id);
        try
        {
            await _service.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
    }
}