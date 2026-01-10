namespace Hive.Application.DTOs;

/// <summary>
/// Data transfer object for Sprint entity.
/// </summary>
public record SprintDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string TeamName { get; init; } = string.Empty;
    public int Quarter { get; init; }
    public int Year { get; init; }
    public int SprintNumber { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

/// <summary>
/// Data transfer object for creating a Sprint.
/// </summary>
public record CreateSprintDto
{
    public string Name { get; init; } = string.Empty;
}

/// <summary>
/// Data transfer object for updating a Sprint's dates.
/// </summary>
public record UpdateSprintDto
{
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
}

/// <summary>
/// Data transfer object for SprintCapacity entity.
/// </summary>
public record SprintCapacityDto
{
    public Guid Id { get; init; }
    public Guid SprintId { get; init; }
    public string SprintName { get; init; } = string.Empty;
    public int TotalCapacityPoints { get; init; }
    public int AvailableMembers { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

/// <summary>
/// Data transfer object for creating or updating SprintCapacity.
/// </summary>
public record CreateSprintCapacityDto
{
    public Guid SprintId { get; init; }
    public int TotalCapacityPoints { get; init; }
    public int AvailableMembers { get; init; }
}

/// <summary>
/// Data transfer object for updating SprintCapacity.
/// </summary>
public record UpdateSprintCapacityDto
{
    public int TotalCapacityPoints { get; init; }
    public int AvailableMembers { get; init; }
}
