namespace Hive.Core.Entities;

/// <summary>
/// One skill on a <see cref="SkillRoadmap"/>, in a named section. Sections are shown in the order
/// of their first skill; skills within a section by <see cref="SortOrder"/>.
/// </summary>
public class RoadmapSkill
{
    public const int MaxSectionLength = 100;
    public const int MaxTitleLength = 200;
    public const int MaxDescriptionLength = 2000;

    public Guid Id { get; private set; }
    public Guid RoadmapId { get; private set; }
    public string Section { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public SkillCategory Category { get; private set; }

    /// <summary>
    /// An optional skill is nice to have: it earns XP but is not needed to complete the roadmap.
    /// </summary>
    public bool IsOptional { get; private set; }

    /// <summary>
    /// The career stage the skill is introduced at. A plan aiming at an earlier stage leaves it out.
    /// </summary>
    public CareerStage FromStage { get; private set; }

    public int SortOrder { get; private set; }

    private RoadmapSkill() { }

    public RoadmapSkill(
        Guid roadmapId,
        string section,
        string title,
        SkillCategory category,
        int sortOrder,
        string? description = null,
        bool isOptional = false,
        CareerStage fromStage = CareerStage.Junior)
    {
        if (roadmapId == Guid.Empty)
        {
            throw new ArgumentException("RoadmapId cannot be empty.", nameof(roadmapId));
        }

        Validate(section, title, category, sortOrder, description);
        ValidateStage(fromStage);

        Id = Guid.NewGuid();
        RoadmapId = roadmapId;
        Section = section.Trim();
        Title = title.Trim();
        Description = description?.Trim() ?? string.Empty;
        Category = category;
        SortOrder = sortOrder;
        IsOptional = isOptional;
        FromStage = fromStage;
    }

    /// <summary>
    /// Whether a plan aiming at <paramref name="stage"/> includes this skill.
    /// </summary>
    public bool AppliesTo(CareerStage stage) => FromStage <= stage;

    public void Update(
        string section,
        string title,
        SkillCategory category,
        string? description,
        bool isOptional,
        CareerStage fromStage = CareerStage.Junior)
    {
        Validate(section, title, category, SortOrder, description);
        ValidateStage(fromStage);

        Section = section.Trim();
        Title = title.Trim();
        Description = description?.Trim() ?? string.Empty;
        Category = category;
        IsOptional = isOptional;
        FromStage = fromStage;
    }

    private static void ValidateStage(CareerStage fromStage)
    {
        if (!Enum.IsDefined(fromStage))
        {
            throw new ArgumentException("Unknown career stage.", nameof(fromStage));
        }
    }

    /// <summary>
    /// Validates the fields a skill has wherever it lives: on a roadmap or on a career plan.
    /// </summary>
    internal static void Validate(string section, string title, SkillCategory category, int sortOrder, string? description)
    {
        if (string.IsNullOrWhiteSpace(section))
        {
            throw new ArgumentException("Section cannot be empty.", nameof(section));
        }

        if (section.Trim().Length > MaxSectionLength)
        {
            throw new ArgumentException($"Section cannot exceed {MaxSectionLength} characters.", nameof(section));
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Skill title cannot be empty.", nameof(title));
        }

        if (title.Trim().Length > MaxTitleLength)
        {
            throw new ArgumentException($"Skill title cannot exceed {MaxTitleLength} characters.", nameof(title));
        }

        if (description is not null && description.Trim().Length > MaxDescriptionLength)
        {
            throw new ArgumentException($"Skill description cannot exceed {MaxDescriptionLength} characters.", nameof(description));
        }

        if (!Enum.IsDefined(category))
        {
            throw new ArgumentException("Unknown skill category.", nameof(category));
        }

        if (sortOrder < 0)
        {
            throw new ArgumentException("Sort order cannot be negative.", nameof(sortOrder));
        }
    }
}
