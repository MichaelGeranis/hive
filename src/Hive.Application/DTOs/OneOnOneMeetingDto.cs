namespace Hive.Application.DTOs;

/// <summary>
/// A 1:1, which is a single markdown note written during the meeting.
/// </summary>
public record OneOnOneMeetingDto
{
    public Guid Id { get; init; }

    /// <summary>
    /// The person the tags resolved to. Null when the 1:1 is not linked to anyone.
    /// </summary>
    public Guid? DirectReportId { get; init; }

    public string? DirectReportName { get; init; }
    public bool IsUnlinked { get; init; }
    public DateOnly MeetingDate { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public string Tags { get; init; } = string.Empty;
    public string[] TagsList { get; init; } = Array.Empty<string>();

    /// <summary>
    /// A short plain-text excerpt of the body, without the line the title came from.
    /// </summary>
    public string Snippet { get; init; } = string.Empty;

    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

/// <summary>
/// Request to open a blank 1:1 note. Without a date it lands on today; a person can be
/// pre-tagged when the note is started from someone's list.
/// </summary>
public record CreateBlankMeetingDto
{
    public DateOnly? MeetingDate { get; init; }
    public string? Tags { get; init; }
}

/// <summary>
/// Request carrying only the body of a 1:1; the title is derived from its first line.
/// </summary>
public record UpdateMeetingContentDto
{
    public string Content { get; init; } = string.Empty;
}

/// <summary>
/// Request replacing the tags of a 1:1. The person it is linked to, and its date when a
/// <c>#YYYYMMDD</c> tag is present, are re-derived from these tags.
/// </summary>
public record UpdateMeetingTagsDto
{
    public string? Tags { get; init; }
}

/// <summary>
/// Request moving a 1:1 to another day.
/// </summary>
public record UpdateMeetingDateDto
{
    public DateOnly MeetingDate { get; init; }
}

/// <summary>
/// How many 1:1s have been held with one person. A null id counts the unlinked ones.
/// </summary>
public record MeetingCountDto
{
    public Guid? DirectReportId { get; init; }
    public int Count { get; init; }
}
