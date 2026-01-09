using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hive.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CalendarSyncController : ControllerBase
{
    private readonly ICalendarSyncService _calendarSyncService;
    private readonly ILogger<CalendarSyncController> _logger;

    public CalendarSyncController(
        ICalendarSyncService calendarSyncService,
        ILogger<CalendarSyncController> logger)
    {
        _calendarSyncService = calendarSyncService;
        _logger = logger;
    }

    /// <summary>
    /// Synchronizes 1:1 meetings from Apple Calendar events
    /// </summary>
    /// <param name="request">The calendar sync request with events</param>
    /// <returns>Sync result with created/updated meetings and errors</returns>
    [HttpPost("sync")]
    public async Task<ActionResult<CalendarSyncResultDto>> SyncCalendarEvents(
        [FromBody] CalendarSyncRequestDto request)
    {
        try
        {
            _logger.LogInformation(
                "Syncing {EventCount} calendar events from calendar: {CalendarEmail}",
                request.Events.Count,
                request.CalendarEmail);

            var result = await _calendarSyncService.SyncCalendarEventsAsync(request.Events);

            _logger.LogInformation(
                "Calendar sync completed. Created: {Created}, Updated: {Updated}, Skipped: {Skipped}, Errors: {Errors}",
                result.CreatedCount,
                result.UpdatedCount,
                result.SkippedEvents.Count,
                result.Errors.Count);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing calendar events");
            return StatusCode(500, new { error = "Failed to sync calendar events", details = ex.Message });
        }
    }
}
