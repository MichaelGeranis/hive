using Hive.Core.Entities;

namespace Hive.Application.DTOs;

/// <summary>
/// Dashboard overview with key metrics across all areas.
/// </summary>
public record DashboardOverviewDto
{
    public TeamOverviewDto Team { get; init; } = new();
    public ReviewsOverviewDto Reviews { get; init; } = new();
    public OneOnOnesOverviewDto OneOnOnes { get; init; } = new();
    public TasksOverviewDto Tasks { get; init; } = new();
    public DateTime GeneratedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Overview of team composition.
/// </summary>
public record TeamOverviewDto
{
    public int TotalDirectReports { get; init; }
    public IReadOnlyList<DirectReportSummaryDto> DirectReports { get; init; } = [];
}

/// <summary>
/// Summary of a direct report for dashboard display.
/// </summary>
public record DirectReportSummaryDto
{
    public Guid Id { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string JobTitle { get; init; } = string.Empty;
    public string Department { get; init; } = string.Empty;
    public DateTime HireDate { get; init; }
    public int TenureMonths { get; init; }
}

/// <summary>
/// Overview of performance reviews.
/// </summary>
public record ReviewsOverviewDto
{
    public int TotalReviews { get; init; }
    public int DraftReviews { get; init; }
    public int SubmittedReviews { get; init; }
    public int AcknowledgedReviews { get; init; }
    public int CompletedReviews { get; init; }
    public double CompletionRate { get; init; }
    public IReadOnlyList<RatingDistributionDto> RatingDistribution { get; init; } = [];
    public IReadOnlyList<ReviewByPeriodDto> ReviewsByPeriod { get; init; } = [];
}

/// <summary>
/// Distribution of performance ratings.
/// </summary>
public record RatingDistributionDto
{
    public PerformanceRating Rating { get; init; }
    public string RatingName { get; init; } = string.Empty;
    public int Count { get; init; }
    public double Percentage { get; init; }
}

/// <summary>
/// Reviews grouped by period.
/// </summary>
public record ReviewByPeriodDto
{
    public string Period { get; init; } = string.Empty;
    public int TotalReviews { get; init; }
    public int CompletedReviews { get; init; }
    public double AverageRating { get; init; }
}

/// <summary>
/// Overview of one-on-one meetings.
/// </summary>
public record OneOnOnesOverviewDto
{
    public int TotalMeetings { get; init; }
    public int CompletedMeetings { get; init; }
    public int ScheduledMeetings { get; init; }
    public int CancelledMeetings { get; init; }
    public int RescheduledMeetings { get; init; }
    public double CompletionRate { get; init; }
    public int TotalMeetingMinutes { get; init; }
    public double AverageMeetingDuration { get; init; }
    public IReadOnlyList<OneOnOneFrequencyDto> FrequencyByDirectReport { get; init; } = [];
    public IReadOnlyList<ActionItemsSummaryDto> ActionItemsSummary { get; init; } = [];
}

/// <summary>
/// One-on-one meeting frequency for a direct report.
/// </summary>
public record OneOnOneFrequencyDto
{
    public Guid DirectReportId { get; init; }
    public string DirectReportName { get; init; } = string.Empty;
    public int TotalMeetings { get; init; }
    public int CompletedMeetings { get; init; }
    public DateTime? LastMeetingDate { get; init; }
    public DateTime? NextScheduledDate { get; init; }
    public int DaysSinceLastMeeting { get; init; }
    public double AverageFrequencyDays { get; init; }
    public string FrequencyStatus { get; init; } = string.Empty; // "On Track", "Overdue", "At Risk"
}

/// <summary>
/// Summary of action items from one-on-ones.
/// </summary>
public record ActionItemsSummaryDto
{
    public int TotalActionItems { get; init; }
    public int OpenItems { get; init; }
    public int InProgressItems { get; init; }
    public int CompletedItems { get; init; }
    public int CancelledItems { get; init; }
    public int OverdueItems { get; init; }
    public double CompletionRate { get; init; }
}

/// <summary>
/// Overview of tasks and projects.
/// </summary>
public record TasksOverviewDto
{
    public ProjectsSummaryDto Projects { get; init; } = new();
    public TasksSummaryDto Tasks { get; init; } = new();
    public IReadOnlyList<TasksByAssigneeDto> TasksByAssignee { get; init; } = [];
    public IReadOnlyList<TasksByTypeDto> TasksByType { get; init; } = [];
    public IReadOnlyList<TasksByPriorityDto> TasksByPriority { get; init; } = [];
    public ProductivityMetricsDto Productivity { get; init; } = new();
}

/// <summary>
/// Summary of projects.
/// </summary>
public record ProjectsSummaryDto
{
    public int TotalProjects { get; init; }
    public int PlanningProjects { get; init; }
    public int ActiveProjects { get; init; }
    public int OnHoldProjects { get; init; }
    public int CompletedProjects { get; init; }
    public int CancelledProjects { get; init; }
    public double CompletionRate { get; init; }
}

/// <summary>
/// Summary of tasks.
/// </summary>
public record TasksSummaryDto
{
    public int TotalTasks { get; init; }
    public int BacklogTasks { get; init; }
    public int TodoTasks { get; init; }
    public int InProgressTasks { get; init; }
    public int InReviewTasks { get; init; }
    public int DoneTasks { get; init; }
    public int CancelledTasks { get; init; }
    public int OverdueTasks { get; init; }
    public int UnassignedTasks { get; init; }
    public double CompletionRate { get; init; }
}

/// <summary>
/// Tasks grouped by assignee.
/// </summary>
public record TasksByAssigneeDto
{
    public Guid? AssigneeId { get; init; }
    public string AssigneeName { get; init; } = string.Empty;
    public int TotalTasks { get; init; }
    public int CompletedTasks { get; init; }
    public int InProgressTasks { get; init; }
    public int OverdueTasks { get; init; }
    public double CompletionRate { get; init; }
    public int TotalEstimatedHours { get; init; }
    public int TotalActualHours { get; init; }
}

/// <summary>
/// Tasks grouped by type.
/// </summary>
public record TasksByTypeDto
{
    public TaskType Type { get; init; }
    public string TypeName { get; init; } = string.Empty;
    public int TotalTasks { get; init; }
    public int CompletedTasks { get; init; }
    public double CompletionRate { get; init; }
}

/// <summary>
/// Tasks grouped by priority.
/// </summary>
public record TasksByPriorityDto
{
    public TaskPriority Priority { get; init; }
    public string PriorityName { get; init; } = string.Empty;
    public int TotalTasks { get; init; }
    public int CompletedTasks { get; init; }
    public int OverdueTasks { get; init; }
    public double CompletionRate { get; init; }
}

/// <summary>
/// Productivity metrics for task completion.
/// </summary>
public record ProductivityMetricsDto
{
    public int TotalEstimatedHours { get; init; }
    public int TotalActualHours { get; init; }
    public double EstimationAccuracy { get; init; }
    public double AverageTaskCompletionDays { get; init; }
    public int TasksCompletedThisWeek { get; init; }
    public int TasksCompletedThisMonth { get; init; }
}

/// <summary>
/// Detailed report for a specific direct report.
/// </summary>
public record DirectReportAnalyticsDto
{
    public Guid DirectReportId { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string JobTitle { get; init; } = string.Empty;
    public int TenureMonths { get; init; }
    public ReviewsAnalyticsDto Reviews { get; init; } = new();
    public OneOnOneAnalyticsDto OneOnOnes { get; init; } = new();
    public TaskAnalyticsDto Tasks { get; init; } = new();
}

/// <summary>
/// Review analytics for a direct report.
/// </summary>
public record ReviewsAnalyticsDto
{
    public int TotalReviews { get; init; }
    public int CompletedReviews { get; init; }
    public PerformanceRating? LatestRating { get; init; }
    public string? LatestRatingName { get; init; }
    public double AverageRating { get; init; }
    public IReadOnlyList<ReviewHistoryDto> ReviewHistory { get; init; } = [];
}

/// <summary>
/// Historical review data.
/// </summary>
public record ReviewHistoryDto
{
    public string Period { get; init; } = string.Empty;
    public PerformanceRating Rating { get; init; }
    public string RatingName { get; init; } = string.Empty;
    public DateTime ReviewDate { get; init; }
    public ReviewStatus Status { get; init; }
    public string StatusName { get; init; } = string.Empty;
}

/// <summary>
/// One-on-one analytics for a direct report.
/// </summary>
public record OneOnOneAnalyticsDto
{
    public int TotalMeetings { get; init; }
    public int CompletedMeetings { get; init; }
    public DateTime? LastMeetingDate { get; init; }
    public DateTime? NextScheduledDate { get; init; }
    public int DaysSinceLastMeeting { get; init; }
    public double AverageMeetingFrequencyDays { get; init; }
    public int TotalMeetingMinutes { get; init; }
    public int OpenActionItems { get; init; }
    public int OverdueActionItems { get; init; }
}

/// <summary>
/// Task analytics for a direct report.
/// </summary>
public record TaskAnalyticsDto
{
    public int TotalTasks { get; init; }
    public int CompletedTasks { get; init; }
    public int InProgressTasks { get; init; }
    public int OverdueTasks { get; init; }
    public double CompletionRate { get; init; }
    public int TotalEstimatedHours { get; init; }
    public int TotalActualHours { get; init; }
    public double AverageTaskCompletionDays { get; init; }
}

/// <summary>
/// Time-based trend data for reporting.
/// </summary>
public record TrendDataDto
{
    public DateTime Date { get; init; }
    public int TasksCompleted { get; init; }
    public int MeetingsHeld { get; init; }
    public int ReviewsCompleted { get; init; }
}

/// <summary>
/// Team velocity metrics based on completed story points per sprint.
/// </summary>
public record TeamVelocityDto
{
    public IReadOnlyList<SprintVelocityDto> Sprints { get; init; } = [];
    public double AverageVelocity { get; init; }
    public int TotalStoryPointsCompleted { get; init; }
    public double CompletionTrend { get; init; } // Percentage change from previous sprint
}

/// <summary>
/// Velocity data for a single sprint.
/// </summary>
public record SprintVelocityDto
{
    public string SprintName { get; init; } = string.Empty;
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public int StoryPointsCompleted { get; init; }
    public int TasksCompleted { get; init; }
    public int TotalTimeSpentMinutes { get; init; }
    public int TotalEstimatedHours { get; init; }
}

/// <summary>
/// Estimation accuracy metrics comparing story points/estimated hours to actual time spent.
/// </summary>
public record EstimationAccuracyDto
{
    public IReadOnlyList<SprintAccuracyDto> Sprints { get; init; } = [];
    public IReadOnlyList<AssigneeAccuracyDto> ByAssignee { get; init; } = [];
    public IReadOnlyList<ProjectAccuracyDto> ByProject { get; init; } = [];
    public double OverallAccuracyPercentage { get; init; }
    public int TotalEstimatedHours { get; init; }
    public int TotalActualHours { get; init; }
    public int TotalVarianceHours { get; init; }
}

/// <summary>
/// Estimation accuracy for a single sprint.
/// </summary>
public record SprintAccuracyDto
{
    public string SprintName { get; init; } = string.Empty;
    public int TasksCompleted { get; init; }
    public int StoryPointsCompleted { get; init; }
    public int EstimatedHours { get; init; }
    public int ActualHours { get; init; }
    public int VarianceHours { get; init; }
    public double AccuracyPercentage { get; init; }
}

/// <summary>
/// Estimation accuracy by assignee.
/// </summary>
public record AssigneeAccuracyDto
{
    public Guid? AssigneeId { get; init; }
    public string AssigneeName { get; init; } = string.Empty;
    public int TasksCompleted { get; init; }
    public int EstimatedHours { get; init; }
    public int ActualHours { get; init; }
    public int VarianceHours { get; init; }
    public double AccuracyPercentage { get; init; }
}

/// <summary>
/// Estimation accuracy by project.
/// </summary>
public record ProjectAccuracyDto
{
    public Guid? ProjectId { get; init; }
    public string ProjectName { get; init; } = string.Empty;
    public int TasksCompleted { get; init; }
    public int EstimatedHours { get; init; }
    public int ActualHours { get; init; }
    public int VarianceHours { get; init; }
    public double AccuracyPercentage { get; init; }
}

/// <summary>
/// Late tasks report - tasks completed after their due date.
/// </summary>
public record LateTasksReportDto
{
    public int TotalLateTasks { get; init; }
    public IReadOnlyList<LateTaskDto> LateTasks { get; init; } = [];
    public IReadOnlyList<LateTasksBySprintDto> BySprint { get; init; } = [];
    public IReadOnlyList<LateTasksByAssigneeDto> ByAssignee { get; init; } = [];
}

/// <summary>
/// A single late task.
/// </summary>
public record LateTaskDto
{
    public Guid TaskId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Sprint { get; init; } = string.Empty;
    public Guid? AssigneeId { get; init; }
    public string? AssigneeName { get; init; }
    public Guid? ProjectId { get; init; }
    public string? ProjectName { get; init; }
    public DateTime DueDate { get; init; }
    public DateTime CompletedAt { get; init; }
    public int DaysLate { get; init; }
}

/// <summary>
/// Late tasks aggregated by sprint.
/// </summary>
public record LateTasksBySprintDto
{
    public string Sprint { get; init; } = string.Empty;
    public int LateTasksCount { get; init; }
    public int TotalDaysLate { get; init; }
    public double AverageDaysLate { get; init; }
}

/// <summary>
/// Late tasks aggregated by assignee.
/// </summary>
public record LateTasksByAssigneeDto
{
    public Guid? AssigneeId { get; init; }
    public string AssigneeName { get; init; } = string.Empty;
    public int LateTasksCount { get; init; }
    public int TotalDaysLate { get; init; }
    public double AverageDaysLate { get; init; }
}

/// <summary>
/// Capacity analysis report comparing committed vs completed story points.
/// </summary>
public record CapacityAnalysisDto
{
    public IReadOnlyList<SprintCapacityAnalysisDto> PastSprints { get; init; } = [];
    public SprintCapacityAnalysisDto? CurrentSprint { get; init; }
    public IReadOnlyList<SprintCapacityAnalysisDto> FutureSprints { get; init; } = [];
    public double AverageUtilization { get; init; }
    public int TotalCommittedPoints { get; init; }
    public int TotalCompletedPoints { get; init; }
}

/// <summary>
/// Capacity analysis for a single sprint.
/// </summary>
public record SprintCapacityAnalysisDto
{
    public Guid SprintId { get; init; }
    public string SprintName { get; init; } = string.Empty;
    public int Year { get; init; }
    public int Quarter { get; init; }
    public int SprintNumber { get; init; }
    public int CommittedPoints { get; init; }
    public int CompletedPoints { get; init; }
    public double UtilizationPercentage { get; init; }
    public string Status { get; init; } = string.Empty; // "Past", "Current", "Future"
}
