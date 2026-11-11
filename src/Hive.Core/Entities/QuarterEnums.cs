namespace Hive.Core.Entities;

/// <summary>
/// Status of a planning quarter.
/// </summary>
public enum QuarterStatus
{
    /// <summary>
    /// Quarter is being planned.
    /// </summary>
    Planning = 0,

    /// <summary>
    /// Quarter is currently active.
    /// </summary>
    Active = 1,

    /// <summary>
    /// Quarter has been completed.
    /// </summary>
    Completed = 2
}
