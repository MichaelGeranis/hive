using Hive.Core.Entities;

namespace Hive.Application.DTOs;

// ---------------------------------------------------------------------------------------------
// Quarters
// ---------------------------------------------------------------------------------------------

/// <summary>
/// A planning quarter, with its target mix and whether its plan has been committed.
/// </summary>
public record QuarterDto
{
    public Guid Id { get; init; }
    public int Year { get; init; }
    public int QuarterNumber { get; init; }
    public string Name { get; init; } = string.Empty;
    public QuarterStatus Status { get; init; }
    public string OkrReference { get; init; } = string.Empty;
    public int ProductRoadmapPercent { get; init; }
    public int TechRoadmapPercent { get; init; }
    public int MaintenancePercent { get; init; }
    public DateTime? CommittedAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public record CreateQuarterDto
{
    public int Year { get; init; }
    public int QuarterNumber { get; init; }
    public string? OkrReference { get; init; }
}

/// <summary>
/// Updates a quarter. The three mix shares are set together or not at all.
/// </summary>
public record UpdateQuarterDto
{
    public string? OkrReference { get; init; }
    public int? ProductRoadmapPercent { get; init; }
    public int? TechRoadmapPercent { get; init; }
    public int? MaintenancePercent { get; init; }
}

// ---------------------------------------------------------------------------------------------
// Initiatives: options, leftovers and standing lanes
// ---------------------------------------------------------------------------------------------

/// <summary>
/// An initiative as the planning page shows it: what it is, where it stands, who is on it,
/// what it needs, and how the plan covers it.
/// </summary>
public record InitiativeDto
{
    public Guid Id { get; init; }
    public Guid QuarterId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Color { get; init; } = string.Empty;
    /// <summary>Null until the option is ready for commitment.</summary>
    public string? TshirtSize { get; init; }
    public string Url { get; init; } = string.Empty;
    public int WorkType { get; init; }
    public int Kind { get; init; }
    public int Status { get; init; }
    public int Rank { get; init; }

    /// <summary>Open readiness issues, as <see cref="ReadinessIssue"/> flags.</summary>
    public int Readiness { get; init; }

    public string DecisionNote { get; init; } = string.Empty;
    public Guid? EarliestSprintId { get; init; }
    public DateTime? ArrivedOn { get; init; }
    public DateTime? DueDate { get; init; }

    /// <summary>The T-shirt size in dev-sprints; null when the size has no mapping, and for lanes.</summary>
    public decimal? Effort { get; init; }

    /// <summary>The dev-sprints the plan gives it, after leave and part-time capacity.</summary>
    public decimal AllocatedEffort { get; init; }

    public ForecastDto? Forecast { get; init; }
    public IReadOnlyList<InitiativeMemberDto> Members { get; init; } = new List<InitiativeMemberDto>();
    public IReadOnlyList<ProductRefDto> Products { get; init; } = new List<ProductRefDto>();
    public IReadOnlyList<ExternalDependencyDto> ExternalDependencies { get; init; } = new List<ExternalDependencyDto>();
    public IReadOnlyList<PersonSuggestionDto> Suggestions { get; init; } = new List<PersonSuggestionDto>();
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public record CreateInitiativeDto
{
    public Guid QuarterId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? TshirtSize { get; init; }
    public string? Url { get; init; }
    public int? WorkType { get; init; }

    /// <summary>Option (default) or Leftover. Standing lanes are added with their own call.</summary>
    public int? Kind { get; init; }

    /// <summary>Where it starts: in discovery by default. Only an option ready for commitment can come with a size.</summary>
    public int? Status { get; init; }

    public DateTime? ArrivedOn { get; init; }
    public DateTime? DueDate { get; init; }
}

/// <summary>
/// Replaces an initiative's details. <see cref="ProductIds"/> left null keeps its products; a
/// null <see cref="TshirtSize"/> leaves the option unsized.
/// </summary>
public record UpdateInitiativeDto
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Color { get; init; }
    public string? TshirtSize { get; init; }
    public string? Url { get; init; }
    public int? WorkType { get; init; }
    public int? Kind { get; init; }
    public DateTime? ArrivedOn { get; init; }
    public DateTime? DueDate { get; init; }
    public int Readiness { get; init; }
    public Guid? EarliestSprintId { get; init; }
    public IReadOnlyList<Guid>? ProductIds { get; init; }
}

/// <summary>Moves an initiative through the planning statuses, with the reason for a decision.</summary>
public record ChangeInitiativeStatusDto
{
    public int Status { get; init; }
    public string? Note { get; init; }
}

/// <summary>The options (and leftovers) in priority order, most important first.</summary>
public record ReorderInitiativesDto
{
    public IReadOnlyList<Guid> OrderedIds { get; init; } = new List<Guid>();
}

public record AddBucketDto
{
    public int WorkType { get; init; }
}

public record ProductRefDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
}

