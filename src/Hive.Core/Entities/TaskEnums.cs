namespace Hive.Core.Entities;

/// <summary>
/// Priority level for tasks.
/// </summary>
public enum TaskPriority
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}

/// <summary>
/// Status of a task.
/// </summary>
public enum TaskStatus
{
    Backlog = 0,
    Todo = 1,
    InProgress = 2,
    InReview = 3,
    Done = 4,
    Cancelled = 5
}

/// <summary>
/// Type of task.
/// </summary>
public enum TaskType
{
    Task = 0,
    Bug = 1,
    Feature = 2,
    Improvement = 3,
    Research = 4,
    Documentation = 5
}

/// <summary>
/// Status of a project.
/// </summary>
public enum ProjectStatus
{
    Planning = 0,
    Active = 1,
    OnHold = 2,
    Completed = 3,
    Cancelled = 4
}
