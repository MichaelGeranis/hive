using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hive.Api.Controllers;

/// <summary>
/// API Controller for managing skill categories.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class SkillCategoriesController : ControllerBase
{
    private readonly ISkillCategoryService _service;
    private readonly ILogger<SkillCategoriesController> _logger;

    public SkillCategoriesController(ISkillCategoryService service, ILogger<SkillCategoriesController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Gets all skill categories.
    /// </summary>
    /// <param name="includeInactive">Include inactive categories in the result.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<SkillCategoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<SkillCategoryDto>>> GetAll(
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting all skill categories (includeInactive: {IncludeInactive})", includeInactive);
        var categories = await _service.GetAllAsync(includeInactive, cancellationToken);
        return Ok(categories);
    }

    /// <summary>
    /// Gets a skill category by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SkillCategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SkillCategoryDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting skill category with ID: {Id}", id);
        var category = await _service.GetByIdAsync(id, cancellationToken);

        if (category is null)
        {
            return NotFound(new { message = $"Skill category with ID '{id}' not found." });
        }

        return Ok(category);
    }

    /// <summary>
    /// Creates a new skill category.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(SkillCategoryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SkillCategoryDto>> Create(
        [FromBody] CreateSkillCategoryDto dto,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating new skill category: {Name}", dto.Name);

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
    /// Updates an existing skill category.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(SkillCategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SkillCategoryDto>> Update(
        Guid id,
        [FromBody] UpdateSkillCategoryDto dto,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating skill category with ID: {Id}", id);

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
    /// Activates a skill category.
    /// </summary>
    [HttpPost("{id:guid}/activate")]
    [ProducesResponseType(typeof(SkillCategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SkillCategoryDto>> Activate(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Activating skill category with ID: {Id}", id);

        try
        {
            var updated = await _service.ActivateAsync(id, cancellationToken);
            return Ok(updated);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Deactivates a skill category.
    /// </summary>
    [HttpPost("{id:guid}/deactivate")]
    [ProducesResponseType(typeof(SkillCategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SkillCategoryDto>> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deactivating skill category with ID: {Id}", id);

        try
        {
            var updated = await _service.DeactivateAsync(id, cancellationToken);
            return Ok(updated);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Deletes a skill category.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting skill category with ID: {Id}", id);

        try
        {
            await _service.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ConflictException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}
