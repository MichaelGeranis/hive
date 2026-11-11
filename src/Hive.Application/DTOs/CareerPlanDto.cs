namespace Hive.Application.DTOs;

/// <summary>
/// A career plan in a list. It deliberately carries no scores: plans are never set side by side.
/// </summary>
public record CareerPlanSummaryDto
{
    public Guid Id { get; init; }
    public Guid DirectReportId { get; init; }
    public string DirectReportName { get; init; } = string.Empty;
    public string RoadmapName { get; init; } = string.Empty;

    /// <summary>
    /// The stage of the person's current role: <c>Junior</c>, <c>Mid</c>, <c>Senior</c> or <c>Staff</c>.
    /// </summary>
    public string CurrentStage { get; init; } = string.Empty;

    /// <summary>
    /// How many skills they are working on now.
    /// </summary>
    public int WorkingOnCount { get; init; }

    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// The last time the plan or any of its skills changed.
    /// </summary>
    public DateTime LastChangedAt { get; init; }

    /// <summary>
    /// Nothing on the plan has changed for 60 days.
    /// </summary>
    public bool IsQuiet { get; init; }
}

/// <summary>
/// One direct report's career plan: every skill by section, where their skills sit on the
/// ladder, their strengths and weaknesses, and the game.
/// </summary>
public record CareerPlanDto : CareerPlanSummaryDto
{
    public string Notes { get; init; } = string.Empty;
    public int SkillCount { get; init; }
    public int HasCount { get; init; }
    public int NotYetCount { get; init; }
    public IReadOnlyList<CareerPlanSectionDto> Sections { get; init; } = [];
    public CareerLadderDto Ladder { get; init; } = new();
    public CareerGameDto Game { get; init; } = new();
}

/// <summary>
/// Where the person's skills sit on the ladder, read against their current stage. An
/// indication, never a goal, and never a comparison with anyone else.
/// </summary>
public record CareerLadderDto
{
    public string CurrentStage { get; init; } = string.Empty;

    /// <summary>
    /// The highest stage whose expectations (and every earlier stage's) they meet at 80% or more;
    /// null when not yet Junior's.
    /// </summary>
    public string? SkillsStage { get; init; }

    /// <summary>
    /// <c>NotAssessed</c>, <c>BelowRole</c>, <c>AtRole</c> or <c>AboveRole</c>.
    /// </summary>
    public string Reading { get; init; } = string.Empty;

    public IReadOnlyList<StageFitDto> Stages { get; init; } = [];

    /// <summary>
    /// Above what their stage expects, or skills of a later stage they already have.
    /// </summary>
    public IReadOnlyList<CareerPlanSkillDto> Strengths { get; init; } = [];

    /// <summary>
    /// Below what their stage expects; widest gap first.
    /// </summary>
    public IReadOnlyList<CareerPlanSkillDto> Weaknesses { get; init; } = [];
}

public record StageFitDto
{
    public string Stage { get; init; } = string.Empty;
    public int ExpectedCount { get; init; }
    public int MetCount { get; init; }
    public int MetPercent { get; init; }
}

/// <summary>
/// The playful layer on one person's own plan.
/// </summary>
public record CareerGameDto
{
    public int Xp { get; init; }
    public int PlayerLevel { get; init; }
    public string PlayerTitle { get; init; } = string.Empty;

    /// <summary>
    /// XP at which the current player level started.
    /// </summary>
    public int CurrentLevelXp { get; init; }

    /// <summary>
    /// XP at which the next player level starts.
    /// </summary>
    public int NextLevelXp { get; init; }

    public int SkillsLevelledUpRecently { get; init; }
    public IReadOnlyList<CareerBadgeDto> Badges { get; init; } = [];
}

public record CareerPlanSectionDto
{
    public string Name { get; init; } = string.Empty;
    public int SkillCount { get; init; }
    public int HasCount { get; init; }
    public int WorkingOnCount { get; init; }
    public bool IsCleared { get; init; }
    public IReadOnlyList<CareerPlanSkillDto> Skills { get; init; } = [];
}

public record CareerPlanSkillDto
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
    /// The stage the skill is expected from.
    /// </summary>
    public string FromStage { get; init; } = string.Empty;

    public int Level { get; init; }
    public string LevelLabel { get; init; } = string.Empty;

    /// <summary>
    /// <c>NotYet</c>, <c>WorkingOn</c> or <c>Has</c>.
    /// </summary>
    public string Status { get; init; } = string.Empty;

    public bool IsFocus { get; init; }

    /// <summary>
    /// The level the person's current stage expects; null when the skill belongs to a later stage.
    /// </summary>
    public int? ExpectedLevel { get; init; }

    public string Evidence { get; init; } = string.Empty;
    public DateTime? LastLevelUpAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public record CareerBadgeDto
{
    public string Key { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public bool IsEarned { get; init; }
}

/// <summary>
/// Makes a direct report's plan from a roadmap.
/// </summary>
public record CreateCareerPlanDto
{
    public Guid DirectReportId { get; init; }
    public Guid RoadmapId { get; init; }

    /// <summary>
    /// The stage of their current role: <c>Junior</c>, <c>Mid</c>, <c>Senior</c> or <c>Staff</c>.
    /// </summary>
    public string CurrentStage { get; init; } = string.Empty;

    public string? Notes { get; init; }
}

/// <summary>
/// Records a change of role stage, or new notes.
/// </summary>
public record UpdateCareerPlanDto
{
    public string CurrentStage { get; init; } = string.Empty;
    public string? Notes { get; init; }
}

/// <summary>
/// Rates a skill. Evidence, when given, replaces the previous evidence.
/// </summary>
public record SetSkillLevelDto
{
    public int Level { get; init; }
    public string? Evidence { get; init; }
}

/// <summary>
/// Puts a skill in or out of what the person is working on now.
/// </summary>
public record SetSkillFocusDto
{
    public bool IsFocus { get; init; }
}

/// <summary>
/// Adds a skill to one plan only, beyond what its roadmap holds.
/// </summary>
public record AddCareerPlanSkillDto
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
    /// The stage the skill is expected from; <c>Junior</c> by default.
    /// </summary>
    public string FromStage { get; init; } = "Junior";
}

/// <summary>
/// A plan after a skill was rated, with what the rating unlocked, so the page can celebrate it.
/// </summary>
public record SkillLevelChangeDto
{
    public CareerPlanDto Plan { get; init; } = new();
    public bool SkillLevelledUp { get; init; }
    public bool PlayerLevelledUp { get; init; }

    /// <summary>
    /// Badges this rating earned.
    /// </summary>
    public IReadOnlyList<CareerBadgeDto> NewBadges { get; init; } = [];
}

/// <summary>
/// What the career plans need from the manager, for the dashboard. No scores.
/// </summary>
public record CareerOverviewDto
{
    public IReadOnlyList<CareerAttentionDto> DirectReportsWithoutPlan { get; init; } = [];

    /// <summary>
    /// Plans with no skill being worked on.
    /// </summary>
    public IReadOnlyList<CareerAttentionDto> NothingInFocus { get; init; } = [];

    /// <summary>
    /// Plans nothing on which has changed for 60 days.
    /// </summary>
    public IReadOnlyList<CareerAttentionDto> Quiet { get; init; } = [];
}

public record CareerAttentionDto
{
    public Guid DirectReportId { get; init; }
    public string FullName { get; init; } = string.Empty;

    /// <summary>
    /// The plan; null for a direct report without one.
    /// </summary>
    public Guid? PlanId { get; init; }

    public DateTime? LastChangedAt { get; init; }
}
