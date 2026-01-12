namespace Hive.Application.DTOs;

/// <summary>
/// Data Transfer Object for Project.
/// </summary>
public record ProjectDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Labels { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public int TotalTasks { get; init; }
    public int CompletedTasks { get; init; }
    public int OpenTasks { get; init; }
    public int ParentCount { get; init; }
}

/// <summary>
/// DTO for creating a new project.
/// </summary>
public record CreateProjectDto
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Labels { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
}

/// <summary>
/// DTO for updating a project.
/// </summary>
public record UpdateProjectDto
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Labels { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
}
