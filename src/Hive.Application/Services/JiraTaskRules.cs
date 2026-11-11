using System.Text.RegularExpressions;
using Hive.Application.DTOs;
using Hive.Core.Entities;
using TaskStatus = Hive.Core.Entities.TaskStatus;

namespace Hive.Application.Services;

/// <summary>
/// How a Jira issue becomes a Hive task: the field mappings and the create and update rules.
/// Shared by the CSV import and the API sync, so both produce the same task from the same
/// issue.
/// </summary>
internal static class JiraTaskRules
{
    // Sprint name pattern: TeamName_QuarterQYear_SSprintNumber (e.g., LP_1Q25_S4)
    private static readonly Regex SprintPatternRegex = new(@"^(\w+)_(\d)Q(\d{2})_S(\d+)$", RegexOptions.Compiled);

    /// <summary>
    /// Creates a task from Jira data.
    /// </summary>
    internal static TeamTask CreateTask(JiraTaskData taskData, Guid? parentId)
    {
        var newTask = new TeamTask(
            taskData.Title,
            description: taskData.Description,
            type: taskData.Type,
            priority: taskData.Priority,
            assigneeId: taskData.AssigneeId,
            projectId: taskData.ProjectId,
            dueDate: taskData.DueDate,
            storyPoints: taskData.StoryPoints,
            tags: taskData.Tags,
            labels: taskData.Labels,
            components: taskData.Components,
            sprint: taskData.Sprint,
            parentId: parentId);

        UpdateTaskStatus(newTask, taskData.Status);
        return newTask;
    }

    /// <summary>
    /// Updates a task from Jira data, keeping every field the manager has overridden.
    /// </summary>
    internal static void ApplyToExisting(TeamTask existingTask, JiraTaskData taskData, Guid? parentId)
    {
        // Capture override state before updating
        var overriddenStoryPoints = existingTask.IsFieldOverridden("StoryPoints");
        var overriddenAssignee = existingTask.IsFieldOverridden("AssigneeId");
        var overriddenPreviousSP = existingTask.IsFieldOverridden("PreviousSprintsStoryPoints");
        var overriddenSprint = existingTask.IsFieldOverridden("Sprint");
        var existingPreviousSP = existingTask.PreviousSprintsStoryPoints;
        var existingSprint = existingTask.Sprint;

        // Update existing task, preserving overridden field values
        existingTask.Update(
            taskData.Title,
            description: taskData.Description,
            type: taskData.Type,
            priority: taskData.Priority,
            dueDate: taskData.DueDate,
            storyPoints: overriddenStoryPoints ? existingTask.StoryPoints : taskData.StoryPoints,
            tags: taskData.Tags,
            labels: taskData.Labels,
            components: taskData.Components,
            sprint: overriddenSprint ? existingSprint : taskData.Sprint,
            parentId: parentId);

        // Only update assignee if NOT overridden
        if (!overriddenAssignee && taskData.AssigneeId != existingTask.AssigneeId)
        {
            existingTask.AssignTo(taskData.AssigneeId);
        }

        if (taskData.ProjectId != existingTask.ProjectId)
        {
            existingTask.AssignToProject(taskData.ProjectId);
        }

        // Update status
        UpdateTaskStatus(existingTask, taskData.Status);

        // Re-apply overrides for Hive-only fields
        if (overriddenPreviousSP)
        {
            existingTask.OverridePreviousSprintsStoryPoints(existingPreviousSP);
        }
        if (overriddenSprint)
        {
            existingTask.OverrideSprint(existingSprint);
        }
    }

    /// <summary>
    /// Finds the assignee by email when Jira shares it, otherwise by full name as the CSV
    /// import does.
    /// </summary>
    internal static Guid? FindAssigneeId(string? email, string? name, IReadOnlyList<DirectReport> directReports)
    {
        if (!string.IsNullOrWhiteSpace(email))
        {
            var byEmail = directReports.FirstOrDefault(dr => dr.Email.Equals(email.Trim(), StringComparison.OrdinalIgnoreCase));
            if (byEmail is not null)
            {
                return byEmail.Id;
            }
        }

        return FindAssigneeId(name, directReports);
    }

    /// <summary>
    /// Joins labels or components the way the CSV import stores them: trimmed, de-duplicated,
    /// comma-separated without spaces.
    /// </summary>
    internal static string JoinValues(IEnumerable<string> values) =>
        string.Join(",", values.Select(v => v.Trim()).Where(v => v.Length > 0).Distinct());

