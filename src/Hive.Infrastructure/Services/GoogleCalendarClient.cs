using System.Net;
using Hive.Application.Interfaces;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Microsoft.Extensions.Logging;
using CultureInfo = System.Globalization.CultureInfo;

namespace Hive.Infrastructure.Services;

/// <summary>
/// <see cref="IGoogleCalendarClient"/> over a calendar's secret iCal address, parsed with Ical.Net
/// and held for 30 minutes in <see cref="GoogleCalendarFeedCache"/>.
/// </summary>
/// <remarks>
/// The secret address authenticates by being secret: no login and no API key. It is never
/// logged and never put in an error message.
/// </remarks>
public class GoogleCalendarClient : IGoogleCalendarClient
{
    // A calendar with more occurrences than this in one range is not being read by a person;
    // stop rather than expand a runaway recurrence
    private const int MaxOccurrences = 5000;

    private readonly HttpClient _httpClient;
    private readonly GoogleCalendarFeedCache _cache;
    private readonly ILogger<GoogleCalendarClient> _logger;

    public GoogleCalendarClient(HttpClient httpClient, GoogleCalendarFeedCache cache, ILogger<GoogleCalendarClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<GoogleCalendarSnapshot?> GetEventsAsync(
        string icalUrl,
        DateTimeOffset from,
        DateTimeOffset to,
        GoogleCalendarRead read,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(icalUrl);

        var feed = _cache.Get(icalUrl);
        if (feed is null)
        {
            if (read == GoogleCalendarRead.CachedOnly)
            {
                return null;
            }

            feed = _cache.Store(icalUrl, Parse(await FetchAsync(icalUrl, cancellationToken)));
            _logger.LogInformation("Fetched the Google Calendar feed");
        }

        IReadOnlyList<GoogleCalendarEvent> events;
        lock (feed.Gate)
        {
            events = Expand(feed.Calendar, from, to);
        }

        return new GoogleCalendarSnapshot(events, feed.SyncedAt);
    }

    private IReadOnlyList<GoogleCalendarEvent> Expand(Calendar calendar, DateTimeOffset from, DateTimeOffset to)
    {
        var rangeStart = new CalDateTime(from.UtcDateTime, "UTC");
        var rangeEnd = new CalDateTime(to.UtcDateTime, "UTC");

        var events = new List<GoogleCalendarEvent>();
        foreach (var occurrence in calendar.GetOccurrences<CalendarEvent>(rangeStart).TakeWhileBefore(rangeEnd))
        {
            if (occurrence.Source is not CalendarEvent source ||
                string.Equals(source.Status, "CANCELLED", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (events.Count == MaxOccurrences)
            {
                _logger.LogWarning("Google Calendar returned more than {Max} occurrences; the rest were left out", MaxOccurrences);
                break;
            }

            events.Add(ToEvent(source, occurrence.Period));
        }

        return events;
    }

    private async Task<string> FetchAsync(string icalUrl, CancellationToken cancellationToken)
    {
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync(icalUrl, cancellationToken);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new GoogleCalendarException("Google Calendar did not answer in time. Try again in a moment.", ex);
        }
        catch (HttpRequestException ex)
        {
            throw new GoogleCalendarException("Google Calendar could not be reached. Check your internet connection.", ex);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Google Calendar answered {StatusCode}", (int)response.StatusCode);
                throw new GoogleCalendarException(response.StatusCode switch
                {
                    HttpStatusCode.NotFound or HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden =>
                        "Google Calendar does not recognise this address. It may have been reset: copy the " +
                        "secret address in iCal format again and save it in Settings.",
                    HttpStatusCode.TooManyRequests =>
                        "Google Calendar is rate limiting requests. Try again in a minute.",
                    _ => $"Google Calendar could not be read ({(int)response.StatusCode})."
                });
            }

            return await response.Content.ReadAsStringAsync(cancellationToken);
        }
    }

    private static Calendar Parse(string body)
    {
        // A sign-in page or an embed link comes back as HTML, not as a calendar
        if (!body.TrimStart().StartsWith("BEGIN:VCALENDAR", StringComparison.OrdinalIgnoreCase))
        {
            throw new GoogleCalendarException(
                "This address is not an iCal feed. Use the secret address in iCal format, ending in .ics.");
        }

        try
        {
            return Calendar.Load(body)
                ?? throw new GoogleCalendarException("The Google Calendar feed was empty.");
        }
        catch (Exception ex) when (ex is not GoogleCalendarException)
        {
            throw new GoogleCalendarException("The Google Calendar feed could not be read.", ex);
        }
    }

    private static GoogleCalendarEvent ToEvent(CalendarEvent source, Period period)
    {
        var start = period.StartTime;
        var end = period.EffectiveEndTime ?? start;
        var title = string.IsNullOrWhiteSpace(source.Summary) ? "(No title)" : source.Summary.Trim();
        var location = string.IsNullOrWhiteSpace(source.Location) ? null : source.Location.Trim();
        // CalDateTime's own ToString follows the machine's culture; an id must not
        var id = start.HasTime
            ? $"{source.Uid}@{start.AsUtc.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture)}"
            : $"{source.Uid}@{start.Date.ToString("yyyyMMdd", CultureInfo.InvariantCulture)}";

        if (!start.HasTime)
        {
            // An all-day event's end date is exclusive in iCal; Hive shows the last day it covers
            var lastDay = end.Date > start.Date ? end.Date.AddDays(-1) : start.Date;
            return new GoogleCalendarEvent(id, title, location, true, null, null, start.Date, lastDay);
        }

        return new GoogleCalendarEvent(
            id, title, location, false,
            new DateTimeOffset(start.AsUtc, TimeSpan.Zero),
            new DateTimeOffset(end.AsUtc, TimeSpan.Zero),
            null, null);
    }
}
