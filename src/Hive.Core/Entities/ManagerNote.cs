namespace Hive.Core.Entities;

/// <summary>
/// Represents a personal note or TODO item for the manager.
/// </summary>
public class ManagerNote
{
    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public NotePriority Priority { get; private set; }
    public bool IsCompleted { get; private set; }
    public DateTime? DueDate { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    private ManagerNote() { }

    public ManagerNote(
        string title,
        string content = "",
        NotePriority priority = NotePriority.Normal,
        DateTime? dueDate = null)
    {
        ValidateTitle(title);
        Id = Guid.NewGuid();
        Title = title.Trim();
        Content = content?.Trim() ?? string.Empty;
        Priority = priority;
        DueDate = dueDate;
        IsCompleted = false;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(string title, string content, NotePriority priority, DateTime? dueDate)
    {
        ValidateTitle(title);
        Title = title.Trim();
        Content = content?.Trim() ?? string.Empty;
        Priority = priority;
        DueDate = dueDate;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ToggleComplete()
    {
        IsCompleted = !IsCompleted;
        CompletedAt = IsCompleted ? DateTime.UtcNow : null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkComplete()
    {
        if (!IsCompleted)
        {
            IsCompleted = true;
            CompletedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void MarkIncomplete()
    {
        if (IsCompleted)
        {
            IsCompleted = false;
            CompletedAt = null;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public bool IsOverdue()
    {
        return !IsCompleted && DueDate.HasValue && DueDate.Value < DateTime.UtcNow;
    }

    private static void ValidateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty.", nameof(title));
        if (title.Length > 200)
            throw new ArgumentException("Title cannot exceed 200 characters.", nameof(title));
    }
}

public enum NotePriority
{
    Low = 0,
    Normal = 1,
    High = 2,
    Urgent = 3
}
