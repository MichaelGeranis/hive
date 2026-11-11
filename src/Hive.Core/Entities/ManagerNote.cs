namespace Hive.Core.Entities;

/// <summary>
/// Represents a personal note written by the manager.
/// </summary>
public class ManagerNote
{
    /// <summary>
    /// The title carried by a note whose first line is still empty.
    /// </summary>
    public const string DefaultTitle = "New Note";

    /// <summary>
    /// The heading, by its first word, under which a journal entry lists what is urgent.
    /// </summary>
    public const string HotSection = "hot";

    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public string Tags { get; private set; } = string.Empty;

    /// <summary>
    /// The folder this note lives in. Null means the note sits at the root ("All Notes").
    /// </summary>
    public Guid? FolderId { get; private set; }

    /// <summary>
    /// Pinned notes are listed above every other note in their folder.
    /// </summary>
    public bool IsPinned { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private ManagerNote() { }

    public ManagerNote(
        string title,
        string content = "",
        string? tags = null,
        Guid? folderId = null)
    {
        ValidateTitle(title);
        Id = Guid.NewGuid();
        Title = title.Trim();
        Content = content?.Trim() ?? string.Empty;
        Tags = NormalizeTags(tags);
        FolderId = folderId;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates an empty note ready to be written into, the way a notes app opens a blank page.
    /// The title is a placeholder until the first line of content replaces it.
    /// </summary>
    public static ManagerNote CreateBlank(Guid? folderId = null)
    {
        return new ManagerNote(DefaultTitle, string.Empty, null, folderId);
    }

    public void Update(string title, string content, string? tags = null)
    {
        ValidateTitle(title);
        Title = title.Trim();
        Content = content?.Trim() ?? string.Empty;
        Tags = NormalizeTags(tags);
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
    /// Derives a note title from the first non-empty line of its content, stripping the
    /// markdown that decorates that line. Falls back to a placeholder for an empty note.
    /// </summary>
    public static string DeriveTitle(string? content) => NoteText.DeriveTitle(content, DefaultTitle);

    /// <summary>
    /// Gets the list of tags as an array.
    /// </summary>
    public string[] GetTagsList() => NoteText.SplitTags(Tags);

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
    private static string NormalizeTags(string? tags) => NoteText.NormalizeTags(tags);

    /// <summary>
    /// The top-level bullets of this note's "hot" section: what is burning right now.
    /// </summary>
    public IReadOnlyList<string> GetHotItems() => NoteText.ExtractSectionItems(Content, HotSection);

    /// <summary>
    /// The latest entry of a journal: the note whose title sorts last. Journal entries are
    /// titled by their date, so the greatest title is the most recent day.
    /// </summary>
    public static ManagerNote? FindLatestJournalEntry(IEnumerable<ManagerNote> journalNotes)
    {
        return journalNotes
            .OrderByDescending(n => n.Title, NoteText.TitleComparer)
            .FirstOrDefault();
    }

    private static void ValidateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty.", nameof(title));
        if (title.Length > 200)
            throw new ArgumentException("Title cannot exceed 200 characters.", nameof(title));
    }
}
