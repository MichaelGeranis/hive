namespace Hive.Application.DTOs;

/// <summary>
/// Data Transfer Object for OneOnOneMeeting.
/// Simplified for note tracking - no scheduling workflow.
/// </summary>
public record OneOnOneMeetingDto
{
    public Guid Id { get; init; }
    public Guid DirectReportId { get; init; }
    public string DirectReportName { get; init; } = string.Empty;
    public DateOnly MeetingDate { get; init; }
    public string Location { get; init; } = string.Empty;
    public string Agenda { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public int NoteCount { get; init; }
    public int OpenActionItemCount { get; init; }
}

/// <summary>
/// DTO for creating a new meeting record.
/// </summary>
public record CreateOneOnOneMeetingDto
{
    public Guid DirectReportId { get; init; }
    public DateOnly MeetingDate { get; init; }
    public string Location { get; init; } = string.Empty;
    public string Agenda { get; init; } = string.Empty;
}

/// <summary>
/// DTO for updating a meeting record.
/// </summary>
public record UpdateOneOnOneMeetingDto
{
    public Guid DirectReportId { get; init; }
    public DateOnly MeetingDate { get; init; }
    public string Location { get; init; } = string.Empty;
    public string Agenda { get; init; } = string.Empty;
}

/// <summary>
/// Full meeting details including notes.
/// </summary>
public record OneOnOneMeetingDetailsDto
{
    public OneOnOneMeetingDto Meeting { get; init; } = null!;
    public IList<MeetingNoteDto> Notes { get; init; } = new List<MeetingNoteDto>();
}
