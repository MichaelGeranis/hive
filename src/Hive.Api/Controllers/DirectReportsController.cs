using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hive.Api.Controllers;

/// <summary>
/// API Controller for managing direct reports.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class DirectReportsController : ControllerBase
{
    private readonly IDirectReportService _service;
    private readonly ILogger<DirectReportsController> _logger;

    public DirectReportsController(IDirectReportService service, ILogger<DirectReportsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Gets all direct reports.
    /// </summary>
    /// <returns>List of all direct reports.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<DirectReportDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<DirectReportDto>>> GetAll(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting all direct reports");
        var reports = await _service.GetAllAsync(cancellationToken);
        return Ok(reports);
    }

    /// <summary>
    /// Gets a direct report by ID.
    /// </summary>
    /// <param name="id">The direct report ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The direct report if found.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(DirectReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DirectReportDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting direct report with ID: {Id}", id);
        var report = await _service.GetByIdAsync(id, cancellationToken);

        if (report is null)
        {
            return NotFound(new { message = $"Direct report with ID '{id}' not found." });
        }

        return Ok(report);
    }

    /// <summary>
    /// Creates a new direct report.
    /// </summary>
    /// <param name="dto">The direct report data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created direct report.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(DirectReportDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DirectReportDto>> Create([FromBody] CreateDirectReportDto dto, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating new direct report: {Email}", dto.Email);

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
    /// Updates an existing direct report.
    /// </summary>
    /// <param name="id">The direct report ID.</param>
    /// <param name="dto">The updated direct report data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated direct report.</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(DirectReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DirectReportDto>> Update(Guid id, [FromBody] UpdateDirectReportDto dto, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating direct report with ID: {Id}", id);

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
    /// Deletes a direct report.
    /// </summary>
    /// <param name="id">The direct report ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting direct report with ID: {Id}", id);

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
