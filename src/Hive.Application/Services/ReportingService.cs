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
    private readonly ISprintRepository _sprintRepository;
    private readonly ISprintCapacityRepository _sprintCapacityRepository;
    private readonly IAppSettingsRepository _appSettingsRepository;

    public ReportingService(
        IDirectReportRepository directReportRepository,
        IPerformanceReviewRepository reviewRepository,
        IOneOnOneMeetingRepository meetingRepository,
        IMeetingNoteRepository noteRepository,
        ITeamTaskRepository taskRepository,
        IProjectRepository projectRepository,
        ISprintRepository sprintRepository,
        ISprintCapacityRepository sprintCapacityRepository,
        IAppSettingsRepository appSettingsRepository)
    {
        _directReportRepository = directReportRepository;
        _reviewRepository = reviewRepository;
        _meetingRepository = meetingRepository;
        _noteRepository = noteRepository;
        _taskRepository = taskRepository;
        _projectRepository = projectRepository;
        _sprintRepository = sprintRepository;
        _sprintCapacityRepository = sprintCapacityRepository;
        _appSettingsRepository = appSettingsRepository;
    }

    /// <summary>
    /// Converts story points to estimated hours using the configured mapping.
    /// </summary>
    private static decimal ConvertStoryPointsToHours(int storyPoints, Dictionary<int, decimal> mapping)
    {
        if (mapping.TryGetValue(storyPoints, out var hours))
        {
            return hours;
        }
        // Fallback: if no mapping exists, return 0
        return 0;
    }

    /// <summary>
    /// Extracts the latest sprint from a comma-separated list of sprint names.
    /// If a task has "LP_4Q25_S5,LP_4Q25_S6", returns "LP_4Q25_S6".
    /// </summary>
    private static string GetLatestSprintFromTask(string sprintValue)
    {
        if (string.IsNullOrWhiteSpace(sprintValue))
            return string.Empty;

        var sprintNames = sprintValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (sprintNames.Length == 0)
            return string.Empty;

        if (sprintNames.Length == 1)
            return sprintNames[0];

        // Parse all sprint names and find the one with highest sort order
        var sprints = sprintNames
            .Select(name =>
            {
                try
                {
                    return new Sprint(name);
                }
                catch
                {
                    return null;
                }
            })
            .Where(s => s != null)
            .ToList();

        if (sprints.Count == 0)
            return sprintNames[0]; // Fallback to first if none could be parsed

        var latestSprint = sprints.OrderByDescending(s => s!.GetSortOrder()).First();
        return latestSprint!.Name;
    }

    public async Task<DashboardOverviewDto> GetDashboardOverviewAsync(int? sprintCount = null, CancellationToken cancellationToken = default)
    {
        var teamOverview = await GetTeamOverviewAsync(cancellationToken);
        var reviewsOverview = await GetReviewsAnalyticsAsync(cancellationToken);
        var oneOnOnesOverview = await GetOneOnOnesAnalyticsAsync(cancellationToken);
        var tasksOverview = await GetTasksAnalyticsAsync(sprintCount, cancellationToken);

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

        // Simplified: Past meetings are those with date before now, upcoming are future
        var now = DateTime.UtcNow;
        var pastMeetings = meetings.Where(m => m.MeetingDate < now).ToList();
        var upcomingMeetings = meetings.Where(m => m.MeetingDate >= now).ToList();

        var totalMinutes = pastMeetings.Sum(m => m.DurationMinutes);
        var avgDuration = pastMeetings.Count > 0 ? Math.Round((double)totalMinutes / pastMeetings.Count, 1) : 0;

        var frequencyByDirectReport = await GetOneOnOneFrequencyReportAsync(cancellationToken);
        var actionItemsSummary = await GetActionItemsSummaryAsync(cancellationToken);

        return new OneOnOnesOverviewDto
        {
            TotalMeetings = meetings.Count,
            CompletedMeetings = pastMeetings.Count,
            ScheduledMeetings = upcomingMeetings.Count,
            CancelledMeetings = 0,  // No longer tracking cancellations
            RescheduledMeetings = 0,  // No longer tracking reschedules
            CompletionRate = meetings.Count > 0 ? Math.Round((double)pastMeetings.Count / meetings.Count * 100, 1) : 0,
            TotalMeetingMinutes = totalMinutes,
            AverageMeetingDuration = avgDuration,
            FrequencyByDirectReport = frequencyByDirectReport,
            ActionItemsSummary = new List<ActionItemsSummaryDto> { actionItemsSummary }
        };
    }

    public async Task<TasksOverviewDto> GetTasksAnalyticsAsync(int? sprintCount = null, CancellationToken cancellationToken = default)
    {
        var allTasks = await _taskRepository.GetAllAsync(cancellationToken);
        var projects = await _projectRepository.GetAllAsync(cancellationToken);
        var directReports = await _directReportRepository.GetAllAsync(cancellationToken);

        // Filter tasks by sprint count if requested
        var tasks = allTasks;
        if (sprintCount.HasValue && sprintCount.Value > 0)
        {
            var allSprints = await _sprintRepository.GetAllAsync(cancellationToken);

            // Get unique sprint names from tasks (using latest sprint per task)
            var uniqueSprintNames = allTasks
                .Where(t => !string.IsNullOrEmpty(t.Sprint))
                .Select(t => GetLatestSprintFromTask(t.Sprint!))
                .Where(s => !string.IsNullOrEmpty(s))
                .Distinct()
                .ToHashSet();

            // Get sprint entities for proper ordering and take the last N
            var sprintNames = allSprints
                .Where(s => uniqueSprintNames.Contains(s.Name))
                .OrderBy(s => s.GetSortOrder())
                .TakeLast(sprintCount.Value)
                .Select(s => s.Name)
                .ToHashSet();

            // Filter tasks to only those whose latest sprint is in the selected sprints
            if (sprintNames.Count > 0)
            {
                tasks = allTasks
                    .Where(t => !string.IsNullOrEmpty(t.Sprint) && sprintNames.Contains(GetLatestSprintFromTask(t.Sprint!)))
                    .ToList();
            }
        }

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

        // Calculate tasks by assignee using filtered tasks
        var directReportMap = directReports.ToDictionary(dr => dr.Id, dr => dr.FullName);
        var tasksByAssignee = tasks
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
                    TotalActualHours = (int)Math.Round(assigneeTasks.Where(t => t.TimeSpentMinutes.HasValue).Sum(t => t.TimeSpentMinutes!.Value) / 60.0)
                };
            })
            .OrderByDescending(a => a.TotalTasks)
            .ToList();

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

        // Average completion days calculation removed since StartedAt/CompletedAt are no longer tracked
        var avgCompletionDays = 0.0;

        var totalEstimated = tasks.Where(t => t.EstimatedHours.HasValue).Sum(t => t.EstimatedHours!.Value);
        var totalActual = (int)Math.Round(tasks.Where(t => t.TimeSpentMinutes.HasValue).Sum(t => t.TimeSpentMinutes!.Value) / 60.0);
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
            TasksCompletedThisWeek = tasks.Count(t => t.Status == TaskStatus.Done && t.UpdatedAt.HasValue && t.UpdatedAt >= weekAgo),
            TasksCompletedThisMonth = tasks.Count(t => t.Status == TaskStatus.Done && t.UpdatedAt.HasValue && t.UpdatedAt >= monthAgo)
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

        // One-on-one analytics (simplified - no status)
        var now = DateTime.UtcNow;
        var pastMeetings = meetings.Where(m => m.MeetingDate < now).OrderByDescending(m => m.MeetingDate).ToList();
        var upcomingMeetings = meetings.Where(m => m.MeetingDate >= now).OrderBy(m => m.MeetingDate).ToList();
        var lastMeeting = pastMeetings.FirstOrDefault();
        var nextMeeting = upcomingMeetings.FirstOrDefault();

        var actionItems = await _noteRepository.GetActionItemsAsync(directReportId, cancellationToken);
        var openActionItems = actionItems.Count(a => a.ActionStatus == ActionItemStatus.Open || a.ActionStatus == ActionItemStatus.InProgress);
        var overdueActionItems = actionItems.Count(a => a.IsOverdue());

        var daysSinceLastMeeting = lastMeeting != null
            ? (int)(DateTime.UtcNow - lastMeeting.MeetingDate).TotalDays
            : -1;

        var avgFrequency = CalculateAverageFrequency(pastMeetings);

        var oneOnOneAnalytics = new OneOnOneAnalyticsDto
        {
            TotalMeetings = meetings.Count,
            CompletedMeetings = pastMeetings.Count,
            LastMeetingDate = lastMeeting?.MeetingDate,
            NextScheduledDate = nextMeeting?.MeetingDate,
            DaysSinceLastMeeting = daysSinceLastMeeting,
            AverageMeetingFrequencyDays = avgFrequency,
            TotalMeetingMinutes = pastMeetings.Sum(m => m.DurationMinutes),
            OpenActionItems = openActionItems,
            OverdueActionItems = overdueActionItems
        };

        // Task analytics
        var completedTasks = tasks.Where(t => t.Status == TaskStatus.Done).ToList();
        var inProgressTasks = tasks.Count(t => t.Status == TaskStatus.InProgress);
        var overdueTasks = tasks.Count(t => t.IsOverdue());

        // Average task completion days removed since StartedAt/CompletedAt are no longer tracked
        var avgTaskDays = 0.0;

        var taskAnalytics = new TaskAnalyticsDto
        {
            TotalTasks = tasks.Count,
            CompletedTasks = completedTasks.Count,
            InProgressTasks = inProgressTasks,
            OverdueTasks = overdueTasks,
            CompletionRate = tasks.Count > 0 ? Math.Round((double)completedTasks.Count / tasks.Count * 100, 1) : 0,
            TotalEstimatedHours = tasks.Where(t => t.EstimatedHours.HasValue).Sum(t => t.EstimatedHours!.Value),
            TotalActualHours = (int)Math.Round(tasks.Where(t => t.TimeSpentMinutes.HasValue).Sum(t => t.TimeSpentMinutes!.Value) / 60.0),
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
            var now = DateTime.UtcNow;
            var pastMeetings = meetings.Where(m => m.MeetingDate < now).OrderByDescending(m => m.MeetingDate).ToList();
            var upcomingMeetings = meetings.Where(m => m.MeetingDate >= now).OrderBy(m => m.MeetingDate).ToList();

            var lastMeeting = pastMeetings.FirstOrDefault();
            var nextMeeting = upcomingMeetings.FirstOrDefault();

            var daysSinceLastMeeting = lastMeeting != null
                ? (int)(DateTime.UtcNow - lastMeeting.MeetingDate).TotalDays
                : -1;

            var avgFrequency = CalculateAverageFrequency(pastMeetings);

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
                CompletedMeetings = pastMeetings.Count,
                LastMeetingDate = lastMeeting?.MeetingDate,
                NextScheduledDate = nextMeeting?.MeetingDate,
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
                    TotalActualHours = (int)Math.Round(assigneeTasks.Where(t => t.TimeSpentMinutes.HasValue).Sum(t => t.TimeSpentMinutes!.Value) / 60.0)
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

    private static double CalculateAverageFrequency(List<OneOnOneMeeting> pastMeetings)
    {
        if (pastMeetings.Count < 2)
            return 0;

        var orderedMeetings = pastMeetings
            .OrderBy(m => m.MeetingDate)
            .ToList();

        if (orderedMeetings.Count < 2)
            return 0;

        var intervals = new List<double>();
        for (int i = 1; i < orderedMeetings.Count; i++)
        {
            var days = (orderedMeetings[i].MeetingDate - orderedMeetings[i - 1].MeetingDate).TotalDays;
            intervals.Add(days);
        }

        return Math.Round(intervals.Average(), 1);
    }

    public async Task<TeamVelocityDto> GetTeamVelocityAsync(int? sprintCount = null, CancellationToken cancellationToken = default)
    {
        var tasks = await _taskRepository.GetAllAsync(cancellationToken);
        var allSprints = await _sprintRepository.GetAllAsync(cancellationToken);
        var appSettings = await _appSettingsRepository.GetAsync(cancellationToken);

        // Parse story points to hours mapping
        var storyPointsMapping = new Dictionary<int, decimal>();
        if (appSettings != null && !string.IsNullOrEmpty(appSettings.StoryPointMappings))
        {
            try
            {
                var mappings = System.Text.Json.JsonSerializer.Deserialize<List<StoryPointMapping>>(appSettings.StoryPointMappings);
                if (mappings != null)
                {
                    storyPointsMapping = mappings.ToDictionary(m => m.Points, m => m.Hours);
                }
            }
            catch
            {
                // If deserialization fails, continue with empty mapping
            }
        }

        var completedTasks = tasks
            .Where(t => t.Status == TaskStatus.Done && t.StoryPoints.HasValue && !string.IsNullOrEmpty(t.Sprint))
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

        // Group completed tasks by their latest sprint
        var tasksByLatestSprint = completedTasks
            .Select(t => new { Task = t, LatestSprint = GetLatestSprintFromTask(t.Sprint!) })
            .Where(x => !string.IsNullOrEmpty(x.LatestSprint))
            .GroupBy(x => x.LatestSprint)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Task).ToList());

        // Get sprint entities for the sprints that have completed tasks
        var sprintEntities = allSprints
            .Where(s => tasksByLatestSprint.ContainsKey(s.Name))
            .OrderBy(s => s.GetSortOrder())
            .ToList();

        var sprints = sprintEntities
            .Select(sprint =>
            {
                var sprintTasks = tasksByLatestSprint[sprint.Name];

                // Calculate estimated hours from story points using the mapping
                var estimatedHours = sprintTasks
                    .Where(t => t.StoryPoints.HasValue)
                    .Sum(t => ConvertStoryPointsToHours(t.StoryPoints!.Value, storyPointsMapping));

                return new SprintVelocityDto
                {
                    SprintName = sprint.Name,
                    StartDate = null, // No longer tracked since CompletedAt removed
                    EndDate = null, // No longer tracked since CompletedAt removed
                    StoryPointsCompleted = sprintTasks.Sum(t => t.StoryPoints!.Value),
                    TasksCompleted = sprintTasks.Count,
                    TotalTimeSpentMinutes = sprintTasks.Sum(t => t.TimeSpentMinutes ?? 0),
                    TotalEstimatedHours = (int)estimatedHours
                };
            })
            .ToList();

        // Filter by sprint count if requested
        if (sprintCount.HasValue && sprintCount.Value > 0)
        {
            sprints = sprints.TakeLast(sprintCount.Value).ToList();
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

    public async Task<EstimationAccuracyDto> GetEstimationAccuracyAsync(int? sprintCount = null, CancellationToken cancellationToken = default)
    {
        var tasks = await _taskRepository.GetAllAsync(cancellationToken);
        var directReports = await _directReportRepository.GetAllAsync(cancellationToken);
        var projects = await _projectRepository.GetAllAsync(cancellationToken);

        // Only consider completed tasks with both estimated hours and time spent
        var completedTasks = tasks
            .Where(t => t.Status == TaskStatus.Done && t.EstimatedHours.HasValue && t.TimeSpentMinutes.HasValue)
            .ToList();

        if (completedTasks.Count == 0)
        {
            return new EstimationAccuracyDto
            {
                Sprints = [],
                ByAssignee = [],
                ByProject = [],
                OverallAccuracyPercentage = 0,
                TotalEstimatedHours = 0,
                TotalActualHours = 0,
                TotalVarianceHours = 0
            };
        }

        var directReportMap = directReports.ToDictionary(dr => dr.Id, dr => dr.FullName);
        var projectMap = projects.ToDictionary(p => p.Id, p => p.Name);
        var allSprints = await _sprintRepository.GetAllAsync(cancellationToken);

        // Calculate by sprint, grouping by latest sprint per task
        var tasksByLatestSprint = completedTasks
            .Where(t => !string.IsNullOrEmpty(t.Sprint))
            .Select(t => new { Task = t, LatestSprint = GetLatestSprintFromTask(t.Sprint!) })
            .Where(x => !string.IsNullOrEmpty(x.LatestSprint))
            .GroupBy(x => x.LatestSprint)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Task).ToList());

        // Order sprints by GetSortOrder() for proper chronological ordering
        var sprintGroups = allSprints
            .Where(s => tasksByLatestSprint.ContainsKey(s.Name))
            .OrderBy(s => s.GetSortOrder())
            .Select(sprint =>
            {
                var tasks = tasksByLatestSprint[sprint.Name];
                var estimated = tasks.Sum(t => t.EstimatedHours!.Value);
                var actual = (int)Math.Round(tasks.Sum(t => t.TimeSpentMinutes!.Value) / 60.0);
                var variance = actual - estimated;
                var accuracy = estimated > 0 ? Math.Max(0, Math.Round(100 - Math.Abs(variance * 100.0 / estimated), 1)) : 0;

                return new SprintAccuracyDto
                {
                    SprintName = sprint.Name,
                    TasksCompleted = tasks.Count,
                    StoryPointsCompleted = tasks.Where(t => t.StoryPoints.HasValue).Sum(t => t.StoryPoints!.Value),
                    EstimatedHours = estimated,
                    ActualHours = actual,
                    VarianceHours = variance,
                    AccuracyPercentage = accuracy
                };
            })
            .ToList();

        // Filter by sprint count if requested
        if (sprintCount.HasValue && sprintCount.Value > 0)
        {
            sprintGroups = sprintGroups.TakeLast(sprintCount.Value).ToList();
        }

        // Get the filtered sprint names for filtering tasks
        var filteredSprintNames = sprintGroups.Select(s => s.SprintName).ToHashSet();

        // Filter completedTasks to only include tasks from the filtered sprints (using latest sprint logic)
        var filteredCompletedTasks = completedTasks
            .Where(t => !string.IsNullOrEmpty(t.Sprint) && filteredSprintNames.Contains(GetLatestSprintFromTask(t.Sprint!)))
            .ToList();

        // If no tasks remain after filtering, use original completedTasks for assignee/project calculations
        if (filteredCompletedTasks.Count == 0)
        {
            filteredCompletedTasks = completedTasks;
        }

        // Calculate by assignee using filtered tasks
        var byAssignee = filteredCompletedTasks
            .GroupBy(t => t.AssigneeId)
            .Select(g =>
            {
                var estimated = g.Sum(t => t.EstimatedHours!.Value);
                var actual = (int)Math.Round(g.Sum(t => t.TimeSpentMinutes!.Value) / 60.0);
                var variance = actual - estimated;
                var accuracy = estimated > 0 ? Math.Max(0, Math.Round(100 - Math.Abs(variance * 100.0 / estimated), 1)) : 0;

                return new AssigneeAccuracyDto
                {
                    AssigneeId = g.Key,
                    AssigneeName = g.Key.HasValue && directReportMap.TryGetValue(g.Key.Value, out var name)
                        ? name
                        : (g.Key.HasValue ? "Unknown" : "Unassigned"),
                    TasksCompleted = g.Count(),
                    EstimatedHours = estimated,
                    ActualHours = actual,
                    VarianceHours = variance,
                    AccuracyPercentage = accuracy
                };
            })
            .OrderByDescending(a => a.TasksCompleted)
            .ToList();

        // Calculate by project using filtered tasks
        var byProject = filteredCompletedTasks
            .Where(t => t.ProjectId.HasValue)
            .GroupBy(t => t.ProjectId)
            .Select(g =>
            {
                var estimated = g.Sum(t => t.EstimatedHours!.Value);
                var actual = (int)Math.Round(g.Sum(t => t.TimeSpentMinutes!.Value) / 60.0);
                var variance = actual - estimated;
                var accuracy = estimated > 0 ? Math.Max(0, Math.Round(100 - Math.Abs(variance * 100.0 / estimated), 1)) : 0;

                return new ProjectAccuracyDto
                {
                    ProjectId = g.Key,
                    ProjectName = g.Key.HasValue && projectMap.TryGetValue(g.Key.Value, out var name)
                        ? name
                        : "Unknown",
                    TasksCompleted = g.Count(),
                    EstimatedHours = estimated,
                    ActualHours = actual,
                    VarianceHours = variance,
                    AccuracyPercentage = accuracy
                };
            })
            .OrderByDescending(p => p.TasksCompleted)
            .ToList();

        // Calculate overall metrics from filtered tasks
        var totalEstimated = filteredCompletedTasks.Sum(t => t.EstimatedHours!.Value);
        var totalActual = (int)Math.Round(filteredCompletedTasks.Sum(t => t.TimeSpentMinutes!.Value) / 60.0);
        var totalVariance = totalActual - totalEstimated;
        var overallAccuracy = totalEstimated > 0
            ? Math.Max(0, Math.Round(100 - Math.Abs(totalVariance * 100.0 / totalEstimated), 1))
            : 0;

        return new EstimationAccuracyDto
        {
            Sprints = sprintGroups,
            ByAssignee = byAssignee,
            ByProject = byProject,
            OverallAccuracyPercentage = overallAccuracy,
            TotalEstimatedHours = totalEstimated,
            TotalActualHours = totalActual,
            TotalVarianceHours = totalVariance
        };
    }

    public async Task<LateTasksReportDto> GetLateTasksReportAsync(CancellationToken cancellationToken = default)
    {
        var tasks = await _taskRepository.GetAllAsync(cancellationToken);
        var directReports = await _directReportRepository.GetAllAsync(cancellationToken);
        var projects = await _projectRepository.GetAllAsync(cancellationToken);

        var directReportMap = directReports.ToDictionary(dr => dr.Id, dr => dr.FullName);
        var projectMap = projects.ToDictionary(p => p.Id, p => p.Name);

        // Find tasks where UpdatedAt > DueDate (using UpdatedAt as proxy for completion date)
        var lateTasks = tasks
            .Where(t => t.Status == TaskStatus.Done
                && t.UpdatedAt.HasValue
                && t.DueDate.HasValue
                && t.UpdatedAt.Value > t.DueDate.Value)
            .Select(t =>
            {
                var daysLate = (int)(t.UpdatedAt!.Value.Date - t.DueDate!.Value.Date).TotalDays;
                return new LateTaskDto
                {
                    TaskId = t.Id,
                    Title = t.Title,
                    Sprint = t.Sprint,
                    AssigneeId = t.AssigneeId,
                    AssigneeName = t.AssigneeId.HasValue && directReportMap.TryGetValue(t.AssigneeId.Value, out var name)
                        ? name
                        : null,
                    ProjectId = t.ProjectId,
                    ProjectName = t.ProjectId.HasValue && projectMap.TryGetValue(t.ProjectId.Value, out var projectName)
                        ? projectName
                        : null,
                    DueDate = t.DueDate.Value,
                    UpdatedAt = t.UpdatedAt.Value,
                    DaysLate = daysLate
                };
            })
            .OrderByDescending(t => t.DaysLate)
            .ToList();

        // Group by sprint
        var bySprint = lateTasks
            .Where(t => !string.IsNullOrEmpty(t.Sprint))
            .GroupBy(t => t.Sprint)
            .Select(g => new LateTasksBySprintDto
            {
                Sprint = g.Key,
                LateTasksCount = g.Count(),
                TotalDaysLate = g.Sum(t => t.DaysLate),
                AverageDaysLate = Math.Round(g.Average(t => t.DaysLate), 1)
            })
            .OrderByDescending(s => s.LateTasksCount)
            .ToList();

        // Group by assignee
        var byAssignee = lateTasks
            .GroupBy(t => t.AssigneeId)
            .Select(g => new LateTasksByAssigneeDto
            {
                AssigneeId = g.Key,
                AssigneeName = g.First().AssigneeName ?? "Unassigned",
                LateTasksCount = g.Count(),
                TotalDaysLate = g.Sum(t => t.DaysLate),
                AverageDaysLate = Math.Round(g.Average(t => t.DaysLate), 1)
            })
            .OrderByDescending(a => a.LateTasksCount)
            .ToList();

        return new LateTasksReportDto
        {
            TotalLateTasks = lateTasks.Count,
            LateTasks = lateTasks,
            BySprint = bySprint,
            ByAssignee = byAssignee
        };
    }

    public async Task<CapacityAnalysisDto> GetCapacityAnalysisAsync(int? sprintCount = null, CancellationToken cancellationToken = default)
    {
        var sprints = await _sprintRepository.GetAllAsync(cancellationToken);
        var sprintCapacities = await _sprintCapacityRepository.GetAllAsync(cancellationToken);
        var tasks = await _taskRepository.GetAllAsync(cancellationToken);

        if (sprints.Count == 0)
        {
            return new CapacityAnalysisDto
            {
                PastSprints = [],
                CurrentSprint = null,
                FutureSprints = [],
                AverageUtilization = 0,
                TotalCommittedPoints = 0,
                TotalCompletedPoints = 0
            };
        }

        var capacityMap = sprintCapacities.ToDictionary(c => c.SprintId);

        // Group tasks by sprint name, extracting the latest sprint if multiple are assigned
        var tasksBySprint = tasks
            .Where(t => !string.IsNullOrEmpty(t.Sprint))
            .Select(t => new { Task = t, LatestSprint = GetLatestSprintFromTask(t.Sprint!) })
            .Where(x => !string.IsNullOrEmpty(x.LatestSprint))
            .GroupBy(x => x.LatestSprint)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Task).ToList());

        // Determine current sprint based on highest sort order from all sprints
        var currentSprintEntity = sprints
            .OrderByDescending(s => s.GetSortOrder())
            .FirstOrDefault();

        var currentSortOrder = currentSprintEntity?.GetSortOrder() ?? 0;

        // Filter sprints to show: if sprintCount specified, take last N sprints (including current)
        var sprintsToShow = sprints;
        if (sprintCount.HasValue && sprintCount.Value > 0)
        {
            // Get current and past sprints only (exclude future)
            var currentAndPastSprints = sprints
                .Where(s => s.GetSortOrder() <= currentSortOrder)
                .OrderByDescending(s => s.GetSortOrder())
                .Take(sprintCount.Value)
                .ToList();

            sprintsToShow = currentAndPastSprints;
        }

        var pastSprints = new List<SprintCapacityAnalysisDto>();
        var futureSprints = new List<SprintCapacityAnalysisDto>();
        SprintCapacityAnalysisDto? currentSprint = null;

        foreach (var sprint in sprintsToShow.OrderByDescending(s => s.GetSortOrder()))
        {
            var sprintTasks = tasksBySprint.TryGetValue(sprint.Name, out var st) ? st : new List<TeamTask>();
            capacityMap.TryGetValue(sprint.Id, out var capacity);

            var completedPoints = sprintTasks
                .Where(t => t.Status == TaskStatus.Done && t.StoryPoints.HasValue)
                .Sum(t => t.StoryPoints!.Value);

            var committedPoints = capacity?.TotalCapacityPoints ?? 0;
            var utilization = committedPoints > 0
                ? Math.Round((double)completedPoints / committedPoints * 100, 1)
                : 0;

            string status;
            if (currentSprintEntity != null && sprint.Id == currentSprintEntity.Id)
            {
                status = "Current";
            }
            else if (sprint.GetSortOrder() < currentSortOrder)
            {
                status = "Past";
            }
            else
            {
                status = "Future";
            }

            var analysis = new SprintCapacityAnalysisDto
            {
                SprintId = sprint.Id,
                SprintName = sprint.Name,
                Year = sprint.Year,
                Quarter = sprint.Quarter,
                SprintNumber = sprint.SprintNumber,
                CommittedPoints = committedPoints,
                CompletedPoints = completedPoints,
                UtilizationPercentage = utilization,
                Status = status
            };

            if (status == "Current")
            {
                currentSprint = analysis;
            }
            else if (status == "Past")
            {
                pastSprints.Add(analysis);
            }
            else
            {
                futureSprints.Add(analysis);
            }
        }

        // Calculate average utilization from past sprints (current sprint may still be in progress)
        var totalCommitted = pastSprints.Sum(s => s.CommittedPoints);
        var totalCompleted = pastSprints.Sum(s => s.CompletedPoints);
        var averageUtilization = pastSprints.Count > 0
            ? Math.Round(pastSprints.Average(s => s.UtilizationPercentage), 1)
            : 0;

        return new CapacityAnalysisDto
        {
            PastSprints = pastSprints.OrderBy(s => s.Year).ThenBy(s => s.Quarter).ThenBy(s => s.SprintNumber).ToList(),
            CurrentSprint = currentSprint,
            FutureSprints = futureSprints.OrderBy(s => s.Year).ThenBy(s => s.Quarter).ThenBy(s => s.SprintNumber).ToList(),
            AverageUtilization = averageUtilization,
            TotalCommittedPoints = totalCommitted,
            TotalCompletedPoints = totalCompleted
        };
    }
}
