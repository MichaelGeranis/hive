using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hive.Api.Controllers;

/// <summary>
/// API Controller for the AI summary of the team's retrospectives.
/// </summary>
[ApiController]
[Route("api/retro-summary")]
[Authorize]
[Produces("application/json")]
public class RetroSummaryController : ControllerBase
{
    private readonly IRetroSummaryService _service;
    private readonly ILogger<RetroSummaryController> _logger;

    public RetroSummaryController(IRetroSummaryService service, ILogger<RetroSummaryController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Gets the retro summary, made again first when the retros changed since the last one.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The retro summary; a failed update carries its error and the last summary.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(RetroSummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<RetroSummaryDto>> Get(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting the retro summary");
        return Ok(await _service.GetAsync(forceRefresh: false, cancellationToken));
    }

    /// <summary>
    /// Makes the retro summary again, even when the retros have not changed.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The new retro summary; a failed update carries its error and the last summary.</returns>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(RetroSummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<RetroSummaryDto>> Refresh(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Refreshing the retro summary");
        return Ok(await _service.GetAsync(forceRefresh: true, cancellationToken));
    }
}
