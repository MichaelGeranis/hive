namespace Hive.Core.Entities;

/// <summary>
/// Represents an assessment of a direct report's proficiency in a specific skill.
/// This is the junction entity linking DirectReport and Skill with assessment data.
/// </summary>
public class SkillAssessment
{
    public Guid Id { get; private set; }
    public Guid DirectReportId { get; private set; }
    public Guid SkillId { get; private set; }
    public ProficiencyLevel Level { get; private set; }
    public ProficiencyLevel? TargetLevel { get; private set; }
    public string Notes { get; private set; } = string.Empty;
    public DateTime? UpdatedAt { get; private set; }

    private SkillAssessment() { }

    public SkillAssessment(
        Guid directReportId,
        Guid skillId,
        ProficiencyLevel level,
        ProficiencyLevel? targetLevel = null,
        string? notes = null)
    {
        ValidateIds(directReportId, skillId);
        ValidateLevels(level, targetLevel);

        Id = Guid.NewGuid();
        DirectReportId = directReportId;
        SkillId = skillId;
        Level = level;
        TargetLevel = targetLevel;
        Notes = notes?.Trim() ?? string.Empty;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateAssessment(
        ProficiencyLevel level,
        ProficiencyLevel? targetLevel = null,
        string? notes = null)
    {
        ValidateLevels(level, targetLevel);

        Level = level;
        TargetLevel = targetLevel;
        Notes = notes?.Trim() ?? string.Empty;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Calculates the gap between current level and target level.
    /// Returns null if no target is set, positive if below target, negative if above target.
    /// </summary>
    public int? GetSkillGap()
    {
        if (!TargetLevel.HasValue || TargetLevel == ProficiencyLevel.None)
            return null;

        return (int)TargetLevel.Value - (int)Level;
    }

    /// <summary>
    /// Checks if the current level meets or exceeds the target.
    /// </summary>
    public bool MeetsTarget()
    {
        if (!TargetLevel.HasValue || TargetLevel == ProficiencyLevel.None)
            return true;

        return Level >= TargetLevel.Value;
    }

    private static void ValidateIds(Guid directReportId, Guid skillId)
    {
        if (directReportId == Guid.Empty)
        {
            throw new ArgumentException("DirectReportId cannot be empty.", nameof(directReportId));
        }

        if (skillId == Guid.Empty)
        {
            throw new ArgumentException("SkillId cannot be empty.", nameof(skillId));
        }
    }

    private static void ValidateLevels(ProficiencyLevel level, ProficiencyLevel? targetLevel)
    {
        if (level == ProficiencyLevel.None)
        {
            throw new ArgumentException("Proficiency level must be set.", nameof(level));
        }

        if (targetLevel.HasValue && targetLevel.Value != ProficiencyLevel.None && targetLevel.Value < level)
        {
            // This is allowed - target can be lower if we want to document where someone is overqualified
            // But we'll just log a note, not throw
        }
    }
}
