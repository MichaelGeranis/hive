using System.Text.RegularExpressions;

namespace Hive.Core.Entities;

/// <summary>
/// Represents a task or work item that can be assigned to team members.
/// </summary>
public class TeamTask
{
    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public TaskType Type { get; private set; }
    public TaskPriority Priority { get; private set; }
    public TaskStatus Status { get; private set; }

    /// <summary>
    /// When Hive saw the status change: the moment of the sync, import or edit that changed
    /// it, not the time Jira records for the transition.
    /// </summary>
    public DateTime StatusChangedAt { get; private set; }
    public Guid? AssigneeId { get; private set; }
    public Guid? ProjectId { get; private set; }
    public DateTime? DueDate { get; private set; }
    public int? StoryPoints { get; private set; }
    public string Tags { get; private set; } = string.Empty;
    public string Labels { get; private set; } = string.Empty;
    public string? Components { get; private set; }
    public string Sprint { get; private set; } = string.Empty;
    public int? PreviousSprintsStoryPoints { get; private set; }
    public Guid? ParentId { get; private set; }
    public string OverriddenFields { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private TeamTask() { }

    public TeamTask(
        string title,
        string? description = null,
        TaskType type = TaskType.Task,
        TaskPriority priority = TaskPriority.Medium,
        Guid? assigneeId = null,
        Guid? projectId = null,
        DateTime? dueDate = null,
        int? storyPoints = null,
        string? tags = null,
        string? labels = null,
        string? components = null,
        string? sprint = null,
        Guid? parentId = null)
    {
        ValidateTitle(title);

        Id = Guid.NewGuid();
        Title = title.Trim();
        Description = description?.Trim() ?? string.Empty;
        Type = type;
        Priority = priority;
        Status = TaskStatus.Backlog;
        AssigneeId = assigneeId;
        ProjectId = projectId;
        DueDate = dueDate;
        StoryPoints = storyPoints;
        Tags = tags?.Trim() ?? string.Empty;
        Labels = labels?.Trim() ?? string.Empty;
        Components = components?.Trim();
        Sprint = sprint?.Trim() ?? string.Empty;
        ParentId = parentId;
        CreatedAt = DateTime.UtcNow;
        StatusChangedAt = CreatedAt;
    }

    public void Update(
        string title,
        string? description,
        TaskType type,
        TaskPriority priority,
        DateTime? dueDate,
        int? storyPoints,
        string? tags,
        string? labels = null,
        string? components = null,
        string? sprint = null,
        Guid? parentId = null)
    {
        ValidateTitle(title);

        Title = title.Trim();
        Description = description?.Trim() ?? string.Empty;
        Type = type;
        Priority = priority;
        DueDate = dueDate;
        StoryPoints = storyPoints;
        Tags = tags?.Trim() ?? string.Empty;
        Labels = labels?.Trim() ?? string.Empty;
        Components = components?.Trim();
        Sprint = sprint?.Trim() ?? string.Empty;
        ParentId = parentId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AssignTo(Guid? assigneeId)
    {
        AssigneeId = assigneeId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AssignToProject(Guid? projectId)
    {
        ProjectId = projectId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AssignToParent(Guid? parentId)
    {
        ParentId = parentId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MoveToBacklog()
    {
        ChangeStatus(TaskStatus.Backlog);
    }

    public void MoveToTodo()
    {
        ChangeStatus(TaskStatus.Todo);
    }

    public void Start()
    {
        if (Status == TaskStatus.Done || Status == TaskStatus.Cancelled)
        {
            throw new InvalidOperationException("Cannot start a completed or cancelled task.");
        }

        ChangeStatus(TaskStatus.InProgress);
    }

    public void Block()
    {
        if (Status == TaskStatus.Done || Status == TaskStatus.Cancelled)
        {
            throw new InvalidOperationException("Cannot block a completed or cancelled task.");
        }

        ChangeStatus(TaskStatus.Blocked);
    }

    public void MoveToReview()
    {
        if (Status != TaskStatus.InProgress && Status != TaskStatus.Blocked && Status != TaskStatus.InTest)
        {
            throw new InvalidOperationException("Only in-progress, blocked, or tested tasks can be moved to review.");
        }

        ChangeStatus(TaskStatus.InReview);
    }

    public void MoveToTest()
    {
        if (Status == TaskStatus.Done || Status == TaskStatus.Cancelled)
        {
            throw new InvalidOperationException("Cannot move a completed or cancelled task to test.");
        }

        ChangeStatus(TaskStatus.InTest);
    }

    public void MoveToPOAcceptance()
    {
        if (Status == TaskStatus.Done || Status == TaskStatus.Cancelled)
        {
            throw new InvalidOperationException("Cannot move a completed or cancelled task to PO acceptance.");
        }

        ChangeStatus(TaskStatus.POAcceptance);
    }

    public void MoveToReadyToRelease()
    {
        if (Status == TaskStatus.Done || Status == TaskStatus.Cancelled)
        {
            throw new InvalidOperationException("Cannot move a completed or cancelled task to ready to release.");
        }

        ChangeStatus(TaskStatus.ReadyToRelease);
    }

    public void Complete()
    {
        if (Status == TaskStatus.Cancelled)
        {
            throw new InvalidOperationException("Cannot complete a cancelled task.");
        }

        ChangeStatus(TaskStatus.Done);
    }

    public void Cancel()
    {
        if (Status == TaskStatus.Done)
        {
            throw new InvalidOperationException("Cannot cancel a completed task.");
        }

        ChangeStatus(TaskStatus.Cancelled);
    }

    public void Reopen()
    {
        if (Status != TaskStatus.Done && Status != TaskStatus.Cancelled)
        {
            throw new InvalidOperationException("Only completed or cancelled tasks can be reopened.");
        }

        ChangeStatus(TaskStatus.Todo);
    }

    private void ChangeStatus(TaskStatus status)
    {
        var now = DateTime.UtcNow;
        if (Status != status)
        {
            StatusChangedAt = now;
        }

        Status = status;
        UpdatedAt = now;
    }

    /// <summary>
    /// The statuses in which work has started but is not finished. A task left in one of them
    /// too long is stale.
    /// </summary>
    public static readonly IReadOnlyList<TaskStatus> InFlightStatuses =
    [
        TaskStatus.InProgress,
        TaskStatus.Blocked,
        TaskStatus.InReview,
        TaskStatus.InTest,
        TaskStatus.POAcceptance,
        TaskStatus.ReadyToRelease
    ];

    public const int DefaultStaleAfterDays = 5;
    public const int MinStaleAfterDays = 1;
    public const int MaxStaleAfterDays = 60;

    /// <summary>
    /// Throws when a stale threshold is outside the range a manager can choose.
    /// </summary>
    public static void ValidateStaleAfterDays(int staleAfterDays)
    {
        if (staleAfterDays < MinStaleAfterDays || staleAfterDays > MaxStaleAfterDays)
        {
            throw new ArgumentException(
                $"The stale task threshold must be between {MinStaleAfterDays} and {MaxStaleAfterDays} days.",
                nameof(staleAfterDays));
        }
    }

    /// <summary>
    /// The latest status change a task may have had and still be stale at <paramref name="now"/>.
    /// </summary>
    public static DateTime StaleCutoff(DateTime now, int staleAfterDays) => now.AddDays(-staleAfterDays);

    /// <summary>
    /// Whole days the task has been in its current status, as Hive has seen it.
    /// </summary>
    public int DaysInStatus(DateTime now) => Math.Max(0, (int)(now - StatusChangedAt).TotalDays);

    /// <summary>
    /// Whether the task is in flight and has not changed status for at least
    /// <paramref name="staleAfterDays"/> days.
    /// </summary>
    public bool IsStale(DateTime now, int staleAfterDays) =>
        InFlightStatuses.Contains(Status) && StatusChangedAt <= StaleCutoff(now, staleAfterDays);

    /// <summary>
    /// Whether the task is finished with: done, or cancelled (Jira's "Won't Do"). Either way
    /// nothing more will happen on it, so it counts toward its parent's progress.
    /// </summary>
    public bool IsResolved() => Status is TaskStatus.Done or TaskStatus.Cancelled;

    /// <summary>
    /// Whether the task carries story points. Zero is no estimate, as Jira clears it to zero.
    /// </summary>
    public bool IsEstimated() => StoryPoints is > 0;

    /// <summary>
    /// Whether the task's type is one that must not be estimated: spikes and support are
    /// time-boxed or unplanned work, not sized delivery.
    /// </summary>
    public bool IsUnestimatedType() => Type is TaskType.Spike or TaskType.Support;

    /// <summary>
    /// Whether the task carries story points its type must not have.
    /// </summary>
    public bool HasUnexpectedStoryPoints() => IsUnestimatedType() && IsEstimated();

    /// <summary>
    /// Whether the task lacks the story points its type must have. Every type but a spike or
    /// support is estimated, except an epic: its size is the sum of its children.
    /// </summary>
    public bool IsMissingStoryPoints() => !IsUnestimatedType() && Type != TaskType.Epic && !IsEstimated();

    /// <summary>
    /// Whether the task has not been started: in the backlog or to do.
    /// </summary>
    public bool IsNotStarted() => Status is TaskStatus.Backlog or TaskStatus.Todo;

    /// <summary>
    /// Whether the task's type must say what done means before it is built: stories and tasks.
    /// A bug describes what is wrong instead, and a spike, support ticket, sub-task or epic is
    /// not built against criteria of its own.
    /// </summary>
    public bool NeedsAcceptanceCriteria() => Type is TaskType.Story or TaskType.Task;

    /// <summary>
    /// Whether the description says what done means: a line that starts an acceptance
    /// criteria section (<c>## Acceptance criteria</c>, <c>**AC:**</c>), or a scenario written
    /// as Given … Then.
    /// </summary>
    public bool HasAcceptanceCriteria() =>
        AcceptanceCriteriaHeading.IsMatch(Description) ||
        (ScenarioGiven.IsMatch(Description) && ScenarioThen.IsMatch(Description));

    /// <summary>
    /// Whether the task must have acceptance criteria and its description has none.
    /// </summary>
    public bool IsMissingAcceptanceCriteria() => NeedsAcceptanceCriteria() && !HasAcceptanceCriteria();

    private static readonly Regex AcceptanceCriteriaHeading = new(
        @"^[\s#>*_\-]*(acceptance\s+criteri(a|on)\b|ac\b\s*[*_]*\s*(:|$))",
        RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);

    private static readonly Regex ScenarioGiven = new(
        @"^[\s>*_\-]*given\b", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);

    private static readonly Regex ScenarioThen = new(
        @"^[\s>*_\-]*then\b", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);

    /// <summary>
    /// Whether the task lacks story points and nothing sizes it instead, or excuses it. A
    /// parent is sized by its children once every child that should be estimated is (spikes
    /// and support are not, so they are left out), as an epic is. A task under a spike or a
    /// support ticket is part of that unestimated work.
    /// </summary>
    /// <param name="children">The tasks under this task's parent; empty when it has none.</param>
    /// <param name="parentTask">The task this task's parent is; null when there is none.</param>
    public bool IsMissingStoryPoints(IReadOnlyCollection<TeamTask> children, TeamTask? parentTask)
    {
        if (!IsMissingStoryPoints() || parentTask?.IsUnestimatedType() == true)
            return false;

        var sizingChildren = children.Where(child => !child.IsUnestimatedType()).ToList();
        return !(sizingChildren.Count > 0 && sizingChildren.All(child => child.IsEstimated()));
    }

    /// <summary>
    /// The team's tasks missing story points (<see cref="IsMissingStoryPoints(IReadOnlyCollection{TeamTask}, TeamTask?)"/>),
    /// with each task's children and parent task worked out from the parents. Only tasks
    /// assigned to someone on the team count; any child, whoever has it, can size its parent.
    /// </summary>
    /// <param name="tasks">Every task, including the tasks that are parents themselves.</param>
    /// <param name="parents">Every parent.</param>
    /// <param name="team">Everyone on the team.</param>
    public static IReadOnlyList<TeamTask> FindMissingStoryPoints(
        IReadOnlyCollection<TeamTask> tasks, IEnumerable<Parent> parents, IEnumerable<DirectReport> team)
    {
        var teamIds = team.Select(member => member.Id).ToHashSet();

        // The task each parent is: the task it links to, or the task carrying its Jira key
        var tasksById = tasks.ToDictionary(t => t.Id);
        var tasksByJiraKey = new Dictionary<string, TeamTask>(StringComparer.OrdinalIgnoreCase);
        foreach (var task in tasks)
        {
            var key = task.GetJiraKey();
            if (key != null)
            {
                tasksByJiraKey.TryAdd(key, task);
            }
        }

        var parentTasks = new Dictionary<Guid, TeamTask>();
        foreach (var parent in parents)
        {
            TeamTask? parentTask = null;
            if (parent.TeamTaskId.HasValue)
            {
                tasksById.TryGetValue(parent.TeamTaskId.Value, out parentTask);
            }
            if (parentTask == null && parent.JiraKey != null)
            {
                tasksByJiraKey.TryGetValue(parent.JiraKey, out parentTask);
            }
            if (parentTask != null)
            {
                parentTasks[parent.Id] = parentTask;
            }
        }

        var childrenByTask = tasks
            .Where(t => t.ParentId.HasValue && parentTasks.ContainsKey(t.ParentId.Value))
            .GroupBy(t => parentTasks[t.ParentId!.Value].Id)
            .ToDictionary(g => g.Key, g => (IReadOnlyCollection<TeamTask>)g.ToList());

        return tasks
            .Where(t => t.AssigneeId.HasValue && teamIds.Contains(t.AssigneeId.Value))
            .Where(t => t.IsMissingStoryPoints(
                childrenByTask.GetValueOrDefault(t.Id) ?? [],
                t.ParentId.HasValue ? parentTasks.GetValueOrDefault(t.ParentId.Value) : null))
            .ToList();
    }

    /// <summary>
    /// Every sprint the task has been in. Jira lists them all for work carried over, e.g.
    /// <c>MAR_3Q26_S5,MAR_3Q26_S6</c>.
    /// </summary>
    public IReadOnlyList<string> GetSprintNames() =>
        Sprint.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    /// <summary>
    /// Whether the task is, or has been, in a sprint: any of its sprints, the same as Jira's
    /// <c>sprint = X</c>. Work carried over counts in every sprint it was in.
    /// </summary>
    public bool IsInSprint(string sprintName) =>
        GetSprintNames().Contains(sprintName.Trim(), StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Whether one of the task's comma-separated labels is <paramref name="label"/>, ignoring case.
    /// </summary>
    public bool HasLabel(string label) => ListContains(Labels, label);

    /// <summary>
    /// Whether one of the task's Jira components is <paramref name="component"/>, ignoring case.
    /// </summary>
    public bool HasComponent(string component) => ListContains(Components, component);

    private static bool ListContains(string? values, string value) =>
        !string.IsNullOrWhiteSpace(values) &&
        values.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Contains(value.Trim(), StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// The Jira key the task carries in its <c>jira:KEY</c> tag, or null when it did not come from Jira.
    /// </summary>
    public string? GetJiraKey()
    {
        var key = Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault(t => t.StartsWith("jira:", StringComparison.OrdinalIgnoreCase))?["jira:".Length..];
        return string.IsNullOrWhiteSpace(key) ? null : key;
    }

    public bool IsOverdue()
    {
        if (!DueDate.HasValue)
            return false;

        return !IsResolved() && DueDate.Value < DateTime.UtcNow;
    }

    public bool IsFieldOverridden(string fieldName)
    {
        if (string.IsNullOrEmpty(OverriddenFields))
            return false;

        return OverriddenFields.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Any(f => f.Trim().Equals(fieldName, StringComparison.OrdinalIgnoreCase));
    }

    public void SetOverride(string fieldName)
    {
        if (IsFieldOverridden(fieldName))
            return;

        OverriddenFields = string.IsNullOrEmpty(OverriddenFields)
            ? fieldName
            : $"{OverriddenFields},{fieldName}";
        UpdatedAt = DateTime.UtcNow;
    }

    public void ClearOverride(string fieldName)
    {
        if (string.IsNullOrEmpty(OverriddenFields))
            return;

        var fields = OverriddenFields.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(f => f.Trim())
            .Where(f => !f.Equals(fieldName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        OverriddenFields = string.Join(",", fields);

        // Reset Hive-only fields to null when clearing override (no Jira source to restore)
        if (fieldName.Equals("PreviousSprintsStoryPoints", StringComparison.OrdinalIgnoreCase))
        {
            PreviousSprintsStoryPoints = null;
        }

        UpdatedAt = DateTime.UtcNow;
    }

    public void ClearAllOverrides()
    {
        OverriddenFields = string.Empty;
        UpdatedAt = DateTime.UtcNow;
    }

    public void OverrideAssignee(Guid? assigneeId)
    {
        AssigneeId = assigneeId;
        SetOverride("AssigneeId");
    }

    public void OverrideStoryPoints(int? storyPoints)
    {
        StoryPoints = storyPoints;
        SetOverride("StoryPoints");
    }

    public void OverridePreviousSprintsStoryPoints(int? previousSprintsStoryPoints)
    {
        if (previousSprintsStoryPoints.HasValue && StoryPoints.HasValue &&
            previousSprintsStoryPoints.Value > StoryPoints.Value)
        {
            throw new ArgumentException("Previous sprints story points cannot exceed total story points.");
        }
        PreviousSprintsStoryPoints = previousSprintsStoryPoints;
        SetOverride("PreviousSprintsStoryPoints");
    }

    public void OverrideSprint(string? sprint)
    {
        Sprint = sprint?.Trim() ?? string.Empty;
        SetOverride("Sprint");
    }

    private static void ValidateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Task title cannot be empty.", nameof(title));
        }

        if (title.Length > 500)
        {
            throw new ArgumentException("Task title cannot exceed 500 characters.", nameof(title));
        }
    }
}
