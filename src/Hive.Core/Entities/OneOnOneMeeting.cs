namespace Hive.Core.Entities;

/// <summary>
/// Represents a one-on-one meeting between manager and direct report.
/// </summary>
public class OneOnOneMeeting
{
    public Guid Id { get; private set; }
    public Guid DirectReportId { get; private set; }
    public DateTime ScheduledDate { get; private set; }
    public int DurationMinutes { get; private set; }
    public string Location { get; private set; } = string.Empty;
    public string Agenda { get; private set; } = string.Empty;
    public MeetingStatus Status { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private OneOnOneMeeting() { }

    public OneOnOneMeeting(
        Guid directReportId,
        DateTime scheduledDate,
        int durationMinutes = 30,
        string? location = null,
        string? agenda = null)
    {
        ValidateDirectReportId(directReportId);
        ValidateDuration(durationMinutes);

        Id = Guid.NewGuid();
        DirectReportId = directReportId;
        ScheduledDate = scheduledDate;
        DurationMinutes = durationMinutes;
        Location = location?.Trim() ?? string.Empty;
        Agenda = agenda?.Trim() ?? string.Empty;
        Status = MeetingStatus.Scheduled;
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateDetails(
        DateTime scheduledDate,
        int durationMinutes,
        string? location,
        string? agenda)
    {
        if (Status == MeetingStatus.Completed)
        {
            throw new InvalidOperationException("Cannot modify a completed meeting.");
        }

        ValidateDuration(durationMinutes);

        ScheduledDate = scheduledDate;
        DurationMinutes = durationMinutes;
        Location = location?.Trim() ?? string.Empty;
        Agenda = agenda?.Trim() ?? string.Empty;
        UpdatedAt = DateTime.UtcNow;

        if (Status == MeetingStatus.Cancelled)
        {
            Status = MeetingStatus.Rescheduled;
        }
    }

    public void Complete()
    {
        if (Status == MeetingStatus.Completed)
        {
            throw new InvalidOperationException("Meeting is already completed.");
        }

        if (Status == MeetingStatus.Cancelled)
        {
            throw new InvalidOperationException("Cannot complete a cancelled meeting.");
        }

        Status = MeetingStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        if (Status == MeetingStatus.Completed)
        {
            throw new InvalidOperationException("Cannot cancel a completed meeting.");
        }

        Status = MeetingStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Reschedule(DateTime newDate)
    {
        if (Status == MeetingStatus.Completed)
        {
            throw new InvalidOperationException("Cannot reschedule a completed meeting.");
        }

        ScheduledDate = newDate;
        Status = MeetingStatus.Rescheduled;
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