    /// <summary>
    /// Whole story points, rounded up as the CSV import does ("0.5" is 1).
    /// </summary>
    internal static int? ToStoryPoints(double? points) =>
        points.HasValue ? (int)Math.Ceiling(points.Value) : null;

    /// <summary>
    /// Every field a sync can change, so an unchanged task is recognised and not rewritten.
    /// </summary>
    internal static string Fingerprint(TeamTask task) => string.Join("\u001f",
        task.Title, task.Description, task.Type, task.Status, task.Priority, task.AssigneeId, task.ProjectId,
        task.DueDate, task.StoryPoints, task.Tags, task.Labels, task.Components, task.Sprint, task.ParentId);

    internal static bool IsValidSprintPattern(string sprintName)
    {
        if (string.IsNullOrWhiteSpace(sprintName))
            return false;

        return SprintPatternRegex.IsMatch(sprintName);
    }

    internal static string? ExtractTeamNameFromSprint(string sprintName)
    {
        if (string.IsNullOrWhiteSpace(sprintName))
            return null;

        var match = SprintPatternRegex.Match(sprintName);
        return match.Success ? match.Groups[1].Value : null;
    }

    internal static TaskType MapIssueTypeToTaskType(string? issueType)
    {
        if (string.IsNullOrWhiteSpace(issueType))
            return TaskType.Task;

        return issueType.ToLowerInvariant() switch
        {
            "epic" => TaskType.Epic,
            "story" => TaskType.Story,
            "sub-task" or "subtask" => TaskType.SubTask,
            "bug" => TaskType.Bug,
            "spike" => TaskType.Spike,
            "support" => TaskType.Support,
            "task" => TaskType.Task,
            _ => TaskType.Task
        };
    }

    internal static TaskStatus MapJiraStatusToTaskStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return TaskStatus.Backlog;

        var lower = status.ToLowerInvariant().Replace(" ", "");

