using Hive.Application.DTOs;

namespace Hive.Application.Interfaces;

/// <summary>
/// Reads the manager's Google Calendar through its secret iCal address. Google is asked only
/// on a sync, and a synced calendar is held in memory for 30 minutes.
/// </summary>
public interface IGoogleCalendarService
{
    /// <summary>
    /// Gets the events that overlap the days from <paramref name="from"/> to
    /// <paramref name="to"/>, both inclusive, ordered by start, from the calendar last synced.
    /// Never asks Google: when nothing was synced in the last 30 minutes, nothing is loaded.
    /// </summary>
    /// <exception cref="Hive.Core.Exceptions.ValidationException">The range is empty or too long.</exception>
    Task<GoogleCalendarEventsDto> GetEventsAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default);

    /// <summary>
    /// As <see cref="GetEventsAsync"/>, but fetches the calendar from Google first unless it was
    /// synced in the last 30 minutes.
    /// </summary>
    /// <exception cref="Hive.Core.Exceptions.ValidationException">The range is empty or too long.</exception>
    /// <exception cref="GoogleCalendarException">The feed could not be fetched or read.</exception>
    Task<GoogleCalendarEventsDto> SyncAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default);
}
