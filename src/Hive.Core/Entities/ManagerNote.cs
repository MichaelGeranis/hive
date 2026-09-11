namespace Hive.Core.Entities;

/// <summary>
/// Represents a personal note or TODO item for the manager.
/// </summary>
public class ManagerNote
{
    /// <summary>
    /// The title carried by a note whose first line is still empty.
    /// </summary>
    public const string DefaultTitle = "New Note";

    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public string Tags { get; private set; } = string.Empty;
    public NotePriority Priority { get; private set; }

    /// <summary>
    /// The folder this note lives in. Null means the note sits at the root ("All Notes").
    /// </summary>
    public Guid? FolderId { get; private set; }

    /// <summary>
    /// Pinned notes are listed above every other note in their folder.
    /// </summary>
    public bool IsPinned { get; private set; }

    /// <summary>
    /// True when the note is tracked as a to-do. Only to-dos are counted as pending or overdue.
    /// </summary>
    public bool IsTodo { get; private set; }

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
        string? tags = null,
        Guid? folderId = null,
        bool isTodo = false)
    {
        ValidateTitle(title);
        Id = Guid.NewGuid();
        Title = title.Trim();
        Content = content?.Trim() ?? string.Empty;
        Tags = NormalizeTags(tags);
        Priority = priority;
        DueDate = dueDate;
        FolderId = folderId;
        IsTodo = isTodo;
        IsCompleted = false;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates an empty note ready to be written into, the way a notes app opens a blank page.
    /// The title is a placeholder until the first line of content replaces it.
    /// </summary>
    public static ManagerNote CreateBlank(Guid? folderId = null, bool isTodo = false)
    {
        return new ManagerNote(DefaultTitle, string.Empty, NotePriority.Normal, null, null, folderId, isTodo);
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
    /// Replaces the body of the note and re-derives the title from its first line.
    /// This is the free-writing path: whitespace is preserved exactly as typed.
    /// </summary>
    public void UpdateContent(string? content)
    {
        Content = content ?? string.Empty;
        Title = DeriveTitle(Content);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Moves the note into a folder, or to the root when null.
    /// </summary>
    public void MoveToFolder(Guid? folderId)
    {
        FolderId = folderId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Pin()
    {
        if (!IsPinned)
        {
            IsPinned = true;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void Unpin()
    {
        if (IsPinned)
        {
            IsPinned = false;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void TogglePin()
    {
        IsPinned = !IsPinned;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Tracks or stops tracking this note as a to-do. Clearing the to-do flag also clears
    /// its completion, because a note that is not a to-do cannot be done.
    /// </summary>
    public void SetTodo(bool isTodo)
    {
        if (IsTodo == isTodo)
        {
            return;
        }

        IsTodo = isTodo;

        if (!isTodo)
        {
            IsCompleted = false;
            CompletedAt = null;
        }

        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Derives a note title from the first non-empty line of its content, stripping the
    /// markdown that decorates that line. Falls back to a placeholder for an empty note.
    /// </summary>
    public static string DeriveTitle(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return DefaultTitle;
        }

        var firstLine = content
            .Split('\n')
            .Select(line => StripMarkdown(line))
            .FirstOrDefault(line => !string.IsNullOrWhiteSpace(line));

        if (string.IsNullOrWhiteSpace(firstLine))
        {
            return DefaultTitle;
        }

        return firstLine.Length > 200 ? firstLine[..200].TrimEnd() : firstLine;
    }

    /// <summary>
    /// Removes the markdown decoration from a single line so it reads as a plain title.
    /// </summary>
    private static string StripMarkdown(string line)
    {
        var text = line.Replace("\r", string.Empty).Trim();

        // Leading block markers: headings, quotes, list bullets, ordered list numbers.
        text = System.Text.RegularExpressions.Regex.Replace(text, @"^(#{1,6}\s+|>\s*|[-*+]\s+|\d+[.)]\s+)", string.Empty);

        // Task list checkbox left behind by a bullet marker.
        text = System.Text.RegularExpressions.Regex.Replace(text, @"^\[[ xX]\]\s*", string.Empty);

        // A horizontal rule carries no title.
        if (System.Text.RegularExpressions.Regex.IsMatch(text, @"^([-*_]\s*){3,}$"))
        {
            return string.Empty;
        }

        // Inline emphasis and code markers.
        text = text.Replace("**", string.Empty)
            .Replace("__", string.Empty)
            .Replace("`", string.Empty)
            .Replace("~~", string.Empty);

        return text.Trim();
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

/// <summary>
/// The order a list of notes is returned in.
/// </summary>
public enum NoteSortOrder
{
    /// <summary>
    /// Pinned first, then most recently edited — how a notes app lists a folder.
    /// </summary>
    Recent = 0,

    /// <summary>
    /// Most urgent first, then soonest due — how a to-do list is read.
    /// </summary>
    Priority = 1
}

public enum NotePriority
{
    Low = 0,
    Normal = 1,
    High = 2,
    Urgent = 3
}
