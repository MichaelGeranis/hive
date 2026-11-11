using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Exceptions;
using Hive.Core.Interfaces;

namespace Hive.Application.Services;

/// <summary>
/// Reads the manager's Google Calendar through its secret iCal address. Nothing is stored in
/// the database: a sync holds the feed in memory for 30 minutes, and every other read is
/// served from there without asking Google.
/// </summary>
public class GoogleCalendarService : IGoogleCalendarService
{
    // Recurring events are expanded per occurrence, so a range is kept to what a page shows
    private const int MaxRangeDays = 100;

    private readonly IAppSettingsRepository _settingsRepository;
    private readonly IGoogleCalendarClient _client;

    public GoogleCalendarService(IAppSettingsRepository settingsRepository, IGoogleCalendarClient client)
    {
        _settingsRepository = settingsRepository ?? throw new ArgumentNullException(nameof(settingsRepository));
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    public Task<GoogleCalendarEventsDto> GetEventsAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default) =>
        ReadAsync(from, to, GoogleCalendarRead.CachedOnly, cancellationToken);

    public Task<GoogleCalendarEventsDto> SyncAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default) =>
        ReadAsync(from, to, GoogleCalendarRead.CachedOrFetch, cancellationToken);

    private async Task<GoogleCalendarEventsDto> ReadAsync(DateOnly from, DateOnly to, GoogleCalendarRead read, CancellationToken cancellationToken)
    {
        if (to < from)
        {
            throw new ValidationException("The end of the range cannot be before its start.");
        }

        if (to.DayNumber - from.DayNumber + 1 > MaxRangeDays)
        {
            throw new ValidationException($"A calendar range cannot exceed {MaxRangeDays} days.");
        }

        var settings = await _settingsRepository.GetAsync(cancellationToken);
        if (settings is null || !settings.HasGoogleCalendarUrl)
        {
            return new GoogleCalendarEventsDto { IsConfigured = false };
        }

        // The range is in days; widen it by a day each side so no time zone drops an event
        // that falls on its first or last day in the manager's own zone
        var rangeStart = new DateTimeOffset(from.AddDays(-1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var rangeEnd = new DateTimeOffset(to.AddDays(2).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

        var snapshot = await _client.GetEventsAsync(settings.GoogleCalendarUrl!, rangeStart, rangeEnd, read, cancellationToken);
        if (snapshot is null)
        {
            return new GoogleCalendarEventsDto { IsConfigured = true, IsLoaded = false };
        }

        return new GoogleCalendarEventsDto
        {
            IsConfigured = true,
            IsLoaded = true,
            SyncedAt = snapshot.SyncedAt,
            // An all-day event is a run of days, so the widened range can be trimmed back exactly.
            // A timed event is kept: which day it falls on is the manager's time zone to decide
            Events = snapshot.Events
                .Where(e => !e.IsAllDay || (e.EndDate >= from && e.StartDate <= to))
                .OrderBy(e => e.StartsAt ?? new DateTimeOffset(e.StartDate!.Value.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero))
                .ThenBy(e => e.Title, StringComparer.OrdinalIgnoreCase)
                .Select(MapToDto)
                .ToList()
        };
    }

    private static GoogleCalendarEventDto MapToDto(GoogleCalendarEvent e) => new()
    {
        Id = e.Id,
        Title = e.Title,
        Location = e.Location,
        IsAllDay = e.IsAllDay,
        StartsAt = e.StartsAt,
        EndsAt = e.EndsAt,
        StartDate = e.StartDate,
        EndDate = e.EndDate
    };
}
