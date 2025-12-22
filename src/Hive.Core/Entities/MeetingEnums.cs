namespace Hive.Core.Entities;

/// <summary>
/// Status of a one-on-one meeting.
/// </summary>
public enum MeetingStatus
{
    Scheduled = 0,
    Completed = 1,
    Cancelled = 2,
    Rescheduled = 3
}

/// <summary>
/// Category of a meeting note.
/// </summary>
public enum NoteCategory
{
    Discussion = 0,
    ActionItem = 1,
    Feedback = 2,
    CareerDevelopment = 3,
    Blocker = 4,
    Achievement = 5,
    Personal = 6,
    FollowUp = 7
}

/// <summary>
/// Status of an action item.
/// </summary>
public enum ActionItemStatus
{
    Open = 0,
    InProgress = 1,
    Completed = 2,
    Cancelled = 3
}
