namespace Hive.Core.Entities;

/// <summary>
/// Represents a personal note or TODO item for the manager.
/// </summary>
public class ManagerNote
{
    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public string Tags { get; private set; } = string.Empty;
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
        DateTime? dueDate = null,
        string? tags = null)
    {
        ValidateTitle(title);
        Id = Guid.NewGuid();
        Title = title.Trim();
        Content = content?.Trim() ?? string.Empty;
        Tags = NormalizeTags(tags);
        Priority = priority;
        DueDate = dueDate;
        IsCompleted = false;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(string title, string content, NotePriority priority, DateTime? dueDate, string? tags = null)
    {
        ValidateTitle(title);
        Title = title.Trim();
        Content = content?.Trim() ?? string.Empty;
        Tags = NormalizeTags(tags);
        Priority = priority;
        DueDate = dueDate;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the list of tags as an array.
    /// </summary>
    public string[] GetTagsList()
    {
        if (string.IsNullOrWhiteSpace(Tags))
            return Array.Empty<string>();
        return Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    /// <summary>
    /// Checks if the note has a specific tag.
    /// </summary>
    public bool HasTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag) || string.IsNullOrWhiteSpace(Tags))
            return false;
        return GetTagsList().Contains(tag.Trim(), StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Normalizes tags to lowercase, comma-separated format.
    /// </summary>
    private static string NormalizeTags(string? tags)
    {
        if (string.IsNullOrWhiteSpace(tags))
            return string.Empty;

        var tagList = tags
            .Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(t => t.ToLowerInvariant())
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Distinct()
            .OrderBy(t => t);

        return string.Join(",", tagList);
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
