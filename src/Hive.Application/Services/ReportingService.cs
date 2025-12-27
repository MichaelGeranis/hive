using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Entities;
using Hive.Core.Interfaces;
using TaskStatus = Hive.Core.Entities.TaskStatus;

namespace Hive.Application.Services;

/// <summary>
/// Service for generating analytics and reports.
/// </summary>
public class ReportingService : IReportingService
{
    private readonly IDirectReportRepository _directReportRepository;
    private readonly IPerformanceReviewRepository _reviewRepository;
    private readonly IOneOnOneMeetingRepository _meetingRepository;
    private readonly IMeetingNoteRepository _noteRepository;
    private readonly ITeamTaskRepository _taskRepository;
    private readonly IProjectRepository _projectRepository;

    public ReportingService(
        IDirectReportRepository directReportRepository,
        IPerformanceReviewRepository reviewRepository,
        IOneOnOneMeetingRepository meetingRepository,
        IMeetingNoteRepository noteRepository,
        ITeamTaskRepository taskRepository,
        IProjectRepository projectRepository)
    {
        _directReportRepository = directReportRepository;
        _reviewRepository = reviewRepository;
        _meetingRepository = meetingRepository;
        _noteRepository = noteRepository;
        _taskRepository = taskRepository;
        _projectRepository = projectRepository;
    }

    public async Task<DashboardOverviewDto> GetDashboardOverviewAsync(CancellationToken cancellationToken = default)
    {
        var teamOverview = await GetTeamOverviewAsync(cancellationToken);
        var reviewsOverview = await GetReviewsAnalyticsAsync(cancellationToken);
        var oneOnOnesOverview = await GetOneOnOnesAnalyticsAsync(cancellationToken);
        var tasksOverview = await GetTasksAnalyticsAsync(cancellationToken);

        return new DashboardOverviewDto
        {
            Team = teamOverview,
            Reviews = reviewsOverview,
            OneOnOnes = oneOnOnesOverview,
            Tasks = tasksOverview,
            GeneratedAt = DateTime.UtcNow
        };
    }

    private async Task<TeamOverviewDto> GetTeamOverviewAsync(CancellationToken cancellationToken)
    {
        var directReports = await _directReportRepository.GetAllAsync(cancellationToken);

        return new TeamOverviewDto
        {
            TotalDirectReports = directReports.Count,
            DirectReports = directReports.Select(dr => new DirectReportSummaryDto
            {
                Id = dr.Id,
                FullName = dr.FullName,
                JobTitle = dr.JobTitle,
                Department = dr.Department,
                HireDate = dr.HireDate,
                TenureMonths = CalculateTenureMonths(dr.HireDate)
            }).ToList()
        };
    }

    public async Task<ReviewsOverviewDto> GetReviewsAnalyticsAsync(CancellationToken cancellationToken = default)
    {
        var reviews = await _reviewRepository.GetAllAsync(cancellationToken);

        var draftCount = reviews.Count(r => r.Status == ReviewStatus.Draft);
        var submittedCount = reviews.Count(r => r.Status == ReviewStatus.Submitted);
        var acknowledgedCount = reviews.Count(r => r.Status == ReviewStatus.Acknowledged);
        var completedCount = reviews.Count(r => r.Status == ReviewStatus.Completed);

        var completedReviews = reviews.Where(r => r.Status == ReviewStatus.Completed).ToList();
        var ratedReviews = completedReviews.Where(r => r.Rating != PerformanceRating.NotRated).ToList();

        var ratingDistribution = Enum.GetValues<PerformanceRating>()
            .Where(r => r != PerformanceRating.NotRated)
            .Select(rating =>
            {
                var count = ratedReviews.Count(r => r.Rating == rating);
                return new RatingDistributionDto
                {
                    Rating = rating,
                    RatingName = rating.ToString(),
                    Count = count,
                    Percentage = ratedReviews.Count > 0 ? Math.Round((double)count / ratedReviews.Count * 100, 1) : 0
                };
            }).ToList();

        var reviewsByPeriod = reviews
            .GroupBy(r => r.ReviewPeriod)
            .Select(g => new ReviewByPeriodDto
            {
                Period = g.Key,
                TotalReviews = g.Count(),
                CompletedReviews = g.Count(r => r.Status == ReviewStatus.Completed),
                AverageRating = g.Where(r => r.Rating != PerformanceRating.NotRated)
                    .Select(r => (int)r.Rating)
                    .DefaultIfEmpty(0)
                    .Average()
            })
            .OrderByDescending(r => r.Period)
            .ToList();

        return new ReviewsOverviewDto
        {
            TotalReviews = reviews.Count,
            DraftReviews = draftCount,
            SubmittedReviews = submittedCount,
            AcknowledgedReviews = acknowledgedCount,
            CompletedReviews = completedCount,
            CompletionRate = reviews.Count > 0 ? Math.Round((double)completedCount / reviews.Count * 100, 1) : 0,
            RatingDistribution = ratingDistribution,
            ReviewsByPeriod = reviewsByPeriod
        };
    }

