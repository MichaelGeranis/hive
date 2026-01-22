namespace Hive.Application.DTOs;

/// <summary>
/// Severity level for planning insights.
/// </summary>
public enum InsightSeverity
{
    Info = 0,
    Warning = 1,
    Critical = 2
}

/// <summary>
/// Type of planning insight.
/// </summary>
public enum InsightType
{
    LeaveConflict = 0,
    DependencyRisk = 1,
    Bottleneck = 2,
    UnassignedWork = 3
}

/// <summary>
/// Data transfer object for a planning insight.
/// </summary>
public record PlanningInsightDto
{
    public InsightType Type { get; init; }
    public InsightSeverity Severity { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public Guid? RelatedInitiativeId { get; init; }
    public Guid? RelatedDirectReportId { get; init; }
    public Guid? RelatedSprintId { get; init; }
    public IReadOnlyList<Guid> AffectedCells { get; init; } = new List<Guid>();
}

/// <summary>
/// Data transfer object for all planning insights.
/// </summary>
public record PlanningInsightsDto
{
    public int TotalInitiatives { get; init; }
    public int AllocatedInitiatives { get; init; }
    public int IssueCount { get; init; }
    public int WarningCount { get; init; }
    public int CriticalCount { get; init; }
    public IReadOnlyList<PlanningInsightDto> Insights { get; init; } = new List<PlanningInsightDto>();
    public IReadOnlyList<TeamMemberSummaryDto> TeamMemberSummaries { get; init; } = new List<TeamMemberSummaryDto>();
    public IReadOnlyList<SprintWorkloadSummaryDto> SprintWorkloads { get; init; } = new List<SprintWorkloadSummaryDto>();
}

/// <summary>
/// Workload summary for a sprint based on T-shirt size estimations.
/// </summary>
public record SprintWorkloadSummaryDto
{
    public Guid SprintId { get; init; }
    public string SprintName { get; init; } = string.Empty;
    public decimal TotalSprintEffort { get; init; }
    public int InitiativeCount { get; init; }
    public int TeamMemberCount { get; init; }
    public IReadOnlyList<TeamMemberWorkloadDto> TeamMemberWorkloads { get; init; } = new List<TeamMemberWorkloadDto>();
}

/// <summary>
/// Workload for a team member in a specific sprint.
/// </summary>
public record TeamMemberWorkloadDto
{
    public Guid DirectReportId { get; init; }
    public string DirectReportName { get; init; } = string.Empty;
    public decimal SprintEffort { get; init; }
    public int InitiativeCount { get; init; }
    public IReadOnlyList<InitiativeWorkloadDto> Initiatives { get; init; } = new List<InitiativeWorkloadDto>();
}

/// <summary>
/// Workload contribution from a single initiative.
/// </summary>
public record InitiativeWorkloadDto
{
    public Guid InitiativeId { get; init; }
    public string InitiativeName { get; init; } = string.Empty;
    public string TshirtSize { get; init; } = string.Empty;
    public decimal SprintEffort { get; init; }
    public string Color { get; init; } = string.Empty;
}
