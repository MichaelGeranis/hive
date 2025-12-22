using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hive.Api.Controllers;

/// <summary>
/// API Controller for managing skill assessments (the competency matrix).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class SkillAssessmentsController : ControllerBase
{
    private readonly ISkillAssessmentService _service;
    private readonly ILogger<SkillAssessmentsController> _logger;

    public SkillAssessmentsController(ISkillAssessmentService service, ILogger<SkillAssessmentsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Gets all skill assessments.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<SkillAssessmentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<SkillAssessmentDto>>> GetAll(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting all skill assessments");
        var assessments = await _service.GetAllAsync(cancellationToken);
        return Ok(assessments);
    }

    /// <summary>
    /// Gets the full skill matrix showing all direct reports and their skills.
    /// </summary>
    [HttpGet("matrix")]
    [ProducesResponseType(typeof(SkillMatrixDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SkillMatrixDto>> GetMatrix(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting skill matrix");
        var matrix = await _service.GetSkillMatrixAsync(cancellationToken);
        return Ok(matrix);
    }

    /// <summary>
    /// Gets all skill gaps (assessments where current level is below target).
    /// </summary>
    [HttpGet("gaps")]
    [ProducesResponseType(typeof(IEnumerable<SkillAssessmentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<SkillAssessmentDto>>> GetGaps(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting skill gaps");
        var gaps = await _service.GetSkillGapsAsync(cancellationToken);
        return Ok(gaps);
    }

    /// <summary>
    /// Gets a skill assessment by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SkillAssessmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SkillAssessmentDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting skill assessment with ID: {Id}", id);
        var assessment = await _service.GetByIdAsync(id, cancellationToken);

        if (assessment is null)
        {
            return NotFound(new { message = $"Skill assessment with ID '{id}' not found." });
        }

        return Ok(assessment);
    }

    /// <summary>
    /// Gets all skill assessments for a specific direct report.
    /// </summary>
    [HttpGet("by-direct-report/{directReportId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<SkillAssessmentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<SkillAssessmentDto>>> GetByDirectReport(
        Guid directReportId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting skill assessments for direct report: {DirectReportId}", directReportId);
        var assessments = await _service.GetByDirectReportIdAsync(directReportId, cancellationToken);
        return Ok(assessments);
    }

    /// <summary>
    /// Gets all assessments for a specific skill (shows who has this skill and at what level).
    /// </summary>
    [HttpGet("by-skill/{skillId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<SkillAssessmentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<SkillAssessmentDto>>> GetBySkill(
        Guid skillId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting skill assessments for skill: {SkillId}", skillId);
        var assessments = await _service.GetBySkillIdAsync(skillId, cancellationToken);
        return Ok(assessments);
    }

    /// <summary>
    /// Creates a new skill assessment.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(SkillAssessmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SkillAssessmentDto>> Create(
        [FromBody] CreateSkillAssessmentDto dto,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating skill assessment for direct report {DirectReportId} and skill {SkillId}",
            dto.DirectReportId, dto.SkillId);

        try
        {
            var created = await _service.CreateAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ConflictException ex)
        {
            return Conflict(new { message = ex.Message });
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
    /// Updates an existing skill assessment.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(SkillAssessmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SkillAssessmentDto>> Update(
        Guid id,
        [FromBody] UpdateSkillAssessmentDto dto,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating skill assessment with ID: {Id}", id);

        try
        {
            var updated = await _service.UpdateAsync(id, dto, cancellationToken);
            return Ok(updated);
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
    /// Bulk assess multiple skills for a direct report.
    /// Creates new assessments or updates existing ones.
    /// </summary>
    [HttpPost("bulk")]
    [ProducesResponseType(typeof(IEnumerable<SkillAssessmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<SkillAssessmentDto>>> BulkAssess(
        [FromBody] BulkSkillAssessmentDto dto,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Bulk assessing {Count} skills for direct report {DirectReportId}",
            dto.Assessments.Count, dto.DirectReportId);

        try
        {
            var results = await _service.BulkAssessAsync(dto, cancellationToken);
            return Ok(results);
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
    /// Deletes a skill assessment.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting skill assessment with ID: {Id}", id);

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
