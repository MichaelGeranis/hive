namespace Hive.Application.DTOs;

/// <summary>
/// Data Transfer Object for Parent.
/// </summary>
public record ParentDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Labels { get; init; } = string.Empty;
    public int TotalTasks { get; init; }
    /// <summary>Tasks that are done or cancelled ("Won't Do"); the parent's progress.</summary>
    public int CompletedTasks { get; init; }
    public int OpenTasks { get; init; }
    /// <summary>Has tasks and every one is resolved: nothing is left to do under it.</summary>
    public bool IsDone { get; init; }
    public int TotalStoryPoints { get; init; }
    /// <summary>Story points of the resolved (done or won't do) tasks.</summary>
    public int CompletedStoryPoints { get; init; }
    public Guid? TeamTaskId { get; init; }
    public string? JiraKey { get; init; }
    public string? JiraIssueType { get; init; }

    /// <summary>The Jira components of the parent, comma-separated: the products it belongs to.</summary>
    public string Components { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

/// <summary>
/// DTO for creating a new parent.
/// </summary>
public record CreateParentDto
{
    public string Name { get; init; } = string.Empty;
    public string Labels { get; init; } = string.Empty;
}

/// <summary>
/// DTO for updating a parent.
/// </summary>
public record UpdateParentDto
{
    public string Name { get; init; } = string.Empty;
    public string Labels { get; init; } = string.Empty;
}
