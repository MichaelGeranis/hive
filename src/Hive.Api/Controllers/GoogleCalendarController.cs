using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hive.Api.Controllers;

/// <summary>
/// API Controller for reading the manager's Google Calendar through its secret iCal address.
/// </summary>
[ApiController]
[Route("api/google-calendar")]
[Authorize]
[Produces("application/json")]
public class GoogleCalendarController : ControllerBase
{
    private readonly IGoogleCalendarService _service;
    private readonly ILogger<GoogleCalendarController> _logger;

    public GoogleCalendarController(IGoogleCalendarService service, ILogger<GoogleCalendarController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Gets the Google Calendar events that overlap a range of days, from the calendar last
    /// synced. Never asks Google: when it was not synced in the last 30 minutes, nothing is loaded.
    /// </summary>
    /// <param name="from">The first day, as yyyy-MM-dd.</param>
    /// <param name="to">The last day, inclusive, as yyyy-MM-dd.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The events, or an empty list flagged as not configured or not loaded.</returns>
    [HttpGet("events")]
    [ProducesResponseType(typeof(GoogleCalendarEventsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GoogleCalendarEventsDto>> GetEvents(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _service.GetEventsAsync(from, to, cancellationToken));
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Syncs the Google Calendar, unless it was synced in the last 30 minutes, and gets the
    /// events that overlap a range of days.
    /// </summary>
    /// <param name="from">The first day, as yyyy-MM-dd.</param>
    /// <param name="to">The last day, inclusive, as yyyy-MM-dd.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The events, or an empty list flagged as not configured.</returns>
    [HttpPost("sync")]
    [ProducesResponseType(typeof(GoogleCalendarEventsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<GoogleCalendarEventsDto>> Sync(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Syncing Google Calendar events from {From} to {To}", from, to);

        try
        {
            return Ok(await _service.SyncAsync(from, to, cancellationToken));
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (GoogleCalendarException ex)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { message = ex.Message });
        }
    }
}
