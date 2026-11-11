namespace Hive.Core.Entities;

/// <summary>
/// A rung on the engineering career ladder. A career plan records the stage of the direct
/// report's current role; a roadmap skill is expected from a stage onward.
/// </summary>
public enum CareerStage
{
    Junior = 0,
    Mid = 1,
    Senior = 2,
    Staff = 3
}

/// <summary>
/// The level each stage expects on a skill. Expectations describe the ladder; they are never
/// targets a plan is made to reach.
/// </summary>
public static class CareerStageExpectations
{
    /// <summary>
    /// The level <paramref name="stage"/> expects on a skill: Junior expects Learning (2), Mid
    /// Practising (3), Senior Proficient (4), and Staff Proficient on hard skills and Expert (5) on
    /// soft ones, since Staff is mostly about influence. A skill expected from a later stage is
    /// expected at Practising in the stage it arrives, and one level more for each stage after.
    /// </summary>
    public static int For(CareerStage stage, SkillCategory category, CareerStage fromStage = CareerStage.Junior)
    {
        var stageLevel = stage switch
        {
            CareerStage.Junior => 2,
            CareerStage.Mid => 3,
            CareerStage.Senior => 4,
            _ => category == SkillCategory.Soft ? 5 : 4
        };

        if (fromStage == CareerStage.Junior)
        {
            return stageLevel;
        }

        var cap = SkillLevel.Practising + Math.Max(0, stage - fromStage);
        return Math.Min(stageLevel, cap);
    }

    /// <summary>
    /// Reads a stage sent as its name, ignoring case.
    /// </summary>
    public static CareerStage Parse(string? stage, string paramName)
    {
        if (!string.IsNullOrWhiteSpace(stage)
            && !int.TryParse(stage, out _)
            && Enum.TryParse<CareerStage>(stage.Trim(), ignoreCase: true, out var parsed)
            && Enum.IsDefined(parsed))
        {
            return parsed;
        }

        throw new ArgumentException("Career stage must be Junior, Mid, Senior or Staff.", paramName);
    }
}
