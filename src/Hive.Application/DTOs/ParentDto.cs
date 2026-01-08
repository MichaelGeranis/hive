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
    public int CompletedTasks { get; init; }
    public int OpenTasks { get; init; }
    public int TotalStoryPoints { get; init; }
    public int TotalTimeSpentMinutes { get; init; }
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
