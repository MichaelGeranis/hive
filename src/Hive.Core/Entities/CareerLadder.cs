namespace Hive.Core.Entities;

/// <summary>
/// Where a direct report's skills sit on the career ladder, read against the stage of their
/// current role: how much of each stage's expectations they meet, whether that reads above, at
/// or below their role, and their own strengths and weaknesses. It only ever compares a person
/// with the ladder, never with anyone else. Derived, never stored.
/// </summary>
public sealed class CareerLadder
{
    /// <summary>
    /// The share of a stage's expectations a person meets for their skills to be at that stage.
    /// </summary>
    public const int StageMetPercent = 80;

    public CareerStage CurrentStage { get; private init; }

    /// <summary>
    /// The highest stage whose expectations, and every earlier stage's, they meet; null when they
    /// do not yet meet Junior's.
    /// </summary>
    public CareerStage? SkillsStage { get; private init; }

    public LadderReading Reading { get; private init; }
    public IReadOnlyList<StageFit> Stages { get; private init; } = [];

    /// <summary>
    /// Skills above what their stage expects, and skills of a later stage they already have;
    /// strongest first.
    /// </summary>
    public IReadOnlyList<CareerPlanSkill> Strengths { get; private init; } = [];

    /// <summary>
    /// Required skills below what their stage expects; widest gap first.
    /// </summary>
    public IReadOnlyList<CareerPlanSkill> Weaknesses { get; private init; } = [];

    private CareerLadder() { }

    public static CareerLadder Assess(IEnumerable<CareerPlanSkill> planSkills, CareerStage currentStage)
    {
        var skills = planSkills.OrderBy(s => s.SortOrder).ToList();

        var stages = Enum.GetValues<CareerStage>()
            .Select(stage =>
            {
                var expected = skills.Where(s => !s.IsOptional && s.FromStage <= stage).ToList();
                var met = expected.Count(s => s.Level >= CareerStageExpectations.For(stage, s.Category, s.FromStage));
                return new StageFit(stage, expected.Count, met,
                    expected.Count == 0 ? 0 : (int)Math.Floor(100.0 * met / expected.Count));
            })
            .ToList();

        CareerStage? skillsStage = null;
        foreach (var fit in stages)
        {
            if (fit.ExpectedCount == 0 || fit.MetPercent < StageMetPercent) break;
            skillsStage = fit.Stage;
        }

        var reading = skills.All(s => s.Level == 0) ? LadderReading.NotAssessed
            : skillsStage is null || skillsStage < currentStage ? LadderReading.BelowRole
            : skillsStage == currentStage ? LadderReading.AtRole
            : LadderReading.AboveRole;

        int Expected(CareerPlanSkill s) => CareerStageExpectations.For(currentStage, s.Category, s.FromStage);

        var strengths = skills
            .Where(s => s.FromStage <= currentStage ? s.Level > Expected(s) : s.Has)
            .OrderByDescending(s => s.FromStage > currentStage)
            .ThenByDescending(s => s.Level - (s.FromStage <= currentStage ? Expected(s) : CareerPlanSkill.HasFromLevel))
            .ThenBy(s => s.SortOrder)
            .ToList();

        var weaknesses = skills
            .Where(s => !s.IsOptional && s.FromStage <= currentStage && s.Level < Expected(s))
            .OrderByDescending(s => Expected(s) - s.Level)
            .ThenBy(s => s.SortOrder)
            .ToList();

        return new CareerLadder
        {
            CurrentStage = currentStage,
            SkillsStage = skillsStage,
            Reading = reading,
            Stages = stages,
            Strengths = strengths,
            Weaknesses = weaknesses
        };
    }

    /// <summary>
    /// The level the direct report's current stage expects on a skill, or null when the skill is
    /// only expected from a later stage.
    /// </summary>
    public int? ExpectedLevel(CareerPlanSkill skill) =>
        skill.FromStage <= CurrentStage ? CareerStageExpectations.For(CurrentStage, skill.Category, skill.FromStage) : null;
}

/// <summary>
/// How many of a stage's expectations a direct report meets.
/// </summary>
public sealed record StageFit(CareerStage Stage, int ExpectedCount, int MetCount, int MetPercent);
