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
}