    public async Task<OneOnOnesOverviewDto> GetOneOnOnesAnalyticsAsync(CancellationToken cancellationToken = default)
    {
        var meetings = await _meetingRepository.GetAllAsync(cancellationToken);
        var directReports = await _directReportRepository.GetAllAsync(cancellationToken);

        var completedMeetings = meetings.Where(m => m.Status == MeetingStatus.Completed).ToList();
        var scheduledMeetings = meetings.Where(m => m.Status == MeetingStatus.Scheduled).ToList();
        var cancelledMeetings = meetings.Where(m => m.Status == MeetingStatus.Cancelled).ToList();
        var rescheduledMeetings = meetings.Where(m => m.Status == MeetingStatus.Rescheduled).ToList();

        var totalMinutes = completedMeetings.Sum(m => m.DurationMinutes);
        var avgDuration = completedMeetings.Count > 0 ? Math.Round((double)totalMinutes / completedMeetings.Count, 1) : 0;

        var frequencyByDirectReport = await GetOneOnOneFrequencyReportAsync(cancellationToken);
        var actionItemsSummary = await GetActionItemsSummaryAsync(cancellationToken);

        return new OneOnOnesOverviewDto
        {
            TotalMeetings = meetings.Count,
            CompletedMeetings = completedMeetings.Count,
            ScheduledMeetings = scheduledMeetings.Count,
            CancelledMeetings = cancelledMeetings.Count,
            RescheduledMeetings = rescheduledMeetings.Count,
            CompletionRate = meetings.Count > 0 ? Math.Round((double)completedMeetings.Count / meetings.Count * 100, 1) : 0,
            TotalMeetingMinutes = totalMinutes,
            AverageMeetingDuration = avgDuration,
            FrequencyByDirectReport = frequencyByDirectReport,
            ActionItemsSummary = new List<ActionItemsSummaryDto> { actionItemsSummary }
        };
    }

