using Hive.Core.Entities;

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
    public ProjectStatus Status { get; init; }
    public string StatusName { get; init; } = string.Empty;
    public DateTime? StartDate { get; init; }
    public DateTime? TargetEndDate { get; init; }
    public DateTime? ActualEndDate { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public int TotalTasks { get; init; }
    public int CompletedTasks { get; init; }
    public int OpenTasks { get; init; }
}

/// <summary>
/// DTO for creating a new project.
/// </summary>
public record CreateProjectDto
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Labels { get; init; } = string.Empty;
    public DateTime? StartDate { get; init; }
    public DateTime? TargetEndDate { get; init; }
}

/// <summary>
/// DTO for updating a project.
/// </summary>
public record UpdateProjectDto
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Labels { get; init; } = string.Empty;
    public DateTime? StartDate { get; init; }
    public DateTime? TargetEndDate { get; init; }
}
