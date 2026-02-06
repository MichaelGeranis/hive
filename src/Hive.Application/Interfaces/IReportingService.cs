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
    /// Gets performance reviews analytics.
    /// </summary>
    Task<ReviewsOverviewDto> GetReviewsAnalyticsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets one-on-one meetings analytics.
    /// </summary>
    Task<OneOnOnesOverviewDto> GetOneOnOnesAnalyticsAsync(CancellationToken cancellationToken = default);

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
    /// Gets one-on-one frequency report for all direct reports.
    /// </summary>
    Task<IReadOnlyList<OneOnOneFrequencyDto>> GetOneOnOneFrequencyReportAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets action items summary.
    /// </summary>
    Task<ActionItemsSummaryDto> GetActionItemsSummaryAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets tasks by assignee report.
    /// </summary>
    Task<IReadOnlyList<TasksByAssigneeDto>> GetTasksByAssigneeReportAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets team velocity metrics based on completed story points per sprint.
    /// </summary>
    /// <param name="sprintCount">Optional number of recent sprints to include. If null, returns all sprints.</param>
    Task<TeamVelocityDto> GetTeamVelocityAsync(int? sprintCount = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets estimation accuracy metrics comparing estimated hours to actual time spent.
    /// </summary>
    /// <param name="sprintCount">Optional number of recent sprints to include. If null, returns all sprints.</param>
    Task<EstimationAccuracyDto> GetEstimationAccuracyAsync(int? sprintCount = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets late tasks report - tasks completed after their due date.
    /// </summary>
    Task<LateTasksReportDto> GetLateTasksReportAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets capacity analysis comparing planned vs actual story points by sprint.
    /// </summary>
    /// <param name="sprintCount">Optional number of recent past sprints to include. If null, returns all sprints.</param>
    Task<CapacityAnalysisDto> GetCapacityAnalysisAsync(int? sprintCount = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports dashboard data to an Excel file.
    /// </summary>
    /// <param name="sprintCount">Optional number of recent sprints to include. If null, returns all sprints.</param>
    Task<byte[]> ExportDashboardToExcelAsync(int? sprintCount = null, CancellationToken cancellationToken = default);
}