    public async Task<TasksOverviewDto> GetTasksAnalyticsAsync(CancellationToken cancellationToken = default)
    {
        var tasks = await _taskRepository.GetAllAsync(cancellationToken);
        var projects = await _projectRepository.GetAllAsync(cancellationToken);
        var directReports = await _directReportRepository.GetAllAsync(cancellationToken);

        var projectsSummary = new ProjectsSummaryDto
        {
            TotalProjects = projects.Count,
            PlanningProjects = projects.Count(p => p.Status == ProjectStatus.Planning),
            ActiveProjects = projects.Count(p => p.Status == ProjectStatus.Active),
            OnHoldProjects = projects.Count(p => p.Status == ProjectStatus.OnHold),
            CompletedProjects = projects.Count(p => p.Status == ProjectStatus.Completed),
            CancelledProjects = projects.Count(p => p.Status == ProjectStatus.Cancelled),
            CompletionRate = projects.Count > 0
                ? Math.Round((double)projects.Count(p => p.Status == ProjectStatus.Completed) / projects.Count * 100, 1)
                : 0
        };

        var doneTasks = tasks.Count(t => t.Status == TaskStatus.Done);
        var tasksSummary = new TasksSummaryDto
        {
            TotalTasks = tasks.Count,
            BacklogTasks = tasks.Count(t => t.Status == TaskStatus.Backlog),
            TodoTasks = tasks.Count(t => t.Status == TaskStatus.Todo),
            InProgressTasks = tasks.Count(t => t.Status == TaskStatus.InProgress),
            InReviewTasks = tasks.Count(t => t.Status == TaskStatus.InReview),
            DoneTasks = doneTasks,
            CancelledTasks = tasks.Count(t => t.Status == TaskStatus.Cancelled),
            OverdueTasks = tasks.Count(t => t.IsOverdue()),
            UnassignedTasks = tasks.Count(t => !t.AssigneeId.HasValue),
            CompletionRate = tasks.Count > 0 ? Math.Round((double)doneTasks / tasks.Count * 100, 1) : 0
        };

        var tasksByAssignee = await GetTasksByAssigneeReportAsync(cancellationToken);

        var tasksByType = Enum.GetValues<TaskType>()
            .Select(type =>
            {
                var typeTasks = tasks.Where(t => t.Type == type).ToList();
                var completed = typeTasks.Count(t => t.Status == TaskStatus.Done);
                return new TasksByTypeDto
                {
                    Type = type,
                    TypeName = type.ToString(),
                    TotalTasks = typeTasks.Count,
                    CompletedTasks = completed,
                    CompletionRate = typeTasks.Count > 0 ? Math.Round((double)completed / typeTasks.Count * 100, 1) : 0
                };
            })
            .Where(t => t.TotalTasks > 0)
            .ToList();

        var tasksByPriority = Enum.GetValues<TaskPriority>()
            .Select(priority =>
            {
                var priorityTasks = tasks.Where(t => t.Priority == priority).ToList();
                var completed = priorityTasks.Count(t => t.Status == TaskStatus.Done);
                return new TasksByPriorityDto
                {
                    Priority = priority,
                    PriorityName = priority.ToString(),
                    TotalTasks = priorityTasks.Count,
                    CompletedTasks = completed,
                    OverdueTasks = priorityTasks.Count(t => t.IsOverdue()),
                    CompletionRate = priorityTasks.Count > 0 ? Math.Round((double)completed / priorityTasks.Count * 100, 1) : 0
                };
            })
            .Where(t => t.TotalTasks > 0)
            .ToList();

        var completedTasks = tasks.Where(t => t.Status == TaskStatus.Done && t.CompletedAt.HasValue && t.StartedAt.HasValue).ToList();
        var avgCompletionDays = completedTasks.Count > 0
            ? Math.Round(completedTasks.Average(t => (t.CompletedAt!.Value - t.StartedAt!.Value).TotalDays), 1)
            : 0;

        var totalEstimated = tasks.Where(t => t.EstimatedHours.HasValue).Sum(t => t.EstimatedHours!.Value);
        var totalActual = tasks.Where(t => t.ActualHours.HasValue).Sum(t => t.ActualHours!.Value);
        var estimationAccuracy = totalEstimated > 0
            ? Math.Round((1 - Math.Abs(totalActual - totalEstimated) / (double)totalEstimated) * 100, 1)
            : 0;

        var now = DateTime.UtcNow;
        var weekAgo = now.AddDays(-7);
        var monthAgo = now.AddDays(-30);

        var productivity = new ProductivityMetricsDto
        {
            TotalEstimatedHours = totalEstimated,
            TotalActualHours = totalActual,
            EstimationAccuracy = Math.Max(0, estimationAccuracy),
            AverageTaskCompletionDays = avgCompletionDays,
            TasksCompletedThisWeek = tasks.Count(t => t.Status == TaskStatus.Done && t.CompletedAt >= weekAgo),
            TasksCompletedThisMonth = tasks.Count(t => t.Status == TaskStatus.Done && t.CompletedAt >= monthAgo)
        };

        return new TasksOverviewDto
        {
            Projects = projectsSummary,
            Tasks = tasksSummary,
            TasksByAssignee = tasksByAssignee,
            TasksByType = tasksByType,
            TasksByPriority = tasksByPriority,
            Productivity = productivity
        };
    }

