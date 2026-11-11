using Hive.Application.DTOs;
using Hive.Application.Formatting;
using Hive.Application.Interfaces;
using Hive.Core.Entities;
using Hive.Core.Exceptions;
using Hive.Core.Interfaces;
using TaskStatus = Hive.Core.Entities.TaskStatus;

namespace Hive.Application.Services;

/// <summary>
/// Service for generating analytics and reports.
/// </summary>
public class ReportingService : IReportingService
{
    private readonly IDirectReportRepository _directReportRepository;
    private readonly IOneOnOneMeetingRepository _meetingRepository;
    private readonly ITeamTaskRepository _taskRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly ISprintRepository _sprintRepository;
    private readonly ISprintCapacityRepository _sprintCapacityRepository;
    private readonly IAppSettingsRepository _appSettingsRepository;
    private readonly IParentRepository _parentRepository;
    private readonly ILeaveRepository _leaveRepository;
    private readonly IProductRepository _productRepository;
    private readonly IManagerNoteRepository _noteRepository;
    private readonly INoteFolderRepository _noteFolderRepository;
    private readonly ICareerPlanRepository _careerPlanRepository;
    private readonly ICareerPlanSkillRepository _careerPlanSkillRepository;

    public ReportingService(
        IDirectReportRepository directReportRepository,
        IOneOnOneMeetingRepository meetingRepository,
        ITeamTaskRepository taskRepository,
        IProjectRepository projectRepository,
        ISprintRepository sprintRepository,
        ISprintCapacityRepository sprintCapacityRepository,
        IAppSettingsRepository appSettingsRepository,
        IParentRepository parentRepository,
        ILeaveRepository leaveRepository,
        IProductRepository productRepository,
        IManagerNoteRepository noteRepository,
        INoteFolderRepository noteFolderRepository,
        ICareerPlanRepository careerPlanRepository,
        ICareerPlanSkillRepository careerPlanSkillRepository)
    {
        _directReportRepository = directReportRepository;
        _meetingRepository = meetingRepository;
        _taskRepository = taskRepository;
        _projectRepository = projectRepository;
        _sprintRepository = sprintRepository;
        _sprintCapacityRepository = sprintCapacityRepository;
        _appSettingsRepository = appSettingsRepository;
        _parentRepository = parentRepository;
        _leaveRepository = leaveRepository;
        _productRepository = productRepository;
        _noteRepository = noteRepository;
        _noteFolderRepository = noteFolderRepository;
        _careerPlanRepository = careerPlanRepository;
        _careerPlanSkillRepository = careerPlanSkillRepository;
    }

    /// <summary>
    /// Filters out tasks that are also parents (tasks whose title matches a parent name).
    /// These tasks should not be included in counts and estimations.
    /// </summary>
    private static List<TeamTask> ExcludeParentTasks(IReadOnlyList<TeamTask> tasks, IReadOnlyList<Parent> parents) =>
        WorkMix.ExcludeParentTasks(tasks, parents);

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
        // Each table is read once and shared by every part of the overview
        var allTasks = await _taskRepository.GetAllAsync(cancellationToken);
        var parents = await _parentRepository.GetAllAsync(cancellationToken);
        var sprints = await _sprintRepository.GetAllAsync(cancellationToken);
        var sprintCapacities = await _sprintCapacityRepository.GetAllAsync(cancellationToken);
        var directReports = await _directReportRepository.GetAllAsync(cancellationToken);
        var projects = await _projectRepository.GetAllAsync(cancellationToken);
        var products = await _productRepository.GetAllAsync(cancellationToken);
        var leaves = await _leaveRepository.GetAllAsync(cancellationToken);
        var meetings = await _meetingRepository.GetAllAsync(cancellationToken);
        var appSettings = await _appSettingsRepository.GetAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var quietCareerPlans = await GetQuietCareerPlanNamesAsync(directReports, leaves, now, cancellationToken);

        var tasksOverview = BuildTasksAnalytics(
            allTasks, projects, directReports, parents, sprints, appSettings, sprintCount);

        // Capacity and velocity are also exposed as their own endpoints, but the warning tile
        // must reflect them whether or not their widgets are shown, so the warnings are
        // derived here rather than in the client.
        var capacityAnalysis = BuildCapacityAnalysis(
            sprints, sprintCapacities, allTasks, parents, leaves, directReports, sprintCount);
        var teamVelocity = BuildTeamVelocity(allTasks, sprints, parents, sprintCount);

        var insights = BuildDashboardInsights(
            tasksOverview.TasksByAssignee,
            capacityAnalysis,
            teamVelocity,
            directReports,
            products,
            allTasks,
            parents,
            sprints,
            leaves,
            meetings,
            appSettings,
            quietCareerPlans,
            now);

        return new DashboardOverviewDto
        {
            Team = BuildTeamOverview(directReports),
            Tasks = tasksOverview,
            Insights = insights,
            GeneratedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// How far ahead the focus panel looks for leave.
    /// </summary>
    private const int LeaveLookAheadDays = 14;

    /// <summary>
    /// How far done work may trail, or lead, time gone before a sprint is behind or ahead of pace.
    /// </summary>
    private const int BehindPaceMargin = 15;
    private const int AheadOfPaceMargin = 10;

    private static string SprintPace(bool hasStarted, int completedPercent, int elapsedPercent)
    {
        if (!hasStarted)
            return "NotStarted";
        var lead = completedPercent - elapsedPercent;
        if (lead <= -BehindPaceMargin)
            return "Behind";
        return lead >= AheadOfPaceMargin ? "Ahead" : "OnPace";
    }

    public async Task<DashboardFocusDto> GetDashboardFocusAsync(CancellationToken cancellationToken = default)
    {
        var directReports = await _directReportRepository.GetAllAsync(cancellationToken);
        var meetings = await _meetingRepository.GetAllAsync(cancellationToken);
        var leaves = await _leaveRepository.GetAllAsync(cancellationToken);
        var appSettings = await _appSettingsRepository.GetAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);

        return new DashboardFocusDto
        {
            Today = today,
            CurrentSprint = await BuildCurrentSprintFocusAsync(directReports, appSettings, now.Date, cancellationToken),
            OneOnOnes = BuildOneOnOneCadence(
                directReports,
                meetings,
                leaves,
                appSettings?.OneOnOneCadenceDays ?? OneOnOneCadence.DefaultDays,
                today),
            Todos = await BuildTodoFocusAsync(cancellationToken),
            Leave = BuildLeaveFocus(leaves, directReports, now.Date),
            TenureMilestones = BuildTenureMilestones(directReports, today)
        };
    }

    /// <summary>
    /// Everyone reaching a tenure milestone within the week, soonest first.
    /// </summary>
    private static List<TenureMilestoneFocusDto> BuildTenureMilestones(
        IReadOnlyList<DirectReport> directReports,
        DateOnly today) =>
        directReports
            .Select(dr => (Person: dr, HireDate: DateOnly.FromDateTime(dr.HireDate)))
            .Select(p => (p.Person, p.HireDate, Milestone: TenureMilestone.Visible(p.HireDate, today)))
            .Where(p => p.Milestone != null)
            .Select(p => new TenureMilestoneFocusDto
            {
                DirectReportId = p.Person.Id,
                FullName = p.Person.FullName,
                HireDate = p.HireDate,
                Kind = p.Milestone!.Kind.ToString(),
                Years = p.Milestone.Years,
                Date = p.Milestone.Date,
                DaysUntil = p.Milestone.DaysUntil(today)
            })
            .OrderBy(m => m.Date)
            .ThenBy(m => m.FullName)
            .ToList();