/// <summary>
/// How the plan covers an initiative: dev-sprints allocated against its size, and the sprint
/// in which the allocation reaches the size.
/// </summary>
public record ForecastDto
{
    public decimal? Estimate { get; init; }
    public decimal Allocated { get; init; }

    /// <summary>Allocated over estimate, in percent; null when the size has no mapping.</summary>
    public int? CoveredPercent { get; init; }

    public Guid? FirstSprintId { get; init; }
    public Guid? DoneSprintId { get; init; }
    public string? DoneSprintName { get; init; }
    public bool DeliveredInQuarter { get; init; }
}

// ---------------------------------------------------------------------------------------------
// People on initiatives
// ---------------------------------------------------------------------------------------------

public record InitiativeMemberDto
{
    public Guid Id { get; init; }
    public Guid InitiativeId { get; init; }
    public Guid DirectReportId { get; init; }
    public string DirectReportName { get; init; } = string.Empty;
    public int Role { get; init; }

    /// <summary>Their best knowledge level (1–5) on the projects behind the initiative's products; 0 when none.</summary>
    public int KnowledgeLevel { get; init; }

    public DateTime CreatedAt { get; init; }
}

public record CreateInitiativeMemberDto
{
    public Guid DirectReportId { get; init; }
    public int Role { get; init; }
}

public record UpdateInitiativeMemberDto
{
    public int Role { get; init; }
}

/// <summary>
/// Someone who could lead or contribute to an initiative, from the knowledge matrix and the
/// tasks they have done on its products.
/// </summary>
public record PersonSuggestionDto
{
    public Guid DirectReportId { get; init; }
    public string Name { get; init; } = string.Empty;
    public int KnowledgeLevel { get; init; }
    public int TaskCount { get; init; }

    /// <summary>Their dev-sprints in the quarter not yet allocated to anything.</summary>
    public decimal FreeEffort { get; init; }

    public bool IsMember { get; init; }
}

// ---------------------------------------------------------------------------------------------
// Dependencies
// ---------------------------------------------------------------------------------------------

public record ExternalDependencyDto
{
    public Guid Id { get; init; }
    public Guid InitiativeId { get; init; }
    public string Team { get; init; } = string.Empty;
    public string Need { get; init; } = string.Empty;
    public int Status { get; init; }
}

public record SaveExternalDependencyDto
{
    public string Team { get; init; } = string.Empty;
    public string? Need { get; init; }
    public int Status { get; init; }
}

public record InitiativeDependencyDto
{
    public Guid Id { get; init; }
    public Guid DependentInitiativeId { get; init; }
    public string DependentInitiativeName { get; init; } = string.Empty;
    public Guid DependencyInitiativeId { get; init; }
    public string DependencyInitiativeName { get; init; } = string.Empty;
    public DependencyType Type { get; init; }
    public string Notes { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}

public record CreateInitiativeDependencyDto
{
    public Guid DependentInitiativeId { get; init; }
    public Guid DependencyInitiativeId { get; init; }
    public DependencyType Type { get; init; } = DependencyType.FinishToStart;
    public string? Notes { get; init; }
}

// ---------------------------------------------------------------------------------------------
// Allocation grid and roster
// ---------------------------------------------------------------------------------------------

public record AllocationDto
{
    public Guid Id { get; init; }
    public Guid InitiativeId { get; init; }
    public Guid DirectReportId { get; init; }
    public Guid SprintId { get; init; }
    public int Percent { get; init; }