    public async Task<DirectReportAnalyticsDto?> GetDirectReportAnalyticsAsync(Guid directReportId, CancellationToken cancellationToken = default)
    {
        var directReport = await _directReportRepository.GetByIdAsync(directReportId, cancellationToken);
        if (directReport == null)
            return null;

        var reviews = await _reviewRepository.GetByDirectReportIdAsync(directReportId, cancellationToken);
        var meetings = await _meetingRepository.GetByDirectReportIdAsync(directReportId, cancellationToken);
        var tasks = await _taskRepository.GetByAssigneeIdAsync(directReportId, cancellationToken);

        // Reviews analytics
        var completedReviews = reviews.Where(r => r.Status == ReviewStatus.Completed).ToList();
        var latestReview = completedReviews.OrderByDescending(r => r.ReviewDate).FirstOrDefault();
        var ratedReviews = completedReviews.Where(r => r.Rating != PerformanceRating.NotRated).ToList();
        var avgRating = ratedReviews.Count > 0 ? Math.Round(ratedReviews.Average(r => (int)r.Rating), 1) : 0;

        var reviewsAnalytics = new ReviewsAnalyticsDto
        {
            TotalReviews = reviews.Count,
            CompletedReviews = completedReviews.Count,
            LatestRating = latestReview?.Rating,
            LatestRatingName = latestReview?.Rating.ToString(),
            AverageRating = avgRating,
            ReviewHistory = reviews.OrderByDescending(r => r.ReviewDate).Select(r => new ReviewHistoryDto
            {
                Period = r.ReviewPeriod,
                Rating = r.Rating,
                RatingName = r.Rating.ToString(),
                ReviewDate = r.ReviewDate,
                Status = r.Status,
                StatusName = r.Status.ToString()
            }).ToList()
        };

        // One-on-one analytics
        var completedMeetings = meetings.Where(m => m.Status == MeetingStatus.Completed).OrderByDescending(m => m.CompletedAt).ToList();
        var scheduledMeetings = meetings.Where(m => m.Status == MeetingStatus.Scheduled).OrderBy(m => m.ScheduledDate).ToList();
        var lastMeeting = completedMeetings.FirstOrDefault();
        var nextMeeting = scheduledMeetings.FirstOrDefault(m => m.ScheduledDate > DateTime.UtcNow);

        var actionItems = await _noteRepository.GetActionItemsAsync(directReportId, cancellationToken);
        var openActionItems = actionItems.Count(a => a.ActionStatus == ActionItemStatus.Open || a.ActionStatus == ActionItemStatus.InProgress);
        var overdueActionItems = actionItems.Count(a => a.IsOverdue());

        var daysSinceLastMeeting = lastMeeting?.CompletedAt != null
            ? (int)(DateTime.UtcNow - lastMeeting.CompletedAt.Value).TotalDays
            : -1;

        var avgFrequency = CalculateAverageFrequency(completedMeetings);

        var oneOnOneAnalytics = new OneOnOneAnalyticsDto
        {
            TotalMeetings = meetings.Count,
            CompletedMeetings = completedMeetings.Count,
            LastMeetingDate = lastMeeting?.CompletedAt,
            NextScheduledDate = nextMeeting?.ScheduledDate,
            DaysSinceLastMeeting = daysSinceLastMeeting,
            AverageMeetingFrequencyDays = avgFrequency,
            TotalMeetingMinutes = completedMeetings.Sum(m => m.DurationMinutes),
            OpenActionItems = openActionItems,
            OverdueActionItems = overdueActionItems
        };

        // Task analytics
        var completedTasks = tasks.Where(t => t.Status == TaskStatus.Done).ToList();
        var inProgressTasks = tasks.Count(t => t.Status == TaskStatus.InProgress);
        var overdueTasks = tasks.Count(t => t.IsOverdue());

        var tasksWithDates = completedTasks.Where(t => t.StartedAt.HasValue && t.CompletedAt.HasValue).ToList();
        var avgTaskDays = tasksWithDates.Count > 0
            ? Math.Round(tasksWithDates.Average(t => (t.CompletedAt!.Value - t.StartedAt!.Value).TotalDays), 1)
            : 0;

        var taskAnalytics = new TaskAnalyticsDto
        {
            TotalTasks = tasks.Count,
            CompletedTasks = completedTasks.Count,
            InProgressTasks = inProgressTasks,
            OverdueTasks = overdueTasks,
            CompletionRate = tasks.Count > 0 ? Math.Round((double)completedTasks.Count / tasks.Count * 100, 1) : 0,
            TotalEstimatedHours = tasks.Where(t => t.EstimatedHours.HasValue).Sum(t => t.EstimatedHours!.Value),
            TotalActualHours = tasks.Where(t => t.ActualHours.HasValue).Sum(t => t.ActualHours!.Value),
            AverageTaskCompletionDays = avgTaskDays
        };

        return new DirectReportAnalyticsDto
        {
            DirectReportId = directReport.Id,
            FullName = directReport.FullName,
            JobTitle = directReport.JobTitle,
            TenureMonths = CalculateTenureMonths(directReport.HireDate),
            Reviews = reviewsAnalytics,
            OneOnOnes = oneOnOneAnalytics,
            Tasks = taskAnalytics
        };
    }

