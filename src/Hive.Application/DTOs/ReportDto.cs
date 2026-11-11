using Hive.Core.Entities;
using TaskStatus = Hive.Core.Entities.TaskStatus;

namespace Hive.Application.DTOs;

/// <summary>
/// Dashboard overview with key metrics across all areas.
/// </summary>
public record DashboardOverviewDto
{
    public TeamOverviewDto Team { get; init; } = new();
    public TasksOverviewDto Tasks { get; init; } = new();
    public DashboardInsightsDto Insights { get; init; } = new();
    public DateTime GeneratedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Computed insights and warnings for the dashboard.
/// </summary>
public record DashboardInsightsDto
{
    public IReadOnlyList<WorkloadWarningDto> WorkloadWarnings { get; init; } = [];
    public IReadOnlyList<KnowledgeSiloDto> KnowledgeSilos { get; init; } = [];
    public IReadOnlyList<UnengagedMemberDto> UnengagedMembers { get; init; } = [];
    public IReadOnlyList<NarrowlyEngagedMemberDto> NarrowlyEngagedMembers { get; init; } = [];
    public IReadOnlyList<UnworkedProductDto> UnworkedProducts { get; init; } = [];
    public int UnmatchedTaskCount { get; init; }

    /// <summary>
    /// Every dashboard warning, computed server-side so the count does not depend on
    /// which widgets the manager has chosen to display.
    /// </summary>
    public IReadOnlyList<DashboardWarningDto> Warnings { get; init; } = [];
}

/// <summary>
/// Who has worked on which product over a period: the Product engagement widget and the
/// product warnings. Only direct reports count, and every task but an epic.
/// </summary>
public record ProductEngagementDto
{
    /// <summary>
    /// How many months back the period reaches; null for all time.
    /// </summary>
    public int? Months { get; init; }

    /// <summary>
    /// Every product, including those nobody has worked on, fewest members first.
    /// </summary>
    public IReadOnlyList<ProductMembershipDto> MembersByProduct { get; init; } = [];
    public IReadOnlyList<MemberProductsDto> ProductsByMember { get; init; } = [];
    public IReadOnlyList<KnowledgeSiloDto> KnowledgeSilos { get; init; } = [];
    public IReadOnlyList<UnworkedProductDto> UnworkedProducts { get; init; } = [];
    public IReadOnlyList<UnengagedMemberDto> UnengagedMembers { get; init; } = [];

    /// <summary>
    /// Direct reports who have worked on at least one product, but fewer than <see cref="MinMemberProducts"/>.
    /// </summary>
    public IReadOnlyList<NarrowlyEngagedMemberDto> NarrowlyEngagedMembers { get; init; } = [];
    public int MinProductMembers { get; init; }
    public int MinMemberProducts { get; init; }
}

/// <summary>
/// The direct reports who have worked on one product: the assignees of tasks whose components name it.
/// </summary>
public record ProductMembershipDto
{
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public int MemberCount { get; init; }
    public IReadOnlyList<string> MemberNames { get; init; } = [];
    public bool IsSilo { get; init; }
}

/// <summary>
/// The products one member has worked on, through the components of their tasks.
/// </summary>
public record MemberProductsDto
{
    public Guid? DirectReportId { get; init; }
    public string MemberName { get; init; } = string.Empty;
    public int ProductCount { get; init; }
    public IReadOnlyList<Guid> ProductIds { get; init; } = [];

