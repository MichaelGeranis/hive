namespace Hive.Application.DTOs;

/// <summary>
/// A skill roadmap in a list: its name and how many skills it holds.
/// </summary>
public record SkillRoadmapSummaryDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// The built-in roadmap it was added from; null for one made by hand.
    /// </summary>
    public string? LibraryKey { get; init; }

    public int SectionCount { get; init; }
    public int SkillCount { get; init; }
    public int HardSkillCount { get; init; }
    public int SoftSkillCount { get; init; }

    /// <summary>
    /// How many skills a plan aiming at each stage would copy, keyed Junior, Mid, Senior, Staff.
    /// </summary>
    public IReadOnlyDictionary<string, int> SkillCountsByStage { get; init; } = new Dictionary<string, int>();

    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// A skill roadmap with its skills, grouped into sections in order.
/// </summary>
public record SkillRoadmapDto : SkillRoadmapSummaryDto
{
    public IReadOnlyList<RoadmapSectionDto> Sections { get; init; } = [];
}

public record RoadmapSectionDto
{
    public string Name { get; init; } = string.Empty;
    public IReadOnlyList<RoadmapSkillDto> Skills { get; init; } = [];
}

public record RoadmapSkillDto
{
    public Guid Id { get; init; }
    public string Section { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// <c>Hard</c> or <c>Soft</c>.
    /// </summary>
    public string Category { get; init; } = string.Empty;

    public bool IsOptional { get; init; }

    /// <summary>
    /// The stage the skill is introduced at: <c>Junior</c>, <c>Mid</c>, <c>Senior</c> or <c>Staff</c>.
    /// </summary>
    public string FromStage { get; init; } = string.Empty;

    public int SortOrder { get; init; }
}

/// <summary>
/// Creates or renames a roadmap.
/// </summary>
public record SaveSkillRoadmapDto
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
}

/// <summary>
/// Adds a skill to a roadmap, or edits one.
/// </summary>
public record SaveRoadmapSkillDto
{
    public string Section { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }

    /// <summary>
    /// <c>Hard</c> or <c>Soft</c>.
    /// </summary>
    public string Category { get; init; } = "Hard";

    public bool IsOptional { get; init; }

    /// <summary>
    /// The stage the skill is introduced at: <c>Junior</c> (every plan), <c>Mid</c>, <c>Senior</c> or <c>Staff</c>.
    /// </summary>
    public string FromStage { get; init; } = "Junior";
}

/// <summary>
/// A built-in roadmap that can be added to Hive and then edited like any other.
/// </summary>
public record RoadmapLibraryItemDto
{
    public string Key { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public int SectionCount { get; init; }
    public int SkillCount { get; init; }
    public int HardSkillCount { get; init; }
    public int SoftSkillCount { get; init; }

    /// <summary>
    /// Whether a roadmap added from this one is already in Hive.
    /// </summary>
    public bool IsAdded { get; init; }
}
