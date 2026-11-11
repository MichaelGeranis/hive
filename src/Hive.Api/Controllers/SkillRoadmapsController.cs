using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hive.Api.Controllers;

/// <summary>
/// API Controller for skill roadmaps and the built-in roadmap library.
/// </summary>
[ApiController]
[Route("api/skill-roadmaps")]
[Authorize]
[Produces("application/json")]
public class SkillRoadmapsController : ControllerBase
{
    private readonly ISkillRoadmapService _service;
    private readonly ILogger<SkillRoadmapsController> _logger;

    public SkillRoadmapsController(ISkillRoadmapService service, ILogger<SkillRoadmapsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Gets every roadmap, by name.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<SkillRoadmapSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<SkillRoadmapSummaryDto>>> GetAll(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting all skill roadmaps");
        return Ok(await _service.GetAllAsync(cancellationToken));
    }

    /// <summary>
    /// Gets a roadmap with its skills.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SkillRoadmapDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SkillRoadmapDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var roadmap = await _service.GetByIdAsync(id, cancellationToken);
        if (roadmap is null)
        {
            return NotFound(new { message = $"Skill roadmap with ID '{id}' not found." });
        }

        return Ok(roadmap);
    }

    /// <summary>
    /// Creates an empty roadmap.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(SkillRoadmapDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SkillRoadmapDto>> Create([FromBody] SaveSkillRoadmapDto dto, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating skill roadmap {Name}", dto.Name);
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
    /// Renames a roadmap or changes its description.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(SkillRoadmapDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<ActionResult<SkillRoadmapDto>> Update(Guid id, [FromBody] SaveSkillRoadmapDto dto, CancellationToken cancellationToken) =>
        Handle(() => _service.UpdateAsync(id, dto, cancellationToken));

    /// <summary>
    /// Deletes a roadmap. Career plans made from it keep their skills.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting skill roadmap {Id}", id);
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

    /// <summary>
    /// Adds a skill to a roadmap.
    /// </summary>
    [HttpPost("{id:guid}/skills")]
    [ProducesResponseType(typeof(SkillRoadmapDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActionResult<SkillRoadmapDto>> AddSkill(Guid id, [FromBody] SaveRoadmapSkillDto dto, CancellationToken cancellationToken) =>
        Handle(() => _service.AddSkillAsync(id, dto, cancellationToken));

    /// <summary>
    /// Edits a skill on a roadmap.
    /// </summary>
    [HttpPut("{id:guid}/skills/{skillId:guid}")]
    [ProducesResponseType(typeof(SkillRoadmapDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActionResult<SkillRoadmapDto>> UpdateSkill(Guid id, Guid skillId, [FromBody] SaveRoadmapSkillDto dto, CancellationToken cancellationToken) =>
        Handle(() => _service.UpdateSkillAsync(id, skillId, dto, cancellationToken));

    /// <summary>
    /// Removes a skill from a roadmap.
    /// </summary>
    [HttpDelete("{id:guid}/skills/{skillId:guid}")]
    [ProducesResponseType(typeof(SkillRoadmapDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActionResult<SkillRoadmapDto>> DeleteSkill(Guid id, Guid skillId, CancellationToken cancellationToken) =>
        Handle(() => _service.DeleteSkillAsync(id, skillId, cancellationToken));

    /// <summary>
    /// Gets the built-in roadmaps and whether each is already added.
    /// </summary>
    [HttpGet("library")]
    [ProducesResponseType(typeof(IEnumerable<RoadmapLibraryItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<RoadmapLibraryItemDto>>> GetLibrary(CancellationToken cancellationToken)
    {
        return Ok(await _service.GetLibraryAsync(cancellationToken));
    }

    /// <summary>
    /// Adds a built-in roadmap to Hive as an editable roadmap.
    /// </summary>
    [HttpPost("library/{key}")]
    [ProducesResponseType(typeof(SkillRoadmapDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<ActionResult<SkillRoadmapDto>> AddFromLibrary(string key, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Adding library roadmap {Key}", key);
        return Handle(() => _service.AddFromLibraryAsync(key, cancellationToken));
    }

    private async Task<ActionResult<T>> Handle<T>(Func<Task<T>> action)
    {
        try
        {
            return Ok(await action());
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
}