    /// <summary>True when the member has worked on fewer products than the minimum.</summary>
    public bool IsNarrow { get; init; }
}

/// <summary>
/// One warning surfaced on the dashboard's warning tile.
/// </summary>
public record DashboardWarningDto
{
    /// <summary>
    /// Stable category key the client maps to an icon and a destination page.
    /// One of: OneOnOnes, Workload, UnengagedMembers, NarrowlyEngagedMembers, OverdueTasks, StaleTasks, UnworkedProducts,
    /// KnowledgeSilos, ScopeCreep, UnmatchedTasks, LowUtilization, VelocityTrend.
    /// </summary>
    public string Category { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Detail { get; init; } = string.Empty;
}

/// <summary>
/// Warning about a team member with concerning workload.
/// </summary>
public record WorkloadWarningDto
{
    public Guid? AssigneeId { get; init; }
    public string AssigneeName { get; init; } = string.Empty;
    public int InProgressTasks { get; init; }
    public int BlockedTasks { get; init; }
    public int InReviewTasks { get; init; }
    public IReadOnlyList<string> Issues { get; init; } = [];
}

/// <summary>
/// A product that is a knowledge silo: someone has worked on it, but fewer than the minimum.
/// </summary>
public record KnowledgeSiloDto
{
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public int MemberCount { get; init; }
    public IReadOnlyList<string> MemberNames { get; init; } = [];
}

/// <summary>
/// A product no direct report has worked on.
/// </summary>
public record UnworkedProductDto
{
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
}

/// <summary>
/// A direct report who has worked on some products, but fewer than the minimum.
/// </summary>
public record NarrowlyEngagedMemberDto
{
    public Guid DirectReportId { get; init; }
    public string FullName { get; init; } = string.Empty;
    public int ProductCount { get; init; }
}

/// <summary>
/// A team member not engaged in any product.
/// </summary>
public record UnengagedMemberDto
{
    public Guid DirectReportId { get; init; }
    public string FullName { get; init; } = string.Empty;
    public bool IsOnLeave { get; init; }
}

/// <summary>
/// Overview of team composition.
/// </summary>
public record TeamOverviewDto
{
    public int TotalReports { get; init; }
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
/// Overview of tasks and projects.
/// </summary>
public record TasksOverviewDto
{
    public ProjectsSummaryDto Projects { get; init; } = new();
    public TasksSummaryDto Tasks { get; init; } = new();
    public IReadOnlyList<TasksByAssigneeDto> TasksByAssignee { get; init; } = [];
    public IReadOnlyList<TasksByTypeDto> TasksByType { get; init; } = [];
    public IReadOnlyList<TasksByTypeSPDto> TasksByTypeSP { get; init; } = [];
    public IReadOnlyList<TasksByPriorityDto> TasksByPriority { get; init; } = [];
    public IReadOnlyList<TasksByLabelDto> TasksByLabel { get; init; } = [];
    public IReadOnlyList<TasksByComponentDto> TasksByComponent { get; init; } = [];
    public SupportDistributionDto SupportDistribution { get; init; } = new();

