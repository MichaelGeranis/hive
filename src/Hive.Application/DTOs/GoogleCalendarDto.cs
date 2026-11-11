namespace Hive.Application.DTOs;

/// <summary>
/// The manager's Google Calendar events in a date range.
/// </summary>
public record GoogleCalendarEventsDto
{
    /// <summary>False when no secret address is set; the events are then empty.</summary>
    public bool IsConfigured { get; init; }

    /// <summary>
    /// False when the calendar has not been synced in the last 30 minutes, so there is nothing
    /// to show until the manager syncs; the events are then empty.
    /// </summary>
    public bool IsLoaded { get; init; }

    /// <summary>When the events were fetched from Google, if they are loaded.</summary>
    public DateTimeOffset? SyncedAt { get; init; }
    public List<GoogleCalendarEventDto> Events { get; init; } = new();
}

/// <summary>
/// One occurrence of a Google Calendar event. A timed event has <see cref="StartsAt"/> and
/// <see cref="EndsAt"/>; an all-day event has <see cref="StartDate"/> and <see cref="EndDate"/>
/// (inclusive).
/// </summary>
public record GoogleCalendarEventDto
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string? Location { get; init; }
    public bool IsAllDay { get; init; }
    public DateTimeOffset? StartsAt { get; init; }
    public DateTimeOffset? EndsAt { get; init; }
    public DateOnly? StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
}
