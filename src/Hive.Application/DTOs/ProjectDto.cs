namespace Hive.Application.DTOs;

/// <summary>
/// Data Transfer Object for Project.
/// </summary>
public record ProjectDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;

    /// <summary>The products the project serves, by name.</summary>
    public List<ProjectProductDto> Products { get; init; } = new();
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

/// <summary>
/// DTO for creating a new project.
/// </summary>
public record CreateProjectDto
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;

    /// <summary>The products the project serves.</summary>
    public List<Guid> ProductIds { get; init; } = new();
}

/// <summary>
/// DTO for updating a project.
/// </summary>
public record UpdateProjectDto
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;

    /// <summary>The products the project serves; replaces the current list.</summary>
    public List<Guid> ProductIds { get; init; } = new();
}