    /// <summary>
    /// Every work category, in a fixed order, even those with no tasks.
    /// </summary>
    public IReadOnlyList<WorkCategoryShareDto> TasksByWorkCategory { get; init; } = [];
}

/// <summary>
/// Summary of projects.
/// </summary>
public record ProjectsSummaryDto
{
    public int TotalProjects { get; init; }
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
    public int BacklogTasks { get; init; }
    public int TodoTasks { get; init; }
    public int BlockedTasks { get; init; }
    public int InProgressTasks { get; init; }
    public int InReviewTasks { get; init; }
    public int InTestTasks { get; init; }
    public int POAcceptanceTasks { get; init; }
    public int ReadyToReleaseTasks { get; init; }
    public int CompletedTasks { get; init; }
    public int CancelledTasks { get; init; }
    public int OverdueTasks { get; init; }
    public double CompletionRate { get; init; }
    public int TotalStoryPoints { get; init; }
    public int CompletedStoryPoints { get; init; }
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
/// Tasks grouped by type with story points.
/// </summary>
public record TasksByTypeSPDto
{
    public TaskType Type { get; init; }
    public string TypeName { get; init; } = string.Empty;
    public int TotalStoryPoints { get; init; }
    public int TaskCount { get; init; }
}

/// <summary>
/// Tasks grouped by label.
/// </summary>
public record TasksByLabelDto
{
    public string Label { get; init; } = string.Empty;
    public int TotalTasks { get; init; }
    public int CompletedTasks { get; init; }
    public int TotalStoryPoints { get; init; }
    public int CompletedStoryPoints { get; init; }
    public double PercentageOfTotal { get; init; }
}

/// <summary>
/// Tasks grouped by component.
/// </summary>
public record TasksByComponentDto
{
    public string Component { get; init; } = string.Empty;
    public int TotalTasks { get; init; }
    public int CompletedTasks { get; init; }
    public int TotalStoryPoints { get; init; }
    public int CompletedStoryPoints { get; init; }
    public double PercentageOfTotal { get; init; }
    public double PercentageOfTotalStoryPoints { get; init; }
}

/// <summary>
/// One work category's share of the tasks and of the story points.
/// </summary>
public record WorkCategoryShareDto
{
    public WorkCategory Category { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public int TaskCount { get; init; }
    public int StoryPoints { get; init; }
    public double PercentageOfTasks { get; init; }
    public double PercentageOfStoryPoints { get; init; }
}

/// <summary>
/// Support and maintenance work, in story points.
/// </summary>
public record SupportDistributionDto
{
    public int CompletedStoryPoints { get; init; }
    public int CompletedTaskCount { get; init; }
    public int AllStoryPoints { get; init; }
    public int AllTaskCount { get; init; }
    public int MaintenanceCompletedStoryPoints { get; init; }
    public int MaintenanceCompletedTaskCount { get; init; }
    public int MaintenanceAllStoryPoints { get; init; }
    public int MaintenanceAllTaskCount { get; init; }
    public IReadOnlyList<SupportByAssigneeDto> ByAssignee { get; init; } = [];
}

/// <summary>
/// Support and maintenance story points by assignee.
/// </summary>
public record SupportByAssigneeDto
{
    public Guid? AssigneeId { get; init; }
    public string AssigneeName { get; init; } = string.Empty;
    public int CompletedStoryPoints { get; init; }
    public int CompletedTaskCount { get; init; }
    public int AllStoryPoints { get; init; }
    public int AllTaskCount { get; init; }
    public int MaintenanceCompletedStoryPoints { get; init; }
    public int MaintenanceCompletedTaskCount { get; init; }
    public int MaintenanceAllStoryPoints { get; init; }
    public int MaintenanceAllTaskCount { get; init; }
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
/// Detailed report for a specific direct report.
/// </summary>
public record DirectReportAnalyticsDto
{
    public Guid DirectReportId { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string JobTitle { get; init; } = string.Empty;
    public int TenureMonths { get; init; }
    public OneOnOneAnalyticsDto OneOnOnes { get; init; } = new();
    public TaskAnalyticsDto Tasks { get; init; } = new();
}

/// <summary>
/// One-on-one analytics for a direct report.
/// </summary>
public record OneOnOneAnalyticsDto
{
    public int TotalMeetings { get; init; }
    public int CompletedMeetings { get; init; }
    public DateOnly? LastMeetingDate { get; init; }
    public DateOnly? NextScheduledDate { get; init; }
    public int DaysSinceLastMeeting { get; init; }
    public double AverageMeetingFrequencyDays { get; init; }
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
    public int TotalStoryPoints { get; init; }
    public int CompletedStoryPoints { get; init; }
}

/// <summary>
/// Time-based trend data for reporting.
/// </summary>
public record TrendDataDto
{
    public DateTime Date { get; init; }
    public int TasksCompleted { get; init; }
    public int MeetingsHeld { get; init; }
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
    public int StoryPointsCompleted { get; init; }
    public int TasksCompleted { get; init; }
}

/// <summary>
/// How much of each sprint's work was carried in from an earlier sprint.
/// </summary>
public record CarryOverTrendDto
{
    public IReadOnlyList<SprintCarryOverDto> Sprints { get; init; } = [];

    /// <summary>Carried-in points as a share of all points, across the sprints shown.</summary>
    public double AverageCarriedInPercent { get; init; }
}

public record SprintCarryOverDto
{
    public string SprintName { get; init; } = string.Empty;
    public int TotalTasks { get; init; }
    public int TotalPoints { get; init; }

    /// <summary>Tasks in the sprint that were also in an earlier one.</summary>
    public int CarriedInTasks { get; init; }
    public int CarriedInPoints { get; init; }

    /// <summary>Points first planned in this sprint.</summary>
    public int NewPoints { get; init; }

    public double CarriedInPercent { get; init; }

    /// <summary>Tasks in the sprint that moved on to a later one unfinished.</summary>
    public int CarriedOutTasks { get; init; }
    public int CarriedOutPoints { get; init; }
}

/// <summary>
/// New bugs, support and maintenance tickets per sprint, counted in tickets.
/// </summary>
public record BugSupportInflowDto
{
    public IReadOnlyList<SprintInflowDto> Sprints { get; init; } = [];
    public double AverageBugsPerSprint { get; init; }
    public double AverageSupportPerSprint { get; init; }
    public double AverageMaintenancePerSprint { get; init; }

