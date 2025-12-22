namespace Hive.Core.Entities;

/// <summary>
/// Represents a note or topic from a one-on-one meeting.
/// Can be a discussion point, action item, feedback, etc.
/// </summary>
public class MeetingNote
{
    public Guid Id { get; private set; }
    public Guid MeetingId { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public NoteCategory Category { get; private set; }
    public bool IsPrivate { get; private set; }
    public ActionItemStatus? ActionStatus { get; private set; }
    public DateTime? ActionDueDate { get; private set; }
    public string? ActionAssignee { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private MeetingNote() { }

    public MeetingNote(
        Guid meetingId,
        string content,
        NoteCategory category,
        bool isPrivate = false)
    {
        ValidateMeetingId(meetingId);
        ValidateContent(content);

        Id = Guid.NewGuid();
        MeetingId = meetingId;
        Content = content.Trim();
        Category = category;
        IsPrivate = isPrivate;
        CreatedAt = DateTime.UtcNow;

        // If it's an action item, set initial status
        if (category == NoteCategory.ActionItem)
        {
            ActionStatus = ActionItemStatus.Open;
        }
    }

    public void UpdateContent(string content, NoteCategory category, bool isPrivate)
    {
        ValidateContent(content);

        Content = content.Trim();
        Category = category;
        IsPrivate = isPrivate;
        UpdatedAt = DateTime.UtcNow;

        // Update action status based on category change
        if (category == NoteCategory.ActionItem && !ActionStatus.HasValue)
        {
            ActionStatus = ActionItemStatus.Open;
        }
        else if (category != NoteCategory.ActionItem)
        {
            ActionStatus = null;
            ActionDueDate = null;
            ActionAssignee = null;
        }
    }

    public void SetActionDetails(DateTime? dueDate, string? assignee)
    {
        if (Category != NoteCategory.ActionItem)
        {
            throw new InvalidOperationException("Action details can only be set for action items.");
        }

        ActionDueDate = dueDate;
        ActionAssignee = assignee?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateActionStatus(ActionItemStatus status)
    {
        if (Category != NoteCategory.ActionItem)
        {
            throw new InvalidOperationException("Action status can only be set for action items.");
        }

        ActionStatus = status;
        UpdatedAt = DateTime.UtcNow;
    }

    public void CompleteAction()
    {
        if (Category != NoteCategory.ActionItem)
        {
            throw new InvalidOperationException("Only action items can be completed.");
        }

        ActionStatus = ActionItemStatus.Completed;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsOverdue()
    {
        if (Category != NoteCategory.ActionItem || !ActionDueDate.HasValue)
        {
            return false;
        }

        return ActionStatus != ActionItemStatus.Completed
               && ActionStatus != ActionItemStatus.Cancelled
               && ActionDueDate.Value < DateTime.UtcNow;
    }

    private static void ValidateMeetingId(Guid meetingId)
    {
        if (meetingId == Guid.Empty)
        {
            throw new ArgumentException("MeetingId cannot be empty.", nameof(meetingId));
        }
    }

    private static void ValidateContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException("Note content cannot be empty.", nameof(content));
        }

        if (content.Length > 4000)
        {
            throw new ArgumentException("Note content cannot exceed 4000 characters.", nameof(content));
        }
    }
}
