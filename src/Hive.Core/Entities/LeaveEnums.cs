namespace Hive.Core.Entities;

/// <summary>
/// Types of leave that can be tracked
/// </summary>
public enum LeaveType
{
    Vacation = 0,
    Sick = 1,
    Other = 2,
    PublicHoliday = 3
}

/// <summary>
/// Status of a leave record.
/// Kept intentionally small - used for simple tracking and future workflows.
/// </summary>
public enum LeaveStatus
{
    Active = 0,
    Cancelled = 1
}
