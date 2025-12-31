using Hive.Core.Entities;

namespace Hive.Application.DTOs;

public record ManagerNoteDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public NotePriority Priority { get; init; }
    public string PriorityName { get; init; } = string.Empty;
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
    public NotePriority Priority { get; init; } = NotePriority.Normal;
    public DateTime? DueDate { get; init; }
}

public record UpdateManagerNoteDto
{
    public string Title { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public NotePriority Priority { get; init; }
    public DateTime? DueDate { get; init; }
}
