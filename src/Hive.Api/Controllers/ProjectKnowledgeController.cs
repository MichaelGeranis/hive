using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hive.Api.Controllers;

/// <summary>
/// API Controller for managing project knowledge assessments (the knowledge matrix).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class ProjectKnowledgeController : ControllerBase
{
    private readonly IProjectKnowledgeService _service;
    private readonly ILogger<ProjectKnowledgeController> _logger;

    public ProjectKnowledgeController(IProjectKnowledgeService service, ILogger<ProjectKnowledgeController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Gets all project knowledge assessments.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ProjectKnowledgeDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ProjectKnowledgeDto>>> GetAll(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting all project knowledge assessments");
        var assessments = await _service.GetAllAsync(cancellationToken);
        return Ok(assessments);
    }

    /// <summary>
    /// Gets the full project knowledge matrix showing all projects and team members' knowledge levels.
    /// </summary>
    [HttpGet("matrix")]
    [ProducesResponseType(typeof(ProjectKnowledgeMatrixDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ProjectKnowledgeMatrixDto>> GetMatrix(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting project knowledge matrix");
        var matrix = await _service.GetMatrixAsync(cancellationToken);
        return Ok(matrix);
    }

    /// <summary>
    /// Gets a project knowledge assessment by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProjectKnowledgeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProjectKnowledgeDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting project knowledge assessment with ID: {Id}", id);
        var assessment = await _service.GetByIdAsync(id, cancellationToken);

        if (assessment is null)
        {
            return NotFound(new { message = $"Project knowledge assessment with ID '{id}' not found." });
        }

        return Ok(assessment);
    }

    /// <summary>
    /// Gets all project knowledge assessments for a specific direct report.
    /// </summary>
    [HttpGet("by-direct-report/{directReportId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<ProjectKnowledgeDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ProjectKnowledgeDto>>> GetByDirectReport(
        Guid directReportId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting project knowledge assessments for direct report: {DirectReportId}", directReportId);
        var assessments = await _service.GetByDirectReportIdAsync(directReportId, cancellationToken);
        return Ok(assessments);
    }

    /// <summary>
    /// Gets all knowledge assessments for a specific project.
    /// </summary>
    [HttpGet("by-project/{projectId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<ProjectKnowledgeDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ProjectKnowledgeDto>>> GetByProject(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting knowledge assessments for project: {ProjectId}", projectId);
        var assessments = await _service.GetByProjectIdAsync(projectId, cancellationToken);
        return Ok(assessments);
    }

    /// <summary>
    /// Creates or updates a project knowledge assessment.
    /// </summary>
    [HttpPut]
    [ProducesResponseType(typeof(ProjectKnowledgeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProjectKnowledgeDto>> CreateOrUpdate(
        [FromBody] CreateOrUpdateProjectKnowledgeDto dto,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating/updating project knowledge for direct report {DirectReportId} and project {ProjectId}",
            dto.DirectReportId, dto.ProjectId);

        try
        {
            var result = await _service.CreateOrUpdateAsync(dto, cancellationToken);
            return Ok(result);
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
    /// Deletes a project knowledge assessment.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting project knowledge assessment with ID: {Id}", id);

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