    /// <summary>The dev-sprints this share is worth after leave and part-time capacity.</summary>
    public decimal Effort { get; init; }
}

/// <summary>One person's sprint and the initiatives that share it; an empty list clears the cell.</summary>
public record CellDto
{
    public Guid DirectReportId { get; init; }
    public Guid SprintId { get; init; }
    public IReadOnlyList<CellShareDto> Shares { get; init; } = new List<CellShareDto>();
}

public record CellShareDto
{
    public Guid InitiativeId { get; init; }
    public int Percent { get; init; }
}

public record SetCellsDto
{
    public IReadOnlyList<CellDto> Cells { get; init; } = new List<CellDto>();
}

/// <summary>A person on the quarter's plan, their planned capacity, and their availability per sprint.</summary>
public record RosterMemberDto
{
    public Guid DirectReportId { get; init; }
    public string Name { get; init; } = string.Empty;
    public bool IsDirect { get; init; }
    public int CapacityPercent { get; init; }
    public decimal TotalSupply { get; init; }

    /// <summary>Their supply in dev-days: one person for one working day.</summary>
    public decimal TotalDays { get; init; }

    public IReadOnlyList<MemberSprintDto> Sprints { get; init; } = new List<MemberSprintDto>();
}

public record MemberSprintDto
{
    public Guid SprintId { get; init; }

    /// <summary>The share of the sprint they are there for, in percent, after leave.</summary>
    public int AvailablePercent { get; init; }

    public int LeaveDays { get; init; }

    /// <summary>What they bring to the sprint in dev-sprints: availability times capacity.</summary>
    public decimal Supply { get; init; }

    public int AllocatedPercent { get; init; }
}

public record SetRosterMemberDto
{
    public Guid DirectReportId { get; init; }
    public int CapacityPercent { get; init; } = QuarterMember.DefaultCapacityPercent;
}

public record SprintNotesDto
{
    public string? Notes { get; init; }
}

// ---------------------------------------------------------------------------------------------
// The board: capacity, waterline, mix, insights
// ---------------------------------------------------------------------------------------------

/// <summary>
/// Everything the planning page needs for one quarter, computed server-side.
/// </summary>
public record PlanningBoardDto
{
    public QuarterDto Quarter { get; init; } = null!;
    public IReadOnlyList<PlanningSprintDto> Sprints { get; init; } = new List<PlanningSprintDto>();
    public IReadOnlyList<RosterMemberDto> Roster { get; init; } = new List<RosterMemberDto>();
    public IReadOnlyList<InitiativeDto> Initiatives { get; init; } = new List<InitiativeDto>();
    public IReadOnlyList<InitiativeDependencyDto> Dependencies { get; init; } = new List<InitiativeDependencyDto>();
    public IReadOnlyList<AllocationDto> Allocations { get; init; } = new List<AllocationDto>();
    public CapacityDto Capacity { get; init; } = new();
    public WaterlineDto Waterline { get; init; } = new();
    public MixDto Mix { get; init; } = new();
    public IReadOnlyList<PlanningInsightDto> Insights { get; init; } = new List<PlanningInsightDto>();
    public PlanningChecklistDto Checklist { get; init; } = new();
    public IReadOnlyList<TshirtSizeMapping> SizeMappings { get; init; } = new List<TshirtSizeMapping>();
    public IReadOnlyList<ProductRefDto> Products { get; init; } = new List<ProductRefDto>();
    public IReadOnlyList<PersonRefDto> People { get; init; } = new List<PersonRefDto>();
    public IReadOnlyList<string> KnownTeams { get; init; } = new List<string>();
}

public record PersonRefDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public bool IsDirect { get; init; }
}

public record PlanningSprintDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public int SprintNumber { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public int WorkingDays { get; init; }
    public string Notes { get; init; } = string.Empty;
}

/// <summary>
/// The team's supply in dev-sprints, what the standing lanes reserve of it, and what is left
/// for options.
/// </summary>
public record CapacityDto
{
    public decimal TotalSupply { get; init; }