    public async Task<IReadOnlyList<OneOnOneFrequencyDto>> GetOneOnOneFrequencyReportAsync(CancellationToken cancellationToken = default)
    {
        var directReports = await _directReportRepository.GetAllAsync(cancellationToken);
        var result = new List<OneOnOneFrequencyDto>();

        foreach (var dr in directReports)
        {
            var meetings = await _meetingRepository.GetByDirectReportIdAsync(dr.Id, cancellationToken);
            var completedMeetings = meetings.Where(m => m.Status == MeetingStatus.Completed).OrderByDescending(m => m.CompletedAt).ToList();
            var scheduledMeetings = meetings.Where(m => m.Status == MeetingStatus.Scheduled).OrderBy(m => m.ScheduledDate).ToList();

            var lastMeeting = completedMeetings.FirstOrDefault();
            var nextMeeting = scheduledMeetings.FirstOrDefault(m => m.ScheduledDate > DateTime.UtcNow);

            var daysSinceLastMeeting = lastMeeting?.CompletedAt != null
                ? (int)(DateTime.UtcNow - lastMeeting.CompletedAt.Value).TotalDays
                : -1;

            var avgFrequency = CalculateAverageFrequency(completedMeetings);

            // Determine frequency status
            string frequencyStatus;
            if (daysSinceLastMeeting < 0)
            {
                frequencyStatus = "No Meetings Yet";
            }
            else if (daysSinceLastMeeting <= 14)
            {
                frequencyStatus = "On Track";
            }
            else if (daysSinceLastMeeting <= 21)
            {
                frequencyStatus = "At Risk";
            }
            else
            {
                frequencyStatus = "Overdue";
            }

            result.Add(new OneOnOneFrequencyDto
            {
                DirectReportId = dr.Id,
                DirectReportName = dr.FullName,
                TotalMeetings = meetings.Count,
                CompletedMeetings = completedMeetings.Count,
                LastMeetingDate = lastMeeting?.CompletedAt,
                NextScheduledDate = nextMeeting?.ScheduledDate,
                DaysSinceLastMeeting = daysSinceLastMeeting,
                AverageFrequencyDays = avgFrequency,
                FrequencyStatus = frequencyStatus
            });
        }

        return result.OrderByDescending(r => r.DaysSinceLastMeeting).ToList();
    }

    public async Task<ActionItemsSummaryDto> GetActionItemsSummaryAsync(CancellationToken cancellationToken = default)
    {
        var actionItems = await _noteRepository.GetActionItemsAsync(null, cancellationToken);

        var openItems = actionItems.Count(a => a.ActionStatus == ActionItemStatus.Open);
        var inProgressItems = actionItems.Count(a => a.ActionStatus == ActionItemStatus.InProgress);
        var completedItems = actionItems.Count(a => a.ActionStatus == ActionItemStatus.Completed);
        var cancelledItems = actionItems.Count(a => a.ActionStatus == ActionItemStatus.Cancelled);
        var overdueItems = actionItems.Count(a => a.IsOverdue());

        var total = actionItems.Count;
        var completionRate = total > 0 ? Math.Round((double)completedItems / total * 100, 1) : 0;

        return new ActionItemsSummaryDto
        {
            TotalActionItems = total,
            OpenItems = openItems,
            InProgressItems = inProgressItems,
            CompletedItems = completedItems,
            CancelledItems = cancelledItems,
            OverdueItems = overdueItems,
            CompletionRate = completionRate
        };
    }

    public async Task<IReadOnlyList<TasksByAssigneeDto>> GetTasksByAssigneeReportAsync(CancellationToken cancellationToken = default)
    {
        var tasks = await _taskRepository.GetAllAsync(cancellationToken);
        var directReports = await _directReportRepository.GetAllAsync(cancellationToken);

        var directReportMap = directReports.ToDictionary(dr => dr.Id, dr => dr.FullName);

        var assignedTasks = tasks
            .GroupBy(t => t.AssigneeId)
            .Select(g =>
            {
                var assigneeTasks = g.ToList();
                var completed = assigneeTasks.Count(t => t.Status == TaskStatus.Done);
                var inProgress = assigneeTasks.Count(t => t.Status == TaskStatus.InProgress);
                var overdue = assigneeTasks.Count(t => t.IsOverdue());

                return new TasksByAssigneeDto
                {
                    AssigneeId = g.Key,
                    AssigneeName = g.Key.HasValue && directReportMap.TryGetValue(g.Key.Value, out var name)
                        ? name
                        : (g.Key.HasValue ? "Unknown" : "Unassigned"),
                    TotalTasks = assigneeTasks.Count,
                    CompletedTasks = completed,
                    InProgressTasks = inProgress,
                    OverdueTasks = overdue,
                    CompletionRate = assigneeTasks.Count > 0 ? Math.Round((double)completed / assigneeTasks.Count * 100, 1) : 0,
                    TotalEstimatedHours = assigneeTasks.Where(t => t.EstimatedHours.HasValue).Sum(t => t.EstimatedHours!.Value),
                    TotalActualHours = assigneeTasks.Where(t => t.ActualHours.HasValue).Sum(t => t.ActualHours!.Value)
                };
            })
            .OrderByDescending(a => a.TotalTasks)
            .ToList();

        return assignedTasks;
    }