        return lower switch
        {
            // Backlog
            "backlog" => TaskStatus.Backlog,

            // To Do
            "todo" => TaskStatus.Todo,
            "to do" => TaskStatus.Todo,
            "toDo" => TaskStatus.Todo,
            "selected for development" => TaskStatus.Todo,
            "selectedfordevelopment" => TaskStatus.Todo,
            "open" => TaskStatus.Todo,

            // Blocked
            "blocked" => TaskStatus.Blocked,
            "blocker" => TaskStatus.Blocked,
            "impediment" => TaskStatus.Blocked,

            // In Progress
            "inprogress" => TaskStatus.InProgress,
            "in progress" => TaskStatus.InProgress,
            "inProgress" => TaskStatus.InProgress,
            "development" => TaskStatus.InProgress,
            "indevelopment" => TaskStatus.InProgress,

            // In Review
            "inreview" => TaskStatus.InReview,
            "in review" => TaskStatus.InReview,
            "inReview" => TaskStatus.InReview,
            "review" => TaskStatus.InReview,
            "code review" => TaskStatus.InReview,
            "codeReview" => TaskStatus.InReview,
            "peer review" => TaskStatus.InReview,
            "peerreview" => TaskStatus.InReview,

            // In Test
            "intest" => TaskStatus.InTest,
            "in test" => TaskStatus.InTest,
            "inTest" => TaskStatus.InTest,
            "testing" => TaskStatus.InTest,
            "qa" => TaskStatus.InTest,
            "qualityassurance" => TaskStatus.InTest,

            // PO Acceptance
            "poacceptance" => TaskStatus.POAcceptance,
            "po acceptance" => TaskStatus.POAcceptance,
            "poAcceptance" => TaskStatus.POAcceptance,
            "productowneracceptance" => TaskStatus.POAcceptance,
            "acceptance" => TaskStatus.POAcceptance,
            "uat" => TaskStatus.POAcceptance,
            "useracceptancetesting" => TaskStatus.POAcceptance,

            // Ready To Release
            "readytorelease" => TaskStatus.ReadyToRelease,
            "ready to release" => TaskStatus.ReadyToRelease,
            "readyToRelease" => TaskStatus.ReadyToRelease,
            "readyfordeploy" => TaskStatus.ReadyToRelease,
            "ready for deploy" => TaskStatus.ReadyToRelease,
            "deployready" => TaskStatus.ReadyToRelease,

            // Done
            "done" => TaskStatus.Done,
            "closed" => TaskStatus.Done,
            "resolved" => TaskStatus.Done,
            "complete" => TaskStatus.Done,
            "completed" => TaskStatus.Done,
            "released" => TaskStatus.Done,
            "deployed" => TaskStatus.Done,

            // Cancelled
            "cancelled" => TaskStatus.Cancelled,
            "canceled" => TaskStatus.Cancelled,
            "rejected" => TaskStatus.Cancelled,
            "wontdo" => TaskStatus.Cancelled,
            "won'tdo" => TaskStatus.Cancelled,

            // Default to Backlog for anything not recognized
            _ => TaskStatus.Backlog
        };
    }

    internal static TaskPriority MapJiraPriorityToTaskPriority(string? priority)
    {
        if (string.IsNullOrWhiteSpace(priority))
            return TaskPriority.Medium;

        return priority.ToLowerInvariant() switch
        {
            "lowest" => TaskPriority.Low,
            "low" => TaskPriority.Low,
            "medium" => TaskPriority.Medium,
            "normal" => TaskPriority.Medium,
            "high" => TaskPriority.High,
            "highest" => TaskPriority.Critical,
            "critical" => TaskPriority.Critical,
            "blocker" => TaskPriority.Critical,
            _ => TaskPriority.Medium
        };
    }

    internal static Guid? FindAssigneeId(string? assigneeName, IReadOnlyList<DirectReport> directReports)
    {
        if (string.IsNullOrWhiteSpace(assigneeName))
            return null;

        // Match by full name
        var match = directReports.FirstOrDefault(dr =>
            dr.FullName.Equals(assigneeName, StringComparison.OrdinalIgnoreCase));

        return match?.Id;
    }

    internal static Guid? FindProjectId(string? projectName, IReadOnlyList<Project> projects)
    {
        if (string.IsNullOrWhiteSpace(projectName))
            return null;

        // Match by project name
        var match = projects.FirstOrDefault(p =>
            p.Name.Equals(projectName, StringComparison.OrdinalIgnoreCase));

        return match?.Id;
    }

    internal static void UpdateTaskStatus(TeamTask task, TaskStatus newStatus)
    {
        if (task.Status == newStatus)
            return;

        // Jira can move a finished issue back to any status. A finished task only changes
        // status through Reopen, which lands it in To Do; from there it moves on as usual.
        if (task.IsResolved())
            task.Reopen();

        switch (newStatus)
        {
            case TaskStatus.Backlog:
                task.MoveToBacklog();
                break;

            case TaskStatus.Todo:
                task.MoveToTodo();
                break;

            case TaskStatus.Blocked:
                // Block method needs to be defined in TeamTask entity
                task.Block();
                break;

            case TaskStatus.InProgress:
                if (task.Status != TaskStatus.Done && task.Status != TaskStatus.Cancelled)
                    task.Start();
                break;

            case TaskStatus.InReview:
                if (task.Status == TaskStatus.InProgress || task.Status == TaskStatus.Blocked || task.Status == TaskStatus.InTest)
                    task.MoveToReview();
                else if (task.Status != TaskStatus.Done && task.Status != TaskStatus.Cancelled)
                {
                    task.Start();
                    task.MoveToReview();
                }
                break;

            case TaskStatus.InTest:
                // MoveToTest method needs to be defined in TeamTask entity
                task.MoveToTest();
                break;

            case TaskStatus.POAcceptance:
                // MoveToPOAcceptance method needs to be defined in TeamTask entity
                task.MoveToPOAcceptance();
                break;

            case TaskStatus.ReadyToRelease:
                // MoveToReadyToRelease method needs to be defined in TeamTask entity
                task.MoveToReadyToRelease();
                break;

            case TaskStatus.Done:
                if (task.Status != TaskStatus.Cancelled)
                    task.Complete();
                break;

            case TaskStatus.Cancelled:
                if (task.Status != TaskStatus.Done)
                    task.Cancel();
                break;
        }
    }

    internal sealed class JiraTaskData
    {
        public string Title { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public TaskType Type { get; init; }
        public TaskStatus Status { get; init; }
        public TaskPriority Priority { get; init; }
        public Guid? AssigneeId { get; init; }
        public Guid? ProjectId { get; init; }
        public int? StoryPoints { get; init; }
        public DateTime? DueDate { get; init; }
        public string Tags { get; init; } = string.Empty;
        public string Labels { get; init; } = string.Empty;
        public string? Components { get; init; }
        public string Sprint { get; init; } = string.Empty;
    }
}
