namespace Hive.Core.Entities;

/// <summary>
/// One skill on a <see cref="CareerPlan"/>: copied from a roadmap skill (or added to the plan by
/// hand), with the direct report's level, whether they are working on it now, and the evidence.
/// </summary>
public class CareerPlanSkill
{
    public const int MaxEvidenceLength = 2000;

    /// <summary>
    /// The level from which a person has a skill: practising it without help.
    /// </summary>
    public const int HasFromLevel = SkillLevel.Practising;

    public Guid Id { get; private set; }
    public Guid CareerPlanId { get; private set; }
    public string Section { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public SkillCategory Category { get; private set; }
    public bool IsOptional { get; private set; }

    /// <summary>
    /// The career stage the skill is expected from, copied from the roadmap.
    /// </summary>
    public CareerStage FromStage { get; private set; }

    public int SortOrder { get; private set; }

    /// <summary>
    /// The current level, 0 (not started) to 5 (expert).
    /// </summary>
    public int Level { get; private set; }

    /// <summary>
    /// Whether the direct report is working on this skill now.
    /// </summary>
    public bool IsFocus { get; private set; }

    /// <summary>
    /// What shows the current level: a 1:1, a piece of work, feedback.
    /// </summary>
    public string Evidence { get; private set; } = string.Empty;

    /// <summary>
    /// The last time the level went up. A level lowered to correct a rating is not a level-up.
    /// </summary>
    public DateTime? LastLevelUpAt { get; private set; }

    public DateTime? UpdatedAt { get; private set; }

    private CareerPlanSkill() { }

    public CareerPlanSkill(
        Guid careerPlanId,
        string section,
        string title,
        SkillCategory category,
        int sortOrder,
        string? description = null,
        bool isOptional = false,
        CareerStage fromStage = CareerStage.Junior)
    {
        if (careerPlanId == Guid.Empty)
        {
            throw new ArgumentException("CareerPlanId cannot be empty.", nameof(careerPlanId));
        }

        RoadmapSkill.Validate(section, title, category, sortOrder, description);

        if (!Enum.IsDefined(fromStage))
        {
            throw new ArgumentException("Unknown career stage.", nameof(fromStage));
        }

        Id = Guid.NewGuid();
        CareerPlanId = careerPlanId;
        Section = section.Trim();
        Title = title.Trim();
        Description = description?.Trim() ?? string.Empty;
        Category = category;
        SortOrder = sortOrder;
        IsOptional = isOptional;
        FromStage = fromStage;
        Level = SkillLevel.Min;
    }

    /// <summary>
    /// Copies a roadmap skill onto a plan, not started.
    /// </summary>
    public static CareerPlanSkill FromRoadmap(Guid careerPlanId, RoadmapSkill skill) =>
        new(careerPlanId, skill.Section, skill.Title, skill.Category, skill.SortOrder, skill.Description, skill.IsOptional,
            skill.FromStage);

    /// <summary>
    /// Whether the direct report has the skill: Practising or above.
    /// </summary>
    public bool Has => Level >= HasFromLevel;

    /// <summary>
    /// Working on it now comes first; otherwise they have it, or not yet.
    /// </summary>
    public CareerSkillStatus Status =>
        IsFocus ? CareerSkillStatus.WorkingOn
        : Has ? CareerSkillStatus.Has
        : CareerSkillStatus.NotYet;

    /// <summary>
    /// Rates the skill. Raising the level records a level-up; the evidence, when given, replaces
    /// the previous evidence.
    /// </summary>
    /// <returns>True when the level went up.</returns>
    public bool SetLevel(int level, string? evidence = null)
    {
        SkillLevel.Validate(level, nameof(level));

        if (evidence is not null && evidence.Trim().Length > MaxEvidenceLength)
        {
            throw new ArgumentException($"Evidence cannot exceed {MaxEvidenceLength} characters.", nameof(evidence));
        }

        var now = DateTime.UtcNow;
        var levelledUp = level > Level;

        Level = level;
        if (evidence is not null)
        {
            Evidence = evidence.Trim();
        }

        if (levelledUp)
        {
            LastLevelUpAt = now;
        }

        UpdatedAt = now;
        return levelledUp;
    }

    /// <summary>
    /// Marks the skill as being worked on now, or not.
    /// </summary>
    public void SetFocus(bool isFocus)
    {
        IsFocus = isFocus;
        UpdatedAt = DateTime.UtcNow;
    }
}