    private async Task<CurrentSprintFocusDto?> BuildCurrentSprintFocusAsync(
        IReadOnlyList<DirectReport> directReports,
        AppSettings? appSettings,
        DateTime today,
        CancellationToken cancellationToken)
    {
        var sprints = await _sprintRepository.GetAllAsync(cancellationToken);
        var sprint = Sprint.FindCurrent(sprints, today);
        if (sprint == null)
            return null;

        var allTasks = await _taskRepository.GetAllAsync(cancellationToken);
        var parents = await _parentRepository.GetAllAsync(cancellationToken);
        var capacities = await _sprintCapacityRepository.GetAllAsync(cancellationToken);

        // A task belongs to the latest sprint it is in, the same rule as capacity and velocity
        var tasks = ExcludeParentTasks(allTasks, parents)
            .Where(t => !string.IsNullOrEmpty(t.Sprint) &&
                        string.Equals(GetLatestSprintFromTask(t.Sprint), sprint.Name, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var start = sprint.GetEstimatedStartDate().Date;
        var end = sprint.GetEstimatedEndDate().Date;
        var isActive = sprint.ContainsDate(today);
        var workingDays = GetWorkingDays(start, end);
        var workingDaysLeft = today > end ? 0 : GetWorkingDays(today < start ? start : today, end);
        var elapsedPercent = workingDays > 0
            ? (int)Math.Round((double)(workingDays - workingDaysLeft) / workingDays * 100)
            : 0;

        var newPoints = tasks.Sum(NewStoryPoints);
        var newCompletedPoints = tasks.Where(t => t.Status == TaskStatus.Done).Sum(NewStoryPoints);
        var completedPercent = newPoints > 0 ? (int)Math.Round((double)newCompletedPoints / newPoints * 100) : 0;
        var totalPoints = tasks.Sum(t => t.StoryPoints ?? 0);
        var committedPoints = capacities.FirstOrDefault(c => c.SprintId == sprint.Id)?.TotalCapacityPoints ?? 0;

        var names = directReports.ToDictionary(dr => dr.Id, dr => dr.FullName);
        var browseUrl = appSettings?.GetJiraBrowseUrl();

        return new CurrentSprintFocusDto
        {
            SprintId = sprint.Id,
            Name = sprint.Name,
            StartDate = DateOnly.FromDateTime(start),
            EndDate = DateOnly.FromDateTime(end),
            IsActive = isActive,
            WorkingDays = workingDays,
            WorkingDaysLeft = workingDaysLeft,
            ElapsedPercent = elapsedPercent,
            CommittedPoints = committedPoints,
            NewPoints = newPoints,
            NewCompletedPoints = newCompletedPoints,
            CarriedOverPoints = tasks.Sum(t => t.PreviousSprintsStoryPoints ?? 0),
            CompletedPercent = completedPercent,
            Pace = SprintPace(today >= start, completedPercent, elapsedPercent),
            ScopeCreepPoints = committedPoints > 0 ? Math.Max(0, totalPoints - committedPoints) : 0,
            TotalTasks = tasks.Count,
            DoneTasks = tasks.Count(t => t.Status == TaskStatus.Done),
            ByStatus = Enum.GetValues<TaskStatus>()
                .Select(status => new SprintStatusCountDto
                {
                    Status = status,
                    StatusName = status.ToString(),
                    TaskCount = tasks.Count(t => t.Status == status),
                    StoryPoints = tasks.Where(t => t.Status == status).Sum(NewStoryPoints)
                })
                // Blocked is always listed, so "none blocked" reads as a fact rather than an absence
                .Where(c => c.TaskCount > 0 || c.Status == TaskStatus.Blocked)
                .ToList(),
            BlockedTasks = tasks
                .Where(t => t.Status == TaskStatus.Blocked)
                .OrderByDescending(t => t.StoryPoints ?? 0)
                .ThenBy(t => t.Title)
                .Select(t =>
                {
                    var key = t.GetJiraKey();
                    return new FocusTaskDto
                    {
                        Id = t.Id,
                        Title = t.Title,
                        JiraKey = key,
                        JiraUrl = key != null && browseUrl != null ? $"{browseUrl}/{key}" : null,
                        AssigneeName = t.AssigneeId.HasValue && names.TryGetValue(t.AssigneeId.Value, out var name) ? name : null,
                        StoryPoints = t.StoryPoints
                    };
                })
                .ToList()
        };
    }

    /// <summary>
    /// Every direct report (<c>IsDirect</c>) against the 1:1 cadence, most urgent first. Only a
    /// 1:1 held on or before today counts; one dated in the future has not happened yet.
    /// </summary>
    private static OneOnOneCadenceOverviewDto BuildOneOnOneCadence(
        IReadOnlyList<DirectReport> directReports,
        IReadOnlyList<OneOnOneMeeting> meetings,
        IReadOnlyList<Leave> leaves,
        int cadenceDays,
        DateOnly today)
    {
        var lastMeetingByPerson = meetings
            .Where(m => m.DirectReportId.HasValue && m.MeetingDate <= today)
            .GroupBy(m => m.DirectReportId!.Value)
            .ToDictionary(g => g.Key, g => g.Max(m => m.MeetingDate));

        var todayDate = today.ToDateTime(TimeOnly.MinValue);
        var onLeave = leaves
            .Where(l => l.Status == LeaveStatus.Active && l.IncludesDate(todayDate))
            .Select(l => l.DirectReportId)
            .ToHashSet();

        var people = directReports
            .Where(dr => dr.IsDirect)
            .Select(dr =>
            {
                DateOnly? last = lastMeetingByPerson.TryGetValue(dr.Id, out var date) ? date : null;
                int? daysSince = last.HasValue ? today.DayNumber - last.Value.DayNumber : null;
                var status = OneOnOneCadence.Evaluate(last, today, cadenceDays);
                return new OneOnOneCadenceDto
                {
                    DirectReportId = dr.Id,
                    FullName = dr.FullName,
                    LastMeetingDate = last,
                    DaysSinceLastMeeting = daysSince,
                    DaysUntilDue = daysSince.HasValue ? cadenceDays - daysSince.Value : null,
                    Status = status.ToString(),
                    IsOnLeave = onLeave.Contains(dr.Id)
                };
            })
            // Never met first, then the longest overdue, so the list reads as a to-do list
            .OrderBy(p => p.DaysUntilDue.HasValue ? 1 : 0)
            .ThenBy(p => p.DaysUntilDue ?? 0)
            .ThenBy(p => p.FullName)
            .ToList();

        return new OneOnOneCadenceOverviewDto
        {
            CadenceDays = cadenceDays,
            NeedsAttentionCount = people.Count(p =>
                !p.IsOnLeave && OneOnOneCadence.NeedsAttention(Enum.Parse<OneOnOneCadenceStatus>(p.Status))),
            People = people
        };
    }

    /// <summary>
    /// The to-dos are the "hot" bullets of the latest journal entry: the note in the Journal
    /// folder whose title sorts last.
    /// </summary>
    private async Task<TodoFocusDto> BuildTodoFocusAsync(CancellationToken cancellationToken)
    {
        var journals = (await _noteFolderRepository.GetAllAsync(cancellationToken))
            .Where(f => f.IsJournal)
            .ToList();

        var journalNotes = new List<ManagerNote>();
        foreach (var journal in journals)
        {
            journalNotes.AddRange(await _noteRepository.GetByFolderAsync(journal.Id, cancellationToken));
        }

        var latest = ManagerNote.FindLatestJournalEntry(journalNotes);

        return new TodoFocusDto
        {
            HasJournal = journals.Count > 0,
            NoteId = latest?.Id,
            NoteTitle = latest?.Title,
            Items = latest?.GetHotItems() ?? []
        };
    }

    private static LeaveFocusDto BuildLeaveFocus(
        IReadOnlyList<Leave> leaves,
        IReadOnlyList<DirectReport> directReports,
        DateTime today)
    {
        var names = directReports.ToDictionary(dr => dr.Id, dr => dr.FullName);
        var horizon = today.AddDays(LeaveLookAheadDays);
        var active = leaves
            .Where(l => l.Status == LeaveStatus.Active && names.ContainsKey(l.DirectReportId))
            .ToList();

        LeaveFocusItemDto Map(Leave l) => new()
        {
            LeaveId = l.Id,
            DirectReportId = l.DirectReportId,
            FullName = names[l.DirectReportId],
            Type = l.Type.ToString(),
            StartDate = DateOnly.FromDateTime(l.StartDate),
            EndDate = DateOnly.FromDateTime(l.EndDate),
            BusinessDays = l.BusinessDaysCount
        };

        return new LeaveFocusDto
        {
            LookAheadDays = LeaveLookAheadDays,
            OutToday = active
                .Where(l => l.IncludesDate(today))
                .OrderBy(l => l.EndDate)
                .ThenBy(l => names[l.DirectReportId])
                .Select(Map)
                .ToList(),
            Upcoming = active
                .Where(l => l.StartDate.Date > today && l.StartDate.Date <= horizon)
                .OrderBy(l => l.StartDate)
                .ThenBy(l => names[l.DirectReportId])
                .Select(Map)
                .ToList()
        };
    }

    private static DashboardInsightsDto BuildDashboardInsights(
        IReadOnlyList<TasksByAssigneeDto> tasksByAssignee,
        CapacityAnalysisDto capacityAnalysis,
        TeamVelocityDto teamVelocity,
        IReadOnlyList<DirectReport> directReports,
        IReadOnlyList<Product> products,
        IReadOnlyList<TeamTask> allTasks,
        IReadOnlyList<Parent> parents,
        IReadOnlyList<Sprint> sprints,
        IReadOnlyList<Leave> leaves,
        IReadOnlyList<OneOnOneMeeting> meetings,
        AppSettings? appSettings,
        IReadOnlyList<string> quietCareerPlans,
        DateTime now)
    {
        var tasks = ExcludeParentTasks(allTasks, parents);

        // Get configurable thresholds (use defaults if settings not found)
        var maxInProgress = appSettings?.MaxInProgressTasks ?? 2;
        var maxBlocked = appSettings?.MaxBlockedTasks ?? 1;
        var maxInReview = appSettings?.MaxInReviewTasks ?? 1;
        var minProductMembers = appSettings?.MinProductMembers ?? 2;
        var minMemberProducts = appSettings?.MinMemberProducts ?? AppSettings.DefaultMinMemberProducts;

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

        // 2. Product engagement, over all time: the warnings do not follow the period chosen on
        // the Product engagement widget, so the warning count never depends on it.
        var today = now.Date;
        var engagement = BuildProductEngagement(
            allTasks, products, directReports, sprints, leaves, minProductMembers, minMemberProducts, months: null, today);

        // 3. Unmatched task count (tasks whose components name no product)
        var unmatchedTaskCount = tasks.Count(task => !products.Any(p => p.IsNamedIn(task.Components)));

        var oneOnOnes = BuildOneOnOneCadence(
            directReports,
            meetings,
            leaves,
            appSettings?.OneOnOneCadenceDays ?? OneOnOneCadence.DefaultDays,
            DateOnly.FromDateTime(today));
        var overdueTasks = tasks
            .Where(t => t.IsOverdue())
            .OrderBy(t => t.DueDate)
            .ToList();
        var staleTaskDays = appSettings?.StaleTaskDays ?? TeamTask.DefaultStaleAfterDays;
        var staleTasks = tasks
            .Where(t => t.IsStale(now, staleTaskDays))
            .OrderBy(t => t.StatusChangedAt)
            .ToList();

        // Spikes and support must not be estimated; every other task must be. Every issue
        // counts, a parent included: a support ticket with sub-tasks is still support.
        var estimatedSpikes = allTasks.Where(t => t.Type == TaskType.Spike && t.HasUnexpectedStoryPoints()).ToList();
        var estimatedSupport = allTasks.Where(t => t.Type == TaskType.Support && t.HasUnexpectedStoryPoints()).ToList();
        // A parent whose children have story points is sized by them; work under a spike or
        // a support ticket is not estimated
        var unestimatedTasks = TeamTask.FindMissingStoryPoints(allTasks, parents, directReports);

        // A fact, not a model's judgement: the stories and tasks about to be built whose
        // description has no acceptance criteria
        var missingAcceptanceCriteria = TaskReadiness.FindUpcoming(allTasks, sprints, today)
            .Where(t => t.IsMissingAcceptanceCriteria())
            .ToList();

        var warnings = BuildDashboardWarnings(
            oneOnOnes,
            overdueTasks,
            staleTasks,
            staleTaskDays,
            now,
            workloadWarnings,
            engagement.UnworkedProducts,
            engagement.KnowledgeSilos,
            engagement.UnengagedMembers,
            engagement.NarrowlyEngagedMembers,
            engagement.MinMemberProducts,
            quietCareerPlans,
            unmatchedTaskCount,
            estimatedSpikes,
            estimatedSupport,
            unestimatedTasks,
            missingAcceptanceCriteria,
            capacityAnalysis,
            teamVelocity);

        return new DashboardInsightsDto
        {
            WorkloadWarnings = workloadWarnings,
            KnowledgeSilos = engagement.KnowledgeSilos,
            UnengagedMembers = engagement.UnengagedMembers,
            NarrowlyEngagedMembers = engagement.NarrowlyEngagedMembers,
            UnworkedProducts = engagement.UnworkedProducts,
            UnmatchedTaskCount = unmatchedTaskCount,
            Warnings = warnings
        };
    }

    /// <summary>
    /// The names of direct reports whose career plan has gone quiet: nothing on it changed for
    /// <see cref="CareerPlan.QuietDays"/> days. Someone on leave today is not flagged.
    /// </summary>
    private async Task<IReadOnlyList<string>> GetQuietCareerPlanNamesAsync(
        IReadOnlyList<DirectReport> directReports,
        IReadOnlyList<Leave> leaves,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var plans = await _careerPlanRepository.GetAllAsync(cancellationToken);
        if (plans.Count == 0) return [];

        var skills = (await _careerPlanSkillRepository.GetAllAsync(cancellationToken)).ToLookup(s => s.CareerPlanId);
        var names = directReports.ToDictionary(dr => dr.Id, dr => dr.FullName);
        var onLeaveToday = leaves
            .Where(l => l.Status == LeaveStatus.Active && l.IncludesDate(now.Date))
            .Select(l => l.DirectReportId)
            .ToHashSet();

        return plans
            .Where(p => !onLeaveToday.Contains(p.DirectReportId)
                        && p.IsQuiet(skills[p.Id].Max(s => s.UpdatedAt), now))
            .Select(p => names.GetValueOrDefault(p.DirectReportId, "Unknown"))
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Threshold below which average capacity utilization is reported as a warning.
    /// </summary>
    private const double LowUtilizationThreshold = 75;

    /// <summary>
    /// A task's story points for the sprint it is in: its points less any already delivered in
    /// earlier sprints, so work carried over is not counted twice.
    /// </summary>
    private static int NewStoryPoints(TeamTask task) => WorkMix.NewStoryPoints(task);

    public async Task<ProductEngagementDto> GetProductEngagementAsync(int? months = null, CancellationToken cancellationToken = default)
    {
        if (months is <= 0)
            throw new DomainException("The period must be a positive number of months.");

        var directReports = await _directReportRepository.GetAllAsync(cancellationToken);
        var products = await _productRepository.GetAllAsync(cancellationToken);
        var tasks = await _taskRepository.GetAllAsync(cancellationToken);
        var sprints = await _sprintRepository.GetAllAsync(cancellationToken);
        var leaves = await _leaveRepository.GetAllAsync(cancellationToken);
        var appSettings = await _appSettingsRepository.GetAsync(cancellationToken);

        return BuildProductEngagement(
            tasks,
            products,
            directReports,
            sprints,
            leaves,
            appSettings?.MinProductMembers ?? 2,
            appSettings?.MinMemberProducts ?? AppSettings.DefaultMinMemberProducts,
            months,
            DateTime.UtcNow.Date);
    }

    /// <summary>
    /// Who has worked on which product. A task belongs to every product its components name,
    /// and its assignee has worked on each of them. Only direct reports count, and every task
    /// but an epic: an epic's components say what it is about, not who did the work. A parent
    /// that is not an epic is real work and counts.
    /// <para>
    /// With <paramref name="months"/>, a task counts only when one of its sprints overlaps the
    /// last that many months, so planned work in a future sprint and a task in no known sprint
    /// do not count. Members are keyed by id, so two people who share a name are two people.
    /// </para>
    /// </summary>
    private static ProductEngagementDto BuildProductEngagement(
        IReadOnlyList<TeamTask> tasks,
        IReadOnlyList<Product> products,
        IReadOnlyList<DirectReport> directReports,
        IReadOnlyList<Sprint> sprints,
        IReadOnlyList<Leave> leaves,
        int minProductMembers,
        int minMemberProducts,
        int? months,
        DateTime today)
    {
        var team = directReports.Where(dr => dr.IsDirect).ToDictionary(dr => dr.Id);

        var since = months.HasValue ? today.AddMonths(-months.Value) : (DateTime?)null;
        var sprintsByName = sprints
            .GroupBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        bool InPeriod(TeamTask task) =>
            since is null ||
            task.GetSprintNames().Any(name =>
                sprintsByName.TryGetValue(name, out var sprint) &&
                sprint.GetEstimatedStartDate().Date <= today &&
                sprint.GetEstimatedEndDate().Date >= since.Value);

        var membersByProduct = products.ToDictionary(p => p.Id, _ => new HashSet<Guid>());
        var productsByMember = new Dictionary<Guid, HashSet<Guid>>();
        var countedTasks = tasks.Where(t =>
            t.Type != TaskType.Epic &&
            t.AssigneeId.HasValue &&
            team.ContainsKey(t.AssigneeId.Value) &&
            InPeriod(t));
        foreach (var task in countedTasks)
        {
            var memberId = task.AssigneeId!.Value;
            foreach (var product in products.Where(p => p.IsNamedIn(task.Components)))
            {
                membersByProduct[product.Id].Add(memberId);
                if (!productsByMember.TryGetValue(memberId, out var memberProducts))
                    productsByMember[memberId] = memberProducts = new HashSet<Guid>();
                memberProducts.Add(product.Id);
            }
        }

        var productsById = products.ToDictionary(p => p.Id);
        List<string> MemberNames(HashSet<Guid> ids) => ids.Select(id => team[id].FullName).OrderBy(n => n).ToList();

        var unworkedProducts = membersByProduct
            .Where(kvp => kvp.Value.Count == 0)
            .Select(kvp => new UnworkedProductDto { ProductId = kvp.Key, ProductName = productsById[kvp.Key].Name })
            .OrderBy(p => p.ProductName)
            .ToList();

        var knowledgeSilos = membersByProduct
            .Where(kvp => kvp.Value.Count > 0 && kvp.Value.Count < minProductMembers)
            .Select(kvp => new KnowledgeSiloDto
            {
                ProductId = kvp.Key,
                ProductName = productsById[kvp.Key].Name,
                MemberCount = kvp.Value.Count,
                MemberNames = MemberNames(kvp.Value)
            })
            .OrderBy(s => s.ProductName)
            .ToList();

        var onLeaveToday = leaves
            .Where(l => l.StartDate.Date <= today && l.EndDate.Date >= today)
            .Select(l => l.DirectReportId)
            .ToHashSet();

        return new ProductEngagementDto
        {
            Months = months,
            MembersByProduct = membersByProduct
                .Select(kvp => new ProductMembershipDto
                {
                    ProductId = kvp.Key,
                    ProductName = productsById[kvp.Key].Name,
                    MemberCount = kvp.Value.Count,
                    MemberNames = MemberNames(kvp.Value),
                    IsSilo = kvp.Value.Count < minProductMembers
                })
                .OrderBy(p => p.MemberCount)
                .ThenBy(p => p.ProductName)
                .ToList(),
            ProductsByMember = productsByMember
                .Select(kvp => new MemberProductsDto
                {
                    DirectReportId = kvp.Key,
                    MemberName = team[kvp.Key].FullName,
                    ProductCount = kvp.Value.Count,
                    ProductIds = kvp.Value.ToList(),
                    IsNarrow = kvp.Value.Count < minMemberProducts
                })
                .OrderByDescending(m => m.ProductCount)
                .ThenBy(m => m.MemberName)
                .ToList(),
            KnowledgeSilos = knowledgeSilos,
            UnworkedProducts = unworkedProducts,
            // Someone on leave today is not flagged: they cannot pick up product work right now.
            UnengagedMembers = team.Values
                .Where(dr => !productsByMember.ContainsKey(dr.Id) && !onLeaveToday.Contains(dr.Id))
                .Select(dr => new UnengagedMemberDto { DirectReportId = dr.Id, FullName = dr.FullName })
                .ToList(),
            // Someone who has worked on a product, but on too few. Leave excuses it, as above.
            NarrowlyEngagedMembers = productsByMember
                .Where(kvp => kvp.Value.Count < minMemberProducts && !onLeaveToday.Contains(kvp.Key))
                .Select(kvp => new NarrowlyEngagedMemberDto
                {
                    DirectReportId = kvp.Key,
                    FullName = team[kvp.Key].FullName,
                    ProductCount = kvp.Value.Count
                })
                .OrderBy(m => m.ProductCount)
                .ThenBy(m => m.FullName)
                .ToList(),
            MinProductMembers = minProductMembers,
            MinMemberProducts = minMemberProducts
        };
    }

    /// <summary>
    /// Collects every dashboard warning into one list. This is the authority on the warning
    /// count: deriving it in the client made the total depend on which widgets happened to be
    /// visible, because the velocity and capacity data were only fetched for a
    /// visible widget.
    /// </summary>
    private static List<DashboardWarningDto> BuildDashboardWarnings(
        OneOnOneCadenceOverviewDto oneOnOnes,
        IReadOnlyList<TeamTask> overdueTasks,
        IReadOnlyList<TeamTask> staleTasks,
        int staleTaskDays,
        DateTime now,
        IReadOnlyList<WorkloadWarningDto> workloadWarnings,
        IReadOnlyList<UnworkedProductDto> unworkedProducts,
        IReadOnlyList<KnowledgeSiloDto> knowledgeSilos,
        IReadOnlyList<UnengagedMemberDto> unengagedMembers,
        IReadOnlyList<NarrowlyEngagedMemberDto> narrowlyEngagedMembers,
        int minMemberProducts,
        IReadOnlyList<string> quietCareerPlans,
        int unmatchedTaskCount,
        IReadOnlyList<TeamTask> estimatedSpikes,
        IReadOnlyList<TeamTask> estimatedSupport,
        IReadOnlyList<TeamTask> unestimatedTasks,
        IReadOnlyList<TeamTask> missingAcceptanceCriteria,
        CapacityAnalysisDto capacityAnalysis,
        TeamVelocityDto teamVelocity)
    {
        var warnings = new List<DashboardWarningDto>();

        // People first: a 1:1 that is overdue is the manager's own work, not the team's.
        // Someone on leave today cannot be met, so they are not counted.
        var dueOneOnOnes = oneOnOnes.People
            .Where(p => !p.IsOnLeave && OneOnOneCadence.NeedsAttention(Enum.Parse<OneOnOneCadenceStatus>(p.Status)))
            .ToList();
        if (dueOneOnOnes.Count > 0)
        {
            var names = dueOneOnOnes.Select(p => p.FullName).ToList();
            warnings.Add(new DashboardWarningDto
            {
                Category = "OneOnOnes",
                Title = dueOneOnOnes.Count == 1
                    ? "1 overdue 1:1"
                    : $"{dueOneOnOnes.Count} overdue 1:1s",
                Detail = names.Count > 3
                    ? $"{string.Join(", ", names.Take(3))} and {names.Count - 3} more"
                    : string.Join(", ", names)
            });
        }

        // One warning per overloaded person.
        foreach (var w in workloadWarnings)
        {
            warnings.Add(new DashboardWarningDto
            {
                Category = "Workload",
                Title = $"{w.AssigneeName} is overloaded",
                Detail = string.Join(", ", w.Issues)
            });
        }

        if (unengagedMembers.Count > 0)
        {
            var names = unengagedMembers.Select(u => u.FullName).ToList();
            warnings.Add(new DashboardWarningDto
            {
                Category = "UnengagedMembers",
                Title = unengagedMembers.Count == 1
                    ? "1 team member is not engaged in any product"
                    : $"{unengagedMembers.Count} team members are not engaged in any product",
                Detail = names.Count > 3
                    ? $"{string.Join(", ", names.Take(3))} and {names.Count - 3} more"
                    : string.Join(", ", names)
            });
        }

        if (narrowlyEngagedMembers.Count > 0)
        {
            var names = narrowlyEngagedMembers.Select(m => m.FullName).ToList();
            warnings.Add(new DashboardWarningDto
            {
                Category = "NarrowlyEngagedMembers",
                Title = narrowlyEngagedMembers.Count == 1
                    ? $"1 team member has worked on fewer than {minMemberProducts} products"
                    : $"{narrowlyEngagedMembers.Count} team members have worked on fewer than {minMemberProducts} products",
                Detail = names.Count > 3
                    ? $"{string.Join(", ", names.Take(3))} and {names.Count - 3} more"
                    : string.Join(", ", names)
            });
        }

        if (quietCareerPlans.Count > 0)
        {
            warnings.Add(new DashboardWarningDto
            {
                Category = "CareerPlans",
                Title = quietCareerPlans.Count == 1
                    ? "1 career plan has gone quiet"
                    : $"{quietCareerPlans.Count} career plans have gone quiet",
                Detail = (quietCareerPlans.Count > 3
                    ? $"{string.Join(", ", quietCareerPlans.Take(3))} and {quietCareerPlans.Count - 3} more"
                    : string.Join(", ", quietCareerPlans))
                    + $": nothing changed in {CareerPlan.QuietDays} days"
            });
        }

        // Delivery risks. Overdue tasks lead them: a missed date is already a problem, the rest
        // are only ways one might happen. All-time, like the rest of the insights.
        if (overdueTasks.Count > 0)
        {
            var titles = overdueTasks.Select(t => $"{t.Title} (due {t.DueDate!.Value.ToDisplayDate()})").ToList();
            warnings.Add(new DashboardWarningDto
            {
                Category = "OverdueTasks",
                Title = overdueTasks.Count == 1 ? "1 overdue task" : $"{overdueTasks.Count} overdue tasks",
                Detail = titles.Count > 2
                    ? $"{string.Join(", ", titles.Take(2))} and {titles.Count - 2} more"
                    : string.Join(", ", titles)
            });
        }

        // Work that has sat in one status since Hive last saw it change, oldest first
        if (staleTasks.Count > 0)
        {
            var titles = staleTasks
                .Select(t => $"{t.Title} ({t.Status}, {t.DaysInStatus(now)} days)")
                .ToList();
            warnings.Add(new DashboardWarningDto
            {
                Category = "StaleTasks",
                Title = staleTasks.Count == 1
                    ? $"1 task stuck for {staleTaskDays}+ days"
                    : $"{staleTasks.Count} tasks stuck for {staleTaskDays}+ days",
                Detail = titles.Count > 2
                    ? $"{string.Join(", ", titles.Take(2))} and {titles.Count - 2} more"
                    : string.Join(", ", titles)
            });
        }

        // A product nobody has worked on is the worst bus factor there is, so it leads the
        // product warnings rather than hiding among the silos.
        if (unworkedProducts.Count > 0)
        {
            var names = unworkedProducts.Select(p => p.ProductName).ToList();
            warnings.Add(new DashboardWarningDto
            {
                Category = "UnworkedProducts",
                Title = unworkedProducts.Count == 1
                    ? "1 product nobody has worked on"
                    : $"{unworkedProducts.Count} products nobody has worked on",
                Detail = names.Count > 3
                    ? $"{string.Join(", ", names.Take(3))} and {names.Count - 3} more"
                    : string.Join(", ", names)
            });
        }

        if (knowledgeSilos.Count > 0)
        {
            warnings.Add(new DashboardWarningDto
            {
                Category = "KnowledgeSilos",
                Title = knowledgeSilos.Count == 1
                    ? "1 product is a knowledge silo"
                    : $"{knowledgeSilos.Count} products are knowledge silos",
                Detail = string.Join(", ", knowledgeSilos.Take(3).Select(s => s.ProductName))
            });
        }

        var currentSprint = capacityAnalysis.CurrentSprint;
        if (currentSprint != null && currentSprint.CommittedPoints > 0 &&
            currentSprint.TotalStoryPoints > currentSprint.CommittedPoints)
        {
            var creep = currentSprint.TotalStoryPoints - currentSprint.CommittedPoints;
            warnings.Add(new DashboardWarningDto
            {
                Category = "ScopeCreep",
                Title = $"{creep} SP of scope creep in {currentSprint.SprintName}",
                Detail = $"{currentSprint.TotalStoryPoints} SP in the sprint against {currentSprint.CommittedPoints} SP committed"
            });
        }

        if (unmatchedTaskCount > 0)
        {
            warnings.Add(new DashboardWarningDto
            {
                Category = "UnmatchedTasks",
                Title = unmatchedTaskCount == 1
                    ? "1 task is not attributed to a product"
                    : $"{unmatchedTaskCount} tasks are not attributed to a product",
                Detail = "Their components name no product, so they are missing from every product breakdown"
            });
        }

        AddEstimatedTypeWarning(warnings, "EstimatedSpikes", estimatedSpikes, "spike", "spikes");
        AddEstimatedTypeWarning(warnings, "EstimatedSupport", estimatedSupport, "support ticket", "support tickets");

        if (unestimatedTasks.Count > 0)
        {
            var titles = unestimatedTasks.Select(t => $"{t.Title} ({t.Type})").ToList();
            warnings.Add(new DashboardWarningDto
            {
                Category = "UnestimatedTasks",
                Title = unestimatedTasks.Count == 1
                    ? "1 task has no story points"
                    : $"{unestimatedTasks.Count} tasks have no story points",
                Detail = titles.Count > 2
                    ? $"{string.Join(", ", titles.Take(2))} and {titles.Count - 2} more"
                    : string.Join(", ", titles)
            });
        }

        if (missingAcceptanceCriteria.Count > 0)
        {
            var titles = missingAcceptanceCriteria.Select(t => t.Title).ToList();
            warnings.Add(new DashboardWarningDto
            {
                Category = "MissingAcceptanceCriteria",
                Title = missingAcceptanceCriteria.Count == 1
                    ? "1 upcoming task has no acceptance criteria"
                    : $"{missingAcceptanceCriteria.Count} upcoming tasks have no acceptance criteria",
                Detail = titles.Count > 2
                    ? $"{string.Join(", ", titles.Take(2))} and {titles.Count - 2} more"
                    : string.Join(", ", titles)
            });
        }

        // Only meaningful once there is at least one sprint to measure; an empty database
        // reports 0% utilization, which is an absence of data rather than a warning.
        var hasCapacityData = capacityAnalysis.PastSprints.Count > 0 ||
                              capacityAnalysis.CurrentSprint != null ||
                              capacityAnalysis.FutureSprints.Count > 0;
        if (hasCapacityData && capacityAnalysis.AverageUtilization < LowUtilizationThreshold)
        {
            warnings.Add(new DashboardWarningDto
            {
                Category = "LowUtilization",
                Title = "Low capacity utilization",
                Detail = $"Average utilization is {capacityAnalysis.AverageUtilization}%, below {LowUtilizationThreshold}%"
            });
        }

        if (teamVelocity.CompletionTrend < 0)
        {
            warnings.Add(new DashboardWarningDto
            {
                Category = "VelocityTrend",
                Title = "Velocity is trending down",
                Detail = $"{teamVelocity.CompletionTrend}% against the previous sprint"
            });
        }

        return warnings;
    }

    private static void AddEstimatedTypeWarning(
        List<DashboardWarningDto> warnings,
        string category,
        IReadOnlyList<TeamTask> estimatedTasks,
        string singular,
        string plural)
    {
        if (estimatedTasks.Count == 0)
            return;

        var titles = estimatedTasks
            .OrderByDescending(t => t.StoryPoints)
            .Select(t => $"{t.Title} ({t.StoryPoints} SP)")
            .ToList();
        warnings.Add(new DashboardWarningDto
        {
            Category = category,
            Title = estimatedTasks.Count == 1
                ? $"1 {singular} has story points"
                : $"{estimatedTasks.Count} {plural} have story points",
            Detail = titles.Count > 2
                ? $"{string.Join(", ", titles.Take(2))} and {titles.Count - 2} more"
                : string.Join(", ", titles)
        });
    }

    private static TeamOverviewDto BuildTeamOverview(IReadOnlyList<DirectReport> allReports)
    {
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

    public async Task<TasksOverviewDto> GetTasksAnalyticsAsync(int? sprintCount = null, CancellationToken cancellationToken = default)
    {
        var allTasks = await _taskRepository.GetAllAsync(cancellationToken);
        var projects = await _projectRepository.GetAllAsync(cancellationToken);
        var directReports = await _directReportRepository.GetAllAsync(cancellationToken);
        var parents = await _parentRepository.GetAllAsync(cancellationToken);
        var appSettings = await _appSettingsRepository.GetAsync(cancellationToken);
        var allSprints = await _sprintRepository.GetAllAsync(cancellationToken);

        return BuildTasksAnalytics(allTasks, projects, directReports, parents, allSprints, appSettings, sprintCount);
    }

    private static TasksOverviewDto BuildTasksAnalytics(
        IReadOnlyList<TeamTask> allTasks,
        IReadOnlyList<Project> projects,
        IReadOnlyList<DirectReport> directReports,
        IReadOnlyList<Parent> parents,
        IReadOnlyList<Sprint> allSprints,
        AppSettings? appSettings,
        int? sprintCount)
    {
        // Exclude tasks that are also parents from counts and estimations
        var filteredTasks = ExcludeParentTasks(allTasks, parents);

        // Filter tasks by sprint count if requested
        var tasks = filteredTasks.AsReadOnly() as IReadOnlyList<TeamTask> ?? filteredTasks;
        if (sprintCount.HasValue && sprintCount.Value > 0)
        {
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
                    BacklogTasks = assigneeTasks.Count(t => t.Status == TaskStatus.Backlog),
                    TodoTasks = assigneeTasks.Count(t => t.Status == TaskStatus.Todo),
                    BlockedTasks = blocked,
                    InProgressTasks = inProgress,
                    InReviewTasks = inReview,
                    InTestTasks = assigneeTasks.Count(t => t.Status == TaskStatus.InTest),
                    POAcceptanceTasks = assigneeTasks.Count(t => t.Status == TaskStatus.POAcceptance),
                    ReadyToReleaseTasks = assigneeTasks.Count(t => t.Status == TaskStatus.ReadyToRelease),
                    CompletedTasks = completed,
                    CancelledTasks = assigneeTasks.Count(t => t.Status == TaskStatus.Cancelled),
                    OverdueTasks = overdue,
                    CompletionRate = assigneeTasks.Count > 0 ? Math.Round((double)completed / assigneeTasks.Count * 100, 1) : 0,
                    TotalStoryPoints = assigneeTasks.Sum(NewStoryPoints),
                    CompletedStoryPoints = assigneeTasks.Where(t => t.Status == TaskStatus.Done).Sum(NewStoryPoints)
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
            // Every type with tasks, even unestimated ones: bugs, support and spikes often carry no points
            .Where(t => t.TaskCount > 0)
            .ToList();

        // Support is recognised by its Jira issue type, maintenance by the configured labels
        var maintenanceLabels = DeserializeLabels(appSettings?.MaintenanceLabels);

        // Calculate support distribution from support tickets (respects sprint filter)
        var allSupportTasks = tasks
            .Where(t => t.Type == TaskType.Support)
            .ToList();
        var completedSupportTasks = allSupportTasks
            .Where(t => t.Status == TaskStatus.Done)
            .ToList();

        // Calculate maintenance distribution from tasks matching configured maintenance labels (respects sprint filter)
        var allMaintenanceTasks = tasks
            .Where(t => ContainsAnyLabel(t, maintenanceLabels))
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
                    CompletedStoryPoints = supportCompleted.Sum(NewStoryPoints),
                    CompletedTaskCount = supportCompleted.Count,
                    AllStoryPoints = supportTasks.Sum(NewStoryPoints),
                    AllTaskCount = supportTasks.Count,
                    MaintenanceCompletedStoryPoints = maintCompleted.Sum(NewStoryPoints),
                    MaintenanceCompletedTaskCount = maintCompleted.Count,
                    MaintenanceAllStoryPoints = maintTasks.Sum(NewStoryPoints),
                    MaintenanceAllTaskCount = maintTasks.Count
                };
            })
            .Where(s => s.AllTaskCount > 0 || s.MaintenanceAllTaskCount > 0)
            .OrderByDescending(s => s.AllStoryPoints + s.MaintenanceAllStoryPoints)
            .ToList();

        var supportDistribution = new SupportDistributionDto
        {
            CompletedStoryPoints = completedSupportTasks.Sum(NewStoryPoints),
            CompletedTaskCount = completedSupportTasks.Count,
            AllStoryPoints = allSupportTasks.Sum(NewStoryPoints),
            AllTaskCount = allSupportTasks.Count,
            MaintenanceCompletedStoryPoints = completedMaintenanceTasks.Sum(NewStoryPoints),
            MaintenanceCompletedTaskCount = completedMaintenanceTasks.Count,
            MaintenanceAllStoryPoints = allMaintenanceTasks.Sum(NewStoryPoints),
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

        // Component distribution (similar to label distribution)
        // Total story points across all tasks, for each component's share
        var totalStoryPointsAllTasks = tasks.Sum(NewStoryPoints);

        var tasksByComponent = tasks
            .Where(t => !string.IsNullOrEmpty(t.Components))
            .SelectMany(t => t.Components!.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(component => new { Task = t, Component = component.Trim() }))
            .GroupBy(x => x.Component, StringComparer.OrdinalIgnoreCase)
            .Select(g =>
            {
                var componentTasks = g.Select(x => x.Task).Distinct().ToList();
                var completedComponentTasks = componentTasks.Where(t => t.Status == TaskStatus.Done).ToList();
                var totalSP = componentTasks.Sum(t =>
                {
                    if (t.PreviousSprintsStoryPoints.HasValue && t.StoryPoints.HasValue)
                        return Math.Max(0, t.StoryPoints.Value - t.PreviousSprintsStoryPoints.Value);
                    return t.StoryPoints ?? 0;
                });
                var completedSP = completedComponentTasks.Sum(t =>
                {
                    if (t.PreviousSprintsStoryPoints.HasValue && t.StoryPoints.HasValue)
                        return Math.Max(0, t.StoryPoints.Value - t.PreviousSprintsStoryPoints.Value);
                    return t.StoryPoints ?? 0;
                });

                return new TasksByComponentDto
                {
                    Component = g.Key,
                    TotalTasks = componentTasks.Count,
                    CompletedTasks = completedComponentTasks.Count,
                    TotalStoryPoints = totalSP,
                    CompletedStoryPoints = completedSP,
                    PercentageOfTotal = totalTaskCount > 0
                        ? Math.Round((double)componentTasks.Count / totalTaskCount * 100, 1)
                        : 0,
                    PercentageOfTotalStoryPoints = totalStoryPointsAllTasks > 0
                        ? Math.Round((double)totalSP / totalStoryPointsAllTasks * 100, 1)
                        : 0
                };
            })
            .OrderByDescending(c => c.TotalStoryPoints)
            .ToList();

        // Work category: the kind of work each task is, from the name of its epic
        var workCategories = new WorkCategories(parents, allTasks, appSettings?.GetWorkCategoryEpicNames());
        var tasksByWorkCategory = WorkMix.ByCategory(tasks, workCategories);

        return new TasksOverviewDto
        {
            Projects = projectsSummary,
            Tasks = tasksSummary,
            TasksByAssignee = tasksByAssignee,
            TasksByType = tasksByType,
            TasksByTypeSP = tasksByTypeSP,
            TasksByPriority = tasksByPriority,
            TasksByLabel = tasksByLabel,
            TasksByComponent = tasksByComponent,
            SupportDistribution = supportDistribution,
            TasksByWorkCategory = tasksByWorkCategory,
        };
    }

    public async Task<DirectReportAnalyticsDto?> GetDirectReportAnalyticsAsync(Guid directReportId, CancellationToken cancellationToken = default)
    {
        var directReport = await _directReportRepository.GetByIdAsync(directReportId, cancellationToken);
        if (directReport == null)
            return null;

        var meetings = await _meetingRepository.GetByDirectReportIdAsync(directReportId, cancellationToken);
        var allTasks = await _taskRepository.GetByAssigneeIdAsync(directReportId, cancellationToken);
        var parents = await _parentRepository.GetAllAsync(cancellationToken);

        // Exclude tasks that are also parents from counts and estimations
        var tasks = ExcludeParentTasks(allTasks, parents);

        // One-on-one analytics (simplified - no status)
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var pastMeetings = meetings.Where(m => m.MeetingDate < today).OrderByDescending(m => m.MeetingDate).ToList();
        var upcomingMeetings = meetings.Where(m => m.MeetingDate >= today).OrderBy(m => m.MeetingDate).ToList();
        var lastMeeting = pastMeetings.FirstOrDefault();
        var nextMeeting = upcomingMeetings.FirstOrDefault();

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
            AverageMeetingFrequencyDays = avgFrequency
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
            TotalStoryPoints = tasks.Sum(NewStoryPoints),
            CompletedStoryPoints = completedTasks.Sum(NewStoryPoints),
        };

        return new DirectReportAnalyticsDto
        {
            DirectReportId = directReport.Id,
            FullName = directReport.FullName,
            JobTitle = directReport.JobTitle,
            TenureMonths = CalculateTenureMonths(directReport.HireDate),
            OneOnOnes = oneOnOneAnalytics,
            Tasks = taskAnalytics
        };
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
        var parents = await _parentRepository.GetAllAsync(cancellationToken);

        return BuildTeamVelocity(allTasks, allSprints, parents, sprintCount);
    }

    private static TeamVelocityDto BuildTeamVelocity(
        IReadOnlyList<TeamTask> allTasks,
        IReadOnlyList<Sprint> allSprints,
        IReadOnlyList<Parent> parents,
        int? sprintCount)
    {
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

                return new SprintVelocityDto
                {
                    SprintName = sprint.Name,
                    StoryPointsCompleted = sprintTasks.Sum(t => t.StoryPoints ?? 0),
                    TasksCompleted = sprintTasks.Count
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

    public async Task<CarryOverTrendDto> GetCarryOverTrendAsync(int? sprintCount = null, CancellationToken cancellationToken = default)
    {
        var (sprints, tasks) = await LoadSprintHistoryAsync(sprintCount, cancellationToken);

        var rows = sprints.Select(sprint =>
        {
            var inSprint = tasks.Where(t => t.Sprints.Any(s => s.Id == sprint.Id)).ToList();
            var carriedIn = inSprint.Where(t => t.Sprints.Any(s => s.IsBefore(sprint))).ToList();
            var carriedOut = inSprint.Where(t => t.Sprints.Any(s => s.IsAfter(sprint))).ToList();
            var totalPoints = inSprint.Sum(t => t.Task.StoryPoints ?? 0);
            var carriedInPoints = carriedIn.Sum(t => t.Task.StoryPoints ?? 0);

            return new SprintCarryOverDto
            {
                SprintName = sprint.Name,
                TotalTasks = inSprint.Count,
                TotalPoints = totalPoints,
                CarriedInTasks = carriedIn.Count,
                CarriedInPoints = carriedInPoints,
                NewPoints = totalPoints - carriedInPoints,
                CarriedInPercent = Percent(carriedInPoints, totalPoints),
                CarriedOutTasks = carriedOut.Count,
                CarriedOutPoints = carriedOut.Sum(t => t.Task.StoryPoints ?? 0)
            };
        }).ToList();

        return new CarryOverTrendDto
        {
            Sprints = rows,
            AverageCarriedInPercent = Percent(rows.Sum(r => r.CarriedInPoints), rows.Sum(r => r.TotalPoints))
        };
    }

    public async Task<BugSupportInflowDto> GetBugSupportInflowAsync(int? sprintCount = null, CancellationToken cancellationToken = default)
    {
        var (sprints, tasks) = await LoadSprintHistoryAsync(sprintCount, cancellationToken);
        var appSettings = await _appSettingsRepository.GetAsync(cancellationToken);
        var maintenanceLabels = DeserializeLabels(appSettings?.MaintenanceLabels);

        // Bugs and support tickets by Jira issue type; maintenance by the Maintenance labels,
        // as nothing else marks it. The counts overlap: a bug labelled maintenance is counted as
        // a bug and as maintenance. The share of new tasks counts each ticket once.
        bool IsUnplanned(TeamTask task) =>
            task.Type is TaskType.Bug or TaskType.Support || ContainsAnyLabel(task, maintenanceLabels);

        // Counted in tickets, not points: bugs, support and maintenance are often not estimated.
        // A task arrives in the first sprint it was in; later sprints only carry it.
        var sprintRows = sprints.Select(sprint =>
        {
            var arrived = tasks.Where(t => t.Sprints[0].Id == sprint.Id).Select(t => t.Task).ToList();
            var unplanned = arrived.Count(IsUnplanned);

            return (Unplanned: unplanned, Row: new SprintInflowDto
            {
                SprintName = sprint.Name,
                NewTasks = arrived.Count,
                BugTasks = arrived.Count(t => t.Type == TaskType.Bug),
                SupportTasks = arrived.Count(t => t.Type == TaskType.Support),
                MaintenanceTasks = arrived.Count(t => ContainsAnyLabel(t, maintenanceLabels)),
                InflowPercent = Percent(unplanned, arrived.Count)
            });
        }).ToList();
        var rows = sprintRows.Select(r => r.Row).ToList();

        double Average(Func<SprintInflowDto, int> count) =>
            rows.Count > 0 ? Math.Round(rows.Average(count), 1) : 0;

        return new BugSupportInflowDto
        {
            Sprints = rows,
            AverageBugsPerSprint = Average(r => r.BugTasks),
            AverageSupportPerSprint = Average(r => r.SupportTasks),
            AverageMaintenancePerSprint = Average(r => r.MaintenanceTasks),
            InflowPercent = Percent(sprintRows.Sum(r => r.Unplanned), rows.Sum(r => r.NewTasks)),
            MaintenanceLabels = maintenanceLabels
        };
    }

    private static double Percent(int part, int whole) =>
        whole > 0 ? Math.Round((double)part / whole * 100, 1) : 0;

    /// <summary>
    /// The sprints a trend covers, oldest first, and every task with the sprints it has been
    /// in, oldest first. Only sprints Hive knows that have started and hold work are covered;
    /// with <paramref name="sprintCount"/>, the last that many of them.
    /// </summary>
    private async Task<(List<Sprint> Sprints, List<(TeamTask Task, List<Sprint> Sprints)> Tasks)> LoadSprintHistoryAsync(
        int? sprintCount,
        CancellationToken cancellationToken)
    {
        var allSprints = await _sprintRepository.GetAllAsync(cancellationToken);
        var allTasks = await _taskRepository.GetAllAsync(cancellationToken);
        var parents = await _parentRepository.GetAllAsync(cancellationToken);

        var sprintsByName = allSprints
            .GroupBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        // Sprints Hive has not stored have no place in the history, so they are dropped
        var tasks = ExcludeParentTasks(allTasks, parents)
            .Select(t => (Task: t, Sprints: t.GetSprintNames()
                .Select(name => sprintsByName.GetValueOrDefault(name))
                .OfType<Sprint>()
                .OrderBy(s => s.GetOrderingKey())
                .ToList()))
            .Where(t => t.Sprints.Count > 0)
            .ToList();

        var withWork = tasks.SelectMany(t => t.Sprints).Select(s => s.Id).ToHashSet();
        var today = DateTime.UtcNow.Date;
        var sprints = allSprints
            .Where(s => withWork.Contains(s.Id) && !s.IsFuture(today))
            .OrderBy(s => s.GetOrderingKey())
            .ToList();
        if (sprintCount.HasValue && sprintCount.Value > 0)
        {
            sprints = sprints.TakeLast(sprintCount.Value).ToList();
        }

        return (sprints, tasks);
    }

    public async Task<CapacityAnalysisDto> GetCapacityAnalysisAsync(int? sprintCount = null, CancellationToken cancellationToken = default)
    {
        var sprints = await _sprintRepository.GetAllAsync(cancellationToken);
        var sprintCapacities = await _sprintCapacityRepository.GetAllAsync(cancellationToken);
        var allTasks = await _taskRepository.GetAllAsync(cancellationToken);
        var parents = await _parentRepository.GetAllAsync(cancellationToken);
        var allLeaves = await _leaveRepository.GetAllAsync(cancellationToken);
        var directReports = await _directReportRepository.GetAllAsync(cancellationToken);

        return BuildCapacityAnalysis(sprints, sprintCapacities, allTasks, parents, allLeaves, directReports, sprintCount);
    }

    private static CapacityAnalysisDto BuildCapacityAnalysis(
        IReadOnlyList<Sprint> sprints,
        IReadOnlyList<SprintCapacity> sprintCapacities,
        IReadOnlyList<TeamTask> allTasks,
        IReadOnlyList<Parent> parents,
        IReadOnlyList<Leave> allLeaves,
        IReadOnlyList<DirectReport> directReports,
        int? sprintCount)
    {
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

        var today = DateTime.UtcNow.Date;
        var currentSprintEntity = Sprint.FindCurrent(sprints, today);

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
                TotalTasks = sprintTasks.Count,
                DoneTasks = completedTasks.Count,
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

        // Leaves and direct reports for predictions and suggestions
        var directReportIds = directReports.Where(dr => dr.IsDirect).Select(dr => dr.Id).ToHashSet();
        var activeLeaves = allLeaves
            .Where(l => l.Status == LeaveStatus.Active && directReportIds.Contains(l.DirectReportId))
            .ToList();
        var totalTeamSize = directReports.Count(dr => dr.IsDirect);
        var sprintEntityMap = sprints.ToDictionary(s => s.Id);

        var avgCompletedSP = pastSprints.Count > 0
            ? (int)Math.Round(pastSprints.Average(s => (double)s.CompletedPoints))
            : (currentSprint?.CompletedPoints ?? 0);

        // People available in a date range: the team, less the share of its working days
        // lost to leave.
        double AvailableMembers(DateTime start, DateTime end)
        {
            var workingDays = GetWorkingDays(start, end);
            var leaveDays = 0;
            foreach (var leave in activeLeaves)
            {
                if (!leave.OverlapsWith(start, end))
                    continue;
                var overlapStart = leave.StartDate > start ? leave.StartDate : start;
                var overlapEnd = leave.EndDate < end ? leave.EndDate : end;
                leaveDays += GetWorkingDays(overlapStart, overlapEnd);
            }
            var lost = workingDays > 0 ? (double)leaveDays / workingDays : 0;
            return Math.Max(0, totalTeamSize - lost);
        }

        // Average availability across the past sprints that produced avgCompletedSP, measured
        // the same way, so a future sprint is scaled by its change in availability rather
        // than penalised twice for leave the past sprints also had.
        // Example: team=5, past avg available=4, completed 40 SP.
        //   Future sprint with 4 available → ratio=4/4=1 → predicted=40
        //   Future sprint with 3 available → ratio=3/4=0.75 → predicted=30
        var pastAvailabilities = pastSprints
            .Where(ps => sprintEntityMap.ContainsKey(ps.SprintId))
            .Select(ps => sprintEntityMap[ps.SprintId])
            .Select(e => AvailableMembers(e.GetEstimatedStartDate(), e.GetEstimatedEndDate()))
            .ToList();
        var pastAverageAvailable = pastAvailabilities.Count > 0 ? pastAvailabilities.Average() : totalTeamSize;

        int Predict(DateTime start, DateTime end, int committedPoints)
        {
            // Mode 1: commitment-based, when the sprint has a commitment and there is history
            if (committedPoints > 0 && averageUtilization > 0)
                return (int)Math.Round(committedPoints * averageUtilization / 100);

            // Mode 2: velocity-based, scaled by availability against the past sprints
            var ratio = pastAverageAvailable > 0 ? AvailableMembers(start, end) / pastAverageAvailable : 1;
            return (int)Math.Round(avgCompletedSP * ratio);
        }

        futureSprints = futureSprints
            .Select(fs => sprintEntityMap.TryGetValue(fs.SprintId, out var entity)
                ? fs with { PredictedPoints = Predict(entity.GetEstimatedStartDate(), entity.GetEstimatedEndDate(), fs.CommittedPoints) }
                : fs)
            .ToList();

        // The sprint after the current one, predicted whatever the sprint filter shows. Hive
        // only knows a sprint once work is planned into it, so when the next one does not
        // exist yet it is projected: it starts the day after the current one ends and lasts
        // as long.
        NextSprintPredictionDto? nextSprint = null;
        if (currentSprintEntity != null && (pastSprints.Count > 0 || currentSprint != null))
        {
            var next = sprints
                .Where(s => s.IsAfter(currentSprintEntity))
                .OrderBy(s => s.GetOrderingKey())
                .FirstOrDefault();

            DateTime start, end;
            if (next != null)
            {
                start = next.GetEstimatedStartDate().Date;
                end = next.GetEstimatedEndDate().Date;
            }
            else
            {
                var currentStart = currentSprintEntity.GetEstimatedStartDate().Date;
                var currentEnd = currentSprintEntity.GetEstimatedEndDate().Date;
                start = currentEnd.AddDays(1);
                end = start.AddDays((currentEnd - currentStart).Days);
            }

            var committed = next != null && capacityMap.TryGetValue(next.Id, out var nextCapacity)
                ? nextCapacity.TotalCapacityPoints
                : 0;

            nextSprint = new NextSprintPredictionDto
            {
                SprintId = next?.Id,
                SprintName = next?.Name,
                IsProjected = next == null,
                StartDate = DateOnly.FromDateTime(start),
                EndDate = DateOnly.FromDateTime(end),
                TeamSize = totalTeamSize,
                AvailableMembers = Math.Round(AvailableMembers(start, end), 1),
                PredictedPoints = Predict(start, end, committed)
            };
        }

        return new CapacityAnalysisDto
        {
            PastSprints = pastSprints.OrderBy(s => s.Year).ThenBy(s => s.Quarter).ThenBy(s => s.SprintNumber).ToList(),
            CurrentSprint = currentSprint,
            FutureSprints = futureSprints.OrderBy(s => s.Year).ThenBy(s => s.Quarter).ThenBy(s => s.SprintNumber).ToList(),
            NextSprint = nextSprint,
            AverageUtilization = averageUtilization,
            TotalCommittedPoints = totalCommitted,
            TotalCompletedPoints = totalCompleted
        };
    }

    /// <summary>
    /// Deserializes a JSON string into a list of labels.
    /// </summary>
    private static List<string> DeserializeLabels(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<string>();
        }

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    /// <summary>
    /// Checks if a task contains any of the specified labels in its Labels or Tags fields.
    /// </summary>
    private static bool ContainsAnyLabel(TeamTask task, List<string> labels)
    {
        if (labels.Count == 0)
        {
            return false;
        }

        foreach (var label in labels)
        {
            if (task.Labels.Contains(label, StringComparison.OrdinalIgnoreCase) ||
                task.Tags.Contains(label, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
