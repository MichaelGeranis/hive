namespace Hive.Core.Entities;

/// <summary>
/// Represents a one-on-one meeting record between manager and direct report.
/// Simplified for note tracking - no scheduling workflow.
/// </summary>
public class OneOnOneMeeting
{
    public Guid Id { get; private set; }
    public Guid DirectReportId { get; private set; }
    public DateTime MeetingDate { get; private set; }
    public int DurationMinutes { get; private set; }
    public string Location { get; private set; } = string.Empty;
    public string Agenda { get; private set; } = string.Empty;
    public bool IsSyncedFromCalendar { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private OneOnOneMeeting() { }

    public OneOnOneMeeting(
        Guid directReportId,
        DateTime meetingDate,
        int durationMinutes = 30,
        string? location = null,
        string? agenda = null)
    {
        ValidateDirectReportId(directReportId);
        ValidateDuration(durationMinutes);

        Id = Guid.NewGuid();
        DirectReportId = directReportId;
        MeetingDate = meetingDate;
        DurationMinutes = durationMinutes;
        Location = location?.Trim() ?? string.Empty;
        Agenda = agenda?.Trim() ?? string.Empty;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(
        Guid directReportId,
        DateTime meetingDate,
        int durationMinutes,
        string? location,
        string? agenda)
    {
        ValidateDirectReportId(directReportId);
        ValidateDuration(durationMinutes);

        DirectReportId = directReportId;
        MeetingDate = meetingDate;
        DurationMinutes = durationMinutes;
        Location = location?.Trim() ?? string.Empty;
        Agenda = agenda?.Trim() ?? string.Empty;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateFromCalendarSync(
        DateTime meetingDate,
        int durationMinutes,
        string? location)
    {
        ValidateDuration(durationMinutes);

        MeetingDate = meetingDate;
        DurationMinutes = durationMinutes;
        Location = location?.Trim() ?? string.Empty;
        UpdatedAt = DateTime.UtcNow;
    }

    private static void ValidateDirectReportId(Guid directReportId)
    {
        if (directReportId == Guid.Empty)
        {
            throw new ArgumentException("DirectReportId cannot be empty.", nameof(directReportId));
        }
    }

    private static void ValidateDuration(int durationMinutes)
    {
        if (durationMinutes < 5 || durationMinutes > 480)
        {
            throw new ArgumentException("Duration must be between 5 and 480 minutes.", nameof(durationMinutes));
        }
    }
}
