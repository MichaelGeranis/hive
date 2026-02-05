namespace Hive.Core.Entities;

/// <summary>
/// Represents a one-on-one meeting record between manager and direct report.
/// Simplified for note tracking - no scheduling workflow.
/// </summary>
public class OneOnOneMeeting
{
    public Guid Id { get; private set; }
    public Guid DirectReportId { get; private set; }
    public DateOnly MeetingDate { get; private set; }
    public string Location { get; private set; } = string.Empty;
    public string Agenda { get; private set; } = string.Empty;
    public bool IsSyncedFromCalendar { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private OneOnOneMeeting() { }

    public OneOnOneMeeting(
        Guid directReportId,
        DateOnly meetingDate,
        string? location = null,
        string? agenda = null)
    {
        ValidateDirectReportId(directReportId);

        Id = Guid.NewGuid();
        DirectReportId = directReportId;
        MeetingDate = meetingDate;
        Location = location?.Trim() ?? string.Empty;
        Agenda = agenda?.Trim() ?? string.Empty;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(
        Guid directReportId,
        DateOnly meetingDate,
        string? location,
        string? agenda)
    {
        ValidateDirectReportId(directReportId);

        DirectReportId = directReportId;
        MeetingDate = meetingDate;
        Location = location?.Trim() ?? string.Empty;
        Agenda = agenda?.Trim() ?? string.Empty;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateFromCalendarSync(
        DateOnly meetingDate,
        string? location)
    {
        MeetingDate = meetingDate;
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
}
