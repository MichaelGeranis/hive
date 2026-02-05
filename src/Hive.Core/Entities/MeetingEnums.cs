namespace Hive.Core.Entities;

/// <summary>
/// Category of a meeting note.
/// </summary>
public enum NoteCategory
{
    Discussion = 0,
    ActionItem = 1,
    Feedback = 2,
    Achievement = 3
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