    /// <summary>The team's supply in dev-days: one person for one working day.</summary>
    public decimal TotalDays { get; init; }

    public decimal TotalAllocated { get; init; }
    public IReadOnlyList<ReservationDto> Reservations { get; init; } = new List<ReservationDto>();
    public decimal OptionCapacity { get; init; }
    public IReadOnlyList<PlanSprintCapacityDto> Sprints { get; init; } = new List<PlanSprintCapacityDto>();
}

public record ReservationDto
{
    public int WorkType { get; init; }
    public int Percent { get; init; }
    public decimal Effort { get; init; }
}

public record PlanSprintCapacityDto
{
    public Guid SprintId { get; init; }
    public decimal Supply { get; init; }

    /// <summary>What the team could bring with nobody away: the roster's planned capacity.</summary>
    public decimal FullSupply { get; init; }

    public decimal Allocated { get; init; }
    public MixSharesDto Mix { get; init; } = new();
}

/// <summary>
/// The options in priority order against the capacity for options: those above the line fit.
/// </summary>
public record WaterlineDto
{
    public decimal Capacity { get; init; }
    public IReadOnlyList<WaterlineRowDto> Rows { get; init; } = new List<WaterlineRowDto>();
    public decimal CommittedEffort { get; init; }
}

public record WaterlineRowDto
{
    public Guid InitiativeId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Color { get; init; } = string.Empty;
    public int Kind { get; init; }
    public int Status { get; init; }
    public int Rank { get; init; }
    public string? TshirtSize { get; init; }
    public decimal? Effort { get; init; }
    public decimal Cumulative { get; init; }
    public bool AboveLine { get; init; }
}

/// <summary>Shares of effort by work type, in percent.</summary>
public record MixSharesDto
{
    public int ProductRoadmap { get; init; }
    public int TechRoadmap { get; init; }
    public int Maintenance { get; init; }
}

public record MixDto
{
    public MixSharesDto Target { get; init; } = new();
    public MixSharesDto Planned { get; init; } = new();
    public bool HasPlan { get; init; }

    /// <summary>The previous quarter's plan, by allocation; null when there was none.</summary>
    public MixSharesDto? PreviousQuarter { get; init; }

    /// <summary>
    /// The share of tickets in the previous quarter's sprints that were bugs, support or
    /// maintenance; null when Hive has no tasks for those sprints.
    /// </summary>
    public int? RecentUnplannedPercent { get; init; }

    public int RecentTaskCount { get; init; }

    /// <summary>
    /// The team's work so far as a mix, by story points, from the same work categories as the
    /// Dashboard's Work mix; null when no task has story points. Offered as a starting target.
    /// </summary>
    public MixSharesDto? PastWork { get; init; }

    public int PastWorkStoryPoints { get; init; }
}

public enum InsightSeverity
{
    Info = 0,
    Warning = 1,
    Critical = 2
}

