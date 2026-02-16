using Hive.Core.Entities;
using TaskStatus = Hive.Core.Entities.TaskStatus;

namespace Hive.Application.DTOs;

/// <summary>
/// Data Transfer Object for TeamTask.
/// </summary>
public record TeamTaskDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public TaskType Type { get; init; }
    public string TypeName { get; init; } = string.Empty;
    public TaskPriority Priority { get; init; }
    public string PriorityName { get; init; } = string.Empty;
    public TaskStatus Status { get; init; }
    public string StatusName { get; init; } = string.Empty;
    public Guid? AssigneeId { get; init; }
    public string? AssigneeName { get; init; }
    public Guid? ProjectId { get; init; }
    public string? ProjectName { get; init; }
    public IReadOnlyList<string> MatchedProjectNames { get; init; } = Array.Empty<string>();
    public Guid? ParentId { get; init; }
    public string? ParentName { get; init; }
    public DateTime? DueDate { get; init; }
    public int? EstimatedHours { get; init; }
    public int? StoryPoints { get; init; }
    public string Tags { get; init; } = string.Empty;
    public string Labels { get; init; } = string.Empty;
    public string? Components { get; init; }
    public string Sprint { get; init; } = string.Empty;
    public int? TimeSpentMinutes { get; init; }
    public int? PreviousSprintsStoryPoints { get; init; }
    public int? NewSprintsStoryPoints { get; init; }
    public string OverriddenFields { get; init; } = string.Empty;
    public bool IsOverdue { get; init; }
    public bool IsParentTask { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

/// <summary>
/// DTO for creating a new task.
/// </summary>
public record CreateTeamTaskDto
{
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public TaskType Type { get; init; } = TaskType.Task;
    public TaskPriority Priority { get; init; } = TaskPriority.Medium;
    public Guid? AssigneeId { get; init; }
    public Guid? ProjectId { get; init; }
    public Guid? ParentId { get; init; }
    public DateTime? DueDate { get; init; }
    public int? EstimatedHours { get; init; }
    public int? StoryPoints { get; init; }
    public string Tags { get; init; } = string.Empty;
    public string Labels { get; init; } = string.Empty;
    public string? Components { get; init; }
    public string Sprint { get; init; } = string.Empty;
    public int? TimeSpentMinutes { get; init; }
}

/// <summary>
/// DTO for updating a task.
/// </summary>
public record UpdateTeamTaskDto
{
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public TaskType Type { get; init; }
    public TaskPriority Priority { get; init; }
    public Guid? ParentId { get; init; }
    public DateTime? DueDate { get; init; }
    public int? EstimatedHours { get; init; }
    public int? StoryPoints { get; init; }
    public string Tags { get; init; } = string.Empty;
    public string Labels { get; init; } = string.Empty;
    public string? Components { get; init; }
    public string Sprint { get; init; } = string.Empty;
    public int? TimeSpentMinutes { get; init; }
}

/// <summary>
/// DTO for assigning a task.
/// </summary>
public record AssignTaskDto
{
    public Guid? AssigneeId { get; init; }
}

/// <summary>
/// DTO for overriding specific task fields (preserves values during Jira re-import).
/// </summary>
public record OverrideTeamTaskFieldsDto
{
    public Guid? AssigneeId { get; init; }
    public bool HasAssigneeOverride { get; init; }
    public int? StoryPoints { get; init; }
    public bool HasEstimationOverride { get; init; }
    public int? TimeSpentMinutes { get; init; }
    public bool HasTimeSpentOverride { get; init; }
    public int? PreviousSprintsStoryPoints { get; init; }
    public bool HasPreviousSprintsStoryPointsOverride { get; init; }
    public string? Sprint { get; init; }
    public bool HasSprintOverride { get; init; }
}

/// <summary>
/// DTO for clearing overrides on specific task fields.
/// </summary>
public record ClearTeamTaskOverridesDto
{
    public List<string> Fields { get; init; } = new();
}

/// <summary>
/// Summary of tasks by status.
/// </summary>
public record TaskSummaryDto
{
    public int TotalTasks { get; init; }
    public int BacklogTasks { get; init; }
    public int TodoTasks { get; init; }
    public int BlockedTasks { get; init; }
    public int InProgressTasks { get; init; }
    public int InReviewTasks { get; init; }
    public int InTestTasks { get; init; }
    public int POAcceptanceTasks { get; init; }
    public int ReadyToReleaseTasks { get; init; }
    public int DoneTasks { get; init; }
    public int CancelledTasks { get; init; }
    public int OverdueTasks { get; init; }
    public int UnassignedTasks { get; init; }
    public IReadOnlyList<string> AllLabels { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> AllSprints { get; init; } = Array.Empty<string>();
}
