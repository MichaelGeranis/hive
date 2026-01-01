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
    Task<DashboardOverviewDto> GetDashboardOverviewAsync(CancellationToken cancellationToken = default);

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
    Task<TasksOverviewDto> GetTasksAnalyticsAsync(CancellationToken cancellationToken = default);

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
    Task<TeamVelocityDto> GetTeamVelocityAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets estimation accuracy metrics comparing estimated hours to actual time spent.
    /// </summary>
    Task<EstimationAccuracyDto> GetEstimationAccuracyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets late tasks report - tasks completed after their due date.
    /// </summary>
    Task<LateTasksReportDto> GetLateTasksReportAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets capacity analysis comparing planned vs actual story points by sprint.
    /// </summary>
    Task<CapacityAnalysisDto> GetCapacityAnalysisAsync(CancellationToken cancellationToken = default);
}