    private static int CalculateTenureMonths(DateTime hireDate)
    {
        var now = DateTime.UtcNow;
        var months = (now.Year - hireDate.Year) * 12 + now.Month - hireDate.Month;
        if (now.Day < hireDate.Day)
            months--;
        return Math.Max(0, months);
    }

    private static double CalculateAverageFrequency(List<OneOnOneMeeting> completedMeetings)
    {
        if (completedMeetings.Count < 2)
            return 0;

        var orderedMeetings = completedMeetings
            .Where(m => m.CompletedAt.HasValue)
            .OrderBy(m => m.CompletedAt!.Value)
            .ToList();

        if (orderedMeetings.Count < 2)
            return 0;

        var intervals = new List<double>();
        for (int i = 1; i < orderedMeetings.Count; i++)
        {
            var days = (orderedMeetings[i].CompletedAt!.Value - orderedMeetings[i - 1].CompletedAt!.Value).TotalDays;
            intervals.Add(days);
        }

        return Math.Round(intervals.Average(), 1);
    }

    public async Task<TeamVelocityDto> GetTeamVelocityAsync(CancellationToken cancellationToken = default)
    {
        var tasks = await _taskRepository.GetAllAsync(cancellationToken);
        var completedTasks = tasks
            .Where(t => t.Status == TaskStatus.Done && t.CompletedAt.HasValue && t.StoryPoints.HasValue)
            .OrderBy(t => t.CompletedAt!.Value)
            .ToList();

        if (completedTasks.Count == 0)
        {
            return new TeamVelocityDto
            {
                Sprints = [],
                AverageVelocity = 0,
                TotalStoryPointsCompleted = 0,
                CompletionTrend = 0
            };
        }

        // Calculate sprints based on 2-week periods from the earliest completed task
        var firstCompletedDate = completedTasks.First().CompletedAt!.Value;
        var lastCompletedDate = completedTasks.Last().CompletedAt!.Value;
        
        var sprints = new List<SprintVelocityDto>();
        var currentSprintStart = firstCompletedDate.Date;
        var sprintNumber = 1;

        while (currentSprintStart <= lastCompletedDate)
        {
            var sprintEnd = currentSprintStart.AddDays(14);
            
            var sprintTasks = completedTasks
                .Where(t => t.CompletedAt!.Value >= currentSprintStart && t.CompletedAt.Value < sprintEnd)
                .ToList();

            if (sprintTasks.Any())
            {
                var sprintVelocity = new SprintVelocityDto
                {
                    SprintName = $"Sprint {sprintNumber}",
                    StartDate = currentSprintStart,
                    EndDate = sprintEnd.AddDays(-1), // End date is inclusive
                    StoryPointsCompleted = sprintTasks.Sum(t => t.StoryPoints!.Value),
                    TasksCompleted = sprintTasks.Count
                };
                sprints.Add(sprintVelocity);
            }

            currentSprintStart = sprintEnd;
            sprintNumber++;
        }

        var totalStoryPoints = sprints.Sum(s => s.StoryPointsCompleted);
        var averageVelocity = sprints.Count > 0 ? Math.Round((double)totalStoryPoints / sprints.Count, 1) : 0;

        // Calculate trend (comparing last sprint to previous sprint)
        double completionTrend = 0;
        if (sprints.Count >= 2)
        {
            var lastSprint = sprints[^1];
            var previousSprint = sprints[^2];
            
            if (previousSprint.StoryPointsCompleted > 0)
            {
                completionTrend = Math.Round(
                    ((double)(lastSprint.StoryPointsCompleted - previousSprint.StoryPointsCompleted) / previousSprint.StoryPointsCompleted) * 100,
                    1);
            }
        }

        return new TeamVelocityDto
        {
            Sprints = sprints,
            AverageVelocity = averageVelocity,
            TotalStoryPointsCompleted = totalStoryPoints,
            CompletionTrend = completionTrend
        };
    }
}