    /// <summary>
    /// Bugs, support and maintenance as a share of all new tasks, across the sprints shown. Each
    /// ticket counts once, so a bug labelled maintenance is not counted twice here.
    /// </summary>
    public double InflowPercent { get; init; }

    /// <summary>The labels that mark a task as maintenance, from settings.</summary>
    public IReadOnlyList<string> MaintenanceLabels { get; init; } = [];
}

public record SprintInflowDto
{
    public string SprintName { get; init; } = string.Empty;

    /// <summary>Tasks whose first sprint is this one.</summary>
    public int NewTasks { get; init; }

    /// <summary>Jira issue type Bug.</summary>
    public int BugTasks { get; init; }

    /// <summary>Jira issue type Support.</summary>
    public int SupportTasks { get; init; }

    /// <summary>Any ticket carrying a Maintenance label, bugs and support tickets included.</summary>
    public int MaintenanceTasks { get; init; }

    /// <summary>Bugs, support and maintenance as a share of the sprint's new tasks, each ticket counted once.</summary>
    public double InflowPercent { get; init; }
}

/// <summary>
/// Capacity analysis report comparing committed vs completed story points.
/// </summary>
public record CapacityAnalysisDto
{
    public IReadOnlyList<SprintCapacityAnalysisDto> PastSprints { get; init; } = [];
    public SprintCapacityAnalysisDto? CurrentSprint { get; init; }
    public IReadOnlyList<SprintCapacityAnalysisDto> FutureSprints { get; init; } = [];

    /// <summary>
    /// The sprint after the current one and the points it is predicted to deliver. Not
    /// limited by the sprint filter; null when there is no sprint history to predict from.
    /// </summary>
    public NextSprintPredictionDto? NextSprint { get; init; }
    public double AverageUtilization { get; init; }
    public int TotalCommittedPoints { get; init; }
    public int TotalCompletedPoints { get; init; }
}

/// <summary>
/// The predicted delivery of the sprint after the current one.
/// </summary>
public record NextSprintPredictionDto
{
    /// <summary>Null when the sprint is projected because Hive does not have it yet.</summary>
    public Guid? SprintId { get; init; }
    public string? SprintName { get; init; }

    /// <summary>True when no such sprint exists yet and its dates follow on from the current one.</summary>
    public bool IsProjected { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
    public int TeamSize { get; init; }

    /// <summary>The team less the working days lost to leave booked in the sprint.</summary>
    public double AvailableMembers { get; init; }
    public int PredictedPoints { get; init; }
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
    public int NewCompletedPoints { get; init; }
    public int CarriedOverPoints { get; init; }
    public double UtilizationPercentage { get; init; }
    public int TotalStoryPoints { get; init; }
    public int TotalTasks { get; init; }
    public int DoneTasks { get; init; }
    public int? PredictedPoints { get; init; }
    public string Status { get; init; } = string.Empty; // "Past", "Current", "Future"
}


/// <summary>
/// What needs the manager's attention now: the current sprint, 1:1s against the cadence,
/// the hot to-dos from the journal, and who is away. None of it follows a sprint window.
/// </summary>
public record DashboardFocusDto
{
    public DateOnly Today { get; init; }
    public CurrentSprintFocusDto? CurrentSprint { get; init; }
    public OneOnOneCadenceOverviewDto OneOnOnes { get; init; } = new();
    public TodoFocusDto Todos { get; init; } = new();
    public LeaveFocusDto Leave { get; init; } = new();

    /// <summary>Tenure milestones in the coming week, soonest first. Empty hides the tile.</summary>
    public IReadOnlyList<TenureMilestoneFocusDto> TenureMilestones { get; init; } = [];
}

/// <summary>
/// A person reaching six months, or another full year, at the company within the week.
/// </summary>
public record TenureMilestoneFocusDto
{
    public Guid DirectReportId { get; init; }
    public string FullName { get; init; } = string.Empty;
    public DateOnly HireDate { get; init; }

    /// <summary><c>SixMonths</c> or <c>Anniversary</c>.</summary>
    public string Kind { get; init; } = string.Empty;

    /// <summary>Full years reached; 0 for the six-month mark.</summary>
    public int Years { get; init; }

    public DateOnly Date { get; init; }

    /// <summary>0 on the day itself.</summary>
    public int DaysUntil { get; init; }
}

/// <summary>
/// The sprint in progress, or the next one when none is running.
/// </summary>
public record CurrentSprintFocusDto
{
    public Guid SprintId { get; init; }
    public string Name { get; init; } = string.Empty;
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }

