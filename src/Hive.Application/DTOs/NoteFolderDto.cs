namespace Hive.Application.DTOs;

public record NoteFolderDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public Guid? ParentFolderId { get; init; }
    public int SortOrder { get; init; }

    /// <summary>
    /// Number of notes filed directly in this folder.
    /// </summary>
    public int NoteCount { get; init; }

    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public record CreateNoteFolderDto
{
    public string Name { get; init; } = string.Empty;
    public Guid? ParentFolderId { get; init; }
}

public record UpdateNoteFolderDto
{
    public string Name { get; init; } = string.Empty;
    public Guid? ParentFolderId { get; init; }
    public int SortOrder { get; init; }
}
