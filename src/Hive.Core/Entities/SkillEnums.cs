namespace Hive.Core.Entities;

/// <summary>
/// Category of skills for grouping and filtering.
/// </summary>
public enum SkillCategory
{
    Technical = 0,
    SoftSkills = 1,
    Leadership = 2,
    DomainKnowledge = 3,
    Tools = 4
}

/// <summary>
/// Proficiency level for skill assessments.
/// </summary>
public enum ProficiencyLevel
{
    None = 0,
    Novice = 1,
    Beginner = 2,
    Intermediate = 3,
    Advanced = 4,
    Expert = 5
}