    /// <summary>False when no sprint contains today and this is the next one to start.</summary>
    public bool IsActive { get; init; }
    public int WorkingDays { get; init; }
    public int WorkingDaysLeft { get; init; }

    /// <summary>Share of the sprint's working days already behind the team, 0–100.</summary>
    public int ElapsedPercent { get; init; }

    /// <summary>Story points the sprint's capacity was set to; 0 when none was set.</summary>
    public int CommittedPoints { get; init; }

    /// <summary>Points in the sprint net of those delivered in earlier sprints.</summary>
    public int NewPoints { get; init; }
    public int NewCompletedPoints { get; init; }
    public int CarriedOverPoints { get; init; }

    /// <summary>Share of the new points already done, 0–100.</summary>
    public int CompletedPercent { get; init; }

    /// <summary>
    /// Done work against time gone: NotStarted, Ahead, OnPace or Behind. Compares
    /// <see cref="CompletedPercent"/> with <see cref="ElapsedPercent"/>.
    /// </summary>
    public string Pace { get; init; } = string.Empty;

    /// <summary>Points in the sprint beyond its commitment; 0 when within it or uncommitted.</summary>
    public int ScopeCreepPoints { get; init; }
    public int TotalTasks { get; init; }
    public int DoneTasks { get; init; }
    public IReadOnlyList<SprintStatusCountDto> ByStatus { get; init; } = [];
    public IReadOnlyList<FocusTaskDto> BlockedTasks { get; init; } = [];
}

/// <summary>
/// How many of the sprint's tasks, and points, sit in one status.
/// </summary>
public record SprintStatusCountDto
{
    public TaskStatus Status { get; init; }
    public string StatusName { get; init; } = string.Empty;
    public int TaskCount { get; init; }
    public int StoryPoints { get; init; }
}

/// <summary>
/// A task the manager should look at, with a link back to Jira when it came from there.
/// </summary>
public record FocusTaskDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? JiraKey { get; init; }
    public string? JiraUrl { get; init; }
    public string? AssigneeName { get; init; }
    public int? StoryPoints { get; init; }
}

/// <summary>
/// Every direct report against the 1:1 cadence, most urgent first.
/// </summary>
public record OneOnOneCadenceOverviewDto
{
    public int CadenceDays { get; init; }
    public int NeedsAttentionCount { get; init; }
    public IReadOnlyList<OneOnOneCadenceDto> People { get; init; } = [];
}

/// <summary>
/// One direct report's last 1:1 and when the next is due.
/// </summary>
public record OneOnOneCadenceDto
{
    public Guid DirectReportId { get; init; }
    public string FullName { get; init; } = string.Empty;
    public DateOnly? LastMeetingDate { get; init; }
    public int? DaysSinceLastMeeting { get; init; }

    /// <summary>Negative when the 1:1 is overdue; null when there has never been one.</summary>
    public int? DaysUntilDue { get; init; }

    /// <summary>One of: OnTrack, DueSoon, Overdue, NeverMet.</summary>
    public string Status { get; init; } = string.Empty;
    public bool IsOnLeave { get; init; }
}

/// <summary>
/// The to-dos: the "hot" bullets of the latest note in the Journal folder.
/// </summary>
public record TodoFocusDto
{
    /// <summary>False when there is no Journal folder to read from.</summary>
    public bool HasJournal { get; init; }

    /// <summary>The latest journal entry, or null when the Journal folder is empty.</summary>
    public Guid? NoteId { get; init; }
    public string? NoteTitle { get; init; }

    /// <summary>The hot bullets as written, in the order they appear in the note.</summary>
    public IReadOnlyList<string> Items { get; init; } = [];
}

/// <summary>
/// Who is away today, and who is going away soon.
/// </summary>
public record LeaveFocusDto
{
    public int LookAheadDays { get; init; }
    public IReadOnlyList<LeaveFocusItemDto> OutToday { get; init; } = [];
    public IReadOnlyList<LeaveFocusItemDto> Upcoming { get; init; } = [];
}

public record LeaveFocusItemDto
{
    public Guid LeaveId { get; init; }
    public Guid DirectReportId { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
    public int BusinessDays { get; init; }
}
