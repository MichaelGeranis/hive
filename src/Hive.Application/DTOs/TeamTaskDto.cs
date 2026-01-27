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
    public Guid? ParentId { get; init; }
    public string? ParentName { get; init; }
    public DateTime? DueDate { get; init; }
    public int? EstimatedHours { get; init; }
    public int? StoryPoints { get; init; }
    public string Tags { get; init; } = string.Empty;
    public string Labels { get; init; } = string.Empty;
    public string Sprint { get; init; } = string.Empty;
    public int? TimeSpentMinutes { get; init; }
    public bool IsOverdue { get; init; }
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
