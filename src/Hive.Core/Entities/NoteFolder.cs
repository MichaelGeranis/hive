namespace Hive.Core.Entities;

/// <summary>
/// Represents a folder that groups manager notes. Folders may be nested.
/// </summary>
public class NoteFolder
{
    /// <summary>
    /// The name of the folder that holds the manager's journal, one note per day.
    /// </summary>
    public const string JournalName = "Journal";

    /// <summary>
    /// The name of the folder that holds the team's retrospectives, one note per retro.
    /// </summary>
    public const string RetroName = "Retro";

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public Guid? ParentFolderId { get; private set; }
    public int SortOrder { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private NoteFolder() { }

    public NoteFolder(string name, Guid? parentFolderId = null, int sortOrder = 0)
    {
        ValidateName(name);
        ValidateSortOrder(sortOrder);

        Id = Guid.NewGuid();
        Name = name.Trim();
        ParentFolderId = parentFolderId;
        SortOrder = sortOrder;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Renames the folder.
    /// </summary>
    public void Rename(string name)
    {
        ValidateName(name);
        Name = name.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Moves the folder under another folder, or to the root when null.
    /// </summary>
    public void MoveTo(Guid? parentFolderId)
    {
        if (parentFolderId.HasValue && parentFolderId.Value == Id)
        {
            throw new ArgumentException("A folder cannot be its own parent.", nameof(parentFolderId));
        }

        ParentFolderId = parentFolderId;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Sets the position of the folder among its siblings.
    /// </summary>
    public void Reorder(int sortOrder)
    {
        ValidateSortOrder(sortOrder);
        SortOrder = sortOrder;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// True for the journal folder, recognised by its name, ignoring case.
    /// </summary>
    public bool IsJournal => string.Equals(Name, JournalName, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// True for the retro folder, recognised by its name, ignoring case.
    /// </summary>
    public bool IsRetro => string.Equals(Name, RetroName, StringComparison.OrdinalIgnoreCase);

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Folder name cannot be empty.", nameof(name));
        }

        if (name.Trim().Length > 100)
        {
            throw new ArgumentException("Folder name cannot exceed 100 characters.", nameof(name));
        }
    }

    private static void ValidateSortOrder(int sortOrder)
    {
        if (sortOrder < 0)
        {
            throw new ArgumentException("Sort order cannot be negative.", nameof(sortOrder));
        }
    }
}
