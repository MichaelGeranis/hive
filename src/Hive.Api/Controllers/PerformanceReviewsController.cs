using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hive.Api.Controllers;

/// <summary>
/// API Controller for managing performance reviews.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class PerformanceReviewsController : ControllerBase
{
    private readonly IPerformanceReviewService _service;
    private readonly ILogger<PerformanceReviewsController> _logger;

    public PerformanceReviewsController(IPerformanceReviewService service, ILogger<PerformanceReviewsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Gets all performance reviews.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<PerformanceReviewDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<PerformanceReviewDto>>> GetAll(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting all performance reviews");
        var reviews = await _service.GetAllAsync(cancellationToken);
        return Ok(reviews);
    }

    /// <summary>
    /// Gets a performance review by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PerformanceReviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PerformanceReviewDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting performance review with ID: {Id}", id);
        var review = await _service.GetByIdAsync(id, cancellationToken);

        if (review is null)
        {
            return NotFound(new { message = $"Performance review with ID '{id}' not found." });
        }

        return Ok(review);
    }

    /// <summary>
    /// Gets all performance reviews for a specific direct report.
    /// </summary>
    [HttpGet("by-direct-report/{directReportId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<PerformanceReviewDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<PerformanceReviewDto>>> GetByDirectReport(Guid directReportId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting performance reviews for direct report: {DirectReportId}", directReportId);
        var reviews = await _service.GetByDirectReportIdAsync(directReportId, cancellationToken);
        return Ok(reviews);
    }

    /// <summary>
    /// Creates a new performance review.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(PerformanceReviewDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PerformanceReviewDto>> Create([FromBody] CreatePerformanceReviewDto dto, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating new performance review for direct report: {DirectReportId}", dto.DirectReportId);

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
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Updates the content of a performance review.
    /// </summary>
    [HttpPut("{id:guid}/content")]
    [ProducesResponseType(typeof(PerformanceReviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PerformanceReviewDto>> UpdateContent(Guid id, [FromBody] UpdatePerformanceReviewContentDto dto, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating content of performance review: {Id}", id);

        try
        {
            var updated = await _service.UpdateContentAsync(id, dto, cancellationToken);
            return Ok(updated);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Deletes a performance review.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting performance review: {Id}", id);

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
