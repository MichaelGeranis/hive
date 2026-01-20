using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hive.Api.Controllers;

/// <summary>
/// API Controller for sentiment analysis operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class SentimentAnalysisController : ControllerBase
{
    private readonly ISentimentAnalysisService _service;
    private readonly ILogger<SentimentAnalysisController> _logger;

    public SentimentAnalysisController(ISentimentAnalysisService service, ILogger<SentimentAnalysisController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Gets the sentiment analysis status (enabled/configured).
    /// </summary>
    /// <returns>The current status of sentiment analysis.</returns>
    [HttpGet("status")]
    [ProducesResponseType(typeof(SentimentStatusDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SentimentStatusDto>> GetStatus(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting sentiment analysis status");
        var status = await _service.GetStatusAsync(cancellationToken);
        return Ok(status);
    }

    /// <summary>
    /// Gets sentiment analysis for a specific direct report.
    /// </summary>
    /// <param name="id">The direct report ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The sentiment analysis for the direct report.</returns>
    [HttpGet("direct-report/{id:guid}")]
    [ProducesResponseType(typeof(SentimentAnalysisDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SentimentAnalysisDto>> GetForDirectReport(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting sentiment analysis for direct report {DirectReportId}", id);
        var analysis = await _service.GetForDirectReportAsync(id, forceRefresh: false, cancellationToken);

        if (analysis is null)
        {
            return NotFound(new { message = "Direct report not found or sentiment analysis not configured" });
        }

        return Ok(analysis);
    }

    /// <summary>
    /// Gets team-wide sentiment overview.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The team sentiment overview.</returns>
    [HttpGet("team")]
    [ProducesResponseType(typeof(TeamSentimentOverviewDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<TeamSentimentOverviewDto>> GetTeamOverview(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting team sentiment overview");
        var overview = await _service.GetTeamOverviewAsync(cancellationToken);
        return Ok(overview);
    }

    /// <summary>
    /// Forces a refresh of sentiment analysis for a specific direct report.
    /// </summary>
    /// <param name="id">The direct report ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The refreshed sentiment analysis.</returns>
    [HttpPost("direct-report/{id:guid}/refresh")]
    [ProducesResponseType(typeof(SentimentAnalysisDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SentimentAnalysisDto>> RefreshForDirectReport(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Refreshing sentiment analysis for direct report {DirectReportId}", id);
        var analysis = await _service.GetForDirectReportAsync(id, forceRefresh: true, cancellationToken);

        if (analysis is null)
        {
            return NotFound(new { message = "Direct report not found or sentiment analysis not configured" });
        }

        return Ok(analysis);
    }

    /// <summary>
    /// Validates a Claude API key.
    /// </summary>
    /// <param name="dto">The API key to validate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The validation result.</returns>
    [HttpPost("validate-api-key")]
    [ProducesResponseType(typeof(ApiKeyValidationResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiKeyValidationResultDto>> ValidateApiKey([FromBody] ValidateApiKeyDto dto, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(dto.ApiKey))
        {
            return BadRequest(new { message = "API key is required" });
        }

        _logger.LogInformation("Validating Claude API key");
        var result = await _service.ValidateApiKeyAsync(dto.ApiKey, cancellationToken);
        return Ok(result);
    }
}
