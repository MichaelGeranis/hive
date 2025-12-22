using Hive.Core.Entities;

namespace Hive.Application.DTOs;

/// <summary>
/// Data Transfer Object for OneOnOneMeeting.
/// </summary>
public record OneOnOneMeetingDto
{
    public Guid Id { get; init; }
    public Guid DirectReportId { get; init; }
    public string DirectReportName { get; init; } = string.Empty;
    public DateTime ScheduledDate { get; init; }
    public int DurationMinutes { get; init; }
    public string Location { get; init; } = string.Empty;
    public string Agenda { get; init; } = string.Empty;
    public MeetingStatus Status { get; init; }
    public string StatusName { get; init; } = string.Empty;
    public DateTime? CompletedAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public int NoteCount { get; init; }
    public int OpenActionItemCount { get; init; }
}

/// <summary>
/// DTO for creating a new meeting.
/// </summary>
public record CreateOneOnOneMeetingDto
{
    public Guid DirectReportId { get; init; }
    public DateTime ScheduledDate { get; init; }
    public int DurationMinutes { get; init; } = 30;
    public string Location { get; init; } = string.Empty;
    public string Agenda { get; init; } = string.Empty;
}

/// <summary>
/// DTO for updating a meeting.
/// </summary>
public record UpdateOneOnOneMeetingDto
{
    public DateTime ScheduledDate { get; init; }
    public int DurationMinutes { get; init; }
    public string Location { get; init; } = string.Empty;
    public string Agenda { get; init; } = string.Empty;
}

/// <summary>
/// DTO for rescheduling a meeting.
/// </summary>
public record RescheduleMeetingDto
{
    public DateTime NewDate { get; init; }
}

/// <summary>
/// Full meeting details including notes.
/// </summary>
public record OneOnOneMeetingDetailsDto
{
    public OneOnOneMeetingDto Meeting { get; init; } = null!;
    public IList<MeetingNoteDto> Notes { get; init; } = new List<MeetingNoteDto>();
}
