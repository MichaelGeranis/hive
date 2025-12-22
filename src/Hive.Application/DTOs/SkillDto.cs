using Hive.Core.Entities;

namespace Hive.Application.DTOs;

/// <summary>
/// Data Transfer Object for Skill.
/// </summary>
public record SkillDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public SkillCategory Category { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

/// <summary>
/// DTO for creating a new Skill.
/// </summary>
public record CreateSkillDto
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public SkillCategory Category { get; init; }
}

/// <summary>
/// DTO for updating a Skill.
/// </summary>
public record UpdateSkillDto
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public SkillCategory Category { get; init; }
}
