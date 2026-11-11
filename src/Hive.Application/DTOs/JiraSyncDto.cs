namespace Hive.Application.DTOs;

/// <summary>
/// Outcome of syncing sprint dates from Jira.
/// </summary>
public record JiraSprintSyncResultDto
{
    /// <summary>How many sprints the Jira board reported.</summary>
    public int JiraSprintCount { get; init; }

    /// <summary>
    /// Future and active team sprints on the Jira board that Hive did not have, now created.
    /// </summary>
    public List<string> Created { get; init; } = new();

    /// <summary>Hive sprints whose dates changed.</summary>
    public List<SprintDateChangeDto> Updated { get; init; } = new();

    /// <summary>How many matched Hive sprints already had the Jira dates.</summary>
    public int UnchangedCount { get; init; }

    /// <summary>Hive sprints found in Jira without both dates set, left untouched.</summary>
    public List<string> MissingDatesInJira { get; init; } = new();

    /// <summary>Hive sprints with no sprint of the same name on the Jira board.</summary>
    public List<string> NotFoundInJira { get; init; } = new();

    /// <summary>Hive sprints whose name appears more than once on the Jira board, left untouched.</summary>
    public List<string> AmbiguousInJira { get; init; } = new();

    /// <summary>Sprints Jira rejected, e.g. an end date before the start date.</summary>
    public List<string> Errors { get; init; } = new();

    public DateTime SyncedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// One sprint's dates before and after a sync.
/// </summary>
public record SprintDateChangeDto
{
    public Guid SprintId { get; init; }
    public string Name { get; init; } = string.Empty;
    public DateTime? PreviousStartDate { get; init; }
    public DateTime? PreviousEndDate { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
}

/// <summary>
/// Outcome of syncing parents from the parent issues of a Jira space.
/// </summary>
public record JiraParentSyncResultDto
{
    /// <summary>
    /// How many parent issues the Jira space holds, done ones included: its epics and every
    /// other issue with sub-tasks.
    /// </summary>
    public int JiraParentCount { get; init; }

    /// <summary>Parents created for Jira issues Hive had not seen.</summary>
    public List<ParentSyncChangeDto> Created { get; init; } = new();

    /// <summary>
    /// Parents whose name, issue type or time spent changed, including parents from a CSV
    /// import that were linked to their Jira issue for the first time.
    /// </summary>
    public List<ParentSyncChangeDto> Updated { get; init; } = new();

    /// <summary>How many parents already matched their Jira issue.</summary>
    public int UnchangedCount { get; init; }

    /// <summary>Parents from Jira whose issue is gone from the space, removed from Hive.</summary>
    public List<ParentSyncChangeDto> Removed { get; init; } = new();

    /// <summary>Issues that could not be applied, e.g. a name another parent already has.</summary>
    public List<string> Errors { get; init; } = new();

    public DateTime SyncedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// One parent created or changed by a sync.
/// </summary>
public record ParentSyncChangeDto
{
    public Guid ParentId { get; init; }
    public string JiraKey { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;

    /// <summary>The name before the sync, when it changed.</summary>
    public string? PreviousName { get; init; }
}

/// <summary>
/// Outcome of syncing products from the components of a Jira space.
/// </summary>
public record JiraProductSyncResultDto
{
    /// <summary>How many components the Jira space has.</summary>
    public int JiraComponentCount { get; init; }

    /// <summary>Products created for components Hive had not seen.</summary>
    public List<ProductSyncChangeDto> Created { get; init; } = new();

    /// <summary>
    /// Products whose name or description changed, including products made by hand that were
    /// linked to their component for the first time.
    /// </summary>
    public List<ProductSyncChangeDto> Updated { get; init; } = new();

    /// <summary>How many products already matched their component.</summary>
    public int UnchangedCount { get; init; }

    /// <summary>Products from Jira whose component is gone from the space, removed from Hive.</summary>
    public List<ProductSyncChangeDto> Removed { get; init; } = new();

    /// <summary>Components that could not be applied, e.g. a name another product already has.</summary>
    public List<string> Errors { get; init; } = new();

    public DateTime SyncedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// One product created, changed or removed by a sync.
/// </summary>
public record ProductSyncChangeDto
{
    public Guid ProductId { get; init; }
    public string JiraComponentId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;

    /// <summary>The name before the sync, when it changed.</summary>
    public string? PreviousName { get; init; }
}

/// <summary>
/// Outcome of syncing tasks from the sprints of a Jira space.
/// </summary>
public record JiraTaskSyncResultDto
{
    /// <summary>How many work items are in the team's sprints in Jira, done ones included.</summary>
    public int JiraTaskCount { get; init; }

    public List<TaskSyncChangeDto> Created { get; init; } = new();
    public List<TaskSyncChangeDto> Updated { get; init; } = new();
    public int UnchangedCount { get; init; }

    /// <summary>Tasks from Jira that are no longer in the team's sprints, removed from Hive.</summary>
    public List<TaskSyncChangeDto> Removed { get; init; } = new();

    /// <summary>Work items only in other teams' sprints or in non-sprint queues, left out.</summary>
    public int OutOfScopeCount { get; init; }

    /// <summary>Sprints Hive did not have, created from the tasks' sprints.</summary>
    public List<string> SprintsCreated { get; init; } = new();

    public List<string> Errors { get; init; } = new();

    /// <summary>The parent sync that runs first, so every task has its parent to link to.</summary>
    public JiraParentSyncResultDto? Parents { get; init; }

    public DateTime SyncedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// One task created, changed or removed by a sync.
/// </summary>
public record TaskSyncChangeDto
{
    public Guid TaskId { get; init; }
    public string JiraKey { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
}

/// <summary>
/// Outcome of syncing everything Hive reads from Jira. Each part runs on its own, so one
/// that is not configured, or that Jira refuses, does not stop the others; its error is
/// reported in place of its result.
/// </summary>
public record JiraSyncAllResultDto
{
    public JiraSprintSyncResultDto? SprintDates { get; init; }
    public string? SprintDatesError { get; init; }
    public JiraProductSyncResultDto? Products { get; init; }
    public string? ProductsError { get; init; }
    public JiraParentSyncResultDto? Parents { get; init; }
    public string? ParentsError { get; init; }
    public JiraTaskSyncResultDto? Tasks { get; init; }
    public string? TasksError { get; init; }
}
