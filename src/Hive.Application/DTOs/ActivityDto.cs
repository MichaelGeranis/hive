using System.ComponentModel.DataAnnotations;

namespace Hive.Application.DTOs;

/// <summary>
/// Data transfer object for Activity entity.
/// </summary>
public record ActivityDto
{
    public Guid Id { get; init; }
    public string ActivityType { get; init; } = string.Empty;
    public string ActivityTypeName { get; init; } = string.Empty;
    public string EntityType { get; init; } = string.Empty;
    public string EntityTypeName { get; init; } = string.Empty;
    public Guid EntityId { get; init; }
    public string EntityName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// Data transfer object for creating a new activity log entry.
/// </summary>
public record CreateActivityDto
{
    [Required]
    public string ActivityType { get; init; } = string.Empty;

    [Required]
    public string EntityType { get; init; } = string.Empty;

    [Required]
    public Guid EntityId { get; init; }

    [Required]
    [MaxLength(500)]
    public string EntityName { get; init; } = string.Empty;

    [Required]
    [MaxLength(1000)]
    public string Description { get; init; } = string.Empty;

    public DateTime? Timestamp { get; init; }
}
