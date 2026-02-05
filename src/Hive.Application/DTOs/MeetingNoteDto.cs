using Hive.Core.Entities;

namespace Hive.Application.DTOs;

/// <summary>
/// Data Transfer Object for MeetingNote.
/// </summary>
public record MeetingNoteDto
{
    public Guid Id { get; init; }
    public Guid MeetingId { get; init; }
    public DateOnly MeetingDate { get; init; }
    public string DirectReportName { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public NoteCategory Category { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public ActionItemStatus? ActionStatus { get; init; }
    public string? ActionStatusName { get; init; }
    public DateTime? ActionDueDate { get; init; }
    public string? ActionAssignee { get; init; }
    public bool IsOverdue { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

/// <summary>
/// DTO for creating a meeting note.
/// </summary>
public record CreateMeetingNoteDto
{
    public Guid MeetingId { get; init; }
    public string Content { get; init; } = string.Empty;
    public NoteCategory Category { get; init; }
    public DateTime? ActionDueDate { get; init; }
    public string? ActionAssignee { get; init; }
}

/// <summary>
/// DTO for updating a meeting note.
/// </summary>
public record UpdateMeetingNoteDto
{
    public string Content { get; init; } = string.Empty;
    public NoteCategory Category { get; init; }
    public DateTime? ActionDueDate { get; init; }
    public string? ActionAssignee { get; init; }
}

/// <summary>
/// DTO for updating action item status.
/// </summary>
public record UpdateActionStatusDto
{
    public ActionItemStatus Status { get; init; }
}
