using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Entities;
using Hive.Core.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hive.Api.Controllers;

/// <summary>
/// API Controller for managing skills in the competency matrix.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class SkillsController : ControllerBase
{
    private readonly ISkillService _service;
    private readonly ILogger<SkillsController> _logger;

    public SkillsController(ISkillService service, ILogger<SkillsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Gets all skills.
    /// </summary>
    /// <param name="includeInactive">Include inactive skills in the result.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<SkillDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<SkillDto>>> GetAll(
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting all skills (includeInactive: {IncludeInactive})", includeInactive);
        var skills = await _service.GetAllAsync(includeInactive, cancellationToken);
        return Ok(skills);
    }

    /// <summary>
    /// Gets a skill by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SkillDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SkillDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting skill with ID: {Id}", id);
        var skill = await _service.GetByIdAsync(id, cancellationToken);

        if (skill is null)
        {
            return NotFound(new { message = $"Skill with ID '{id}' not found." });
        }

        return Ok(skill);
    }

    /// <summary>
    /// Gets skills by category.
    /// </summary>
    [HttpGet("by-category/{category}")]
    [ProducesResponseType(typeof(IEnumerable<SkillDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<SkillDto>>> GetByCategory(
        SkillCategory category,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting skills by category: {Category}", category);
        var skills = await _service.GetByCategoryAsync(category, cancellationToken);
        return Ok(skills);
    }

    /// <summary>
    /// Creates a new skill.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(SkillDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SkillDto>> Create(
        [FromBody] CreateSkillDto dto,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating new skill: {Name}", dto.Name);

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
    /// Updates an existing skill.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(SkillDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SkillDto>> Update(
        Guid id,
        [FromBody] UpdateSkillDto dto,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating skill with ID: {Id}", id);

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
    /// Activates a skill.
    /// </summary>
    [HttpPost("{id:guid}/activate")]
    [ProducesResponseType(typeof(SkillDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SkillDto>> Activate(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Activating skill with ID: {Id}", id);

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
    /// Deactivates a skill.
    /// </summary>
    [HttpPost("{id:guid}/deactivate")]
    [ProducesResponseType(typeof(SkillDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SkillDto>> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deactivating skill with ID: {Id}", id);

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
    /// Deletes a skill.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting skill with ID: {Id}", id);

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
