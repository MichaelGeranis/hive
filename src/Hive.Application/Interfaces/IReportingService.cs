using Hive.Application.DTOs;

namespace Hive.Application.Interfaces;

/// <summary>
/// Service interface for generating analytics and reports.
/// </summary>
public interface IReportingService
{
    /// <summary>
    /// Gets the complete dashboard overview with all metrics.
    /// </summary>
    /// <param name="sprintCount">Optional number of recent sprints to include for task metrics. If null, returns all tasks.</param>
    Task<DashboardOverviewDto> GetDashboardOverviewAsync(int? sprintCount = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets who has worked on which product: direct reports only, every task but an epic.
    /// </summary>
    /// <param name="months">How many months back to look, by the tasks' sprints. If null, all time.</param>
    Task<ProductEngagementDto> GetProductEngagementAsync(int? months = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets what needs the manager's attention now: the current sprint, 1:1s against the
    /// cadence, to-dos coming due and who is away. Independent of the sprint filter.
    /// </summary>
    Task<DashboardFocusDto> GetDashboardFocusAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets tasks and projects analytics.
    /// </summary>
    /// <param name="sprintCount">Optional number of recent sprints to include for task metrics. If null, returns all tasks.</param>
    Task<TasksOverviewDto> GetTasksAnalyticsAsync(int? sprintCount = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets detailed analytics for a specific direct report.
    /// </summary>
    Task<DirectReportAnalyticsDto?> GetDirectReportAnalyticsAsync(Guid directReportId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets team velocity metrics based on completed story points per sprint.
    /// </summary>
    /// <param name="sprintCount">Optional number of recent sprints to include. If null, returns all sprints.</param>
    Task<TeamVelocityDto> GetTeamVelocityAsync(int? sprintCount = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets capacity analysis comparing planned vs actual story points by sprint.
    /// </summary>
    /// <param name="sprintCount">Optional number of recent past sprints to include. If null, returns all sprints.</param>
    Task<CapacityAnalysisDto> GetCapacityAnalysisAsync(int? sprintCount = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets, per sprint, how much work was carried in from an earlier sprint.
    /// </summary>
    /// <param name="sprintCount">Optional number of recent sprints to include. If null, returns all sprints.</param>
    Task<CarryOverTrendDto> GetCarryOverTrendAsync(int? sprintCount = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets, per sprint, the new bug, support and maintenance tickets, counted in tickets.
    /// </summary>
    /// <param name="sprintCount">Optional number of recent sprints to include. If null, returns all sprints.</param>
    Task<BugSupportInflowDto> GetBugSupportInflowAsync(int? sprintCount = null, CancellationToken cancellationToken = default);
}
