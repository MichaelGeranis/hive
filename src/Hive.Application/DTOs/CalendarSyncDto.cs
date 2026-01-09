namespace Hive.Application.DTOs;

public class AppleCalendarEventDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public string Calendar { get; set; } = string.Empty;
}

public class CalendarSyncRequestDto
{
    public string CalendarEmail { get; set; } = string.Empty;
    public List<AppleCalendarEventDto> Events { get; set; } = new();
}

public class CalendarSyncResultDto
{
    public int TotalEvents { get; set; }
    public int CreatedCount { get; set; }
    public int UpdatedCount { get; set; }
    public List<string> SyncedMeetings { get; set; } = new();
    public List<string> SkippedEvents { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}
