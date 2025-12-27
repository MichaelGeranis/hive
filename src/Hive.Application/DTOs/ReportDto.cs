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