public record PlanningInsightDto
{
    public string Type { get; init; } = string.Empty;
    public InsightSeverity Severity { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public Guid? InitiativeId { get; init; }
    public Guid? DirectReportId { get; init; }
    public Guid? SprintId { get; init; }
}

/// <summary>What is still open on the way from discovery to a committed plan.</summary>
public record PlanningChecklistDto
{
    public int OptionCount { get; init; }
    public int InDiscoveryCount { get; init; }
    public int UnsizedCount { get; init; }
    public int NotReadyCount { get; init; }
    public int OpenDependencyCount { get; init; }
    public int WithoutLeadCount { get; init; }
    public int UndecidedCount { get; init; }
    public int UnallocatedCommittedCount { get; init; }
    public bool HasSprints { get; init; }
    public bool HasRoster { get; init; }
    public bool MixOnTarget { get; init; }
    public bool Committed { get; init; }
}

// ---------------------------------------------------------------------------------------------
// Draft, scenario, commitment, review
// ---------------------------------------------------------------------------------------------

public record DraftAllocationDto
{
    public Guid InitiativeId { get; init; }
    public string InitiativeName { get; init; } = string.Empty;
    public Guid DirectReportId { get; init; }
    public string DirectReportName { get; init; } = string.Empty;
    public Guid SprintId { get; init; }
    public string SprintName { get; init; } = string.Empty;
    public int Percent { get; init; }
}

/// <summary>
/// A proposed fill of the grid's free cells: options in priority order, then the lanes.
/// </summary>
public record PlanDraftDto
{
    public bool Applied { get; init; }
    public IReadOnlyList<DraftAllocationDto> Allocations { get; init; } = new List<DraftAllocationDto>();
    public IReadOnlyList<ScenarioOptionDto> Options { get; init; } = new List<ScenarioOptionDto>();
}

public record ScenarioAbsenceDto
{
    public Guid DirectReportId { get; init; }
    public IReadOnlyList<Guid> SprintIds { get; init; } = new List<Guid>();
}

public record ScenarioDelayDto
{
    public Guid InitiativeId { get; init; }
    public Guid NotBeforeSprintId { get; init; }
}

/// <summary>What if someone is away, or an option cannot start until later.</summary>
public record ScenarioRequestDto
{
    public IReadOnlyList<ScenarioAbsenceDto> Absences { get; init; } = new List<ScenarioAbsenceDto>();
    public IReadOnlyList<ScenarioDelayDto> Delays { get; init; } = new List<ScenarioDelayDto>();
}

public record ScenarioOptionDto
{
    public Guid InitiativeId { get; init; }
    public string Name { get; init; } = string.Empty;
    public ForecastDto Before { get; init; } = new();
    public ForecastDto After { get; init; } = new();
    public bool AboveLineBefore { get; init; }
    public bool AboveLineAfter { get; init; }
}

public record ScenarioDto
{
    public decimal SupplyBefore { get; init; }
    public decimal SupplyAfter { get; init; }
    public decimal OptionCapacityBefore { get; init; }
    public decimal OptionCapacityAfter { get; init; }
    public IReadOnlyList<ScenarioOptionDto> Options { get; init; } = new List<ScenarioOptionDto>();
}

/// <summary>One initiative as it stood in the committed plan.</summary>
public record BaselineOptionDto
{
    public Guid InitiativeId { get; init; }
    public string Name { get; init; } = string.Empty;
    public int Status { get; init; }
    public string? TshirtSize { get; init; }
    public decimal? Estimate { get; init; }
    public decimal Allocated { get; init; }
    public int? CoveredPercent { get; init; }
    public string? DoneSprintName { get; init; }
    public bool AboveLine { get; init; }
}

public record BaselineDto
{
    public DateTime CommittedAt { get; init; }
    public decimal TotalSupply { get; init; }
    public decimal OptionCapacity { get; init; }
    public MixSharesDto PlannedMix { get; init; } = new();
    public IReadOnlyList<BaselineOptionDto> Options { get; init; } = new List<BaselineOptionDto>();
}

public record ReviewOptionDto
{
    public Guid InitiativeId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Color { get; init; } = string.Empty;
    public BaselineOptionDto? AtCommit { get; init; }
    public int StatusNow { get; init; }
    public decimal AllocatedNow { get; init; }
    public int? CoveredNow { get; init; }
    public string? DoneSprintNow { get; init; }
}

/// <summary>The committed plan against what happened, and a summary to share with the PM.</summary>
public record PlanReviewDto
{
    public DateTime? CommittedAt { get; init; }
    public IReadOnlyList<ReviewOptionDto> Options { get; init; } = new List<ReviewOptionDto>();
    public string Summary { get; init; } = string.Empty;
}

public record CalibrationRowDto
{
    public string Size { get; init; } = string.Empty;
    public decimal? Estimate { get; init; }
    public int DeliveredCount { get; init; }
    public decimal AverageActual { get; init; }

    /// <summary>Actual over estimate: above 1 means the size ran long.</summary>
    public decimal? Ratio { get; init; }
}

/// <summary>Delivered options by size: what each size was estimated at, and what it took.</summary>
public record CalibrationDto
{
    public IReadOnlyList<CalibrationRowDto> Sizes { get; init; } = new List<CalibrationRowDto>();
    public int DeliveredCount { get; init; }
}
