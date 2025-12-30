using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hive.Api.Controllers;

/// <summary>
/// API Controller for importing tasks from Jira CSV exports.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class JiraImportController : ControllerBase
{
    private readonly IJiraImportService _service;
    private readonly ILogger<JiraImportController> _logger;

    public JiraImportController(IJiraImportService service, ILogger<JiraImportController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Preview a Jira CSV import without making changes.
    /// </summary>
    /// <param name="request">The CSV content to preview.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Preview information about the import.</returns>
    [HttpPost("preview")]
    [ProducesResponseType(typeof(JiraImportPreviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<JiraImportPreviewDto>> PreviewImport(
        [FromBody] CsvContentDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Previewing Jira CSV import");

            if (string.IsNullOrWhiteSpace(request.CsvContent))
            {
                return BadRequest("CSV content cannot be empty");
            }

            var result = await _service.PreviewImportAsync(request.CsvContent, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error previewing Jira CSV import");
            return BadRequest($"Error previewing CSV: {ex.Message}");
        }
    }

    /// <summary>
    /// Import tasks from a Jira CSV export.
    /// </summary>
    /// <param name="request">The import request with CSV content and options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result of the import operation.</returns>
    [HttpPost("import")]
    [ProducesResponseType(typeof(JiraImportResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<JiraImportResultDto>> Import(
        [FromBody] JiraImportRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting Jira CSV import");

            if (string.IsNullOrWhiteSpace(request.CsvContent))
            {
                return BadRequest("CSV content cannot be empty");
            }

            var result = await _service.ImportAsync(request, cancellationToken);
            _logger.LogInformation(
                "Jira import completed: {SuccessCount} succeeded, {ErrorCount} errors, {SkippedCount} skipped",
                result.SuccessCount, result.ErrorCount, result.SkippedCount);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing Jira CSV");
            return BadRequest($"Error importing CSV: {ex.Message}");
        }
    }
}

/// <summary>
/// DTO for CSV content.
/// </summary>
public record CsvContentDto
{
    public string CsvContent { get; init; } = string.Empty;
}
