using Hive.Core.Entities;

namespace Hive.Application.DTOs;

public record ManagerNoteDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public string Tags { get; init; } = string.Empty;
    public string[] TagsList { get; init; } = Array.Empty<string>();
    public NotePriority Priority { get; init; }
    public string PriorityName { get; init; } = string.Empty;
    public Guid? FolderId { get; init; }
    public bool IsPinned { get; init; }
    public bool IsTodo { get; init; }

    /// <summary>
    /// A short plain-text excerpt of the body, without the line the title came from.
    /// </summary>
    public string Snippet { get; init; } = string.Empty;

    public bool IsCompleted { get; init; }
    public DateTime? DueDate { get; init; }
    public bool IsOverdue { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
}

public record CreateManagerNoteDto
{
    public string Title { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public string? Tags { get; init; }
    public NotePriority Priority { get; init; } = NotePriority.Normal;
    public DateTime? DueDate { get; init; }
    public Guid? FolderId { get; init; }
    public bool IsTodo { get; init; }
}

/// <summary>
/// Request to open a blank note in a folder, the way a notes app starts a new page.
/// </summary>
public record CreateBlankNoteDto
{
    public Guid? FolderId { get; init; }
    public bool IsTodo { get; init; }
}

/// <summary>
/// Request carrying only the body of a note; the title is derived from its first line.
/// </summary>
public record UpdateNoteContentDto
{
    public string Content { get; init; } = string.Empty;
}

/// <summary>
/// Request to file a note under a folder, or at the root when null.
/// </summary>
public record MoveNoteDto
{
    public Guid? FolderId { get; init; }
}

public record UpdateManagerNoteDto
{
    public string Title { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public string? Tags { get; init; }
    public NotePriority Priority { get; init; }
    public DateTime? DueDate { get; init; }
    public Guid? FolderId { get; init; }
    public bool IsTodo { get; init; }
}
