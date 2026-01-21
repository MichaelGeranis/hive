namespace Hive.Application.DTOs;

/// <summary>
/// Data Transfer Object for SkillCategory.
/// </summary>
public record SkillCategoryDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public int SortOrder { get; init; }
    public bool IsActive { get; init; }
    public int SkillCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

/// <summary>
/// DTO for creating a new SkillCategory.
/// </summary>
public record CreateSkillCategoryDto
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public int SortOrder { get; init; }
}

/// <summary>
/// DTO for updating a SkillCategory.
/// </summary>
public record UpdateSkillCategoryDto
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public int SortOrder { get; init; }
}
