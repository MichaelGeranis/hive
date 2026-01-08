using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hive.Api.Controllers;

/// <summary>
/// API Controller for managing parents (task groupings like Epics).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class ParentsController : ControllerBase
{
    private readonly IParentService _service;
    private readonly ILogger<ParentsController> _logger;

    public ParentsController(IParentService service, ILogger<ParentsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Gets all parents.
    /// </summary>
    /// <returns>List of all parents.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ParentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ParentDto>>> GetAll(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting all parents");
        var parents = await _service.GetAllAsync(cancellationToken);
        return Ok(parents);
    }

    /// <summary>
    /// Gets a parent by ID.
    /// </summary>
    /// <param name="id">The parent ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The parent if found.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ParentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ParentDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting parent with ID: {Id}", id);
        var parent = await _service.GetByIdAsync(id, cancellationToken);

        if (parent is null)
        {
            return NotFound(new { message = $"Parent with ID '{id}' not found." });
        }

        return Ok(parent);
    }

    /// <summary>
    /// Gets a parent by name.
    /// </summary>
    /// <param name="name">The parent name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The parent if found.</returns>
    [HttpGet("by-name/{name}")]
    [ProducesResponseType(typeof(ParentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ParentDto>> GetByName(string name, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting parent with name: {Name}", name);
        var parent = await _service.GetByNameAsync(name, cancellationToken);

        if (parent is null)
        {
            return NotFound(new { message = $"Parent with name '{name}' not found." });
        }

        return Ok(parent);
    }

    /// <summary>
    /// Creates a new parent.
    /// </summary>
    /// <param name="dto">The parent data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created parent.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ParentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ParentDto>> Create([FromBody] CreateParentDto dto, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating new parent: {Name}", dto.Name);

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
    /// Updates a parent.
    /// </summary>
    /// <param name="id">The parent ID.</param>
    /// <param name="dto">The updated parent data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated parent.</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ParentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ParentDto>> Update(Guid id, [FromBody] UpdateParentDto dto, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating parent with ID: {Id}", id);

        try
        {
            var updated = await _service.UpdateAsync(id, dto, cancellationToken);
            return Ok(updated);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
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
    /// Deletes a parent.
    /// </summary>
    /// <param name="id">The parent ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting parent with ID: {Id}", id);

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
