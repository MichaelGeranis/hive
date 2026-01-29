using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hive.Api.Controllers;

/// <summary>
/// API Controller for managing knowledge contribution points.
/// Points track team member contributions per project through manual additions and automatic calculations from completed tasks.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class KnowledgePointsController : ControllerBase
{
    private readonly IKnowledgePointService _service;
    private readonly ILogger<KnowledgePointsController> _logger;

    public KnowledgePointsController(IKnowledgePointService service, ILogger<KnowledgePointsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Gets all knowledge points records with calculated totals.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<KnowledgePointDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<KnowledgePointDto>>> GetAll(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting all knowledge points");
        var points = await _service.GetAllAsync(cancellationToken);
        return Ok(points);
    }

    /// <summary>
    /// Gets a knowledge points record by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(KnowledgePointDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<KnowledgePointDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting knowledge points with ID: {Id}", id);
        var points = await _service.GetByIdAsync(id, cancellationToken);

        if (points is null)
        {
            return NotFound(new { message = $"Knowledge points with ID '{id}' not found." });
        }

        return Ok(points);
    }

    /// <summary>
    /// Gets all knowledge points for a specific direct report.
    /// </summary>
    [HttpGet("by-direct-report/{directReportId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<KnowledgePointDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<KnowledgePointDto>>> GetByDirectReport(
        Guid directReportId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting knowledge points for direct report: {DirectReportId}", directReportId);
        var points = await _service.GetByDirectReportIdAsync(directReportId, cancellationToken);
        return Ok(points);
    }

    /// <summary>
    /// Gets all knowledge points for a specific project.
    /// </summary>
    [HttpGet("by-project/{projectId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<KnowledgePointDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<KnowledgePointDto>>> GetByProject(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting knowledge points for project: {ProjectId}", projectId);
        var points = await _service.GetByProjectIdAsync(projectId, cancellationToken);
        return Ok(points);
    }

    /// <summary>
    /// Creates or updates manual points for a direct report and project combination.
    /// </summary>
    [HttpPut]
    [ProducesResponseType(typeof(KnowledgePointDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<KnowledgePointDto>> CreateOrUpdate(
        [FromBody] CreateOrUpdateKnowledgePointDto dto,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating/updating knowledge points for direct report {DirectReportId} and project {ProjectId}",
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
    /// Adds points incrementally to an existing record (or creates a new one).
    /// </summary>
    [HttpPost("add")]
    [ProducesResponseType(typeof(KnowledgePointDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<KnowledgePointDto>> AddPoints(
        [FromBody] AddKnowledgePointsDto dto,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Adding {Points} points for direct report {DirectReportId} and project {ProjectId}",
            dto.PointsToAdd, dto.DirectReportId, dto.ProjectId);

        try
        {
            var result = await _service.AddPointsAsync(dto, cancellationToken);
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
    /// Deletes a knowledge points record.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting knowledge points with ID: {Id}", id);

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
    /// Gets suggestions for knowledge level increases based on accumulated points.
    /// Returns combinations where total points >= 21 and current knowledge level < 5.
    /// </summary>
    [HttpGet("suggestions")]
    [ProducesResponseType(typeof(IEnumerable<KnowledgeLevelSuggestionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<KnowledgeLevelSuggestionDto>>> GetSuggestions(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting knowledge level increase suggestions");
        var suggestions = await _service.GetLevelIncreaseSuggestionsAsync(cancellationToken);
        return Ok(suggestions);
    }

    /// <summary>
    /// Calculates the automatic points for a direct report and project from completed tasks.
    /// </summary>
    [HttpGet("automatic/{directReportId:guid}/{projectId:guid}")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<ActionResult<int>> GetAutomaticPoints(
        Guid directReportId,
        Guid projectId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Calculating automatic points for direct report {DirectReportId} and project {ProjectId}",
            directReportId, projectId);
        var automaticPoints = await _service.CalculateAutomaticPointsAsync(directReportId, projectId, cancellationToken);
        return Ok(automaticPoints);
    }
}
