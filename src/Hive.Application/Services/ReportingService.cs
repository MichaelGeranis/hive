using System.Security.Cryptography.X509Certificates;
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
    private readonly IParentRepository _parentRepository;
    private readonly ILeaveRepository _leaveRepository;

    public ReportingService(
        IDirectReportRepository directReportRepository,
        IPerformanceReviewRepository reviewRepository,
        IOneOnOneMeetingRepository meetingRepository,
        IMeetingNoteRepository noteRepository,
        ITeamTaskRepository taskRepository,
        IProjectRepository projectRepository,
        ISprintRepository sprintRepository,
        ISprintCapacityRepository sprintCapacityRepository,
        IAppSettingsRepository appSettingsRepository,
        IParentRepository parentRepository,
        ILeaveRepository leaveRepository)
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
        _parentRepository = parentRepository;
        _leaveRepository = leaveRepository;
    }

    /// <summary>
    /// Filters out tasks that are also parents (tasks whose title matches a parent name).
    /// These tasks should not be included in counts and estimations.
    /// </summary>
    private static List<TeamTask> ExcludeParentTasks(IReadOnlyList<TeamTask> tasks, IReadOnlyList<Parent> parents)
    {
        var parentNames = parents.Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return tasks.Where(t => !parentNames.Contains(t.Title)).ToList();
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

        var latestSprint = sprints.OrderByDescending(s => s!.GetOrderingKey()).First();
        return latestSprint!.Name;
    }

    public async Task<DashboardOverviewDto> GetDashboardOverviewAsync(int? sprintCount = null, CancellationToken cancellationToken = default)
    {
        var teamOverview = await GetTeamOverviewAsync(cancellationToken);
        var reviewsOverview = await GetReviewsAnalyticsAsync(cancellationToken);
        var oneOnOnesOverview = await GetOneOnOnesAnalyticsAsync(cancellationToken);
        var tasksOverview = await GetTasksAnalyticsAsync(sprintCount, cancellationToken);
        var insights = await GetDashboardInsightsAsync(tasksOverview.TasksByAssignee, cancellationToken);

        return new DashboardOverviewDto
        {
            Team = teamOverview,
            Reviews = reviewsOverview,
            OneOnOnes = oneOnOnesOverview,
            Tasks = tasksOverview,
            Insights = insights,
            GeneratedAt = DateTime.UtcNow
        };
    }

    private async Task<DashboardInsightsDto> GetDashboardInsightsAsync(
        IReadOnlyList<TasksByAssigneeDto> tasksByAssignee,
        CancellationToken cancellationToken)
    {
        var directReports = await _directReportRepository.GetAllAsync(cancellationToken);
        var projects = await _projectRepository.GetAllAsync(cancellationToken);
        var allTasks = await _taskRepository.GetAllAsync(cancellationToken);
        var parents = await _parentRepository.GetAllAsync(cancellationToken);
        var leaves = await _leaveRepository.GetAllAsync(cancellationToken);
        var appSettings = await _appSettingsRepository.GetAsync(cancellationToken);

        var tasks = ExcludeParentTasks(allTasks, parents);

        // Get configurable thresholds (use defaults if settings not found)
        var maxInProgress = appSettings?.MaxInProgressTasks ?? 2;
        var maxBlocked = appSettings?.MaxBlockedTasks ?? 1;
        var maxInReview = appSettings?.MaxInReviewTasks ?? 1;
        var minProjectMembers = appSettings?.MinProjectMembers ?? 2;

        // 1. Workload warnings (using configurable thresholds)
        var workloadWarnings = tasksByAssignee
            .Where(a => a.InProgressTasks > maxInProgress || a.BlockedTasks > maxBlocked || a.InReviewTasks > maxInReview)
            .Select(a =>
            {
                var issues = new List<string>();
                if (a.InProgressTasks > maxInProgress) issues.Add($"{a.InProgressTasks} in progress");
                if (a.BlockedTasks > maxBlocked) issues.Add($"{a.BlockedTasks} blocked");
                if (a.InReviewTasks > maxInReview) issues.Add($"{a.InReviewTasks} in review");

                return new WorkloadWarningDto
                {
                    AssigneeId = a.AssigneeId,
                    AssigneeName = a.AssigneeName,
                    InProgressTasks = a.InProgressTasks,
                    BlockedTasks = a.BlockedTasks,
                    InReviewTasks = a.InReviewTasks,
                    Issues = issues
                };
            })
            .ToList();

        // 2. Knowledge silos (projects with < minProjectMembers members engaged)
        var projectLabelsMap = projects
            .Where(p => !string.IsNullOrWhiteSpace(p.Labels))
            .ToDictionary(
                p => p.Id,
                p => p.Labels!.Split(',').Select(l => l.Trim().ToLowerInvariant()).ToHashSet()
            );

        var membersByProject = new Dictionary<Guid, HashSet<string>>();
        foreach (var task in tasks.Where(t => t.AssigneeId.HasValue && !string.IsNullOrWhiteSpace(t.Labels)))
        {
            var taskLabels = task.Labels!.Split(',').Select(l => l.Trim().ToLowerInvariant()).ToHashSet();
            var assigneeName = directReports.FirstOrDefault(dr => dr.Id == task.AssigneeId)?.FullName ?? "Unknown";

            foreach (var project in projects)
            {
                if (!projectLabelsMap.TryGetValue(project.Id, out var projectLabels)) continue;
                if (taskLabels.Overlaps(projectLabels))
                {
                    if (!membersByProject.ContainsKey(project.Id))
                        membersByProject[project.Id] = new HashSet<string>();
                    membersByProject[project.Id].Add(assigneeName);
                }
            }
        }

        var knowledgeSilos = membersByProject
            .Where(kvp => kvp.Value.Count < minProjectMembers)
            .Select(kvp =>
            {
                var project = projects.First(p => p.Id == kvp.Key);
                return new KnowledgeSiloDto
                {
                    ProjectId = project.Id,
                    ProjectName = project.Name,
                    MemberCount = kvp.Value.Count,
                    MemberNames = kvp.Value.ToList()
                };
            })
            .ToList();

        // 3. Unengaged members (direct reports not engaged in any project, excluding those on leave today)
        var today = DateTime.UtcNow.Date;
        var directReportsOnLeaveToday = leaves
            .Where(l => l.StartDate.Date <= today && l.EndDate.Date >= today)
            .Select(l => l.DirectReportId)
            .ToHashSet();

        var engagedMemberIds = tasks
            .Where(t => t.AssigneeId.HasValue && !string.IsNullOrWhiteSpace(t.Labels))
            .Where(t =>
            {
                var taskLabels = t.Labels!.Split(',').Select(l => l.Trim().ToLowerInvariant()).ToHashSet();
                return projects.Any(p =>
                    projectLabelsMap.TryGetValue(p.Id, out var projectLabels) &&
                    taskLabels.Overlaps(projectLabels));
            })
            .Select(t => t.AssigneeId!.Value)
            .ToHashSet();

        var unengagedMembers = directReports
            .Where(dr => dr.IsDirect && !engagedMemberIds.Contains(dr.Id))
            .Select(dr => new UnengagedMemberDto
            {
                DirectReportId = dr.Id,
                FullName = dr.FullName,
                IsOnLeave = directReportsOnLeaveToday.Contains(dr.Id)
            })
            .Where(u => !u.IsOnLeave) // Exclude those on leave from the warning
            .ToList();

        // 4. Unmatched task count (tasks not matching any project by labels)
        var unmatchedTaskCount = tasks.Count(task =>
        {
            if (string.IsNullOrWhiteSpace(task.Labels))
                return true;

            var taskLabels = task.Labels.Split(',').Select(l => l.Trim().ToLowerInvariant()).ToHashSet();
            return !projects.Any(p =>
                projectLabelsMap.TryGetValue(p.Id, out var projectLabels) &&
                taskLabels.Overlaps(projectLabels));
        });

        return new DashboardInsightsDto
        {
            WorkloadWarnings = workloadWarnings,
            KnowledgeSilos = knowledgeSilos,
            UnengagedMembers = unengagedMembers,
            UnmatchedTaskCount = unmatchedTaskCount
        };
    }

    private async Task<TeamOverviewDto> GetTeamOverviewAsync(CancellationToken cancellationToken)
    {
        var allReports = await _directReportRepository.GetAllAsync(cancellationToken);
        // Filter to only include direct reports (not indirect reports from team members)
        var directReports = allReports.Where(dr => dr.IsDirect).ToList();

        return new TeamOverviewDto
        {
            TotalReports = allReports.Count,
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
        var allReviews = await _reviewRepository.GetAllAsync(cancellationToken);
        var directReports = await _directReportRepository.GetAllAsync(cancellationToken);

        // Filter reviews to only include reviews for direct reports (IsDirect = true)
        var directReportIds = directReports.Where(dr => dr.IsDirect).Select(dr => dr.Id).ToHashSet();
        var reviews = allReviews.Where(r => directReportIds.Contains(r.DirectReportId)).ToList();

        var ratedReviews = reviews.Where(r => r.Rating != PerformanceRating.NotRated).ToList();

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
            RatingDistribution = ratingDistribution,
            ReviewsByPeriod = reviewsByPeriod
        };
    }

    public async Task<OneOnOnesOverviewDto> GetOneOnOnesAnalyticsAsync(CancellationToken cancellationToken = default)
    {
        var meetings = await _meetingRepository.GetAllAsync(cancellationToken);
        var directReports = await _directReportRepository.GetAllAsync(cancellationToken);

        // Simplified: Past meetings are those with date before now, upcoming are future
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var pastMeetings = meetings.Where(m => m.MeetingDate < today).ToList();
        var upcomingMeetings = meetings.Where(m => m.MeetingDate >= today).ToList();

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
            FrequencyByDirectReport = frequencyByDirectReport,
            ActionItemsSummary = new List<ActionItemsSummaryDto> { actionItemsSummary }
        };
    }

    public async Task<TasksOverviewDto> GetTasksAnalyticsAsync(int? sprintCount = null, CancellationToken cancellationToken = default)
    {
        var allTasks = await _taskRepository.GetAllAsync(cancellationToken);
        var projects = await _projectRepository.GetAllAsync(cancellationToken);
        var directReports = await _directReportRepository.GetAllAsync(cancellationToken);
        var parents = await _parentRepository.GetAllAsync(cancellationToken);

        // Exclude tasks that are also parents from counts and estimations
        var filteredTasks = ExcludeParentTasks(allTasks, parents);

        // Filter tasks by sprint count if requested
        var tasks = filteredTasks.AsReadOnly() as IReadOnlyList<TeamTask> ?? filteredTasks;
        if (sprintCount.HasValue && sprintCount.Value > 0)
        {
            var allSprints = await _sprintRepository.GetAllAsync(cancellationToken);

            // Get unique sprint names from tasks (using latest sprint per task)
            var uniqueSprintNames = filteredTasks
                .Where(t => !string.IsNullOrEmpty(t.Sprint))
                .Select(t => GetLatestSprintFromTask(t.Sprint!))
                .Where(s => !string.IsNullOrEmpty(s))
                .Distinct()
                .ToHashSet();

            // Get sprint entities for proper ordering and take the last N (excluding future sprints)
            var today = DateTime.UtcNow.Date;
            var sprintNames = allSprints
                .Where(s => uniqueSprintNames.Contains(s.Name))
                .Where(s => !s.IsFuture(today)) // Only current and past sprints
                .OrderBy(s => s.GetOrderingKey())
                .TakeLast(sprintCount.Value)
                .Select(s => s.Name)
                .ToHashSet();

            // Filter tasks to only those whose latest sprint is in the selected sprints
            if (sprintNames.Count > 0)
            {
                tasks = filteredTasks
                    .Where(t => !string.IsNullOrEmpty(t.Sprint) && sprintNames.Contains(GetLatestSprintFromTask(t.Sprint!)))
                    .ToList();
            }
        }

        var projectsSummary = new ProjectsSummaryDto
        {
            TotalProjects = projects.Count
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
                var blocked = assigneeTasks.Count(t => t.Status == TaskStatus.Blocked);
                var inReview = assigneeTasks.Count(t => t.Status == TaskStatus.InReview);
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
                    BlockedTasks = blocked,
                    InReviewTasks = inReview,
                    OverdueTasks = overdue,
                    CompletionRate = assigneeTasks.Count > 0 ? Math.Round((double)completed / assigneeTasks.Count * 100, 1) : 0,
                    TotalEstimatedHours = assigneeTasks.Sum(t => t.EstimatedHours ?? 0),
                    TotalActualHours = (int)Math.Round(assigneeTasks.Sum(t => t.TimeSpentMinutes ?? 0) / 60.0)
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

        var totalEstimated = tasks.Sum(t => t.EstimatedHours ?? 0);
        var totalActual = (int)Math.Round(tasks.Sum(t => t.TimeSpentMinutes ?? 0) / 60.0);
        var estimationAccuracy = totalEstimated > 0
            ? Math.Round((1 - Math.Abs(totalActual - totalEstimated) / (double)totalEstimated) * 100, 1)
            : 0;

        var productivity = new ProductivityMetricsDto
        {
            TotalEstimatedHours = totalEstimated,
            TotalActualHours = totalActual,
            EstimationAccuracy = Math.Max(0, estimationAccuracy),
        };

        // Calculate story points by task type
        var tasksByTypeSP = Enum.GetValues<TaskType>()
            .Select(type =>
            {
                var typeTasks = tasks.Where(t => t.Type == type).ToList();
                return new TasksByTypeSPDto
                {
                    Type = type,
                    TypeName = type.ToString(),
                    TotalStoryPoints = typeTasks.Sum(t => t.StoryPoints ?? 0),
                    TaskCount = typeTasks.Count
                };
            })
            .Where(t => t.TotalStoryPoints > 0)
            .ToList();

        // Calculate hours by task type
        var tasksByTypeHours = Enum.GetValues<TaskType>()
            .Select(type =>
            {
                var typeTasks = tasks.Where(t => t.Type == type).ToList();
                return new TasksByTypeHoursDto
                {
                    Type = type,
                    TypeName = type.ToString(),
                    TotalHours = Math.Round(typeTasks.Sum(t => t.TimeSpentMinutes ?? 0) / 60.0, 1),
                    TaskCount = typeTasks.Count
                };
            })
            .Where(t => t.TotalHours > 0)
            .ToList();

        // Calculate support distribution from tasks tagged with 'support' (respects sprint filter)
        var allSupportTasks = tasks
            .Where(t => t.Labels.Contains("support", StringComparison.OrdinalIgnoreCase) ||
                        t.Tags.Contains("support", StringComparison.OrdinalIgnoreCase))
            .ToList();
        var completedSupportTasks = allSupportTasks
            .Where(t => t.Status == TaskStatus.Done)
            .ToList();

        // Calculate maintenance distribution from tasks tagged with 'maintenance' (respects sprint filter)
        var allMaintenanceTasks = tasks
            .Where(t => t.Labels.Contains("maintenance", StringComparison.OrdinalIgnoreCase) ||
                        t.Tags.Contains("maintenance", StringComparison.OrdinalIgnoreCase))
            .ToList();
        var completedMaintenanceTasks = allMaintenanceTasks
            .Where(t => t.Status == TaskStatus.Done)
            .ToList();

        // Group by assignee across both support and maintenance tasks
        var maintenanceByAssigneeMap = allMaintenanceTasks
            .GroupBy(t => t.AssigneeId ?? Guid.Empty)
            .ToDictionary(g => g.Key, g => g.ToList());

        var allAssigneeIds = allSupportTasks.Select(t => t.AssigneeId)
            .Union(allMaintenanceTasks.Select(t => t.AssigneeId))
            .Distinct();

        var supportByAssignee = allAssigneeIds
            .Select(assigneeId =>
            {
                var key = assigneeId ?? Guid.Empty;
                var supportTasks = allSupportTasks.Where(t => (t.AssigneeId ?? Guid.Empty) == key).ToList();
                var supportCompleted = supportTasks.Where(t => t.Status == TaskStatus.Done).ToList();
                var maintTasks = maintenanceByAssigneeMap.GetValueOrDefault(key) ?? [];
                var maintCompleted = maintTasks.Where(t => t.Status == TaskStatus.Done).ToList();

                return new SupportByAssigneeDto
                {
                    AssigneeId = assigneeId,
                    AssigneeName = assigneeId.HasValue && directReportMap.TryGetValue(assigneeId.Value, out var name)
                        ? name
                        : (!assigneeId.HasValue || assigneeId.Value == Guid.Empty ? "Unassigned" : "Unknown"),
                    CompletedHours = Math.Round(supportCompleted.Sum(t => t.TimeSpentMinutes ?? 0) / 60.0, 1),
                    CompletedTaskCount = supportCompleted.Count,
                    AllHours = Math.Round(supportTasks.Sum(t => t.TimeSpentMinutes ?? 0) / 60.0, 1),
                    AllTaskCount = supportTasks.Count,
                    MaintenanceCompletedHours = Math.Round(maintCompleted.Sum(t => t.TimeSpentMinutes ?? 0) / 60.0, 1),
                    MaintenanceCompletedTaskCount = maintCompleted.Count,
                    MaintenanceAllHours = Math.Round(maintTasks.Sum(t => t.TimeSpentMinutes ?? 0) / 60.0, 1),
                    MaintenanceAllTaskCount = maintTasks.Count
                };
            })
            .Where(s => s.AllHours > 0 || s.CompletedHours > 0 || s.MaintenanceAllHours > 0 || s.MaintenanceCompletedHours > 0)
            .OrderByDescending(s => s.AllHours + s.MaintenanceAllHours)
            .ToList();

        var supportDistribution = new SupportDistributionDto
        {
            CompletedHours = Math.Round(completedSupportTasks.Sum(t => t.TimeSpentMinutes ?? 0) / 60.0, 1),
            CompletedTaskCount = completedSupportTasks.Count,
            AllHours = Math.Round(allSupportTasks.Sum(t => t.TimeSpentMinutes ?? 0) / 60.0, 1),
            AllTaskCount = allSupportTasks.Count,
            MaintenanceCompletedHours = Math.Round(completedMaintenanceTasks.Sum(t => t.TimeSpentMinutes ?? 0) / 60.0, 1),
            MaintenanceCompletedTaskCount = completedMaintenanceTasks.Count,
            MaintenanceAllHours = Math.Round(allMaintenanceTasks.Sum(t => t.TimeSpentMinutes ?? 0) / 60.0, 1),
            MaintenanceAllTaskCount = allMaintenanceTasks.Count,
            ByAssignee = supportByAssignee
        };

        // Calculate tasks by label (filtered by sprint)
        var totalTaskCount = tasks.Count;
        var tasksByLabel = tasks
            .Where(t => !string.IsNullOrEmpty(t.Labels))
            .SelectMany(t => t.Labels.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(label => new { Task = t, Label = label.Trim() }))
            .GroupBy(x => x.Label, StringComparer.OrdinalIgnoreCase)
            .Select(g =>
            {
                var labelTasks = g.Select(x => x.Task).Distinct().ToList();
                var completedLabelTasks = labelTasks.Where(t => t.Status == TaskStatus.Done).ToList();
                var totalSP = labelTasks.Sum(t =>
                {
                    if (t.PreviousSprintsStoryPoints.HasValue && t.StoryPoints.HasValue)
                        return Math.Max(0, t.StoryPoints.Value - t.PreviousSprintsStoryPoints.Value);
                    return t.StoryPoints ?? 0;
                });
                var completedSP = completedLabelTasks.Sum(t =>
                {
                    if (t.PreviousSprintsStoryPoints.HasValue && t.StoryPoints.HasValue)
                        return Math.Max(0, t.StoryPoints.Value - t.PreviousSprintsStoryPoints.Value);
                    return t.StoryPoints ?? 0;
                });
                return new TasksByLabelDto
                {
                    Label = g.Key,
                    TotalTasks = labelTasks.Count,
                    CompletedTasks = completedLabelTasks.Count,
                    TotalStoryPoints = totalSP,
                    CompletedStoryPoints = completedSP,
                    PercentageOfTotal = totalTaskCount > 0
                        ? Math.Round((double)labelTasks.Count / totalTaskCount * 100, 1)
                        : 0
                };
            })
            .OrderByDescending(l => l.TotalTasks)
            .ToList();

        return new TasksOverviewDto
        {
            Projects = projectsSummary,
            Tasks = tasksSummary,
            TasksByAssignee = tasksByAssignee,
            TasksByType = tasksByType,
            TasksByTypeSP = tasksByTypeSP,
            TasksByTypeHours = tasksByTypeHours,
            TasksByPriority = tasksByPriority,
            TasksByLabel = tasksByLabel,
            SupportDistribution = supportDistribution,
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
        var allTasks = await _taskRepository.GetByAssigneeIdAsync(directReportId, cancellationToken);
        var parents = await _parentRepository.GetAllAsync(cancellationToken);

        // Exclude tasks that are also parents from counts and estimations
        var tasks = ExcludeParentTasks(allTasks, parents);

        // Reviews analytics
        var ratedReviews = reviews.Where(r => r.Rating != PerformanceRating.NotRated).ToList();
        var latestReview = reviews.OrderByDescending(r => r.ReviewDate).FirstOrDefault();
        var avgRating = ratedReviews.Count > 0 ? Math.Round(ratedReviews.Average(r => (int)r.Rating), 1) : 0;

        var reviewsAnalytics = new ReviewsAnalyticsDto
        {
            TotalReviews = reviews.Count,
            CompletedReviews = reviews.Count,
            LatestRating = latestReview?.Rating,
            LatestRatingName = latestReview?.Rating.ToString(),
            AverageRating = avgRating,
            ReviewHistory = reviews.OrderByDescending(r => r.ReviewDate).Select(r => new ReviewHistoryDto
            {
                Period = r.ReviewPeriod,
                Rating = r.Rating,
                RatingName = r.Rating.ToString(),
                ReviewDate = r.ReviewDate
            }).ToList()
        };

        // One-on-one analytics (simplified - no status)
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var pastMeetings = meetings.Where(m => m.MeetingDate < today).OrderByDescending(m => m.MeetingDate).ToList();
        var upcomingMeetings = meetings.Where(m => m.MeetingDate >= today).OrderBy(m => m.MeetingDate).ToList();
        var lastMeeting = pastMeetings.FirstOrDefault();
        var nextMeeting = upcomingMeetings.FirstOrDefault();

        var actionItems = await _noteRepository.GetActionItemsAsync(directReportId, cancellationToken);
        var openActionItems = actionItems.Count(a => a.ActionStatus == ActionItemStatus.Open || a.ActionStatus == ActionItemStatus.InProgress);
        var overdueActionItems = actionItems.Count(a => a.IsOverdue());

        var daysSinceLastMeeting = lastMeeting != null
            ? today.DayNumber - lastMeeting.MeetingDate.DayNumber
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
            OpenActionItems = openActionItems,
            OverdueActionItems = overdueActionItems
        };

        // Task analytics
        var completedTasks = tasks.Where(t => t.Status == TaskStatus.Done).ToList();
        var inProgressTasks = tasks.Count(t => t.Status == TaskStatus.InProgress);
        var overdueTasks = tasks.Count(t => t.IsOverdue());

        var taskAnalytics = new TaskAnalyticsDto
        {
            TotalTasks = tasks.Count,
            CompletedTasks = completedTasks.Count,
            InProgressTasks = inProgressTasks,
            OverdueTasks = overdueTasks,
            CompletionRate = tasks.Count > 0 ? Math.Round((double)completedTasks.Count / tasks.Count * 100, 1) : 0,
            TotalEstimatedHours = tasks.Sum(t => t.EstimatedHours ?? 0),
            TotalActualHours = (int)Math.Round(tasks.Sum(t => t.TimeSpentMinutes ?? 0) / 60.0),
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
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var pastMeetings = meetings.Where(m => m.MeetingDate < today).OrderByDescending(m => m.MeetingDate).ToList();
            var upcomingMeetings = meetings.Where(m => m.MeetingDate >= today).OrderBy(m => m.MeetingDate).ToList();

            var lastMeeting = pastMeetings.FirstOrDefault();
            var nextMeeting = upcomingMeetings.FirstOrDefault();

            var daysSinceLastMeeting = lastMeeting != null
                ? today.DayNumber - lastMeeting.MeetingDate.DayNumber
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
        var allTasks = await _taskRepository.GetAllAsync(cancellationToken);
        var directReports = await _directReportRepository.GetAllAsync(cancellationToken);
        var parents = await _parentRepository.GetAllAsync(cancellationToken);

        // Exclude tasks that are also parents from counts and estimations
        var tasks = ExcludeParentTasks(allTasks, parents);

        var directReportMap = directReports.ToDictionary(dr => dr.Id, dr => dr.FullName);

        var assignedTasks = tasks
            .GroupBy(t => t.AssigneeId)
            .Select(g =>
            {
                var assigneeTasks = g.ToList();
                var completed = assigneeTasks.Count(t => t.Status == TaskStatus.Done);
                var inProgress = assigneeTasks.Count(t => t.Status == TaskStatus.InProgress);
                var blocked = assigneeTasks.Count(t => t.Status == TaskStatus.Blocked);
                var inReview = assigneeTasks.Count(t => t.Status == TaskStatus.InReview);
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
                    BlockedTasks = blocked,
                    InReviewTasks = inReview,
                    OverdueTasks = overdue,
                    CompletionRate = assigneeTasks.Count > 0 ? Math.Round((double)completed / assigneeTasks.Count * 100, 1) : 0,
                    TotalEstimatedHours = assigneeTasks.Sum(t => t.EstimatedHours ?? 0),
                    TotalActualHours = (int)Math.Round(assigneeTasks.Sum(t => t.TimeSpentMinutes ?? 0) / 60.0)
                };
            })
            .OrderByDescending(a => a.TotalTasks)
            .ToList();

        return assignedTasks;
    }

    /// <summary>
    /// Counts weekday-only days in a date range (inclusive).
    /// </summary>
    private static int GetWorkingDays(DateTime start, DateTime end)
    {
        int count = 0;
        for (var date = start.Date; date <= end.Date; date = date.AddDays(1))
        {
            if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
                count++;
        }
        return count;
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
            var days = orderedMeetings[i].MeetingDate.DayNumber - orderedMeetings[i - 1].MeetingDate.DayNumber;
            intervals.Add(days);
        }

        return Math.Round(intervals.Average(), 1);
    }

    public async Task<TeamVelocityDto> GetTeamVelocityAsync(int? sprintCount = null, CancellationToken cancellationToken = default)
    {
        var allTasks = await _taskRepository.GetAllAsync(cancellationToken);
        var allSprints = await _sprintRepository.GetAllAsync(cancellationToken);
        var appSettings = await _appSettingsRepository.GetAsync(cancellationToken);
        var parents = await _parentRepository.GetAllAsync(cancellationToken);

        // Exclude tasks that are also parents from counts and estimations
        var tasks = ExcludeParentTasks(allTasks, parents);

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
            .OrderBy(s => s.GetOrderingKey())
            .ToList();

        // Filter by sprint count if requested (excluding future sprints)
        if (sprintCount.HasValue && sprintCount.Value > 0)
        {
            var today = DateTime.UtcNow.Date;
            sprintEntities = sprintEntities
                .Where(sprint => !sprint.IsFuture(today))
                .TakeLast(sprintCount.Value)
                .ToList();
        }

        var sprints = sprintEntities
            .Select(sprint =>
            {
                var sprintTasks = tasksByLatestSprint[sprint.Name];
                var estimatedHours = sprintTasks.Sum(t => t.EstimatedHours ?? 0);

                // Calculate new vs carried over story points
                var newSP = sprintTasks.Sum(t =>
                {
                    if (t.PreviousSprintsStoryPoints.HasValue && t.StoryPoints.HasValue)
                        return Math.Max(0, t.StoryPoints.Value - t.PreviousSprintsStoryPoints.Value);
                    return t.StoryPoints ?? 0;
                });
                var carriedOverSP = sprintTasks.Sum(t => t.PreviousSprintsStoryPoints ?? 0);

                return new SprintVelocityDto
                {
                    SprintName = sprint.Name,
                    StoryPointsCompleted = newSP + carriedOverSP, // Total for backward compatibility
                    NewStoryPointsCompleted = newSP,
                    CarriedOverStoryPoints = carriedOverSP,
                    TasksCompleted = sprintTasks.Count,
                    TotalTimeSpentMinutes = sprintTasks.Sum(t => t.TimeSpentMinutes ?? 0),
                    TotalEstimatedHours = estimatedHours
                };
            })
            .ToList();

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
        var parents = await _parentRepository.GetAllAsync(cancellationToken);

        // Exclude tasks that are also parents from counts and estimations
        var allTasks = ExcludeParentTasks(tasks, parents);

        if (allTasks.Count == 0)
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
        var tasksByLatestSprint = allTasks
            .Where(t => !string.IsNullOrEmpty(t.Sprint))
            .Select(t => new { Task = t, LatestSprint = GetLatestSprintFromTask(t.Sprint!) })
            .Where(x => !string.IsNullOrEmpty(x.LatestSprint))
            .GroupBy(x => x.LatestSprint)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Task).ToList());

        // Order sprints by GetOrderingKey() for proper chronological ordering (prioritizes actual dates over calculated dates)
        var sprintGroups = allSprints
            .Where(s => tasksByLatestSprint.ContainsKey(s.Name))
            .OrderBy(s => s.GetOrderingKey())
            .Select(sprint =>
            {
                var tasks = tasksByLatestSprint[sprint.Name];
                var estimated = tasks.Sum(t => t.EstimatedHours ?? 0);
                var actual = (int)Math.Round(tasks.Sum(t => t.TimeSpentMinutes ?? 0) / 60.0);
                var variance = actual - estimated;
                var accuracy = estimated > 0 ? Math.Round(Math.Min(estimated, actual) / (double)Math.Max(estimated, actual) * 100, 1) : 0;

                return new SprintAccuracyDto
                {
                    SprintName = sprint.Name,
                    TasksCompleted = tasks.Count,
                    StoryPointsCompleted = tasks.Sum(t => t.StoryPoints ?? 0),
                    EstimatedHours = estimated,
                    ActualHours = actual,
                    VarianceHours = variance,
                    AccuracyPercentage = accuracy
                };
            })
            .ToList();

        // Filter by sprint count if requested (excluding future sprints)
        if (sprintCount.HasValue && sprintCount.Value > 0)
        {
            var today = DateTime.UtcNow.Date;
            sprintGroups = sprintGroups
                .Where(s => {
                    var sprint = allSprints.FirstOrDefault(sp => sp.Name == s.SprintName);
                    return sprint != null && !sprint.IsFuture(today);
                })
                .TakeLast(sprintCount.Value)
                .ToList();
        }

        // Get the filtered sprint names for filtering tasks
        var filteredSprintNames = sprintGroups.Select(s => s.SprintName).ToHashSet();

        // Filter completedTasks to only include tasks from the filtered sprints (using latest sprint logic)
        var filteredCompletedTasks = allTasks
            .Where(t => !string.IsNullOrEmpty(t.Sprint) && filteredSprintNames.Contains(GetLatestSprintFromTask(t.Sprint!)))
            .ToList();

        // If no tasks remain after filtering, use original completedTasks for assignee/project calculations
        if (filteredCompletedTasks.Count == 0)
        {
            filteredCompletedTasks = allTasks;
        }

        // Calculate by assignee using filtered tasks
        var byAssignee = filteredCompletedTasks
            .GroupBy(t => t.AssigneeId)
            .Select(g =>
            {
                var estimated = g.Sum(t => t.EstimatedHours ?? 0);
                var actual = (int)Math.Round(g.Sum(t => t.TimeSpentMinutes ?? 0) / 60.0);
                var variance = actual - estimated;
                var accuracy = estimated > 0 ? Math.Round(Math.Min(estimated, actual) / (double)Math.Max(estimated, actual) * 100, 1) : 0;

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
                var estimated = g.Sum(t => t.EstimatedHours ?? 0);
                var actual = (int)Math.Round(g.Sum(t => t.TimeSpentMinutes ?? 0) / 60.0);
                var variance = actual - estimated;
                var accuracy = estimated > 0 ? Math.Round(Math.Min(estimated, actual) / (double)Math.Max(estimated, actual) * 100, 1) : 0;

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
        var totalEstimated = filteredCompletedTasks.Sum(t => t.EstimatedHours ?? 0);
        var totalActual = (int)Math.Round(filteredCompletedTasks.Sum(t => t.TimeSpentMinutes ?? 0) / 60.0);
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
        var allTasks = await _taskRepository.GetAllAsync(cancellationToken);
        var directReports = await _directReportRepository.GetAllAsync(cancellationToken);
        var projects = await _projectRepository.GetAllAsync(cancellationToken);
        var parents = await _parentRepository.GetAllAsync(cancellationToken);

        // Exclude tasks that are also parents from counts and estimations
        var tasks = ExcludeParentTasks(allTasks, parents);

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
        var allTasks = await _taskRepository.GetAllAsync(cancellationToken);
        var parents = await _parentRepository.GetAllAsync(cancellationToken);

        // Exclude tasks that are also parents from counts and estimations
        var tasks = ExcludeParentTasks(allTasks, parents);

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

        // Determine current sprint based on today's date (sprint that contains today)
        var today = DateTime.UtcNow.Date;
        var currentSprintEntity = sprints
            .Where(s => s.ContainsDate(today))
            .OrderByDescending(s => s.GetOrderingKey())
            .FirstOrDefault();

        // If no sprint contains today, find the nearest upcoming sprint
        if (currentSprintEntity == null)
        {
            currentSprintEntity = sprints
                .Where(s => s.GetEstimatedStartDate().Date >= today)
                .OrderBy(s => s.GetOrderingKey())
                .FirstOrDefault();
        }

        // If still no sprint found, fall back to the most recent past sprint
        if (currentSprintEntity == null)
        {
            currentSprintEntity = sprints
                .Where(s => s.GetEstimatedEndDate().Date < today)
                .OrderByDescending(s => s.GetOrderingKey())
                .FirstOrDefault();
        }

        // Filter sprints to show: if sprintCount specified, take last N sprints (including current)
        var sprintsToShow = sprints;
        if (sprintCount.HasValue && sprintCount.Value > 0)
        {
            // Get current and past sprints only (exclude future)
            var currentAndPastSprints = sprints
                .Where(s => !s.IsFuture(today))
                .OrderByDescending(s => s.GetOrderingKey())
                .Take(sprintCount.Value)
                .ToList();

            sprintsToShow = currentAndPastSprints;
        }

        var pastSprints = new List<SprintCapacityAnalysisDto>();
        var futureSprints = new List<SprintCapacityAnalysisDto>();
        SprintCapacityAnalysisDto? currentSprint = null;

        foreach (var sprint in sprintsToShow.OrderByDescending(s => s.GetOrderingKey()))
        {
            var sprintTasks = tasksBySprint.TryGetValue(sprint.Name, out var st) ? st : new List<TeamTask>();
            capacityMap.TryGetValue(sprint.Id, out var capacity);

            var completedTasks = sprintTasks.Where(t => t.Status == TaskStatus.Done).ToList();

            // Calculate completed points (total SP from done tasks)
            var completedPoints = completedTasks.Sum(t => t.StoryPoints ?? 0);

            // Calculate new completed points (excluding carried over)
            var newCompletedPoints = completedTasks.Sum(t =>
            {
                if (t.PreviousSprintsStoryPoints.HasValue && t.StoryPoints.HasValue)
                    return Math.Max(0, t.StoryPoints.Value - t.PreviousSprintsStoryPoints.Value);
                return t.StoryPoints ?? 0;
            });

            // Compute total story points for this sprint (including carried over)
            var totalStoryPoints = sprintTasks.Sum(t => t.StoryPoints ?? 0);

            // Calculate carried over points for all tasks (not just completed)
            var carriedOverPoints = sprintTasks.Sum(t => t.PreviousSprintsStoryPoints ?? 0);

            var committedPoints = capacity?.TotalCapacityPoints ?? 0;
            var utilization = committedPoints > 0
                ? Math.Round((double)completedPoints / committedPoints * 100, 1)
                : 0;

            string status;
            if (currentSprintEntity != null && sprint.Id == currentSprintEntity.Id)
            {
                status = "Current";
            }
            else if (sprint.IsPast(today))
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
                NewCompletedPoints = newCompletedPoints,
                CarriedOverPoints = carriedOverPoints,
                TotalStoryPoints = totalStoryPoints,
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

        // Calculate average utilization from past and current sprints
        var sprintsForUtilization = currentSprint != null
            ? pastSprints.Append(currentSprint).ToList()
            : pastSprints;
        var totalCommitted = sprintsForUtilization.Sum(s => s.CommittedPoints);
        var totalCompleted = sprintsForUtilization.Sum(s => s.CompletedPoints);
        var averageUtilization = sprintsForUtilization.Count > 0
            ? Math.Round(sprintsForUtilization.Average(s => s.UtilizationPercentage), 1)
            : 0;

        // Load leaves and direct reports for predictions and suggestions
        var allLeaves = await _leaveRepository.GetAllAsync(cancellationToken);
        var directReports = await _directReportRepository.GetAllAsync(cancellationToken);
        var directReportIds = directReports.Where(dr => dr.IsDirect).Select(dr => dr.Id).ToHashSet();
        var activeLeaves = allLeaves
            .Where(l => l.Status == LeaveStatus.Active && directReportIds.Contains(l.DirectReportId))
            .ToList();
        var totalTeamSize = directReports.Count(dr => dr.IsDirect);
        var sprintEntityMap = sprints.ToDictionary(s => s.Id);

        // Compute predicted points for future sprints
        if (futureSprints.Count > 0)
        {
            var avgCompletedSP = pastSprints.Count > 0
                ? (int)Math.Round(pastSprints.Average(s => (double)s.CompletedPoints))
                : (currentSprint?.CompletedPoints ?? 0);

            // Compute average effective availability across past sprints using the same
            // leave-overlap method we use for future sprints.  This way, if past sprints
            // also had reduced capacity, the ratio correctly reflects relative change
            // rather than double-penalizing.
            // Example: team=5, past avg available=4, completed 40 SP.
            //   Future sprint with 4 available → ratio=4/4=1 → predicted=40 (correct)
            //   Future sprint with 3 available → ratio=3/4=0.75 → predicted=30 (correct)
            var pastAvailabilities = new List<double>();
            foreach (var ps in pastSprints)
            {
                if (!sprintEntityMap.TryGetValue(ps.SprintId, out var psEntity))
                    continue;

                var psStart = psEntity.GetEstimatedStartDate();
                var psEnd = psEntity.GetEstimatedEndDate();
                var psWorkingDays = GetWorkingDays(psStart, psEnd);

                var psLeaveDays = 0;
                foreach (var leave in activeLeaves)
                {
                    if (!leave.OverlapsWith(psStart, psEnd))
                        continue;
                    var overlapStart = leave.StartDate > psStart ? leave.StartDate : psStart;
                    var overlapEnd = leave.EndDate < psEnd ? leave.EndDate : psEnd;
                    psLeaveDays += GetWorkingDays(overlapStart, overlapEnd);
                }

                var psLostCapacity = psWorkingDays > 0 ? (double)psLeaveDays / psWorkingDays : 0;
                pastAvailabilities.Add(Math.Max(0, totalTeamSize - psLostCapacity));
            }
            var pastAverageAvailable = pastAvailabilities.Count > 0
                ? pastAvailabilities.Average()
                : totalTeamSize;

            futureSprints = futureSprints.Select(fs =>
            {
                if (!sprintEntityMap.TryGetValue(fs.SprintId, out var sprintEntity))
                    return fs;

                var sprintStart = sprintEntity.GetEstimatedStartDate();
                var sprintEnd = sprintEntity.GetEstimatedEndDate();
                var workingDaysInSprint = GetWorkingDays(sprintStart, sprintEnd);

                // Calculate total leave working days overlapping this sprint
                var totalLeaveDays = 0;
                foreach (var leave in activeLeaves)
                {
                    if (!leave.OverlapsWith(sprintStart, sprintEnd))
                        continue;

                    var overlapStart = leave.StartDate > sprintStart ? leave.StartDate : sprintStart;
                    var overlapEnd = leave.EndDate < sprintEnd ? leave.EndDate : sprintEnd;
                    totalLeaveDays += GetWorkingDays(overlapStart, overlapEnd);
                }

                var lostCapacity = workingDaysInSprint > 0
                    ? (double)totalLeaveDays / workingDaysInSprint
                    : 0;
                var futureAvailable = Math.Max(0, totalTeamSize - lostCapacity);

                int predictedPoints;
                if (fs.CommittedPoints > 0 && averageUtilization > 0)
                {
                    // Mode 1: Commitment-based
                    predictedPoints = (int)Math.Round(fs.CommittedPoints * averageUtilization / 100);
                }
                else
                {
                    // Mode 2: Velocity-based — scale by availability ratio relative to
                    // what the team actually had during the past sprints that produced avgCompletedSP.
                    var ratio = pastAverageAvailable > 0 ? futureAvailable / pastAverageAvailable : 1;
                    predictedPoints = (int)Math.Round(avgCompletedSP * ratio);
                }

                return new SprintCapacityAnalysisDto
                {
                    SprintId = fs.SprintId,
                    SprintName = fs.SprintName,
                    Year = fs.Year,
                    Quarter = fs.Quarter,
                    SprintNumber = fs.SprintNumber,
                    CommittedPoints = fs.CommittedPoints,
                    CompletedPoints = fs.CompletedPoints,
                    TotalStoryPoints = fs.TotalStoryPoints,
                    UtilizationPercentage = fs.UtilizationPercentage,
                    PredictedPoints = predictedPoints,
                    Status = fs.Status
                };
            }).ToList();
        }

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

    public async Task<byte[]> ExportDashboardToExcelAsync(int? sprintCount = null, CancellationToken cancellationToken = default)
    {
        // Gather all dashboard data
        var dashboard = await GetDashboardOverviewAsync(sprintCount, cancellationToken);
        var velocity = await GetTeamVelocityAsync(sprintCount, cancellationToken);
        var accuracy = await GetEstimationAccuracyAsync(sprintCount, cancellationToken);
        var capacity = await GetCapacityAnalysisAsync(sprintCount, cancellationToken);

        OfficeOpenXml.ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;

        using var package = new OfficeOpenXml.ExcelPackage();

        // 1. Summary Sheet
        AddSummarySheet(package, dashboard, velocity, accuracy, capacity, sprintCount);

        // 2. Team Overview Sheet
        AddTeamOverviewSheet(package, dashboard);

        // 3. Tasks Distribution Sheet
        AddTasksDistributionSheet(package, dashboard);

        // 4. Members Workload Sheet
        AddMembersWorkloadSheet(package, dashboard);

        // 5. Team Velocity Sheet
        AddTeamVelocitySheet(package, velocity);

        // 6. Capacity Analysis Sheet
        AddCapacityAnalysisSheet(package, capacity);

        // 7. Estimation Accuracy Sheet
        AddEstimationAccuracySheet(package, accuracy);

        // 8. Insights Sheet
        AddInsightsSheet(package, dashboard);

        return await Task.FromResult(package.GetAsByteArray());
    }

    private static void AddSummarySheet(
        OfficeOpenXml.ExcelPackage package,
        DashboardOverviewDto dashboard,
        TeamVelocityDto velocity,
        EstimationAccuracyDto accuracy,
        CapacityAnalysisDto capacity,
        int? sprintCount)
    {
        var worksheet = package.Workbook.Worksheets.Add("Summary");

        // Title
        worksheet.Cells[1, 1].Value = "Dashboard Report";
        worksheet.Cells[1, 1].Style.Font.Bold = true;
        worksheet.Cells[1, 1].Style.Font.Size = 16;

        // Report metadata
        worksheet.Cells[3, 1].Value = "Generated At:";
        worksheet.Cells[3, 2].Value = dashboard.GeneratedAt.ToString("yyyy-MM-dd HH:mm:ss UTC");

        worksheet.Cells[4, 1].Value = "Sprint Filter:";
        worksheet.Cells[4, 2].Value = sprintCount.HasValue ? $"Last {sprintCount} sprints" : "All sprints";

        // Key Metrics Section
        worksheet.Cells[6, 1].Value = "Key Metrics";
        worksheet.Cells[6, 1].Style.Font.Bold = true;
        worksheet.Cells[6, 1].Style.Font.Size = 14;

        var metricsRow = 7;
        var metrics = new (string Label, object Value)[]
        {
            ("Team Members", dashboard.Team.TotalReports),
            ("Direct Reports", dashboard.Team.TotalDirectReports),
            ("Total Projects", dashboard.Tasks.Projects.TotalProjects),
            ("Total Tasks", dashboard.Tasks.Tasks.TotalTasks),
            ("Completed Tasks", dashboard.Tasks.Tasks.DoneTasks),
            ("Completion Rate", $"{dashboard.Tasks.Tasks.CompletionRate}%"),
            ("Average Velocity (SP)", velocity.AverageVelocity),
            ("Total Story Points Completed", velocity.TotalStoryPointsCompleted),
            ("Velocity Trend", $"{velocity.CompletionTrend}%"),
            ("Average Utilization", $"{capacity.AverageUtilization}%"),
            ("Total Committed Points", capacity.TotalCommittedPoints),
            ("Total Completed Points", capacity.TotalCompletedPoints),
            ("Estimation Accuracy", $"{accuracy.OverallAccuracyPercentage}%"),
            ("Total Estimated Hours", accuracy.TotalEstimatedHours),
            ("Total Actual Hours", accuracy.TotalActualHours),
            ("Variance Hours", accuracy.TotalVarianceHours)
        };

        foreach (var (label, value) in metrics)
        {
            worksheet.Cells[metricsRow, 1].Value = label;
            worksheet.Cells[metricsRow, 2].Value = value;
            metricsRow++;
        }

        // Style the header row
        using (var range = worksheet.Cells[6, 1, 6, 2])
        {
            range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(198, 224, 240));
        }

        worksheet.Column(1).Width = 30;
        worksheet.Column(2).Width = 25;
    }

    private static void AddTeamOverviewSheet(OfficeOpenXml.ExcelPackage package, DashboardOverviewDto dashboard)
    {
        var worksheet = package.Workbook.Worksheets.Add("Team Overview");

        // Headers
        var headers = new[] { "Name", "Job Title", "Department", "Hire Date", "Tenure (Months)" };
        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cells[1, i + 1].Value = headers[i];
            worksheet.Cells[1, i + 1].Style.Font.Bold = true;
        }

        // Style header row
        using (var range = worksheet.Cells[1, 1, 1, headers.Length])
        {
            range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(198, 224, 240));
        }

        // Data rows
        var row = 2;
        foreach (var member in dashboard.Team.DirectReports)
        {
            worksheet.Cells[row, 1].Value = member.FullName;
            worksheet.Cells[row, 2].Value = member.JobTitle;
            worksheet.Cells[row, 3].Value = member.Department;
            worksheet.Cells[row, 4].Value = member.HireDate.ToString("yyyy-MM-dd");
            worksheet.Cells[row, 5].Value = member.TenureMonths;
            row++;
        }

        // Auto-fit columns
        for (int i = 1; i <= headers.Length; i++)
        {
            worksheet.Column(i).AutoFit();
        }
    }

    private static void AddTasksDistributionSheet(OfficeOpenXml.ExcelPackage package, DashboardOverviewDto dashboard)
    {
        var worksheet = package.Workbook.Worksheets.Add("Tasks Distribution");

        // Headers
        var headers = new[] { "Type", "Total Tasks", "Completed", "Story Points", "Hours", "Completion Rate" };
        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cells[1, i + 1].Value = headers[i];
            worksheet.Cells[1, i + 1].Style.Font.Bold = true;
        }

        // Style header row
        using (var range = worksheet.Cells[1, 1, 1, headers.Length])
        {
            range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(198, 224, 240));
        }

        // Data rows
        var row = 2;
        foreach (var taskType in dashboard.Tasks.TasksByType)
        {
            var spData = dashboard.Tasks.TasksByTypeSP.FirstOrDefault(t => t.Type == taskType.Type);
            var hoursData = dashboard.Tasks.TasksByTypeHours.FirstOrDefault(t => t.Type == taskType.Type);

            worksheet.Cells[row, 1].Value = taskType.TypeName;
            worksheet.Cells[row, 2].Value = taskType.TotalTasks;
            worksheet.Cells[row, 3].Value = taskType.CompletedTasks;
            worksheet.Cells[row, 4].Value = spData?.TotalStoryPoints ?? 0;
            worksheet.Cells[row, 5].Value = hoursData?.TotalHours ?? 0;
            worksheet.Cells[row, 6].Value = $"{taskType.CompletionRate}%";
            row++;
        }

        // Total row
        worksheet.Cells[row, 1].Value = "Total";
        worksheet.Cells[row, 1].Style.Font.Bold = true;
        worksheet.Cells[row, 2].Value = dashboard.Tasks.Tasks.TotalTasks;
        worksheet.Cells[row, 3].Value = dashboard.Tasks.Tasks.DoneTasks;
        worksheet.Cells[row, 4].Value = dashboard.Tasks.TasksByTypeSP.Sum(t => t.TotalStoryPoints);
        worksheet.Cells[row, 5].Value = dashboard.Tasks.TasksByTypeHours.Sum(t => t.TotalHours);
        worksheet.Cells[row, 6].Value = $"{dashboard.Tasks.Tasks.CompletionRate}%";

        // Auto-fit columns
        for (int i = 1; i <= headers.Length; i++)
        {
            worksheet.Column(i).AutoFit();
        }
    }

    private static void AddMembersWorkloadSheet(OfficeOpenXml.ExcelPackage package, DashboardOverviewDto dashboard)
    {
        var worksheet = package.Workbook.Worksheets.Add("Members Workload");

        // Headers
        var headers = new[] { "Member", "Total Tasks", "Completed", "In Progress", "Blocked", "In Review", "Overdue", "Completion Rate", "Estimated Hours", "Actual Hours" };
        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cells[1, i + 1].Value = headers[i];
            worksheet.Cells[1, i + 1].Style.Font.Bold = true;
        }

        // Style header row
        using (var range = worksheet.Cells[1, 1, 1, headers.Length])
        {
            range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(198, 224, 240));
        }

        // Data rows
        var row = 2;
        foreach (var assignee in dashboard.Tasks.TasksByAssignee)
        {
            worksheet.Cells[row, 1].Value = assignee.AssigneeName;
            worksheet.Cells[row, 2].Value = assignee.TotalTasks;
            worksheet.Cells[row, 3].Value = assignee.CompletedTasks;
            worksheet.Cells[row, 4].Value = assignee.InProgressTasks;
            worksheet.Cells[row, 5].Value = assignee.BlockedTasks;
            worksheet.Cells[row, 6].Value = assignee.InReviewTasks;
            worksheet.Cells[row, 7].Value = assignee.OverdueTasks;
            worksheet.Cells[row, 8].Value = $"{assignee.CompletionRate}%";
            worksheet.Cells[row, 9].Value = assignee.TotalEstimatedHours;
            worksheet.Cells[row, 10].Value = assignee.TotalActualHours;
            row++;
        }

        // Auto-fit columns
        for (int i = 1; i <= headers.Length; i++)
        {
            worksheet.Column(i).AutoFit();
        }
    }

    private static void AddTeamVelocitySheet(OfficeOpenXml.ExcelPackage package, TeamVelocityDto velocity)
    {
        var worksheet = package.Workbook.Worksheets.Add("Team Velocity");

        // Headers
        var headers = new[] { "Sprint", "Story Points Completed", "New SP", "Carried Over SP", "Tasks Completed", "Time Spent (Minutes)", "Estimated Hours" };
        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cells[1, i + 1].Value = headers[i];
            worksheet.Cells[1, i + 1].Style.Font.Bold = true;
        }

        // Style header row
        using (var range = worksheet.Cells[1, 1, 1, headers.Length])
        {
            range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(198, 224, 240));
        }

        // Data rows
        var row = 2;
        foreach (var sprint in velocity.Sprints)
        {
            worksheet.Cells[row, 1].Value = sprint.SprintName;
            worksheet.Cells[row, 2].Value = sprint.StoryPointsCompleted;
            worksheet.Cells[row, 3].Value = sprint.NewStoryPointsCompleted;
            worksheet.Cells[row, 4].Value = sprint.CarriedOverStoryPoints;
            worksheet.Cells[row, 5].Value = sprint.TasksCompleted;
            worksheet.Cells[row, 6].Value = sprint.TotalTimeSpentMinutes;
            worksheet.Cells[row, 7].Value = sprint.TotalEstimatedHours;
            row++;
        }

        // Summary row
        row++;
        worksheet.Cells[row, 1].Value = "Summary";
        worksheet.Cells[row, 1].Style.Font.Bold = true;
        row++;
        worksheet.Cells[row, 1].Value = "Average Velocity";
        worksheet.Cells[row, 2].Value = velocity.AverageVelocity;
        row++;
        worksheet.Cells[row, 1].Value = "Total Story Points";
        worksheet.Cells[row, 2].Value = velocity.TotalStoryPointsCompleted;
        row++;
        worksheet.Cells[row, 1].Value = "Completion Trend";
        worksheet.Cells[row, 2].Value = $"{velocity.CompletionTrend}%";

        // Auto-fit columns
        for (int i = 1; i <= headers.Length; i++)
        {
            worksheet.Column(i).AutoFit();
        }
    }

    private static void AddCapacityAnalysisSheet(OfficeOpenXml.ExcelPackage package, CapacityAnalysisDto capacity)
    {
        var worksheet = package.Workbook.Worksheets.Add("Capacity Analysis");

        // Headers
        var headers = new[] { "Sprint", "Status", "Committed Points", "Completed Points", "New Completed", "Carried Over", "Predicted Points", "Utilization %" };
        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cells[1, i + 1].Value = headers[i];
            worksheet.Cells[1, i + 1].Style.Font.Bold = true;
        }

        // Style header row
        using (var range = worksheet.Cells[1, 1, 1, headers.Length])
        {
            range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(198, 224, 240));
        }

        // Combine all sprints in order
        var allSprints = capacity.PastSprints
            .Concat(capacity.CurrentSprint != null ? new[] { capacity.CurrentSprint } : Array.Empty<SprintCapacityAnalysisDto>())
            .Concat(capacity.FutureSprints)
            .ToList();

        // Data rows
        var row = 2;
        foreach (var sprint in allSprints)
        {
            worksheet.Cells[row, 1].Value = sprint.SprintName;
            worksheet.Cells[row, 2].Value = sprint.Status;
            worksheet.Cells[row, 3].Value = sprint.CommittedPoints;
            worksheet.Cells[row, 4].Value = sprint.CompletedPoints;
            worksheet.Cells[row, 5].Value = sprint.NewCompletedPoints;
            worksheet.Cells[row, 6].Value = sprint.CarriedOverPoints;
            worksheet.Cells[row, 7].Value = sprint.PredictedPoints;
            worksheet.Cells[row, 8].Value = sprint.UtilizationPercentage;
            row++;
        }

        // Summary row
        row++;
        worksheet.Cells[row, 1].Value = "Summary";
        worksheet.Cells[row, 1].Style.Font.Bold = true;
        row++;
        worksheet.Cells[row, 1].Value = "Average Utilization";
        worksheet.Cells[row, 2].Value = $"{capacity.AverageUtilization}%";
        row++;
        worksheet.Cells[row, 1].Value = "Total Committed Points";
        worksheet.Cells[row, 2].Value = capacity.TotalCommittedPoints;
        row++;
        worksheet.Cells[row, 1].Value = "Total Completed Points";
        worksheet.Cells[row, 2].Value = capacity.TotalCompletedPoints;

        // Auto-fit columns
        for (int i = 1; i <= headers.Length; i++)
        {
            worksheet.Column(i).AutoFit();
        }
    }

    private static void AddEstimationAccuracySheet(OfficeOpenXml.ExcelPackage package, EstimationAccuracyDto accuracy)
    {
        var worksheet = package.Workbook.Worksheets.Add("Estimation Accuracy");

        // Section 1: By Sprint
        worksheet.Cells[1, 1].Value = "Accuracy by Sprint";
        worksheet.Cells[1, 1].Style.Font.Bold = true;
        worksheet.Cells[1, 1].Style.Font.Size = 14;

        var sprintHeaders = new[] { "Sprint", "Tasks Completed", "Story Points", "Estimated Hours", "Actual Hours", "Variance", "Accuracy %" };
        for (int i = 0; i < sprintHeaders.Length; i++)
        {
            worksheet.Cells[2, i + 1].Value = sprintHeaders[i];
            worksheet.Cells[2, i + 1].Style.Font.Bold = true;
        }

        using (var range = worksheet.Cells[2, 1, 2, sprintHeaders.Length])
        {
            range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(198, 224, 240));
        }

        var row = 3;
        foreach (var sprint in accuracy.Sprints)
        {
            worksheet.Cells[row, 1].Value = sprint.SprintName;
            worksheet.Cells[row, 2].Value = sprint.TasksCompleted;
            worksheet.Cells[row, 3].Value = sprint.StoryPointsCompleted;
            worksheet.Cells[row, 4].Value = sprint.EstimatedHours;
            worksheet.Cells[row, 5].Value = sprint.ActualHours;
            worksheet.Cells[row, 6].Value = sprint.VarianceHours;
            worksheet.Cells[row, 7].Value = sprint.AccuracyPercentage;
            row++;
        }

        // Section 2: By Assignee
        row += 2;
        worksheet.Cells[row, 1].Value = "Accuracy by Assignee";
        worksheet.Cells[row, 1].Style.Font.Bold = true;
        worksheet.Cells[row, 1].Style.Font.Size = 14;
        row++;

        var assigneeHeaders = new[] { "Assignee", "Tasks Completed", "Estimated Hours", "Actual Hours", "Variance", "Accuracy %" };
        for (int i = 0; i < assigneeHeaders.Length; i++)
        {
            worksheet.Cells[row, i + 1].Value = assigneeHeaders[i];
            worksheet.Cells[row, i + 1].Style.Font.Bold = true;
        }

        using (var range = worksheet.Cells[row, 1, row, assigneeHeaders.Length])
        {
            range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(198, 224, 240));
        }

        row++;
        foreach (var assignee in accuracy.ByAssignee)
        {
            worksheet.Cells[row, 1].Value = assignee.AssigneeName;
            worksheet.Cells[row, 2].Value = assignee.TasksCompleted;
            worksheet.Cells[row, 3].Value = assignee.EstimatedHours;
            worksheet.Cells[row, 4].Value = assignee.ActualHours;
            worksheet.Cells[row, 5].Value = assignee.VarianceHours;
            worksheet.Cells[row, 6].Value = assignee.AccuracyPercentage;
            row++;
        }

        // Auto-fit columns
        for (int i = 1; i <= 7; i++)
        {
            worksheet.Column(i).AutoFit();
        }
    }

    private static void AddInsightsSheet(OfficeOpenXml.ExcelPackage package, DashboardOverviewDto dashboard)
    {
        var worksheet = package.Workbook.Worksheets.Add("Insights");

        var row = 1;

        // Section 1: Workload Warnings
        worksheet.Cells[row, 1].Value = "Workload Warnings";
        worksheet.Cells[row, 1].Style.Font.Bold = true;
        worksheet.Cells[row, 1].Style.Font.Size = 14;
        row++;

        if (dashboard.Insights.WorkloadWarnings.Count > 0)
        {
            var warnHeaders = new[] { "Team Member", "In Progress", "Blocked", "In Review", "Issues" };
            for (int i = 0; i < warnHeaders.Length; i++)
            {
                worksheet.Cells[row, i + 1].Value = warnHeaders[i];
                worksheet.Cells[row, i + 1].Style.Font.Bold = true;
            }

            using (var range = worksheet.Cells[row, 1, row, warnHeaders.Length])
            {
                range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(255, 200, 200));
            }

            row++;
            foreach (var warning in dashboard.Insights.WorkloadWarnings)
            {
                worksheet.Cells[row, 1].Value = warning.AssigneeName;
                worksheet.Cells[row, 2].Value = warning.InProgressTasks;
                worksheet.Cells[row, 3].Value = warning.BlockedTasks;
                worksheet.Cells[row, 4].Value = warning.InReviewTasks;
                worksheet.Cells[row, 5].Value = string.Join(", ", warning.Issues);
                row++;
            }
        }
        else
        {
            worksheet.Cells[row, 1].Value = "No workload warnings";
            row++;
        }

        // Section 2: Knowledge Silos
        row += 2;
        worksheet.Cells[row, 1].Value = "Knowledge Silos";
        worksheet.Cells[row, 1].Style.Font.Bold = true;
        worksheet.Cells[row, 1].Style.Font.Size = 14;
        row++;

        if (dashboard.Insights.KnowledgeSilos.Count > 0)
        {
            var siloHeaders = new[] { "Project", "Member Count", "Members" };
            for (int i = 0; i < siloHeaders.Length; i++)
            {
                worksheet.Cells[row, i + 1].Value = siloHeaders[i];
                worksheet.Cells[row, i + 1].Style.Font.Bold = true;
            }

            using (var range = worksheet.Cells[row, 1, row, siloHeaders.Length])
            {
                range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(255, 235, 200));
            }

            row++;
            foreach (var silo in dashboard.Insights.KnowledgeSilos)
            {
                worksheet.Cells[row, 1].Value = silo.ProjectName;
                worksheet.Cells[row, 2].Value = silo.MemberCount;
                worksheet.Cells[row, 3].Value = string.Join(", ", silo.MemberNames);
                row++;
            }
        }
        else
        {
            worksheet.Cells[row, 1].Value = "No knowledge silos detected";
            row++;
        }

        // Section 3: Unengaged Members
        row += 2;
        worksheet.Cells[row, 1].Value = "Unengaged Members";
        worksheet.Cells[row, 1].Style.Font.Bold = true;
        worksheet.Cells[row, 1].Style.Font.Size = 14;
        row++;

        if (dashboard.Insights.UnengagedMembers.Count > 0)
        {
            worksheet.Cells[row, 1].Value = "Member";
            worksheet.Cells[row, 1].Style.Font.Bold = true;

            using (var range = worksheet.Cells[row, 1, row, 1])
            {
                range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(200, 200, 255));
            }

            row++;
            foreach (var member in dashboard.Insights.UnengagedMembers)
            {
                worksheet.Cells[row, 1].Value = member.FullName;
                row++;
            }
        }
        else
        {
            worksheet.Cells[row, 1].Value = "All team members are engaged in projects";
            row++;
        }

        // Section 4: Additional Stats
        row += 2;
        worksheet.Cells[row, 1].Value = "Additional Statistics";
        worksheet.Cells[row, 1].Style.Font.Bold = true;
        worksheet.Cells[row, 1].Style.Font.Size = 14;
        row++;

        worksheet.Cells[row, 1].Value = "Unmatched Tasks (not linked to any project)";
        worksheet.Cells[row, 2].Value = dashboard.Insights.UnmatchedTaskCount;

        // Auto-fit columns
        for (int i = 1; i <= 5; i++)
        {
            worksheet.Column(i).AutoFit();
        }
    }
}
