using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Entities;
using Hive.Core.Interfaces;

namespace Hive.Application.Services;

public class CalendarSyncService : ICalendarSyncService
{
    private readonly IOneOnOneMeetingRepository _meetingRepository;
    private readonly IDirectReportRepository _directReportRepository;

    public CalendarSyncService(
        IOneOnOneMeetingRepository meetingRepository,
        IDirectReportRepository directReportRepository)
    {
        _meetingRepository = meetingRepository;
        _directReportRepository = directReportRepository;
    }

    /// <summary>
    /// Synchronizes calendar events from Apple Calendar
    /// Pattern matching: "[DirectReportFirstName] / [ManagerName]"
    /// </summary>
    public async Task<CalendarSyncResultDto> SyncCalendarEventsAsync(
        IEnumerable<AppleCalendarEventDto> events)
    {
        var result = new CalendarSyncResultDto
        {
            TotalEvents = events.Count(),
            SyncedMeetings = new List<string>(),
            SkippedEvents = new List<string>(),
            Errors = new List<string>()
        };

        // Get all direct reports to match against
        var directReports = await _directReportRepository.GetAllAsync();
        var directReportsByFirstName = directReports
            .GroupBy(dr => dr.FirstName.ToLowerInvariant())
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var calendarEvent in events)
        {
            try
            {
                // Extract direct report name from title pattern "[FirstName] / [ManagerName]"
                var directReportFirstName = ExtractDirectReportName(calendarEvent.Title);

                if (string.IsNullOrEmpty(directReportFirstName))
                {
                    result.SkippedEvents.Add($"Event '{calendarEvent.Title}' doesn't match pattern '[FirstName] / [ManagerName]'");
                    continue;
                }

                // Find matching direct report
                if (!directReportsByFirstName.TryGetValue(
                    directReportFirstName.ToLowerInvariant(),
                    out var matchingReports))
                {
                    result.SkippedEvents.Add($"No direct report found with first name '{directReportFirstName}'");
                    continue;
                }

                // If multiple direct reports have the same first name, log a warning and skip
                if (matchingReports.Count > 1)
                {
                    result.Errors.Add($"Multiple direct reports found with first name '{directReportFirstName}'. Please use a more specific naming convention.");
                    continue;
                }

                var directReport = matchingReports.First();

                // Check if meeting already exists (by Apple Calendar Event ID)
                var existingMeeting = await _meetingRepository.GetByAppleCalendarEventIdAsync(calendarEvent.Id);

                if (existingMeeting != null)
                {
                    // Update existing meeting
                    existingMeeting.UpdateFromCalendarSync(
                        calendarEvent.StartDate,
                        CalculateDurationMinutes(calendarEvent.StartDate, calendarEvent.EndDate),
                        calendarEvent.Location
                    );

                    await _meetingRepository.UpdateAsync(existingMeeting);
                    result.UpdatedCount++;
                }
                else
                {
                    // Create new meeting
                    var newMeeting = new OneOnOneMeeting(
                        directReport.Id,
                        calendarEvent.StartDate,
                        CalculateDurationMinutes(calendarEvent.StartDate, calendarEvent.EndDate),
                        calendarEvent.Location,
                        agenda: null // Synced meetings don't have agenda initially
                    );

                    newMeeting.SetAppleCalendarSync(calendarEvent.Id);

                    await _meetingRepository.AddAsync(newMeeting);
                    result.CreatedCount++;
                }

                result.SyncedMeetings.Add($"{directReport.FirstName} {directReport.LastName} - {calendarEvent.StartDate:yyyy-MM-dd HH:mm}");
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Error processing event '{calendarEvent.Title}': {ex.Message}");
            }
        }

        return result;
    }

    /// <summary>
    /// Extract direct report first name from meeting title
    /// Patterns supported:
    ///   - "[FirstName] / [ManagerName]" (e.g., "George / Michail")
    ///   - "[FirstName] : [ManagerName]" (e.g., "Zaharenia : Michail")
    /// </summary>
    private static string? ExtractDirectReportName(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return null;
        }

        // Try splitting by "/" first, then by ":"
        string[] parts;

        if (title.Contains('/'))
        {
            parts = title.Split('/', StringSplitOptions.TrimEntries);
        }
        else if (title.Contains(':'))
        {
            parts = title.Split(':', StringSplitOptions.TrimEntries);
        }
        else
        {
            return null;
        }

        if (parts.Length != 2)
        {
            return null;
        }

        return parts[0].Trim();
    }

    private static int CalculateDurationMinutes(DateTime startDate, DateTime endDate)
    {
        var duration = (int)(endDate - startDate).TotalMinutes;

        // Default to 30 minutes if duration is invalid
        if (duration < 5 || duration > 480)
        {
            return 30;
        }

        return duration;
    }
}
