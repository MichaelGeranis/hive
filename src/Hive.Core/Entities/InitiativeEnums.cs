namespace Hive.Core.Entities;

/// <summary>
/// Status of an initiative.
/// </summary>
public enum InitiativeStatus
{
    /// <summary>
    /// Initiative is planned but not started.
    /// </summary>
    Planned = 0,

    /// <summary>
    /// Initiative is actively being worked on.
    /// </summary>
    InProgress = 1,

    /// <summary>
    /// Initiative is temporarily on hold.
    /// </summary>
    OnHold = 2,

    /// <summary>
    /// Initiative has been completed.
    /// </summary>
    Completed = 3,

    /// <summary>
    /// Initiative has been cancelled.
    /// </summary>
    Cancelled = 4
}

/// <summary>
/// Priority level of an initiative.
/// </summary>
public enum InitiativePriority
{
    /// <summary>
    /// Low priority.
    /// </summary>
    Low = 0,

    /// <summary>
    /// Medium priority.
    /// </summary>
    Medium = 1,

    /// <summary>
    /// High priority.
    /// </summary>
    High = 2,

    /// <summary>
    /// Critical priority - must be completed.
    /// </summary>
    Critical = 3
}

/// <summary>
/// Type of dependency between initiatives.
/// </summary>
public enum DependencyType
{
    /// <summary>
    /// Dependent initiative can only start after dependency is finished.
    /// </summary>
    FinishToStart = 0,

    /// <summary>
    /// Dependent initiative can only start when dependency starts.
    /// </summary>
    StartToStart = 1,

    /// <summary>
    /// Dependent initiative can only finish when dependency finishes.
    /// </summary>
    FinishToFinish = 2
}
