namespace Hive.Core.Entities;

/// <summary>
/// Type of checklist - determines usage context.
/// </summary>
public enum ChecklistType
{
    Interview = 0,
    Onboarding = 1
}

/// <summary>
/// Type of item within a checklist.
/// </summary>
public enum ChecklistItemType
{
    Question = 0,
    Topic = 1,
    Task = 2,
    Document = 3,
    Training = 4
}

/// <summary>
/// Status of a checklist instance item.
/// </summary>
public enum ChecklistItemStatus
{
    Pending = 0,
    InProgress = 1,
    Completed = 2,
    Skipped = 3,
    NotApplicable = 4
}

/// <summary>
/// Overall status of a checklist instance.
/// </summary>
public enum ChecklistInstanceStatus
{
    NotStarted = 0,
    InProgress = 1,
    Completed = 2,
    Cancelled = 3
}
