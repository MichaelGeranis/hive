namespace Hive.Application.Interfaces;

/// <summary>
/// Read-only client for a Google Calendar's secret iCal address. Hive never writes to the calendar.
/// </summary>
public interface IGoogleCalendarClient
{
    /// <summary>
    /// Gets every event in the feed that overlaps the range, with recurring events expanded
    /// into their occurrences. Cancelled events are left out. A feed fetched in the last 30
    /// minutes is served from memory without asking Google.
    /// </summary>
    /// <param name="icalUrl">The calendar's secret address in iCal format.</param>
    /// <param name="from">Start of the range, inclusive.</param>
    /// <param name="to">End of the range, exclusive.</param>
    /// <param name="read">Whether Google may be asked when nothing fresh is held in memory.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The events and when the feed was fetched; null for <see cref="GoogleCalendarRead.CachedOnly"/> when nothing fresh is held.</returns>
    /// <exception cref="GoogleCalendarException">The feed could not be fetched or read.</exception>
    Task<GoogleCalendarSnapshot?> GetEventsAsync(
        string icalUrl,
        DateTimeOffset from,
        DateTimeOffset to,
        GoogleCalendarRead read,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Where the events may come from.
/// </summary>
public enum GoogleCalendarRead
{
    /// <summary>Only a feed already held in memory; Google is never asked.</summary>
    CachedOnly,

    /// <summary>A feed held in memory, or Google when there is none or it has expired.</summary>
    CachedOrFetch
}

/// <summary>
/// The events in a range, read from a feed fetched at <paramref name="SyncedAt"/>.
/// </summary>
public record GoogleCalendarSnapshot(IReadOnlyList<GoogleCalendarEvent> Events, DateTimeOffset SyncedAt);

/// <summary>
/// One occurrence of a calendar event. A timed event has <see cref="StartsAt"/> and
/// <see cref="EndsAt"/>; an all-day event has <see cref="StartDate"/> and <see cref="EndDate"/>.
/// </summary>
/// <param name="Id">Unique per occurrence: the event's UID and its start.</param>
/// <param name="Title">The event's summary.</param>
/// <param name="Location">Where it happens, if the event says.</param>
/// <param name="IsAllDay">Whether the event takes whole days rather than a time slot.</param>
/// <param name="StartsAt">When a timed event starts.</param>
/// <param name="EndsAt">When a timed event ends.</param>
/// <param name="StartDate">The first day of an all-day event.</param>
/// <param name="EndDate">The last day of an all-day event, inclusive.</param>
public record GoogleCalendarEvent(
    string Id,
    string Title,
    string? Location,
    bool IsAllDay,
    DateTimeOffset? StartsAt,
    DateTimeOffset? EndsAt,
    DateOnly? StartDate,
    DateOnly? EndDate
);

/// <summary>
/// The calendar feed could not be fetched or read. The message is safe to show the manager,
/// and never contains the secret address.
/// </summary>
public class GoogleCalendarException : Exception
{
    public GoogleCalendarException(string message) : base(message)
    {
    }

    public GoogleCalendarException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
