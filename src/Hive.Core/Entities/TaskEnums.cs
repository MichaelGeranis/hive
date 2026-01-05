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
    Blocked = 2,
    InProgress = 3,
    InReview = 4,
    InTest = 5,
    POAcceptance = 6,
    ReadyToRelease = 7,
    Done = 8,
    Cancelled = 9
}

/// <summary>
/// Type of task.
/// </summary>
public enum TaskType
{
    Task = 0,
    Epic = 1,
    Story = 2,
    SubTask = 3,
    Bug = 4,
    Spike = 5,
    Support = 6
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
